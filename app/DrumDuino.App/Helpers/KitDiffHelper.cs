using DrumDuino.Core.Models;

namespace DrumDuino.App.Helpers;

public static class KitDiffHelper
{
    public static bool PadEquals(PadConfig a, PadConfig b)
    {
        return a.Type == b.Type
               && a.Note == b.Note
               && a.Threshold == b.Threshold
               && a.ScanTime == b.ScanTime
               && a.MaskTime == b.MaskTime
               && a.Retrigger == b.Retrigger
               && a.Curve == b.Curve
               && a.CurveForm == b.CurveForm
               && a.XTalk == b.XTalk
               && a.XTalkGroup == b.XTalkGroup
               && a.Gain == b.Gain
               && a.Channel == b.Channel;
    }

    public static bool KitsEqual(DrumKit a, DrumKit b)
    {
        if (a.Pads.Count != b.Pads.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Pads.Count; i++)
        {
            if (!PadEquals(a.Pads[i], b.Pads[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static HashSet<int> GetDiffPadIndices(DrumKit baseline, DrumKit current)
    {
        var diffs = new HashSet<int>();
        var count = Math.Min(baseline.Pads.Count, current.Pads.Count);
        for (var i = 0; i < count; i++)
        {
            if (!PadEquals(baseline.Pads[i], current.Pads[i]))
            {
                diffs.Add(i);
            }
        }

        return diffs;
    }
}
