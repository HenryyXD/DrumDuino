using Avalonia;
using Avalonia.Media;
using DrumDuino.App.Helpers;
using DrumDuino.Core;

namespace DrumDuino.App.Controls;

public class IntensityChartControl : AnalyticsRenderControl
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

        var accent = ThemeBrushes.Get("AccentBrush", "#F0B429");
        var idle = ThemeBrushes.Get("SurfaceRaisedBrush", "#1A2028");
        var border = ThemeBrushes.Get("BorderBrush", "#2A313C");

        const int seconds = 8;
        const int padCount = MicroDrumConstants.PadCount;
        var colW = bounds.Width / seconds;
        var rowH = bounds.Height / padCount;

        for (var pad = 0; pad < padCount; pad++)
        {
            for (var s = 0; s < seconds; s++)
            {
                var vel = Vm.GetIntensityCell(pad, s);
                var rect = new Rect(s * colW, pad * rowH, colW - 1, rowH - 1);
                if (vel == 0)
                {
                    context.DrawRectangle(idle, new Pen(border, 0.5), rect);
                }
                else
                {
                    var opacity = 0.2 + vel / 127.0 * 0.8;
                    context.DrawRectangle(accent, null, rect, opacity);
                }
            }
        }
    }
}
