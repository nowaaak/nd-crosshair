using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NdCrosshair.App.Controls;

public sealed class EnumMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true && parameter is string name ? Enum.Parse(targetType, name) : Binding.DoNothing;
}

public sealed class EnumVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var names = (parameter?.ToString() ?? string.Empty).Split('|');
        return names.Contains(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class Inverter : IValueConverter
{
    public static Inverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;
}

public sealed class VisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visible = value switch
        {
            bool flag => flag,
            string text => !string.IsNullOrEmpty(text),
            int number => number != 0,
            null => false,
            _ => true,
        };

        return visible != Invert ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
