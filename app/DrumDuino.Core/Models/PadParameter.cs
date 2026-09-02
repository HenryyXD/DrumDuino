namespace DrumDuino.Core.Models;

public enum PadParameter : byte
{
    Note = 0x00,
    Threshold = 0x01,
    ScanTime = 0x02,
    MaskTime = 0x03,
    Retrigger = 0x04,
    Curve = 0x05,
    XTalk = 0x06,
    XTalkGroup = 0x07,
    CurveForm = 0x08,
    Gain = 0x09,
    Dual = 0x0A,
    Type = 0x0D,
    Channel = 0x0E
}
