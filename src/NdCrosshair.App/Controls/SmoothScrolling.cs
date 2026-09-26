using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NdCrosshair.App.Controls;

public static class SmoothScrolling
{
    private const double PixelsPerLine = 32;
    private const double DeltaPerNotch = 120;
    private const double Sharpness = 16;
    private const double MaxFrameSeconds = 0.1;
    private const double SnapDistance = 0.5;
    private const double ExternalChangeTolerance = 1;

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(SmoothScrolling), new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State", typeof(ScrollState), typeof(SmoothScrolling));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not ScrollViewer viewer)
        {
            return;
        }

        if (viewer.GetValue(StateProperty) is ScrollState existing)
        {
            existing.Detach();
            viewer.ClearValue(StateProperty);
        }

        if (e.NewValue is true)
        {
            viewer.SetValue(StateProperty, new ScrollState(viewer));
        }
    }

    private static DependencyObject? GetParent(DependencyObject element) =>
        element is Visual or Visual3D ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);

    private sealed class ScrollState
    {
        private readonly ScrollViewer viewer;
        private double target;
        private double expected = double.NaN;
        private TimeSpan lastFrame;
        private bool animating;

        public ScrollState(ScrollViewer viewer)
        {
            this.viewer = viewer;
            viewer.PreviewMouseWheel += OnPreviewMouseWheel;
            viewer.ScrollChanged += OnScrollChanged;
            viewer.Unloaded += OnUnloaded;
        }

        public void Detach()
        {
            Stop();
            viewer.PreviewMouseWheel -= OnPreviewMouseWheel;
            viewer.ScrollChanged -= OnScrollChanged;
            viewer.Unloaded -= OnUnloaded;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled
                || Keyboard.Modifiers != ModifierKeys.None
                || viewer.ScrollableHeight <= 0
                || InnerViewerCanScroll(e.OriginalSource as DependencyObject, e.Delta))
            {
                return;
            }

            e.Handled = true;
            var start = animating ? target : viewer.VerticalOffset;
            target = Math.Clamp(start - e.Delta / DeltaPerNotch * StepSize(), 0, viewer.ScrollableHeight);

            if (!SystemParameters.ClientAreaAnimation)
            {
                ScrollTo(target);
                return;
            }

            if (!animating)
            {
                animating = true;
                lastFrame = TimeSpan.Zero;
                CompositionTarget.Rendering += OnRendering;
            }
        }

        private double StepSize()
        {
            var lines = SystemParameters.WheelScrollLines;
            return lines < 0 ? viewer.ViewportHeight : lines * PixelsPerLine;
        }

        private bool InnerViewerCanScroll(DependencyObject? source, int delta)
        {
            for (var element = source; element is not null && !ReferenceEquals(element, viewer); element = GetParent(element))
            {
                if (element is ScrollViewer inner
                    && inner.ScrollableHeight > 0
                    && (delta > 0 ? inner.VerticalOffset > 0 : inner.VerticalOffset < inner.ScrollableHeight))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            var now = ((RenderingEventArgs)e).RenderingTime;
            if (now == lastFrame)
            {
                return;
            }

            var seconds = lastFrame == TimeSpan.Zero ? 1 / 60.0 : Math.Min((now - lastFrame).TotalSeconds, MaxFrameSeconds);
            lastFrame = now;

            target = Math.Min(target, viewer.ScrollableHeight);
            var current = viewer.VerticalOffset;
            var next = current + (target - current) * (1 - Math.Exp(-Sharpness * seconds));
            if (Math.Abs(target - next) < SnapDistance)
            {
                next = target;
                Stop();
            }

            ScrollTo(next);
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (animating
                && ReferenceEquals(e.OriginalSource, viewer)
                && e.VerticalChange != 0
                && Math.Abs(e.VerticalOffset - expected) > ExternalChangeTolerance)
            {
                Stop();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => Stop();

        private void ScrollTo(double offset)
        {
            expected = offset;
            viewer.ScrollToVerticalOffset(offset);
        }

        private void Stop()
        {
            if (animating)
            {
                animating = false;
                CompositionTarget.Rendering -= OnRendering;
            }
        }
    }
}
