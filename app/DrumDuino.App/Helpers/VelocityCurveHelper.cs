using DrumDuino.Core.Models;

namespace DrumDuino.App.Helpers;

public static class VelocityCurveHelper
{
    /// <summary>
    /// Generates 32 points (input 0..127) for curve preview.
    /// </summary>
    public static IReadOnlyList<(double X, double Y)> GetPreviewPoints(VelocityCurve curve, byte curveForm)
    {
        var form = curveForm / 127.0;
        var points = new List<(double X, double Y)>(32);
        for (var i = 0; i <= 31; i++)
        {
            var input = i / 31.0;
            var output = curve switch
            {
                VelocityCurve.Linear => input,
                VelocityCurve.Exp => Math.Pow(input, 1.5 + form * 2),
                VelocityCurve.Log => Math.Log(1 + input * 9) / Math.Log(10),
                VelocityCurve.Sigma => input < 0.5
                    ? 2 * input * input
                    : 1 - Math.Pow(-2 * input + 2, 2) / 2,
                VelocityCurve.Flat => input < (0.2 + form * 0.3) ? 0 : 1,
                _ => input
            };

            output = Math.Clamp(output, 0, 1);
            points.Add((input, output));
        }

        return points;
    }
}
