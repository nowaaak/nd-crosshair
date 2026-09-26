using NdCrosshair.App.Localization;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal sealed partial class MainViewModel
{
    public IReadOnlyList<string> FontChoices { get; } =
    [
        "Segoe UI",
        "Segoe UI Black",
        "Bahnschrift",
        "Arial",
        "Arial Black",
        "Impact",
        "Consolas",
        "Segoe UI Emoji",
        "Segoe UI Symbol",
    ];

    public bool IsTextLayer => SelectedLayer is TextLayer;

    public string DesignTabShapeLabel => SelectedLayer switch
    {
        TextLayer => Loc.T("TabText"),
        ImageLayer => Loc.T("TabImage"),
        _ => Loc.T("TabShape"),
    };

    public string TextContent
    {
        get => SelectedText?.Text ?? string.Empty;
        set => EditText(text => text with { Text = value ?? string.Empty });
    }

    public string TextFontFamily
    {
        get => SelectedText?.FontFamily ?? TextLayer.DefaultFontFamily;
        set => EditText(text => text with { FontFamily = value });
    }

    public int TextFontSize
    {
        get => SelectedText?.FontSize ?? 0;
        set => EditText(text => text with { FontSize = value });
    }

    public bool TextBold
    {
        get => SelectedText?.Bold ?? false;
        set => EditText(text => text with { Bold = value });
    }

    public int TextRotation
    {
        get => SelectedText?.Rotation ?? 0;
        set => EditText(text => text with { Rotation = value });
    }

    private TextLayer? SelectedText => SelectedLayer as TextLayer;

    public void AddTextLayer() => AddLayer(new TextLayer { OffsetY = 24 });

    private void EditText(Func<TextLayer, TextLayer> update)
    {
        if (SelectedText is { } text)
        {
            EditLayer(update(text));
        }
    }
}
