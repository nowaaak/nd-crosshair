using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using NdCrosshair.App.Services;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal sealed class PresetItem : INotifyPropertyChanged
{
    private string name;
    private CrosshairDesign design;
    private HotkeyBinding hotkey;
    private string hotkeyStatus = string.Empty;
    private ImageSource? thumbnail;

    public PresetItem(string name, CrosshairDesign design, Guid? id = null, HotkeyBinding? hotkey = null)
    {
        this.name = name;
        this.design = design;
        this.hotkey = hotkey ?? HotkeyBinding.None;
        Id = id ?? Guid.NewGuid();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; }

    public string Name
    {
        get => name;
        set
        {
            var limited = value.Length > Preset.MaxNameLength ? value[..Preset.MaxNameLength] : value;
            if (limited == name)
            {
                return;
            }

            name = limited;
            OnPropertyChanged();
        }
    }

    public CrosshairDesign Design
    {
        get => design;
        set
        {
            if (value == design)
            {
                return;
            }

            design = value;
            thumbnail = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Thumbnail));
        }
    }

    public HotkeyBinding Hotkey
    {
        get => hotkey;
        set
        {
            var normalized = (value ?? HotkeyBinding.None).Normalize();
            if (normalized == hotkey)
            {
                return;
            }

            hotkey = normalized;
            OnPropertyChanged();
        }
    }

    public string HotkeyStatus
    {
        get => hotkeyStatus;
        set
        {
            if (value == hotkeyStatus)
            {
                return;
            }

            hotkeyStatus = value;
            OnPropertyChanged();
        }
    }

    public ImageSource Thumbnail => thumbnail ??= CrosshairBitmaps.ToBitmapSource(DesignRenderer.Render(design, WpfLayerContent.Instance));

    public Preset ToPreset() => new(Preset.NormalizeName(Name), Design) { Id = Id, Hotkey = Hotkey };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
