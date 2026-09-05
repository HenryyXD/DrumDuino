using Avalonia;
using Avalonia.Media;
using DrumDuino.App.Helpers;
using DrumDuino.Core;

namespace DrumDuino.App.Controls;

public class TimelineControl : AnalyticsRenderControl
{
    public override void Render(DrawingContext context)
    {
        if (Vm is null)
        {
            return;
        }

        var bounds = Bounds;
        if (bounds.Width < 40 || bounds.Height < 40)
        {
            return;
        }

        var hits = Vm.GetTimelineHits();
        var accent = ThemeBrushes.Get("AccentBrush", "#F0B429");
        var warn = ThemeBrushes.Get("SuccessBrush", "#3DD68C");
        var border = ThemeBrushes.Get("BorderBrush", "#2A313C");
        var textBrush = ThemeBrushes.Get("TextMutedBrush", "#6B7280");

        const double leftPad = 56;
        const double topPad = 8;
        const double bottomPad = 16;
        var plotW = bounds.Width - leftPad - 4;
        var plotH = bounds.Height - topPad - bottomPad;
        var now = DateTimeOffset.UtcNow;
        const double windowSec = 8.0;

        context.DrawLine(new Pen(border, 1), new Point(leftPad, topPad), new Point(leftPad, topPad + plotH));
        context.DrawLine(new Pen(border, 1), new Point(leftPad, topPad + plotH), new Point(leftPad + plotW, topPad + plotH));

        for (var sec = 0; sec <= 8; sec += 2)
        {
            var x = leftPad + sec / windowSec * plotW;
            context.DrawLine(new Pen(border, 0.5), new Point(x, topPad), new Point(x, topPad + plotH));
            var ft = new FormattedText(
                $"{sec}s",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter"),
                9,
                textBrush);
            context.DrawText(ft, new Point(x - 8, topPad + plotH + 2));
        }

        var padCount = MicroDrumConstants.PadCount;
        var rowH = plotH / padCount;

        foreach (var hit in hits)
        {
            var age = (now - hit.Time).TotalSeconds;
            if (age < 0 || age > windowSec)
            {
                continue;
            }

            var x = leftPad + (windowSec - age) / windowSec * plotW;
            var y = topPad + hit.PadIndex * rowH + rowH / 2;
            var r = 2 + hit.Velocity / 40.0;
            context.DrawEllipse(accent, null, new Point(x, y), r, r);
        }

        if (!string.IsNullOrEmpty(Vm.CrosstalkMessage))
        {
            var ft = new FormattedText(
                "● possível crosstalk",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter"),
                9,
                warn);
            context.DrawText(ft, new Point(leftPad, 0));
        }
    }
}
