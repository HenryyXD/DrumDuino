using System.Globalization;
using DrumDuino.Core.Models;

using DrumDuino.Core;

namespace DrumDuino.Core.Presets;

public static class PinsIniImporter
{
    /// <summary>
    /// Legacy pins.ini line format:
    /// name;type;note;threshold;scantime;masktime;retrigger;curveform;xtalk;gain;xtalkgroup;channel;reserved
    /// </summary>
    public static DrumKit Import(string path, int padCount = MicroDrumConstants.PadCount)
    {
        var lines = File.ReadAllLines(path);
        var kit = DrumKit.CreateDefault(padCount);

        for (var i = 0; i < Math.Min(lines.Length, padCount); i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = line.Split(';');
            if (fields.Length < 7)
            {
                continue;
            }

            var pad = kit.Pads[i];
            pad.Name = string.IsNullOrWhiteSpace(fields[0]) ? $"Pad {i + 1}" : fields[0];
            pad.Type = (PadType)ParseByte(fields, 1, (byte)pad.Type);
            pad.Note = ParseByte(fields, 2, pad.Note);
            pad.Threshold = ParseByte(fields, 3, pad.Threshold);
            pad.ScanTime = ParseByte(fields, 4, pad.ScanTime);
            pad.MaskTime = ParseByte(fields, 5, pad.MaskTime);
            pad.Retrigger = ParseByte(fields, 6, pad.Retrigger);
            pad.CurveForm = ParseByte(fields, 7, pad.CurveForm);
            pad.XTalk = ParseByte(fields, 8, pad.XTalk);
            pad.Gain = ParseByte(fields, 9, pad.Gain);
            pad.XTalkGroup = ParseByte(fields, 10, pad.XTalkGroup);
            pad.Channel = ParseByte(fields, 11, pad.Channel);
        }

        kit.Name = Path.GetFileNameWithoutExtension(path);
        return kit.Normalize();
    }

    private static byte ParseByte(string[] fields, int index, byte fallback)
    {
        if (index >= fields.Length)
        {
            return fallback;
        }

        return byte.TryParse(fields[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }
}
