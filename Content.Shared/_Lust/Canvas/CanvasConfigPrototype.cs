using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Canvas;

/// <summary>
/// Настройки баланса и поведения инструментов Canvas.
/// </summary>
[Prototype]
public sealed partial class CanvasConfigPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public float SaveCooldownSeconds { get; private set; } = 1f;

    [DataField]
    public int MaxSavesPerRound { get; private set; } = 400;

    [DataField]
    public int FillTolerance { get; private set; } = 36;

    [DataField]
    public int UndoLimit { get; private set; } = 20;

    [DataField]
    public float BrushCoreFraction { get; private set; } = 0.65f;

    [DataField]
    public int PaletteSlotCount { get; private set; } = 12;

    [DataField]
    public int MinBrushSize { get; private set; } = 2;

    [DataField]
    public int MaxBrushSize { get; private set; } = 32;

    [DataField]
    public int DefaultBrushSize { get; private set; } = 2;
}
