using Content.Shared._Lust.Preferences;
using Robust.Shared.Serialization;

// Partial к HumanoidCharacterProfile: неймспейс обязан совпадать с основной частью класса,
// поэтому здесь корень Content.Shared.Preferences, а не Content.Shared._Lust.Preferences.
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Возраст персонажа скрыт и отображается как неизвестный в консолях и записях станции.
    /// </summary>
    [DataField]
    public bool AgeIsUnknown { get; set; }

    /// <summary>
    /// Портфолио персонажа.
    /// </summary>
    [DataField]
    public PortfolioProfile Portfolio { get; set; } = new();

    /// <summary>
    /// Возвращает копию профиля со скрытым или открытым возрастом.
    /// </summary>
    /// <param name="ageIsUnknown">True — возраст неизвестен.</param>
    /// <returns>Новый профиль, исходный не меняется.</returns>
    public HumanoidCharacterProfile WithAgeIsUnknown(bool ageIsUnknown)
    {
        return new(this) { AgeIsUnknown = ageIsUnknown };
    }

    /// <summary>
    /// Возвращает копию профиля с заменённым портфолио. Портфолио копируется, а не разделяется.
    /// </summary>
    /// <param name="portfolio">Новое портфолио.</param>
    /// <returns>Новый профиль, исходный не меняется.</returns>
    public HumanoidCharacterProfile WithPortfolio(PortfolioProfile portfolio)
    {
        return new(this) { Portfolio = new(portfolio) };
    }

    private void CopyPortfolio(HumanoidCharacterProfile other)
    {
        AgeIsUnknown = other.AgeIsUnknown;
        Portfolio = new(other.Portfolio);
    }

    private bool PortfolioEquals(HumanoidCharacterProfile other)
    {
        return AgeIsUnknown == other.AgeIsUnknown &&
               Portfolio.Equals(other.Portfolio);
    }

    private void AddPortfolioHashCode(ref HashCode hashCode)
    {
        hashCode.Add(AgeIsUnknown);
        hashCode.Add(Portfolio);
    }

    private void EnsurePortfolioValid()
    {
        Portfolio.EnsureValid();
    }
}
