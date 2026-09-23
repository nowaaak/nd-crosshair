using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NdCrosshair.Core;

namespace NdCrosshair.App.Controls;

public partial class ColorPicker : UserControl
{
    public static readonly DependencyProperty HexProperty = DependencyProperty.Register(
        nameof(Hex), typeof(string), typeof(ColorPicker),
        new FrameworkPropertyMetadata("#000000", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHexChanged));

    private const double KeyboardStep = 0.02;
    private const double HueStep = 3;

    private double hue;
    private double saturation;
    private double value;
    private bool updating;

    public ColorPicker()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ReadHex(Hex);
            UpdateVisuals();
        };
        SvArea.SizeChanged += (_, _) => UpdateVisuals();
        HueArea.SizeChanged += (_, _) => UpdateVisuals();
    }

    public string Hex
    {
        get => (string)GetValue(HexProperty);
        set => SetValue(HexProperty, value);
    }

    private static void OnHexChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        var picker = (ColorPicker)target;
        if (picker.updating)
        {
            return;
        }

        picker.ReadHex(e.NewValue as string);
        picker.UpdateVisuals();
    }

    private void ReadHex(string? hex)
    {
        if (!CrosshairColor.TryParseHex(hex, out var color))
        {
            return;
        }

        var (h, s, v) = ColorMath.ToHsv(color);
        if (s > 0 && v > 0)
        {
            hue = h;
        }

        saturation = s;
        value = v;
    }

    private void Commit()
    {
        var color = ColorMath.FromHsv(hue, saturation, value);
        updating = true;
        try
        {
            Hex = color.ToHex();
        }
        finally
        {
            updating = false;
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        var hueColor = ColorMath.FromHsv(hue, 1, 1);
        HueLayer.Fill = new SolidColorBrush(Color.FromRgb(hueColor.R, hueColor.G, hueColor.B));

        var current = ColorMath.FromHsv(hue, saturation, value);
        Swatch.Background = new SolidColorBrush(Color.FromRgb(current.R, current.G, current.B));
        if (!HexBox.IsKeyboardFocused)
        {
            HexBox.Text = current.ToHex();
        }

        Canvas.SetLeft(SvThumb, saturation * SvArea.ActualWidth - SvThumb.Width / 2);
        Canvas.SetTop(SvThumb, (1 - value) * SvArea.ActualHeight - SvThumb.Height / 2);
        Canvas.SetLeft(HueThumb, hue / 360 * (HueArea.ActualWidth - HueThumb.Width));
        Canvas.SetTop(HueThumb, (HueArea.ActualHeight - HueThumb.Height) / 2);
    }

    private void SetSaturationValue(Point position)
    {
        saturation = Math.Clamp(position.X / Math.Max(1, SvArea.ActualWidth), 0, 1);
        value = Math.Clamp(1 - position.Y / Math.Max(1, SvArea.ActualHeight), 0, 1);
        Commit();
    }

    private void SetHue(Point position)
    {
        var usable = Math.Max(1, HueArea.ActualWidth - HueThumb.Width);
        hue = Math.Clamp((position.X - HueThumb.Width / 2) / usable, 0, 1) * 359.9;
        Commit();
    }

    private void OnSvMouseDown(object sender, MouseButtonEventArgs e)
    {
        SvArea.Focus();
        SvArea.CaptureMouse();
        SetSaturationValue(e.GetPosition(SvArea));
    }

    private void OnSvMouseMove(object sender, MouseEventArgs e)
    {
        if (SvArea.IsMouseCaptured)
        {
            SetSaturationValue(e.GetPosition(SvArea));
        }
    }

    private void OnHueMouseDown(object sender, MouseButtonEventArgs e)
    {
        HueArea.Focus();
        HueArea.CaptureMouse();
        SetHue(e.GetPosition(HueArea));
    }

    private void OnHueMouseMove(object sender, MouseEventArgs e)
    {
        if (HueArea.IsMouseCaptured)
        {
            SetHue(e.GetPosition(HueArea));
        }
    }

    private void OnAreaMouseUp(object sender, MouseButtonEventArgs e) => ((UIElement)sender).ReleaseMouseCapture();

    private void OnSvKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                saturation = Math.Max(0, saturation - KeyboardStep);
                break;
            case Key.Right:
                saturation = Math.Min(1, saturation + KeyboardStep);
                break;
            case Key.Up:
                value = Math.Min(1, value + KeyboardStep);
                break;
            case Key.Down:
                value = Math.Max(0, value - KeyboardStep);
                break;
            default:
                return;
        }

        e.Handled = true;
        Commit();
    }

    private void OnHueKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
            case Key.Down:
                hue = Math.Max(0, hue - HueStep);
                break;
            case Key.Right:
            case Key.Up:
                hue = Math.Min(359.9, hue + HueStep);
                break;
            default:
                return;
        }

        e.Handled = true;
        Commit();
    }

    private void OnHexKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyHexBox();
            HexBox.SelectAll();
        }
    }

    private void OnHexLostFocus(object sender, KeyboardFocusChangedEventArgs e) => ApplyHexBox();

    private void ApplyHexBox()
    {
        if (CrosshairColor.TryParseHex(HexBox.Text, out var color))
        {
            Hex = color.ToHex();
        }
        else
        {
            UpdateVisuals();
        }
    }
}
