using Avalonia;
using Avalonia.Controls;

namespace DrumDuino.App.Controls;

public partial class ParameterRow : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ParameterRow, string>(nameof(Label));

    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<ParameterRow, string?>(nameof(Hint));

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ParameterRow, double>(nameof(Value), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ParameterRow, double>(nameof(Minimum), 0d);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ParameterRow, double>(nameof(Maximum), 127d);

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Hint
    {
        get => GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public ParameterRow()
    {
        InitializeComponent();
    }
}
