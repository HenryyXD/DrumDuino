namespace DrumDuino.Core;

public static class MicroDrumConstants
{
    public const byte ManufacturerId = 0x77;
    public const int DefaultBaudRate = 115200;
    public const int PadCount = 16;

    public const byte GeneralSettingsPin = 0x7E;
    public const byte HiHatSettingsPin = 0x4C;
    public const byte EndTransmissionMarker = 0x7F;
    public const byte AskAllParameters = 0x7F;
}
