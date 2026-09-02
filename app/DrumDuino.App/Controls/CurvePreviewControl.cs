using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DrumDuino.App.Helpers;
using DrumDuino.Core.Models;

namespace DrumDuino.App.Controls;

public class CurvePreviewControl : Control
{
    public static readonly StyledProperty<VelocityCurve> CurveProperty =
        AvaloniaProperty.Register<CurvePreviewControl, VelocityCurve>(nameof(Curve));

    public static readonly StyledProperty<byte> CurveFormProperty =
        AvaloniaProperty.Register<CurvePreviewControl, byte>(nameof(CurveForm));

    public VelocityCurve Curve
    {
        get => GetValue(CurveProperty);
        set => SetValue(CurveProperty, value);
    }

    public byte CurveForm
    {
        get => GetValue(CurveFormProperty);
        set => SetValue(CurveFormProperty, value);
    }

    static CurvePreviewControl()
    {
        AffectsRender<CurvePreviewControl>(CurveProperty, CurveFormProperty);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = Bounds;
        if (bounds.Width < 4 || bounds.Height < 4)
        {
            return;
        }

        var bg = ThemeBrushes.Get("SurfaceRaisedBrush", "#1A2028");
        var accent = ThemeBrushes.Get("AccentBrush", "#F0B429");
        var border = ThemeBrushes.Get("BorderBrush", "#2A313C");

        var rect = new Rect(0, 0, bounds.Width, bounds.Height);
        context.DrawRectangle(bg, new Pen(border, 1), rect);

        var points = VelocityCurveHelper.GetPreviewPoints(Curve, CurveForm);
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            var first = true;
            foreach (var (x, y) in points)
            {
                var px = 4 + x * (bounds.Width - 8);
                var py = bounds.Height - 4 - y * (bounds.Height - 8);
                if (first)
                {
                    ctx.BeginFigure(new Point(px, py), false);
                    first = false;
                }
                else
                {
                    ctx.LineTo(new Point(px, py));
                }
            }
        }

        context.DrawGeometry(null, new Pen(accent, 2), geo);
    }
}
