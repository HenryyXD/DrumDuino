using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using DrumDuino.App.Helpers;
using DrumDuino.App.ViewModels;
using DrumDuino.Core;

namespace DrumDuino.App.Controls;

public abstract class AnalyticsRenderControl : Control
{
    private DispatcherTimer? _timer;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _timer.Tick += (_, _) => InvalidateVisual();
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer?.Stop();
        _timer = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected MainViewModel? Vm => DataContext as MainViewModel;
}

public class HeatmapControl : AnalyticsRenderControl
{
    public override void Render(DrawingContext context)
    {
        if (Vm is null)
        {
            return;
        }

        var bounds = Bounds;
        if (bounds.Width < 16 || bounds.Height < 16)
        {
            return;
        }

        var padCount = MicroDrumConstants.PadCount;
        var buckets = 8;
        var cellW = bounds.Width / buckets;
        var cellH = bounds.Height / padCount;
        var accent = ThemeBrushes.Get("AccentBrush", "#F0B429");
        var idle = ThemeBrushes.Get("SurfaceRaisedBrush", "#1A2028");
        var border = ThemeBrushes.Get("BorderBrush", "#2A313C");

        for (var pad = 0; pad < padCount; pad++)
        {
            for (var b = 0; b < buckets; b++)
            {
                var intensity = Vm.GetHeatmapCell(pad, b);
                var rect = new Rect(b * cellW, pad * cellH, cellW - 1, cellH - 1);
                if (intensity <= 0.01)
                {
                    context.DrawRectangle(idle, new Pen(border, 0.5), rect);
                }
                else
                {
                    var opacity = 0.25 + intensity * 0.75;
                    context.DrawRectangle(accent, null, rect, opacity);
                }
            }
        }
    }
}
