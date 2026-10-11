using Content.Shared._Lust.Preferences;
using Robust.Shared.Serialization;

// Partial к HumanoidCharacterProfile: неймспейс обязан совпадать с основной частью класса,
// поэтому здесь корень Content.Shared.Preferences, а не Content.Shared._Lust.Preferences.
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Портфолио персонажа.
    /// </summary>
    [DataField]
    public PortfolioProfile Portfolio { get; set; } = new();

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
        Portfolio = new(other.Portfolio);
    }

    private bool PortfolioEquals(HumanoidCharacterProfile other)
    {
        return Portfolio.Equals(other.Portfolio);
    }

    private void AddPortfolioHashCode(ref HashCode hashCode)
    {
        hashCode.Add(Portfolio);
    }

    private void EnsurePortfolioValid()
    {
        Portfolio.EnsureValid();
    }
}
