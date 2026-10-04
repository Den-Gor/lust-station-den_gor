using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Lust.Preferences;

public static class PortfolioGenerator
{
    /// <summary>
    /// Выбирает строку из пула. Пустая строка, если пула нет или он пуст.
    /// </summary>
    public static string Generate(string poolId, ProtoId<SpeciesPrototype> species)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        if (!protoMan.TryIndex<PortfolioPoolPrototype>(poolId, out var pool))
            return string.Empty;

        var strings = pool.General;
        if (pool.Species.TryGetValue(species.Id, out var racial)
            && racial.Count > 0
            && random.Prob(0.5f))
        {
            strings = racial;
        }

        if (strings.Count == 0)
            return string.Empty;

        return Loc.GetString(random.Pick(strings));
    }

    /// <summary>
    /// Случайное значение из энума.
    /// </summary>
    public static T PickEnum<T>() where T : struct, Enum
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        return random.Pick(Enum.GetValues<T>());
    }
}