using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrumDuino.App.Helpers;
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
    private readonly MidiOutputService _midiOut = new();
    private readonly HitAnalyticsService _analytics = new();
    private readonly KitHistoryService _history = new();
    private readonly PadProfileService _profiles = new();
    private readonly DynamicsTrainingService _dynamicsTrainer = new();
    private readonly DispatcherTimer _hitDecayTimer;
    private readonly DispatcherTimer _analyticsRefreshTimer;
    private readonly DispatcherTimer _historyDebounceTimer;
    private readonly DispatcherTimer _trainingUiTimer;
    private bool _historyDirty;

    private DrumKit? _eepromBaseline;
    private DrumKit? _ramBaseline;
    private readonly List<byte> _wizardVelocities = [];

    public ObservableCollection<PadViewModel> Pads { get; } = [];
    public ObservableCollection<string> SerialPorts { get; } = [];
    public ObservableCollection<MidiInputDevice> MidiInputDevices { get; } = [];
    public ObservableCollection<PadProfileInfo> PadProfiles { get; } = [];
    public ObservableCollection<HitRecord> RecentHits { get; } = [];

    public IReadOnlyList<PadType> PadTypes { get; } = Enum.GetValues<PadType>();
    public IReadOnlyList<VelocityCurve> VelocityCurves { get; } = Enum.GetValues<VelocityCurve>();
    public IReadOnlyList<EditorSection> EditorSections { get; } = Enum.GetValues<EditorSection>();
    public IReadOnlyList<DynamicsPattern> DynamicsPatterns { get; } = Enum.GetValues<DynamicsPattern>();
    public DynamicsTrainingService DynamicsTrainer => _dynamicsTrainer;

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
    private bool _isMonitorEnabled = true;

    [ObservableProperty]
    private AppPage _currentPage = AppPage.Configuration;

    [ObservableProperty]
    private EditorSection _currentEditorSection = EditorSection.Identity;

    [ObservableProperty]
    private string _statusMessage = "Desconectado — edite offline ou conecte a porta COM.";

    [ObservableProperty]
    private string _kitName = "Default";

    [ObservableProperty]
    private string _padSearchText = string.Empty;

    [ObservableProperty]
    private bool _singlePadMonitorMode;

    [ObservableProperty]
    private bool _hasRamDiff;

    [ObservableProperty]
    private bool _hasEepromDiff;

    [ObservableProperty]
    private string _diffSummary = string.Empty;

    [ObservableProperty]
    private string _crosstalkMessage = string.Empty;

    [ObservableProperty]
    private bool _isWizardActive;

    [ObservableProperty]
    private int _wizardHitCount;

    [ObservableProperty]
    private byte _wizardSuggestedThreshold;

    [ObservableProperty]
    private byte _wizardSuggestedMask;

    [ObservableProperty]
    private string _wizardStatus = string.Empty;

    [ObservableProperty]
    private int? _duplicateTargetIndex;

    [ObservableProperty]
    private PadProfileInfo? _selectedProfile;

    [ObservableProperty]
    private string _newProfileName = string.Empty;

    [ObservableProperty]
    private byte _batchThreshold = 20;

    [ObservableProperty]
    private VelocityCurve _batchCurve = VelocityCurve.Exp;

    [ObservableProperty]
    private DynamicsPattern _selectedDynamicsPattern = DynamicsPattern.Crescendo;

    [ObservableProperty]
    private double _trainingDurationSec = 16;

    [ObservableProperty]
    private int _trainingTolerance = 18;

    [ObservableProperty]
    private bool _trainingFocusSelectedPad = true;

    [ObservableProperty]
    private bool _isTrainingRunning;

    [ObservableProperty]
    private string _trainingScoreLabel = "Score: —";

    [ObservableProperty]
    private string _trainingStatusLabel = "Escolha um padrão e inicie o treino.";

    [ObservableProperty]
    private string _trainingProgressLabel = "0.0 / 16.0 s";

    public bool HasEepromBaseline => _eepromBaseline is not null;

    public string ConnectionButtonText => IsConnected ? "Desconectar" : "Conectar";
    public bool HasSelectedPad => SelectedPad is not null;
    public bool IsConfigPage => CurrentPage == AppPage.Configuration;
    public bool IsAnalyticsPage => CurrentPage == AppPage.Analytics;
    public bool IsTrainingPage => CurrentPage == AppPage.Training;
    public bool CanUndo => _history.UndoCount > 0;
    public bool CanRedo => _history.RedoCount > 0;
    public string UndoLabel => CanUndo ? $"Desfazer ({_history.UndoCount})" : "Desfazer";
    public string HistoryLabel => $"Snapshots: {_history.UndoCount}";

    public ConnectionMode Mode => IsConnected ? ConnectionMode.Tool : ConnectionMode.Disconnected;

    public IBrush ModeDotBrush => Mode switch
    {
        ConnectionMode.Tool => new SolidColorBrush(Color.Parse("#F0B429")),
        ConnectionMode.Midi => new SolidColorBrush(Color.Parse("#5B9CF5")),
        _ => new SolidColorBrush(Color.Parse("#6B7280"))
    };

    public string ModeLabel => Mode switch
    {
        ConnectionMode.Tool => "Tool",
        ConnectionMode.Midi => "MIDI",
        _ => "Off"
    };

    public IBrush ConnectionDotBrush => IsConnected
        ? new SolidColorBrush(Color.Parse("#3DD68C"))
        : new SolidColorBrush(Color.Parse("#6B7280"));

    public string MonitorSourceHint => IsConnected
        ? "Monitor via serial (modo Tool)."
        : SelectedMidiInput is null
            ? "Selecione uma entrada MIDI ou conecte a COM."
            : $"Monitor via MIDI: {SelectedMidiInput.Name}";

    public IEnumerable<PadViewModel> BatchSelectedPads => Pads.Where(p => p.IsBatchSelected);

    private static bool PadMatchesSearch(PadViewModel pad, string? query) =>
        string.IsNullOrWhiteSpace(query)
        || pad.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
        || pad.NoteLabel.Contains(query, StringComparison.OrdinalIgnoreCase);

    private void RefreshPadListVisibility()
    {
        foreach (var pad in Pads)
        {
            pad.IsVisibleInList = PadMatchesSearch(pad, PadSearchText);
        }
    }

    public MainViewModel()
    {
        _client.PadHitReceived += OnPadHitReceived;
        _midiInput.NoteReceived += OnMidiNoteReceived;
        _analytics.HistoryChanged += OnAnalyticsHistoryChanged;

        _hitDecayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _hitDecayTimer.Tick += (_, _) =>
        {
            // Peak-hold: bars stay at last hit (DecayHit no-ops with holdLastHit=true).
        };
        // Timer kept for future optional fade; not decaying by default.

        _analyticsRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _analyticsRefreshTimer.Tick += (_, _) => RefreshAnalyticsDisplay();
        _analyticsRefreshTimer.Start();

        _historyDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _historyDebounceTimer.Tick += (_, _) =>
        {
            _historyDebounceTimer.Stop();
            if (_historyDirty)
            {
                _historyDirty = false;
                PushHistorySnapshot();
            }
        };

        _trainingUiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _trainingUiTimer.Tick += (_, _) => RefreshTrainingUi();
        _dynamicsTrainer.Changed += () => Dispatcher.UIThread.Post(RefreshTrainingUi);

        LoadDefaultKit();
        RefreshPorts();
        RefreshMidiInputs();
        RefreshProfiles();
        PushHistorySnapshot();
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
    private void RefreshProfiles()
    {
        PadProfiles.Clear();
        foreach (var profile in _profiles.ListProfiles())
        {
            PadProfiles.Add(profile);
        }
    }

    [RelayCommand]
    private void ShowConfiguration() => CurrentPage = AppPage.Configuration;

    [RelayCommand]
    private void ShowAnalytics()
    {
        CurrentPage = AppPage.Analytics;
        RefreshAnalyticsDisplay();
    }

    [RelayCommand]
    private void ShowTraining()
    {
        CurrentPage = AppPage.Training;
        if (!IsMonitorEnabled)
        {
            IsMonitorEnabled = true;
            UpdateMonitorRouting();
        }

        RefreshTrainingUi();
    }

    [RelayCommand]
    private void StartDynamicsTraining()
    {
        if (!IsMonitorEnabled)
        {
            IsMonitorEnabled = true;
            UpdateMonitorRouting();
        }

        var focus = TrainingFocusSelectedPad ? SelectedPad?.Index : null;
        _dynamicsTrainer.Configure(
            SelectedDynamicsPattern,
            TrainingDurationSec,
            TrainingTolerance,
            20,
            110,
            focus);
        _dynamicsTrainer.Start();
        _trainingUiTimer.Start();
        IsTrainingRunning = true;
        TrainingStatusLabel = focus is int idx
            ? $"Treino ativo — foque no pad {Pads.FirstOrDefault(p => p.Index == idx)?.Name ?? idx.ToString()}."
            : "Treino ativo — qualquer pad.";
        StatusMessage = "Dynamics training iniciado (estilo Beat Studio).";
        RefreshTrainingUi();
    }

    [RelayCommand]
    private void StopDynamicsTraining()
    {
        _dynamicsTrainer.Stop();
        _trainingUiTimer.Stop();
        IsTrainingRunning = false;
        TrainingStatusLabel = $"Parado. Score final: {_dynamicsTrainer.ScorePercent:0}% ({_dynamicsTrainer.InToleranceCount}/{_dynamicsTrainer.HitCount}).";
        RefreshTrainingUi();
    }

    [RelayCommand]
    private void ResetDynamicsTraining()
    {
        _dynamicsTrainer.Reset();
        _trainingUiTimer.Stop();
        IsTrainingRunning = false;
        TrainingStatusLabel = "Resetado. Pronto para novo treino.";
        RefreshTrainingUi();
    }

    private void RefreshTrainingUi()
    {
        IsTrainingRunning = _dynamicsTrainer.IsRunning;
        TrainingProgressLabel = $"{_dynamicsTrainer.ElapsedSec:0.0} / {_dynamicsTrainer.DurationSec:0.0} s";
        TrainingScoreLabel = _dynamicsTrainer.HitCount == 0
            ? "Score: —"
            : $"Score: {_dynamicsTrainer.ScorePercent:0}%  ({_dynamicsTrainer.InToleranceCount}/{_dynamicsTrainer.HitCount} na faixa)";

        if (_dynamicsTrainer.IsRunning && _dynamicsTrainer.ElapsedSec >= _dynamicsTrainer.DurationSec)
        {
            _dynamicsTrainer.Stop();
            _trainingUiTimer.Stop();
            IsTrainingRunning = false;
            TrainingStatusLabel = $"Concluído! Score: {_dynamicsTrainer.ScorePercent:0}%.";
            StatusMessage = TrainingStatusLabel;
        }

        OnPropertyChanged(nameof(DynamicsTrainer));
    }

    [RelayCommand]
    private void SetEditorSection(string section)
    {
        if (Enum.TryParse<EditorSection>(section, out var parsed))
        {
            CurrentEditorSection = parsed;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (!_history.TryUndo(BuildKitFromView(), out var restored) || restored is null)
        {
            return;
        }

        ApplyKit(restored);
        UpdateDiffState();
        StatusMessage = "Alteração desfeita.";
        NotifyHistoryChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (!_history.TryRedo(BuildKitFromView(), out var restored) || restored is null)
        {
            return;
        }

        ApplyKit(restored);
        UpdateDiffState();
        StatusMessage = "Alteração refeita.";
        NotifyHistoryChanged();
    }
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
            PushHistorySnapshot();
            var names = Pads.ToDictionary(p => p.Index, p => p.Name);
            var kit = await _client.ReadKitAsync();
            ApplyKit(kit, names);
            _eepromBaseline = kit.Clone();
            _ramBaseline = kit.Clone();
            UpdateDiffState();
            ResetToEepromCommand.NotifyCanExecuteChanged();
            StatusMessage = "Configuração lida do módulo.";
        }, "Falha ao ler do módulo.");
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    private async Task ApplyToDeviceAsync()
    {
        await RunBusyAsync(async () =>
        {
            PushHistorySnapshot();
            var kit = BuildKitFromView();
            await _client.WriteKitAsync(kit, saveToEeprom: false);
            _ramBaseline = kit.Clone();
            UpdateDiffState();
            StatusMessage = "Configuração aplicada na RAM do módulo.";
        }, "Falha ao aplicar configuração.");
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    private async Task SaveEepromAsync()
    {
        await RunBusyAsync(async () =>
        {
            PushHistorySnapshot();
            var kit = BuildKitFromView();
            await _client.WriteKitAsync(kit, saveToEeprom: true);
            _eepromBaseline = kit.Clone();
            _ramBaseline = kit.Clone();
            UpdateDiffState();
            ResetToEepromCommand.NotifyCanExecuteChanged();
            StatusMessage = "Configuração salva na EEPROM.";
        }, "Falha ao salvar EEPROM.");
    }

    [RelayCommand(CanExecute = nameof(HasEepromBaseline))]
    private void ResetToEeprom()
    {
        if (_eepromBaseline is null)
        {
            StatusMessage = "Leia o módulo primeiro para obter baseline EEPROM.";
            return;
        }

        PushHistorySnapshot();
        ApplyKit(_eepromBaseline.Clone());
        StatusMessage = "Pads resetados para baseline EEPROM (app).";
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

    [RelayCommand]
    private void SendTestNote()
    {
        if (SelectedPad is null)
        {
            StatusMessage = "Selecione um pad para testar.";
            return;
        }

        try
        {
            if (_midiOut.GetDeviceNames().Count == 0)
            {
                StatusMessage = "Nenhuma saída MIDI disponível para test note.";
                return;
            }

            _midiOut.Open(0);
            _midiOut.SendNoteOn(SelectedPad.Note, 100, SelectedPad.Channel);
            StatusMessage = $"Test note enviada: {SelectedPad.NoteLabel} vel 100.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Falha no test note: {ex.Message}";
        }
    }

    [RelayCommand]
    private void StartThresholdWizard()
    {
        if (SelectedPad is null)
        {
            StatusMessage = "Selecione um pad para calibrar.";
            return;
        }

        if (!IsMonitorEnabled)
        {
            IsMonitorEnabled = true;
            UpdateMonitorRouting();
        }

        IsWizardActive = true;
        WizardHitCount = 0;
        _wizardVelocities.Clear();
        WizardSuggestedThreshold = 0;
        WizardSuggestedMask = 0;
        WizardStatus = "Bata 10 vezes no pad (fraco e forte).";
        StatusMessage = "Assistente de threshold ativo.";
    }

    [RelayCommand]
    private void CancelThresholdWizard()
    {
        IsWizardActive = false;
        _wizardVelocities.Clear();
        WizardStatus = string.Empty;
        StatusMessage = "Assistente cancelado.";
    }

    [RelayCommand]
    private void ApplyWizardSuggestion()
    {
        if (SelectedPad is null || WizardSuggestedThreshold == 0)
        {
            return;
        }

        PushHistorySnapshot();
        SelectedPad.Threshold = WizardSuggestedThreshold;
        if (WizardSuggestedMask > 0)
        {
            SelectedPad.MaskTime = WizardSuggestedMask;
        }

        IsWizardActive = false;
        UpdateDiffState();
        StatusMessage = $"Threshold {WizardSuggestedThreshold}, mask {WizardSuggestedMask} aplicados.";
    }

    [RelayCommand]
    private void SavePadProfile()
    {
        if (SelectedPad is null)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewProfileName)
            ? $"{SelectedPad.Name} profile"
            : NewProfileName;
        _profiles.SaveProfile(name, SelectedPad.ToModel());
        NewProfileName = string.Empty;
        RefreshProfiles();
        StatusMessage = $"Profile salvo: {name}";
    }

    [RelayCommand]
    private void LoadPadProfile()
    {
        if (SelectedPad is null || SelectedProfile is null)
        {
            return;
        }

        PushHistorySnapshot();
        var pad = _profiles.LoadProfile(SelectedProfile.FilePath);
        SelectedPad.ReplaceFrom(pad);
        UpdateDiffState();
        StatusMessage = $"Profile carregado: {SelectedProfile.Name}";
    }

    [RelayCommand]
    private void ApplyBatchSettings()
    {
        var targets = BatchSelectedPads.ToList();
        if (targets.Count == 0 && SelectedPad is not null)
        {
            targets.Add(SelectedPad);
        }

        if (targets.Count == 0)
        {
            StatusMessage = "Selecione pads (checkbox) para aplicar em lote.";
            return;
        }

        PushHistorySnapshot();
        foreach (var pad in targets)
        {
            pad.Threshold = BatchThreshold;
            pad.Curve = BatchCurve;
        }

        UpdateDiffState();
        StatusMessage = $"Lote aplicado em {targets.Count} pad(s).";
    }

    [RelayCommand]
    private void DuplicatePad()
    {
        if (SelectedPad is null || DuplicateTargetIndex is not int target || target < 0 || target >= Pads.Count)
        {
            StatusMessage = "Selecione pad origem e índice destino válido.";
            return;
        }

        if (target == SelectedPad.Index)
        {
            StatusMessage = "Destino deve ser diferente da origem.";
            return;
        }

        PushHistorySnapshot();
        var source = SelectedPad.ToModel();
        var dest = Pads[target];
        var destName = dest.Name;
        dest.ReplaceFrom(source);
        dest.Name = destName;
        SelectedPad = dest;
        UpdateDiffState();
        StatusMessage = $"Config copiada para pad {target}.";
    }

    [RelayCommand]
    private void MovePadUp()
    {
        if (SelectedPad is null || SelectedPad.Index <= 0)
        {
            return;
        }

        SwapPads(SelectedPad.Index, SelectedPad.Index - 1);
    }

    [RelayCommand]
    private void MovePadDown()
    {
        if (SelectedPad is null || SelectedPad.Index >= Pads.Count - 1)
        {
            return;
        }

        SwapPads(SelectedPad.Index, SelectedPad.Index + 1);
    }

    [RelayCommand]
    private void ToggleBatchSelectAll()
    {
        var allSelected = Pads.All(p => p.IsBatchSelected);
        foreach (var pad in Pads)
        {
            pad.IsBatchSelected = !allSelected;
        }
    }

    [RelayCommand]
    private void ClearAnalytics()
    {
        _analytics.Clear();
        CrosstalkMessage = string.Empty;
        RefreshAnalyticsDisplay();
    }

    public void ImportPinsIni(string path)
    {
        PushHistorySnapshot();
        var kit = PinsIniImporter.Import(path);
        ApplyKit(kit);
        KitName = kit.Name;
        UpdateDiffState();
        StatusMessage = $"Preset importado: {Path.GetFileName(path)}";
    }

    public void LoadJsonPreset(string path)
    {
        PushHistorySnapshot();
        var kit = KitPresetSerializer.Load(path);
        ApplyKit(kit);
        KitName = kit.Name;
        UpdateDiffState();
        StatusMessage = $"Preset carregado: {Path.GetFileName(path)}";
    }

    public void SaveJsonPreset(string path)
    {
        var kit = BuildKitFromView();
        KitPresetSerializer.Save(kit, path);
        StatusMessage = $"Preset salvo: {Path.GetFileName(path)}";
    }

    public double GetHeatmapCell(int padIndex, int bucket)
    {
        var heatmap = _analytics.GetHeatmap();
        if (padIndex < 0 || padIndex >= heatmap.GetLength(0) || bucket < 0 || bucket >= heatmap.GetLength(1))
        {
            return 0;
        }

        return heatmap[padIndex, bucket];
    }

    public byte GetIntensityCell(int padIndex, int second)
    {
        var grid = _analytics.GetIntensityGrid();
        if (padIndex < 0 || padIndex >= grid.GetLength(0) || second < 0 || second >= grid.GetLength(1))
        {
            return 0;
        }

        return grid[padIndex, second];
    }

    public IReadOnlyList<HitRecord> GetTimelineHits() => _analytics.History;

    private bool CanConnect() => !IsConnected && !IsBusy && !string.IsNullOrWhiteSpace(SelectedPort);
    private bool CanToggleConnection() => !IsBusy && (IsConnected || !string.IsNullOrWhiteSpace(SelectedPort));

    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(ConnectionButtonText));
        OnPropertyChanged(nameof(ConnectionDotBrush));
        OnPropertyChanged(nameof(ModeDotBrush));
        OnPropertyChanged(nameof(ModeLabel));
        OnPropertyChanged(nameof(MonitorSourceHint));
        ConnectCommand.NotifyCanExecuteChanged();
        ToggleConnectionCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        ReadFromDeviceCommand.NotifyCanExecuteChanged();
        ApplyToDeviceCommand.NotifyCanExecuteChanged();
        SaveEepromCommand.NotifyCanExecuteChanged();
        ResetToEepromCommand.NotifyCanExecuteChanged();
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

    partial void OnSelectedPadChanged(PadViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedPad));
        UpdateMonitorMuteState();
    }

    partial void OnSinglePadMonitorModeChanged(bool value) => UpdateMonitorMuteState();

    private void UpdateMonitorMuteState()
    {
        foreach (var pad in Pads)
        {
            pad.IsMonitorMuted = SinglePadMonitorMode && SelectedPad?.Index != pad.Index;
        }
    }

    partial void OnCurrentPageChanged(AppPage value)
    {
        OnPropertyChanged(nameof(IsConfigPage));
        OnPropertyChanged(nameof(IsAnalyticsPage));
        OnPropertyChanged(nameof(IsTrainingPage));
    }

    partial void OnPadSearchTextChanged(string value) => RefreshPadListVisibility();

    private void OnPadConfigChanged()
    {
        UpdateDiffState();
        _historyDirty = true;
        _historyDebounceTimer.Stop();
        _historyDebounceTimer.Start();
    }

    private void WirePad(PadViewModel pad)
    {
        pad.ConfigChanged += OnPadConfigChanged;
        pad.IsVisibleInList = PadMatchesSearch(pad, PadSearchText);
    }

    private void UnwirePads()
    {
        foreach (var pad in Pads)
        {
            pad.ConfigChanged -= OnPadConfigChanged;
        }
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

    private void OnPadHitReceived(PadHitEvent hit) =>
        Dispatcher.UIThread.Post(() => ProcessHit(hit.PadIndex, hit.Value));

    private void OnMidiNoteReceived(byte note, byte velocity)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var pad = Pads.FirstOrDefault(p => p.Note == note);
            if (pad is not null)
            {
                ProcessHit(pad.Index, velocity);
            }
        });
    }

    private void ProcessHit(int padIndex, byte velocity)
    {
        var pad = Pads.FirstOrDefault(p => p.Index == padIndex);
        pad?.RegisterHit(velocity);
        _analytics.RecordHit(padIndex, velocity);
        _dynamicsTrainer.TryRecordHit(padIndex, velocity, out _);

        if (IsWizardActive && SelectedPad?.Index == padIndex)
        {
            _wizardVelocities.Add(velocity);
            WizardHitCount = _wizardVelocities.Count;
            if (_wizardVelocities.Count >= 10)
            {
                var min = _wizardVelocities.Min();
                var max = _wizardVelocities.Max();
                WizardSuggestedThreshold = (byte)Math.Clamp((int)(min * 0.75), 1, 127);
                WizardSuggestedMask = (byte)Math.Clamp(15 + (max - min) / 8, 5, 60);
                WizardStatus = $"Sugestão: threshold {WizardSuggestedThreshold}, mask {WizardSuggestedMask}";
            }
            else
            {
                WizardStatus = $"Golpe {WizardHitCount}/10 — continue batendo.";
            }
        }

        var xtalk = _analytics.LastCrosstalk;
        if (xtalk is not null)
        {
            var nameA = Pads.FirstOrDefault(p => p.Index == xtalk.PadA)?.Name ?? $"#{xtalk.PadA}";
            var nameB = Pads.FirstOrDefault(p => p.Index == xtalk.PadB)?.Name ?? $"#{xtalk.PadB}";
            CrosstalkMessage = $"Crosstalk: {nameA} + {nameB} @ {xtalk.Time.LocalDateTime:HH:mm:ss}";
        }
    }

    private void OnAnalyticsHistoryChanged() =>
        Dispatcher.UIThread.Post(RefreshAnalyticsDisplay);

    private void RefreshAnalyticsDisplay()
    {
        RecentHits.Clear();
        foreach (var hit in _analytics.History.TakeLast(20).Reverse())
        {
            RecentHits.Add(hit);
        }

        OnPropertyChanged(nameof(RecentHits));
    }

    private void LoadDefaultKit()
    {
        var jsonPath = RepoPaths.FindPresetPath("kit-atual.json");
        if (jsonPath is not null)
        {
            var kit = KitPresetSerializer.Load(jsonPath);
            ApplyKit(kit);
            KitName = kit.Name;
            StatusMessage = $"Preset carregado: {Path.GetFileName(jsonPath)}";
            _history.Clear();
            PushHistorySnapshot();
            return;
        }

        var iniPath = RepoPaths.FindPresetPath("kit-atual.ini");
        if (iniPath is not null)
        {
            var kit = PinsIniImporter.Import(iniPath);
            ApplyKit(kit);
            KitName = kit.Name;
            StatusMessage = $"Preset importado: {Path.GetFileName(iniPath)}";
            _history.Clear();
            PushHistorySnapshot();
            return;
        }

        ApplyKit(DrumKit.CreateDefault());
        _history.Clear();
        PushHistorySnapshot();
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

        UnwirePads();
        Pads.Clear();
        foreach (var pad in normalized.Pads)
        {
            var vm = new PadViewModel(pad.Clone());
            WirePad(vm);
            Pads.Add(vm);
        }

        KitName = normalized.Name;
        SelectedPad = Pads.FirstOrDefault();
        UpdateDiffState();
        UpdateMonitorMuteState();
        RefreshPadListVisibility();
        ResetToEepromCommand.NotifyCanExecuteChanged();
    }

    private void SwapPads(int indexA, int indexB)
    {
        PushHistorySnapshot();
        var modelA = Pads[indexA].ToModel();
        var modelB = Pads[indexB].ToModel();
        var nameA = Pads[indexA].Name;
        var nameB = Pads[indexB].Name;

        Pads[indexA].ReplaceFrom(modelB);
        Pads[indexA].Name = nameA;
        Pads[indexB].ReplaceFrom(modelA);
        Pads[indexB].Name = nameB;
        SelectedPad = Pads[indexB];
        UpdateDiffState();
        StatusMessage = $"Pads {indexA} e {indexB} trocados.";
    }

    private void UpdateDiffState()
    {
        var current = BuildKitFromView();
        HasEepromDiff = _eepromBaseline is not null && !KitDiffHelper.KitsEqual(_eepromBaseline, current);
        HasRamDiff = _ramBaseline is not null && !KitDiffHelper.KitsEqual(_ramBaseline, current);

        var eepromDiffs = _eepromBaseline is not null
            ? KitDiffHelper.GetDiffPadIndices(_eepromBaseline, current)
            : [];
        var ramDiffs = _ramBaseline is not null
            ? KitDiffHelper.GetDiffPadIndices(_ramBaseline, current)
            : [];

        foreach (var pad in Pads)
        {
            pad.HasDiff = eepromDiffs.Contains(pad.Index) || ramDiffs.Contains(pad.Index);
        }

        var parts = new List<string>();
        if (HasRamDiff)
        {
            parts.Add("RAM");
        }

        if (HasEepromDiff)
        {
            parts.Add("EEPROM");
        }

        DiffSummary = parts.Count == 0 ? "Sincronizado" : string.Join(" ≠ App", parts) + " ≠ App";
        OnPropertyChanged(nameof(DiffSummary));
    }

    private void PushHistorySnapshot()
    {
        _historyDirty = false;
        _historyDebounceTimer.Stop();
        _history.Push(BuildKitFromView());
        NotifyHistoryChanged();
    }

    private void NotifyHistoryChanged()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoLabel));
        OnPropertyChanged(nameof(HistoryLabel));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
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
        _analyticsRefreshTimer.Stop();
        _historyDebounceTimer.Stop();
        _trainingUiTimer.Stop();
        _client.PadHitReceived -= OnPadHitReceived;
        _midiInput.NoteReceived -= OnMidiNoteReceived;
        _analytics.HistoryChanged -= OnAnalyticsHistoryChanged;

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
        _midiOut.Dispose();
        _client.Disconnect();
        await _client.DisposeAsync();
    }
}
