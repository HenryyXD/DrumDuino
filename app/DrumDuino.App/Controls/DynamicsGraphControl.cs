using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using DrumDuino.App.Helpers;
using DrumDuino.App.ViewModels;

namespace DrumDuino.App.Controls;

/// <summary>
/// Inspired by Beat Studio Live Play Dynamics Graph:
/// X = time, Y = velocity 0–127, dashed target envelope + tolerance band + hit dots.
/// </summary>
public class DynamicsGraphControl : Control
{
    private DispatcherTimer? _timer;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += (_, _) => InvalidateVisual();
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer?.Stop();
        _timer = null;
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var bounds = Bounds;
        if (bounds.Width < 80 || bounds.Height < 80)
        {
            return;
        }

        var trainer = vm.DynamicsTrainer;
        var accent = ThemeBrushes.Get("AccentBrush", "#F0B429");
        var success = ThemeBrushes.Get("SuccessBrush", "#3DD68C");
        var danger = new SolidColorBrush(Color.Parse("#F07178"));
        var border = ThemeBrushes.Get("BorderBrush", "#2A313C");
        var muted = ThemeBrushes.Get("TextMutedBrush", "#6B7280");
        var band = new SolidColorBrush(Color.Parse("#40F0B429"));
        var track = ThemeBrushes.Get("SurfaceRaisedBrush", "#1A2028");

        const double left = 44;
        const double right = 12;
        const double top = 16;
        const double bottom = 28;
        var plotW = bounds.Width - left - right;
        var plotH = bounds.Height - top - bottom;
        var duration = Math.Max(1, trainer.DurationSec);

        context.FillRectangle(track, new Rect(left, top, plotW, plotH));

        for (var v = 0; v <= 127; v += 32)
        {
            var y = top + plotH - v / 127.0 * plotH;
            context.DrawLine(new Pen(border, 0.5), new Point(left, y), new Point(left + plotW, y));
            var label = new FormattedText(
                v.ToString(),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter"),
                9,
                muted);
            context.DrawText(label, new Point(4, y - 6));
        }

        var step = Math.Max(1, (int)(duration / 8));
        for (var sec = 0; sec <= (int)duration; sec += step)
        {
            var x = left + sec / duration * plotW;
            context.DrawLine(new Pen(border, 0.5), new Point(x, top), new Point(x, top + plotH));
            var label = new FormattedText(
                $"{sec}s",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter"),
                9,
                muted);
            context.DrawText(label, new Point(x - 8, top + plotH + 6));
        }

        const int samples = 80;
        var bandHi = new Point[samples + 1];
        var bandLo = new Point[samples + 1];
        var targetPts = new Point[samples + 1];
        for (var i = 0; i <= samples; i++)
        {
            var t = duration * i / samples;
            var target = trainer.GetTargetAt(t);
            var x = left + t / duration * plotW;
            var hi = Math.Min(127, target + trainer.Tolerance);
            var lo = Math.Max(0, target - trainer.Tolerance);
            bandHi[i] = new Point(x, top + plotH - hi / 127.0 * plotH);
            bandLo[i] = new Point(x, top + plotH - lo / 127.0 * plotH);
            targetPts[i] = new Point(x, top + plotH - target / 127.0 * plotH);
        }

        var bandGeo = new StreamGeometry();
        using (var ctx = bandGeo.Open())
        {
            ctx.BeginFigure(bandHi[0], true);
            for (var i = 1; i <= samples; i++)
            {
                ctx.LineTo(bandHi[i]);
            }

            for (var i = samples; i >= 0; i--)
            {
                ctx.LineTo(bandLo[i]);
            }

            ctx.EndFigure(true);
        }

        context.DrawGeometry(band, null, bandGeo);

        var targetGeo = new StreamGeometry();
        using (var ctx = targetGeo.Open())
        {
            ctx.BeginFigure(targetPts[0], false);
            for (var i = 1; i <= samples; i++)
            {
                ctx.LineTo(targetPts[i]);
            }
        }

        context.DrawGeometry(null, new Pen(accent, 2) { DashStyle = new DashStyle([4, 3], 0) }, targetGeo);

        if (trainer.IsRunning)
        {
            var elapsed = trainer.ElapsedSec;
            var px = left + Math.Clamp(elapsed / duration, 0, 1) * plotW;
            context.DrawLine(new Pen(accent, 1.5), new Point(px, top), new Point(px, top + plotH));
        }

        foreach (var hit in trainer.Hits)
        {
            var x = left + hit.TimeSec / duration * plotW;
            var y = top + plotH - hit.Velocity / 127.0 * plotH;
            var brush = hit.Error <= trainer.Tolerance ? success : danger;
            context.DrawEllipse(brush, null, new Point(x, y), 4, 4);
            var ty = top + plotH - hit.Target / 127.0 * plotH;
            context.DrawLine(new Pen(brush, 1), new Point(x, y), new Point(x, ty));
        }

        var title = new FormattedText(
            "Velocity",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter"),
            9,
            muted);
        context.DrawText(title, new Point(2, 2));

        var legend = new FormattedText(
            "— alvo   ● hit (verde=ok)",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter"),
            9,
            muted);
        context.DrawText(legend, new Point(left, 2));
    }
}
