using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NdCrosshair.App;

public partial class ColorField : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(ColorField), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HexProperty = DependencyProperty.Register(
        nameof(Hex), typeof(string), typeof(ColorField),
        new FrameworkPropertyMetadata("#000000", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ColorField()
    {
        InitializeComponent();
    }

    public IReadOnlyList<string> Swatches { get; } =
        ["#00FF00", "#00FFFF", "#FFFF00", "#FF00FF", "#FF0000", "#FFFFFF", "#000000"];

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Hex
    {
        get => (string)GetValue(HexProperty);
        set => SetValue(HexProperty, value);
    }

    private void OnOpenPicker(object sender, RoutedEventArgs e) => PickerPopup.IsOpen = true;

    private void OnSwatch(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string hex })
        {
            Hex = hex;
        }
    }

    private void OnHexKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox box)
        {
            box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            box.SelectAll();
        }
    }
}
