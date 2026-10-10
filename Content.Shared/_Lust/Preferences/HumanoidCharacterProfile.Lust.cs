using Content.Shared.Humanoid;
using Content.Shared._Lust.Preferences;
using Robust.Shared.Serialization;

// Partial к HumanoidCharacterProfile: неймспейс обязан совпадать с основной частью класса,
// поэтому здесь корень Content.Shared.Preferences, а не Content.Shared._Lust.Preferences.
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    [DataField]
    public Erp Erp { get; set; } = Erp.Ask;

    [DataField]
    public Virginity Virginity { get; set; } = Virginity.No;

    [DataField]
    public Virginity AnalVirginity { get; set; } = Virginity.Yes;

    [DataField]
    public bool AgeIsUnknown { get; set; }

    /// <summary>
    /// портфолио персонажа.
    /// </summary>
    [DataField]
    public PortfolioProfile Portfolio { get; set; } = new();

    public HumanoidCharacterProfile WithErp(Erp erp)
    {
        return new(this) { Erp = erp };
    }

    public HumanoidCharacterProfile WithVirginity(Virginity virginity)
    {
        return new(this) { Virginity = virginity };
    }

    public HumanoidCharacterProfile WithAnalVirginity(Virginity analVirginity)
    {
        return new(this) { AnalVirginity = analVirginity };
    }

    public HumanoidCharacterProfile WithAgeIsUnknown(bool ageIsUnknown)
    {
        return new(this) { AgeIsUnknown = ageIsUnknown };
    }

    public HumanoidCharacterProfile WithPortfolio(PortfolioProfile portfolio)
    {
        return new(this) { Portfolio = new(portfolio) };
    }

    private void CopyLustProfile(HumanoidCharacterProfile other)
    {
        Erp = other.Erp;
        Virginity = other.Virginity;
        AnalVirginity = other.AnalVirginity;
        // профиль
        AgeIsUnknown = other.AgeIsUnknown;
        Portfolio = new(other.Portfolio);
    }

    private bool LustProfileEquals(HumanoidCharacterProfile other)
    {
        return Erp == other.Erp &&
               Virginity == other.Virginity &&
               AnalVirginity == other.AnalVirginity &&
                // профиль
                AgeIsUnknown == other.AgeIsUnknown &&
                Portfolio.Equals(other.Portfolio);
    }

    private void AddLustHashCode(ref HashCode hashCode)
    {
        hashCode.Add((int) Erp);
        hashCode.Add((int) Virginity);
        hashCode.Add((int) AnalVirginity);
        // профиль
        hashCode.Add(AgeIsUnknown);
        hashCode.Add(Portfolio);
    }

    // валидация полей портфолио, вызывается из EnsureValid().
    private void EnsureLustProfileValid()
    {
        Portfolio.EnsureValid();
    }
}
