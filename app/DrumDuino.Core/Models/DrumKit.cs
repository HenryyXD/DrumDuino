using DrumDuino.Core;

namespace DrumDuino.Core.Models;

public sealed class DrumKit
{
    public string Name { get; set; } = "Default";
    public List<PadConfig> Pads { get; set; } = [];

    public static DrumKit CreateDefault(int padCount = MicroDrumConstants.PadCount)
    {
        var kit = new DrumKit();
        for (var i = 0; i < padCount; i++)
        {
            kit.Pads.Add(PadConfig.CreateDefault(i));
        }

        return kit;
    }

    public PadConfig GetPad(int index) => Pads[index];

    public DrumKit Clone()
    {
        return new DrumKit
        {
            Name = Name,
            Pads = Pads.Select(p => p.Clone()).ToList()
        };
    }

    /// <summary>
    /// Returns a kit with exactly <paramref name="padCount"/> pads, merging by each pad's <see cref="PadConfig.Index"/>.
    /// Missing indices keep defaults.
    /// </summary>
    public DrumKit Normalize(int padCount = MicroDrumConstants.PadCount)
    {
        var result = CreateDefault(padCount);
        result.Name = Name;

        foreach (var pad in Pads)
        {
            if (pad.Index < 0 || pad.Index >= padCount)
            {
                continue;
            }

            var merged = pad.Clone();
            result.Pads[pad.Index] = merged;
        }

        return result;
    }
}
