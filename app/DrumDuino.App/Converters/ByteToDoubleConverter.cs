using System.Globalization;
using Avalonia.Data.Converters;

namespace DrumDuino.App.Converters;

public sealed class ByteToDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is byte b ? (double)b : 0d;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double d)
        {
            return 0;
        }

        var min = 0;
        var max = 127;
        if (parameter is string range && range.Contains(','))
        {
            var parts = range.Split(',');
            if (parts.Length == 2
                && int.TryParse(parts[0], out var parsedMin)
                && int.TryParse(parts[1], out var parsedMax))
            {
                min = parsedMin;
                max = parsedMax;
            }
        }

        return (byte)Math.Clamp((int)Math.Round(d), min, max);
    }
}
