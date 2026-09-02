using System.IO.Ports;
using DrumDuino.Core.Models;
using DrumDuino.Core.Protocol;

namespace DrumDuino.Core.Serial;

public sealed class MicroDrumClient : IAsyncDisposable
{
    private readonly SysExFrameReader _reader = new();
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private readonly object _messageLock = new();
    private SerialPort? _port;
    private TaskCompletionSource<SysExMessage>? _pendingResponse;
    private readonly List<SysExMessage> _collectedMessages = [];

    public event Action<PadHitEvent>? PadHitReceived;

    public bool IsConnected => _port?.IsOpen == true;
    public string? PortName => _port?.PortName;

    public void Connect(string portName, int baudRate = MicroDrumConstants.DefaultBaudRate)
    {
        Disconnect();
        Thread.Sleep(150);

        try
        {
            _port = SerialPortHelper.OpenPort(portName, baudRate);
        }
        catch (IOException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new IOException(SerialPortHelper.DescribeOpenFailure(portName, ex), ex);
        }

        _port.DataReceived += OnDataReceived;

        // Arduino Mega reinicia quando DTR sobe na abertura da porta.
        Thread.Sleep(1800);
        DrainInputBuffer();
    }

    private void DrainInputBuffer()
    {
        if (_port is null || !_port.IsOpen)
        {
            return;
        }

        try
        {
            _port.DiscardInBuffer();
            _port.DiscardOutBuffer();
        }
        catch (IOException)
        {
            // Port may close during shutdown.
        }
    }

    public void Disconnect()
    {
        if (_port is null)
        {
            return;
        }

        _port.DataReceived -= OnDataReceived;
        try
        {
            if (_port.IsOpen)
            {
                var disableFrame = SysExCodec.Encode(new SysExMessage(SysExCommands.Diagnostic, 0, 0, 0));
                _port.Write(disableFrame, 0, disableFrame.Length);
            }
        }
        catch
        {
            // Best effort when closing.
        }

        if (_port.IsOpen)
        {
            _port.Close();
        }

        _port.Dispose();
        _port = null;
        Thread.Sleep(100);
        lock (_messageLock)
        {
            _pendingResponse = null;
            _collectedMessages.Clear();
        }
    }

    public async Task<DrumMode> EnterToolModeAsync(CancellationToken cancellationToken = default)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
            {
                await Task.Delay(600, cancellationToken);
            }

