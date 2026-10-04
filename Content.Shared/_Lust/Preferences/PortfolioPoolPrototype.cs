using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Preferences;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype()]
public sealed partial class PortfolioPoolPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<string> General = new();

    [DataField]
    public Dictionary<string,List<string>>  Species = new Dictionary<string, List<string>>();


}
