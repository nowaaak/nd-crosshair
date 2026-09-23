using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NdCrosshair.App;

public partial class SettingSlider : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(SettingSlider), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(int), typeof(SettingSlider),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(int), typeof(SettingSlider), new PropertyMetadata(0));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(int), typeof(SettingSlider), new PropertyMetadata(100));

    public SettingSlider()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    private void OnValueBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox box)
        {
            box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            box.SelectAll();
        }
    }
}
