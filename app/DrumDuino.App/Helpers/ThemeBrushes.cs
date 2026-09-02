using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace DrumDuino.App.Helpers;

internal static class ThemeBrushes
{
    public static IBrush Get(string key, string fallbackHex)
    {
        if (Application.Current is { } app
            && app.Resources.TryGetResource(key, app.ActualThemeVariant, out var resource)
            && resource is IBrush brush)
        {
            return brush;
        }

        return new SolidColorBrush(Color.Parse(fallbackHex));
    }
}
