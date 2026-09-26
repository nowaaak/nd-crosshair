using NdCrosshair.App.Localization;
using NdCrosshair.App.Services;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal sealed partial class MainViewModel
{
    private string imageStatus = string.Empty;

    public bool IsImageLayer => SelectedLayer is ImageLayer;

    public bool HasLayerStyle => SelectedLayer is not ImageLayer;

    public bool ShareOmitsImages => DesignShareCode.OmitsLayers(Design);

    public int ImageWidth
    {
        get => SelectedImage?.Width ?? 0;
        set => EditImage(image => image with { Width = value });
    }

    public int ImageRotation
    {
        get => SelectedImage?.Rotation ?? 0;
        set => EditImage(image => image with { Rotation = value });
    }

    public int ImageOpacity
    {
        get => SelectedImage?.Opacity ?? 0;
        set => EditImage(image => image with { Opacity = value });
    }

    public bool IsImageMissing => SelectedImage is { } image && WpfLayerContent.Instance.RenderImage(image, 1, null) is null;

    public string ImageStatus
    {
        get => imageStatus;
        private set => SetField(ref imageStatus, value);
    }

    private ImageLayer? SelectedImage => SelectedLayer as ImageLayer;

    public void AddImageLayer(string path)
    {
        var result = ImageStore.Import(path);
        ImageStatus = result.Error ?? string.Empty;
        if (result.FileName is { } fileName)
        {
            AddLayer(new ImageLayer { FileName = fileName });
        }
    }

    public void ReplaceImage(string path)
    {
        var result = ImageStore.Import(path);
        ImageStatus = result.Error ?? string.Empty;
        if (result.FileName is { } fileName)
        {
            EditImage(image => image with { FileName = fileName });
        }
    }

    private void EditImage(Func<ImageLayer, ImageLayer> update)
    {
        if (SelectedImage is { } image)
        {
            EditLayer(update(image));
        }
    }
}
