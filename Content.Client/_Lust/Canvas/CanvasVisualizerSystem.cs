using Content.Shared._Lust.Canvas.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Lust.Canvas;

public sealed class CanvasVisualizerSystem : VisualizerSystem<CanvasComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, CanvasComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = (uid, args.Sprite);
        if (!SpriteSystem.LayerMapTryGet(sprite, CanvasVisualLayers.DrawingIndicator, out var layer, false))
            return;

        var hasDrawing = AppearanceSystem.TryGetData<bool>(uid, CanvasVisuals.HasDrawing, out var visible, args.Component)
                         && visible;
        SpriteSystem.LayerSetVisible(sprite, layer, hasDrawing);
    }
}

public enum CanvasVisualLayers : byte
{
    DrawingIndicator,
}
