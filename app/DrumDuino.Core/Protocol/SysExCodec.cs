using DrumDuino.Core;

namespace DrumDuino.Core.Protocol;

public readonly record struct SysExMessage(byte Command, byte Data1, byte Data2, byte Data3)
{
    public bool IsEndOfTransmission =>
        Command == 0x02
        && Data1 == MicroDrumConstants.EndTransmissionMarker
        && Data2 == MicroDrumConstants.EndTransmissionMarker;
}

public static class SysExCodec
{
    public static byte[] Encode(SysExMessage message)
    {
        return
        [
            0xF0,
            MicroDrumConstants.ManufacturerId,
            message.Command,
            message.Data1,
            message.Data2,
            message.Data3,
            0xF7
        ];
    }

    public static bool TryParseFrame(ReadOnlySpan<byte> frame, out SysExMessage message)
    {
        message = default;
        if (frame.Length != 7
            || frame[0] != 0xF0
            || frame[1] != MicroDrumConstants.ManufacturerId
            || frame[6] != 0xF7)
        {
            return false;
        }

        message = new SysExMessage(frame[2], frame[3], frame[4], frame[5]);
        return true;
    }
}

public static class SysExCommands
{
    public const byte AskMode = 0x00;
    public const byte SetMode = 0x01;
    public const byte AskSetting = 0x02;
    public const byte SetSetting = 0x03;
    public const byte SaveSetting = 0x04;
    public const byte Diagnostic = 0x6F;
}
