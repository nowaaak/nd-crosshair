using System.ComponentModel;
using System.Runtime.CompilerServices;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal sealed class PresetChoice : INotifyPropertyChanged
{
    private string name;

    public PresetChoice(Guid? id, string name)
    {
        Id = id;
        this.name = name;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid? Id { get; }

    public string Name
    {
        get => name;
        set
        {
            if (value != name)
            {
                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }
}

internal sealed class GameRuleItem : INotifyPropertyChanged
{
    private Guid? presetId;

    public GameRuleItem(GameRule rule)
    {
        Kind = rule.Kind;
        Pattern = rule.Pattern;
        presetId = rule.PresetId;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public GameMatchKind Kind { get; }

    public string Pattern { get; }

    public bool IsProcess => Kind == GameMatchKind.Process;

    public Guid? PresetId
    {
        get => presetId;
        set
        {
            if (value != presetId)
            {
                presetId = value;
                OnPropertyChanged();
            }
        }
    }

    public GameRule ToRule() => new(Kind, Pattern) { PresetId = PresetId };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