            try
            {
                var response = await SendCommandAsync(
                    new SysExMessage(SysExCommands.SetMode, (byte)DrumMode.Tool, 0, 0),
                    cancellationToken);
                return (DrumMode)response.Data1;
            }
            catch (TimeoutException ex)
            {
                lastError = ex;
            }
        }

        throw new TimeoutException(
            "O módulo não respondeu. Aguarde o LED parar de piscar, confira a porta COM e tente de novo.",
            lastError);
    }

    public async Task<DrumMode> ReturnToMidiModeAsync(CancellationToken cancellationToken = default)
    {
        await SetDiagnosticModeAsync(false, cancellationToken);
        var response = await SendCommandAsync(
            new SysExMessage(SysExCommands.SetMode, (byte)DrumMode.Midi, 0, 0),
            cancellationToken);
        return (DrumMode)response.Data1;
    }

    public async Task SetDiagnosticModeAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            WriteMessage(new SysExMessage(SysExCommands.Diagnostic, (byte)(enabled ? 1 : 0), 0, 0));
            await Task.Delay(20, cancellationToken);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async Task<DrumMode> AskModeAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync(
            new SysExMessage(SysExCommands.AskMode, 0, 0, 0),
            cancellationToken);
        return (DrumMode)response.Data1;
    }

    public async Task<PadConfig> ReadPadAsync(int padIndex, CancellationToken cancellationToken = default)
    {
        ValidatePadIndex(padIndex);
        var messages = await SendSettingRequestAsync(
            (byte)padIndex,
            MicroDrumConstants.AskAllParameters,
            cancellationToken);

        var pad = PadConfig.CreateDefault(padIndex);
        foreach (var message in messages)
        {
            if (message.Command != SysExCommands.AskSetting || message.Data1 != padIndex)
            {
                continue;
            }

            pad.ApplyParameter((PadParameter)message.Data2, message.Data3);
        }

        return pad;
    }

    public async Task<DrumKit> ReadKitAsync(CancellationToken cancellationToken = default)
    {
        var kit = DrumKit.CreateDefault();
        for (var i = 0; i < MicroDrumConstants.PadCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pad = await ReadPadAsync(i, cancellationToken);
            pad.Name = kit.Pads[i].Name;
            kit.Pads[i] = pad;
        }

        return kit;
    }

    public async Task WritePadAsync(PadConfig pad, CancellationToken cancellationToken = default)
    {
        ValidatePadIndex(pad.Index);
        EnsureConnected();
        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            foreach (var (parameter, value) in pad.EnumerateParameters())
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteMessage(new SysExMessage(SysExCommands.SetSetting, (byte)pad.Index, (byte)parameter, value));
                await Task.Delay(5, cancellationToken);
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async Task SavePadToEepromAsync(PadConfig pad, CancellationToken cancellationToken = default)
    {
        ValidatePadIndex(pad.Index);
        EnsureConnected();
        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            foreach (var (parameter, value) in pad.EnumerateParameters())
            {
                if ((byte)parameter > 13)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                WriteMessage(new SysExMessage(
                    SysExCommands.SaveSetting,
                    (byte)pad.Index,
                    (byte)parameter,
                    value));
                await Task.Delay(5, cancellationToken);
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async Task WriteKitAsync(DrumKit kit, bool saveToEeprom, CancellationToken cancellationToken = default)
    {
        foreach (var pad in kit.Pads.Take(MicroDrumConstants.PadCount))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WritePadAsync(pad, cancellationToken);
            if (saveToEeprom)
            {
                await SavePadToEepromAsync(pad, cancellationToken);
            }
        }
    }

    private async Task<SysExMessage> SendCommandAsync(SysExMessage message, CancellationToken cancellationToken)
    {
        EnsureConnected();
        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            lock (_messageLock)
            {
                _collectedMessages.Clear();
                _pendingResponse = new TaskCompletionSource<SysExMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            WriteMessage(message);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(4));
            var deadline = DateTime.UtcNow.AddSeconds(4);

            while (DateTime.UtcNow < deadline)
            {
                PollIncomingBytes();

                TaskCompletionSource<SysExMessage>? pending;
                lock (_messageLock)
                {
                    pending = _pendingResponse;
                }

                if (pending?.Task.IsCompleted == true)
                {
                    return await pending.Task;
                }

                await Task.Delay(15, cancellationToken);
            }

            throw new TimeoutException("Timed out waiting for device response.");
        }
        finally
        {
            lock (_messageLock)
            {
                _pendingResponse = null;
            }

            _ioLock.Release();
        }
    }

    private async Task<IReadOnlyList<SysExMessage>> SendSettingRequestAsync(
        byte target,
        byte parameter,
        CancellationToken cancellationToken)
    {
        EnsureConnected();
        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            lock (_messageLock)
            {
                _collectedMessages.Clear();
            }

            WriteMessage(new SysExMessage(SysExCommands.AskSetting, target, parameter, 0));

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            var deadline = DateTime.UtcNow.AddSeconds(3);

            while (DateTime.UtcNow < deadline)
            {
                PollIncomingBytes();

                bool done;
                lock (_messageLock)
                {
                    done = _collectedMessages.Any(m => m.IsEndOfTransmission);
                }

                if (done)
                {
                    break;
                }

                await Task.Delay(15, cancellationToken);
            }

            lock (_messageLock)
            {
                if (!_collectedMessages.Any(m => m.IsEndOfTransmission))
                {
                    throw new TimeoutException("Timed out waiting for setting responses.");
                }

                return _collectedMessages.ToArray();
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private void WriteMessage(SysExMessage message)
    {
        var frame = SysExCodec.Encode(message);
        _port!.Write(frame, 0, frame.Length);
    }

    private void OnDataReceived(object? sender, SerialDataReceivedEventArgs e) => PollIncomingBytes();

    private void PollIncomingBytes()
    {
        if (_port is null || !_port.IsOpen)
        {
            return;
        }

        try
        {
            var bytesToRead = _port.BytesToRead;
            if (bytesToRead <= 0)
            {
                return;
            }

            var buffer = new byte[bytesToRead];
            _port.Read(buffer, 0, buffer.Length);
            ProcessIncomingBytes(buffer);
        }
        catch (IOException)
        {
            // Port closed while reading.
        }
    }

    private void ProcessIncomingBytes(byte[] buffer)
    {
        var parsed = new List<SysExMessage>();
        _reader.PushRange(buffer, parsed);
        lock (_messageLock)
        {
            foreach (var message in parsed)
            {
                if (message.IsEndOfTransmission)
                {
                    _collectedMessages.Add(message);
                    continue;
                }

                if (_pendingResponse is not null
                    && message.Command is SysExCommands.AskMode or SysExCommands.SetMode)
                {
                    _pendingResponse.TrySetResult(message);
                    continue;
                }

                if (message.Command == SysExCommands.AskSetting)
                {
                    _collectedMessages.Add(message);
                    continue;
                }

                if (message.Command == SysExCommands.Diagnostic)
                {
                    var hit = new PadHitEvent(message.Data1, message.Data2, DateTime.UtcNow);
                    PadHitReceived?.Invoke(hit);
                }
            }
        }
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Serial port is not connected.");
        }
    }

    private static void ValidatePadIndex(int padIndex)
    {
        if (padIndex < 0 || padIndex >= MicroDrumConstants.PadCount)
        {
            throw new ArgumentOutOfRangeException(nameof(padIndex));
        }
    }

    public ValueTask DisposeAsync()
    {
        Disconnect();
        _ioLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
