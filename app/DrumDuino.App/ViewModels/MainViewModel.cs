using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrumDuino.App.Services;
using DrumDuino.Core;
using DrumDuino.Core.Models;
using DrumDuino.Core.Presets;
using DrumDuino.Core.Serial;

namespace DrumDuino.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly MicroDrumClient _client = new();
    private readonly MidiInputService _midiInput = new();
    private readonly DispatcherTimer _hitDecayTimer;

    public ObservableCollection<PadViewModel> Pads { get; } = [];
    public ObservableCollection<string> SerialPorts { get; } = [];
    public ObservableCollection<MidiInputDevice> MidiInputDevices { get; } = [];
    public IReadOnlyList<PadType> PadTypes { get; } = Enum.GetValues<PadType>();
    public IReadOnlyList<VelocityCurve> VelocityCurves { get; } = Enum.GetValues<VelocityCurve>();

    [ObservableProperty]
    private string? _selectedPort;

    [ObservableProperty]
    private MidiInputDevice? _selectedMidiInput;

    [ObservableProperty]
    private PadViewModel? _selectedPad;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isMonitorEnabled;

    [ObservableProperty]
    private AppPage _currentPage = AppPage.Configuration;

    [ObservableProperty]
    private string _statusMessage = "Desconectado — edite offline ou conecte a porta COM.";

    [ObservableProperty]
    private string _kitName = "Default";

    public string ConnectionButtonText => IsConnected ? "Desconectar" : "Conectar";
    public IBrush ConnectionDotBrush => IsConnected
        ? new SolidColorBrush(Color.Parse("#3DD68C"))
        : new SolidColorBrush(Color.Parse("#6B7280"));
    public bool HasSelectedPad => SelectedPad is not null;
    public bool IsConfigPage => CurrentPage == AppPage.Configuration;
    public bool IsMonitorPage => CurrentPage == AppPage.Monitor;
    public string MonitorSourceHint => IsConnected
        ? "Monitor via serial (modo Tool)."
        : SelectedMidiInput is null
            ? "Selecione uma entrada MIDI ou conecte a COM."
            : $"Monitor via MIDI: {SelectedMidiInput.Name}";

    public MainViewModel()
    {
        _client.PadHitReceived += OnPadHitReceived;
        _midiInput.NoteReceived += OnMidiNoteReceived;

        _hitDecayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        _hitDecayTimer.Tick += (_, _) =>
        {
            foreach (var pad in Pads)
            {
                pad.DecayHit();
            }
        };
        _hitDecayTimer.Start();

        LoadDefaultKit();
        RefreshPorts();
        RefreshMidiInputs();
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        SerialPorts.Clear();
        foreach (var port in SerialPortHelper.GetOrderedPortNames())
        {
            SerialPorts.Add(port);
        }

        if (SelectedPort is null && SerialPorts.Count > 0)
        {
            SelectedPort = SerialPorts[0];
        }
    }

    [RelayCommand]
    private void RefreshMidiInputs()
    {
        MidiInputDevices.Clear();
        foreach (var device in _midiInput.GetDevices())
        {
            MidiInputDevices.Add(device);
        }

        if (SelectedMidiInput is null && MidiInputDevices.Count > 0)
        {
            SelectedMidiInput = MidiInputDevices[0];
        }
    }

    [RelayCommand]
    private void ShowConfiguration() => CurrentPage = AppPage.Configuration;

    [RelayCommand]
    private void ShowMonitor() => CurrentPage = AppPage.Monitor;

    [RelayCommand(CanExecute = nameof(CanToggleConnection))]
    private async Task ToggleConnectionAsync()
    {
        if (IsConnected)
        {
            await DisconnectAsync();
        }
        else
        {
            await ConnectAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedPort))
        {
            StatusMessage = "Selecione uma porta COM.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            StopMidiMonitor();
            _client.Connect(SelectedPort, MicroDrumConstants.DefaultBaudRate);
            try
            {
                var mode = await _client.EnterToolModeAsync();
                IsConnected = true;
                StatusMessage = $"Conectado em {SelectedPort} — modo {mode}.";
                if (IsMonitorEnabled)
                {
                    await _client.SetDiagnosticModeAsync(true);
                }
            }
            catch
            {
                _client.Disconnect();
                IsConnected = false;
                throw;
            }
        }, "Falha ao conectar:");
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    private async Task DisconnectAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (_client.IsConnected)
            {
                await _client.SetDiagnosticModeAsync(false);
                await _client.ReturnToMidiModeAsync();
            }

            _client.Disconnect();
            IsConnected = false;
            StatusMessage = "Desconectado — voltou para modo MIDI.";
            UpdateMonitorRouting();
        }, "Falha ao desconectar.");
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    private async Task ReadFromDeviceAsync()
    {
        await RunBusyAsync(async () =>
        {
            var names = Pads.ToDictionary(p => p.Index, p => p.Name);
            var kit = await _client.ReadKitAsync();
            ApplyKit(kit, names);
            StatusMessage = "Configuração lida do módulo.";
        }, "Falha ao ler do módulo.");
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    private async Task ApplyToDeviceAsync()
    {
        await RunBusyAsync(async () =>
        {
            var kit = BuildKitFromView();
            await _client.WriteKitAsync(kit, saveToEeprom: false);
            StatusMessage = "Configuração aplicada na RAM do módulo.";
        }, "Falha ao aplicar configuração.");
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    private async Task SaveEepromAsync()
    {
        await RunBusyAsync(async () =>
        {
            var kit = BuildKitFromView();
            await _client.WriteKitAsync(kit, saveToEeprom: true);
            StatusMessage = "Configuração salva na EEPROM.";
        }, "Falha ao salvar EEPROM.");
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    private async Task ReturnToMidiAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _client.SetDiagnosticModeAsync(false);
            await _client.ReturnToMidiModeAsync();
            StatusMessage = "Módulo em modo MIDI — pode fechar o app e tocar.";
        }, "Falha ao voltar para MIDI.");
    }

    public void ImportPinsIni(string path)
    {
        var kit = PinsIniImporter.Import(path);
        ApplyKit(kit);
        KitName = kit.Name;
        StatusMessage = $"Preset importado: {Path.GetFileName(path)}";
    }

    public void LoadJsonPreset(string path)
    {
        var kit = KitPresetSerializer.Load(path);
        ApplyKit(kit);
        KitName = kit.Name;
        StatusMessage = $"Preset carregado: {Path.GetFileName(path)}";
    }

    public void SaveJsonPreset(string path)
    {
        var kit = BuildKitFromView();
        KitPresetSerializer.Save(kit, path);
        StatusMessage = $"Preset salvo: {Path.GetFileName(path)}";
    }

    private bool CanConnect() => !IsConnected && !IsBusy && !string.IsNullOrWhiteSpace(SelectedPort);
    private bool CanToggleConnection() => !IsBusy && (IsConnected || !string.IsNullOrWhiteSpace(SelectedPort));

    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(ConnectionButtonText));
        OnPropertyChanged(nameof(ConnectionDotBrush));
        OnPropertyChanged(nameof(MonitorSourceHint));
        ConnectCommand.NotifyCanExecuteChanged();
        ToggleConnectionCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        ReadFromDeviceCommand.NotifyCanExecuteChanged();
        ApplyToDeviceCommand.NotifyCanExecuteChanged();
        SaveEepromCommand.NotifyCanExecuteChanged();
        ReturnToMidiCommand.NotifyCanExecuteChanged();
        UpdateMonitorRouting();
    }

    partial void OnIsBusyChanged(bool value)
    {
        ConnectCommand.NotifyCanExecuteChanged();
        ToggleConnectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPortChanged(string? value)
    {
        ConnectCommand.NotifyCanExecuteChanged();
        ToggleConnectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPadChanged(PadViewModel? value) => OnPropertyChanged(nameof(HasSelectedPad));

    partial void OnCurrentPageChanged(AppPage value)
    {
        OnPropertyChanged(nameof(IsConfigPage));
        OnPropertyChanged(nameof(IsMonitorPage));
    }

    partial void OnSelectedMidiInputChanged(MidiInputDevice? value)
    {
        OnPropertyChanged(nameof(MonitorSourceHint));
        UpdateMonitorRouting();
    }

    partial void OnIsMonitorEnabledChanged(bool value) => UpdateMonitorRouting();

    private async void UpdateMonitorRouting()
    {
        if (!IsMonitorEnabled)
        {
            StopMidiMonitor();
            if (IsConnected && _client.IsConnected)
            {
                try
                {
                    await _client.SetDiagnosticModeAsync(false);
                }
                catch
                {
                    // Ignore routing errors.
                }
            }

            return;
        }

        if (IsConnected && _client.IsConnected)
        {
            StopMidiMonitor();
            try
            {
                await _client.SetDiagnosticModeAsync(true);
                StatusMessage = "Monitor serial ativo — bata nos pads.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Falha ao ativar monitor: {ex.Message}";
            }

            return;
        }

        if (SelectedMidiInput is not null)
        {
            try
            {
                _midiInput.Open(SelectedMidiInput.Index);
                StatusMessage = $"Monitor MIDI ativo: {SelectedMidiInput.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Falha ao abrir MIDI: {ex.Message}";
            }
        }
    }

    private void StopMidiMonitor() => _midiInput.Close();

    private void OnPadHitReceived(PadHitEvent hit)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var pad = Pads.FirstOrDefault(p => p.Index == hit.PadIndex);
            pad?.RegisterHit(hit.Value);
        });
    }

    private void OnMidiNoteReceived(byte note, byte velocity)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var pad = Pads.FirstOrDefault(p => p.Note == note);
            pad?.RegisterHit(velocity);
        });
    }

    private void LoadDefaultKit()
    {
        var jsonPath = RepoPaths.FindPresetPath("kit-atual.json");
        if (jsonPath is not null)
        {
            LoadJsonPreset(jsonPath);
            return;
        }

        var iniPath = RepoPaths.FindPresetPath("kit-atual.ini");
        if (iniPath is not null)
        {
            ImportPinsIni(iniPath);
            return;
        }

        ApplyKit(DrumKit.CreateDefault());
    }

    private DrumKit BuildKitFromView()
    {
        var kit = DrumKit.CreateDefault();
        kit.Name = KitName;
        foreach (var pad in Pads)
        {
            if (pad.Index >= 0 && pad.Index < MicroDrumConstants.PadCount)
            {
                kit.Pads[pad.Index] = pad.ToModel();
            }
        }

        return kit;
    }

    private void ApplyKit(DrumKit kit, IReadOnlyDictionary<int, string>? preservedNames = null)
    {
        var normalized = kit.Normalize();
        if (preservedNames is not null)
        {
            foreach (var pad in normalized.Pads)
            {
                if (preservedNames.TryGetValue(pad.Index, out var name)
                    && !string.IsNullOrWhiteSpace(name))
                {
                    pad.Name = name;
                }
            }
        }

        Pads.Clear();
        foreach (var pad in normalized.Pads)
        {
            Pads.Add(new PadViewModel(pad.Clone()));
        }

        KitName = normalized.Name;
        SelectedPad = Pads.FirstOrDefault();
    }

    private async Task RunBusyAsync(Func<Task> action, string errorPrefix)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusMessage = $"{errorPrefix} {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _hitDecayTimer.Stop();
        _client.PadHitReceived -= OnPadHitReceived;
        _midiInput.NoteReceived -= OnMidiNoteReceived;

        if (_client.IsConnected)
        {
            try
            {
                await _client.SetDiagnosticModeAsync(false);
                await _client.ReturnToMidiModeAsync();
            }
            catch
            {
                // Best effort on shutdown.
            }
        }

        _midiInput.Dispose();
        _client.Disconnect();
        await _client.DisposeAsync();
    }
}
