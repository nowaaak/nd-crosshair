using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using NdCrosshair.App.Localization;
using NdCrosshair.Core;

namespace NdCrosshair.App.Controls;

public partial class HotkeyBox : UserControl
{
    public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
        nameof(Hotkey), typeof(HotkeyBinding), typeof(HotkeyBox),
        new FrameworkPropertyMetadata(HotkeyBinding.None, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDisplayChanged));

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(string), typeof(HotkeyBox), new PropertyMetadata(string.Empty, OnDisplayChanged));

    public static readonly DependencyProperty IsCapturingProperty = DependencyProperty.Register(
        nameof(IsCapturing), typeof(bool), typeof(HotkeyBox),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(HotkeyBox), new PropertyMetadata(string.Empty, OnDisplayChanged));

    private bool focused;

    public HotkeyBox()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Loc.Instance.LanguageChanged += OnLanguageChanged;
            UpdateDisplay();
        };
        Unloaded += (_, _) => Loc.Instance.LanguageChanged -= OnLanguageChanged;
    }

    public HotkeyBinding Hotkey
    {
        get => (HotkeyBinding)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public bool IsCapturing
    {
        get => (bool)GetValue(IsCapturingProperty);
        set => SetValue(IsCapturingProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    private static void OnDisplayChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) =>
        ((HotkeyBox)target).UpdateDisplay();

    private void OnLanguageChanged(object? sender, EventArgs e) => UpdateDisplay();

    private void UpdateDisplay()
    {
        KeyBox.Text = focused ? Loc.T("HotkeyPressKey") : HotkeyFormatter.Format(Hotkey);
        AutomationProperties.SetName(KeyBox, Label);
        StatusText.Text = Status;
        StatusText.Visibility = string.IsNullOrEmpty(Status) || focused ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnClear(object sender, RoutedEventArgs e) => Hotkey = HotkeyBinding.None;

    private void OnGotFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        focused = true;
        IsCapturing = true;
        UpdateDisplay();
    }

    private void OnLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        focused = false;
        IsCapturing = false;
        UpdateDisplay();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var modifiers = Keyboard.Modifiers;

        if (key == Key.Tab && modifiers is ModifierKeys.None or ModifierKeys.Shift)
        {
            return;
        }

        e.Handled = true;

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return;
        }

        if (key == Key.Escape)
        {
            Keyboard.ClearFocus();
            return;
        }

        var flags = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            flags |= HotkeyBinding.ModifierAlt;
        }

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            flags |= HotkeyBinding.ModifierControl;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            flags |= HotkeyBinding.ModifierShift;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            flags |= HotkeyBinding.ModifierWin;
        }

        Hotkey = new HotkeyBinding(flags, KeyInterop.VirtualKeyFromKey(key));
        Keyboard.ClearFocus();
    }
}
