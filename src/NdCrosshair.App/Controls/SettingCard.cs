using System.Windows;
using System.Windows.Controls;

namespace NdCrosshair.App.Controls;

public class SettingCard : ContentControl
{
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(SettingCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(SettingCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(SettingCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BodyProperty = DependencyProperty.Register(
        nameof(Body), typeof(object), typeof(SettingCard), new PropertyMetadata(null));

    static SettingCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SettingCard), new FrameworkPropertyMetadata(typeof(SettingCard)));
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public object? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }
}
