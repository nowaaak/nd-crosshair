using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using NdCrosshair.Core;
using NdCrosshair.Core.Localization;

namespace NdCrosshair.App.Localization;

internal sealed class Loc : INotifyPropertyChanged
{
    private IReadOnlyDictionary<string, string> strings = UiStrings.German;

    private Loc()
    {
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? LanguageChanged;

    public static Loc Instance { get; } = new();

    public AppLanguage EffectiveLanguage { get; private set; } = AppLanguage.German;

    public string this[string key] => strings.TryGetValue(key, out var value) ? value : key;

    public static string T(string key) => Instance[key];

    public static string Format(string key, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Instance[key], arguments);

    public void SetLanguage(AppLanguage language)
    {
        var effective = language == AppLanguage.System ? DetectSystemLanguage() : language;
        var changed = effective != EffectiveLanguage;
        EffectiveLanguage = effective;
        strings = effective == AppLanguage.English ? UiStrings.English : UiStrings.German;

        if (changed)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static AppLanguage DetectSystemLanguage() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? AppLanguage.German : AppLanguage.English;
}

[MarkupExtensionReturnType(typeof(string))]
public sealed class TExtension : MarkupExtension
{
    public TExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = Loc.Instance,
            Mode = BindingMode.OneWay,
        };
        return binding.ProvideValue(serviceProvider);
    }
}
