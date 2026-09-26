using NdCrosshair.Core;

namespace NdCrosshair.App;

internal sealed partial class MainViewModel
{
    public bool IsShapeLayer => SelectedLayer is ShapeLayer;

    public ShapeKind ShapeKind
    {
        get => SelectedShape?.Kind ?? ShapeKind.Triangle;
        set => EditShape(shape => shape with { Kind = value });
    }

    public int ShapeWidth
    {
        get => SelectedShape?.Width ?? 0;
        set => EditShape(shape => shape with { Width = value });
    }

    public int ShapeHeight
    {
        get => SelectedShape?.Height ?? 0;
        set => EditShape(shape => shape with { Height = value });
    }

    public int ShapeThickness
    {
        get => SelectedShape?.Thickness ?? 0;
        set => EditShape(shape => shape with { Thickness = value });
    }

    public int ShapeSweep
    {
        get => SelectedShape?.Sweep ?? 0;
        set => EditShape(shape => shape with { Sweep = value });
    }

    public int ShapeRotation
    {
        get => SelectedShape?.Rotation ?? 0;
        set => EditShape(shape => shape with { Rotation = value });
    }

    public bool ShapeFilled
    {
        get => SelectedShape?.Filled ?? false;
        set => EditShape(shape => shape with { Filled = value });
    }

    public bool ShapeSupportsFill => SelectedShape?.SupportsFill == true;

    public bool ShapeUsesThickness => SelectedShape is { } shape && !(shape.SupportsFill && shape.Filled);

    public bool ShapeUsesHeight => SelectedShape?.Kind is not (null or ShapeKind.Arc);

    public bool IsArcShape => SelectedShape?.Kind == ShapeKind.Arc;

    private ShapeLayer? SelectedShape => SelectedLayer as ShapeLayer;

    public void AddShapeLayer() => AddLayer(new ShapeLayer());

    private void EditShape(Func<ShapeLayer, ShapeLayer> update)
    {
        if (SelectedShape is { } shape)
        {
            EditLayer(update(shape));
        }
    }
}
