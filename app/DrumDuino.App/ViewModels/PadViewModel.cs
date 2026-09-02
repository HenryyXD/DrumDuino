using DrumDuino.App.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using DrumDuino.Core.Models;

namespace DrumDuino.App.ViewModels;

public partial class PadViewModel : ObservableObject
{
    private readonly PadConfig _pad;

    public PadViewModel(PadConfig pad)
    {
        _pad = pad;
    }

    public int Index => _pad.Index;

    public string SlotLabel => $"Pad {Index + 1:D2}";

    public string Name
    {
        get => _pad.Name;
        set
        {
            if (_pad.Name == value)
            {
                return;
            }

            _pad.Name = value;
            OnPropertyChanged();
            NotifyConfigChanged();
        }
    }

    public PadType Type
    {
        get => _pad.Type;
        set
        {
            if (_pad.Type == value)
            {
                return;
            }

            _pad.Type = value;
            OnPropertyChanged();
            NotifyConfigChanged();
        }
    }

    public byte Note
    {
        get => _pad.Note;
        set
        {
            if (_pad.Note == value)
            {
                return;
            }

            _pad.Note = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NoteLabel));
            NotifyConfigChanged();
        }
    }

    public string NoteLabel => MidiNoteHelper.Format(Note);

    public byte Threshold
    {
        get => _pad.Threshold;
        set => SetByte(value, v => _pad.Threshold = v, nameof(Threshold));
    }

    public byte ScanTime
    {
        get => _pad.ScanTime;
        set => SetByte(value, v => _pad.ScanTime = v, nameof(ScanTime));
    }

    public byte MaskTime
    {
        get => _pad.MaskTime;
        set => SetByte(value, v => _pad.MaskTime = v, nameof(MaskTime));
    }

    public byte Retrigger
    {
        get => _pad.Retrigger;
        set => SetByte(value, v => _pad.Retrigger = v, nameof(Retrigger));
    }

    public VelocityCurve Curve
    {
        get => _pad.Curve;
        set
        {
            if (_pad.Curve == value)
            {
                return;
            }

            _pad.Curve = value;
            OnPropertyChanged();
            NotifyConfigChanged();
        }
    }

    public byte CurveForm
    {
        get => _pad.CurveForm;
        set => SetByte(value, v => _pad.CurveForm = v, nameof(CurveForm));
    }

    public byte XTalk
    {
        get => _pad.XTalk;
        set => SetByte(value, v => _pad.XTalk = v, nameof(XTalk));
    }

    public byte XTalkGroup
    {
        get => _pad.XTalkGroup;
        set => SetByte(value, v => _pad.XTalkGroup = v, nameof(XTalkGroup));
    }

    public byte Gain
    {
        get => _pad.Gain;
        set => SetByte(value, v => _pad.Gain = v, nameof(Gain));
    }

    public byte Channel
    {
        get => _pad.Channel;
        set => SetByte(value, v => _pad.Channel = v, nameof(Channel));
    }

    public string Summary => $"{Type} · {NoteLabel} · thr {Threshold}";

    public event Action? ConfigChanged;

    private void NotifyConfigChanged()
    {
        ConfigChanged?.Invoke();
        OnPropertyChanged(nameof(Summary));
    }

    [ObservableProperty]
    private bool _hasDiff;

    [ObservableProperty]
    private bool _isMonitorMuted;

    [ObservableProperty]
    private bool _isBatchSelected;

    [ObservableProperty]
    private bool _isVisibleInList = true;

    [ObservableProperty]
    private int _lastHitValue;

    [ObservableProperty]
    private int _hitLevel;

    public double HitLevelPercent => HitLevel / 127.0;

    public string LastHitLabel => LastHitValue > 0 ? LastHitValue.ToString() : "—";

    public void RegisterHit(byte value)
    {
        LastHitValue = value;
        HitLevel = Math.Min(127, (int)value);
        OnPropertyChanged(nameof(LastHitLabel));
        OnPropertyChanged(nameof(HitLevelPercent));
    }

    public void DecayHit(int amount = 6)
    {
        if (HitLevel <= 0)
        {
            return;
        }

        HitLevel = Math.Max(0, HitLevel - amount);
        OnPropertyChanged(nameof(HitLevelPercent));
    }

    public void ClearHit()
    {
        LastHitValue = 0;
        HitLevel = 0;
        OnPropertyChanged(nameof(LastHitLabel));
        OnPropertyChanged(nameof(HitLevelPercent));
    }

    public PadConfig ToModel() => _pad.Clone();

    public void ReplaceFrom(PadConfig pad)
    {
        _pad.Name = pad.Name;
        _pad.Type = pad.Type;
        _pad.Note = pad.Note;
        _pad.Threshold = pad.Threshold;
        _pad.ScanTime = pad.ScanTime;
        _pad.MaskTime = pad.MaskTime;
        _pad.Retrigger = pad.Retrigger;
        _pad.Curve = pad.Curve;
        _pad.CurveForm = pad.CurveForm;
        _pad.XTalk = pad.XTalk;
        _pad.XTalkGroup = pad.XTalkGroup;
        _pad.Gain = pad.Gain;
        _pad.Channel = pad.Channel;

        OnPropertyChanged(string.Empty);
        NotifyConfigChanged();
    }

    private void SetByte(byte value, Action<byte> assign, string propertyName)
    {
        var current = propertyName switch
        {
            nameof(Threshold) => _pad.Threshold,
            nameof(ScanTime) => _pad.ScanTime,
            nameof(MaskTime) => _pad.MaskTime,
            nameof(Retrigger) => _pad.Retrigger,
            nameof(CurveForm) => _pad.CurveForm,
            nameof(XTalk) => _pad.XTalk,
            nameof(XTalkGroup) => _pad.XTalkGroup,
            nameof(Gain) => _pad.Gain,
            nameof(Channel) => _pad.Channel,
            _ => value
        };

        if (current == value)
        {
            return;
        }

        assign(value);
        OnPropertyChanged(propertyName);
        NotifyConfigChanged();
    }
}
