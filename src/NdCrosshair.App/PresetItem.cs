using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal sealed class PresetItem : INotifyPropertyChanged
{
    private string name;
    private CrosshairSettings settings;
    private ImageSource? thumbnail;

    public PresetItem(string name, CrosshairSettings settings)
    {
        this.name = name;
        this.settings = settings;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public CrosshairSettings Settings
    {
        get => settings;
        set
        {
            if (value == settings)
            {
                return;
            }

            settings = value;
            thumbnail = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Thumbnail));
        }
    }

    public ImageSource Thumbnail => thumbnail ??= CrosshairBitmaps.ToBitmapSource(CrosshairRenderer.Render(settings));

    public Preset ToPreset() => new(Preset.NormalizeName(Name), Settings);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
