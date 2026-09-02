using NAudio.Midi;

namespace DrumDuino.App.Services;

public sealed record MidiInputDevice(int Index, string Name);

public sealed class MidiInputService : IDisposable
{
    private MidiIn? _midiIn;

    public event Action<byte, byte>? NoteReceived;

    public IReadOnlyList<MidiInputDevice> GetDevices()
    {
        var devices = new List<MidiInputDevice>();
        for (var i = 0; i < MidiIn.NumberOfDevices; i++)
        {
            var info = MidiIn.DeviceInfo(i);
            devices.Add(new MidiInputDevice(i, info.ProductName));
        }

        return devices;
    }

    public void Open(int deviceIndex)
    {
        Close();
        _midiIn = new MidiIn(deviceIndex);
        _midiIn.MessageReceived += OnMessageReceived;
        _midiIn.Start();
    }

    public void Close()
    {
        if (_midiIn is null)
        {
            return;
        }

        _midiIn.MessageReceived -= OnMessageReceived;
        _midiIn.Stop();
        _midiIn.Dispose();
        _midiIn = null;
    }

    private void OnMessageReceived(object? sender, MidiInMessageEventArgs e)
    {
        if (e.MidiEvent is not NoteEvent noteEvent || noteEvent.Velocity <= 0)
        {
            return;
        }

        NoteReceived?.Invoke((byte)noteEvent.NoteNumber, (byte)noteEvent.Velocity);
    }

    public void Dispose() => Close();
}
