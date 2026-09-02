namespace DrumDuino.Core.Models;

public sealed class PadConfig
{
    public int Index { get; init; }
    public string Name { get; set; } = string.Empty;
    public PadType Type { get; set; } = PadType.Piezo;
    public byte Note { get; set; }
    public byte Threshold { get; set; }
    public byte ScanTime { get; set; }
    public byte MaskTime { get; set; }
    public byte Retrigger { get; set; }
    public VelocityCurve Curve { get; set; } = VelocityCurve.Exp;
    public byte CurveForm { get; set; } = 31;
    public byte XTalk { get; set; }
    public byte XTalkGroup { get; set; }
    public byte Gain { get; set; } = 99;
    public byte Channel { get; set; }

    public static PadConfig CreateDefault(int index) => new()
    {
        Index = index,
        Name = $"Pad {index + 1}",
        Type = PadType.Disabled,
        Note = 36,
        Threshold = 20,
        ScanTime = 25,
        MaskTime = 30,
        Retrigger = 0,
        Curve = VelocityCurve.Exp,
        CurveForm = 31,
        Gain = 99
    };

    public PadConfig Clone() => new()
    {
        Index = Index,
        Name = Name,
        Type = Type,
        Note = Note,
        Threshold = Threshold,
        ScanTime = ScanTime,
        MaskTime = MaskTime,
        Retrigger = Retrigger,
        Curve = Curve,
        CurveForm = CurveForm,
        XTalk = XTalk,
        XTalkGroup = XTalkGroup,
        Gain = Gain,
        Channel = Channel
    };

    public void ApplyParameter(PadParameter parameter, byte value)
    {
        switch (parameter)
        {
            case PadParameter.Note:
                Note = value;
                break;
            case PadParameter.Threshold:
                Threshold = value;
                break;
            case PadParameter.ScanTime:
                ScanTime = value;
                break;
            case PadParameter.MaskTime:
                MaskTime = value;
                break;
            case PadParameter.Retrigger:
                Retrigger = value;
                break;
            case PadParameter.Curve:
                Curve = (VelocityCurve)value;
                break;
            case PadParameter.XTalk:
                XTalk = value;
                break;
            case PadParameter.XTalkGroup:
                XTalkGroup = value;
                break;
            case PadParameter.CurveForm:
                CurveForm = value;
                break;
            case PadParameter.Gain:
                Gain = value;
                break;
            case PadParameter.Type:
                Type = (PadType)value;
                break;
            case PadParameter.Channel:
                Channel = value;
                break;
        }
    }

    public IEnumerable<(PadParameter Parameter, byte Value)> EnumerateParameters()
    {
        yield return (PadParameter.Note, Note);
        yield return (PadParameter.Threshold, Threshold);
        yield return (PadParameter.ScanTime, ScanTime);
        yield return (PadParameter.MaskTime, MaskTime);
        yield return (PadParameter.Retrigger, Retrigger);
        yield return (PadParameter.Curve, (byte)Curve);
        yield return (PadParameter.XTalk, XTalk);
        yield return (PadParameter.XTalkGroup, XTalkGroup);
        yield return (PadParameter.CurveForm, CurveForm);
        yield return (PadParameter.Gain, Gain);
        yield return (PadParameter.Type, (byte)Type);
        yield return (PadParameter.Channel, Channel);
    }
}
