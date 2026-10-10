using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Preferences;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype]
public sealed partial class PortfolioPoolPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Ключи локализации общих вариантов. Используются, если расового списка нет или он не выпал.
    /// </summary>
    [DataField(required: true)]
    public List<string> General = new();

    /// <summary>
    /// Расовые варианты: ключ — ID вида, значение — ключи локализации.
    /// </summary>
    [DataField]
    public Dictionary<string, List<string>> Species = new();
}
