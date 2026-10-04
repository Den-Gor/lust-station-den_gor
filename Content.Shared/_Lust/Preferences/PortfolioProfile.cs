using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Preferences;

/// <summary>
/// Lust-Edit: портфолио персонажа, по аналогии с SunriseCharacterProfile.
/// Хранится объектом внутри HumanoidCharacterProfile, колонки БД остаются те же.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class PortfolioProfile : IEquatable<PortfolioProfile>
{
    /// <summary>
    /// Отличительные признаки (многострочное).
    /// </summary>
    [DataField]
    public string DistinguishingFeatures = string.Empty;

    /// <summary>
    /// Регион (однострочное).
    /// </summary>
    [DataField]
    public string Region = string.Empty;

    /// <summary>
    /// Планета или колония (однострочное).
    /// </summary>
    [DataField]
    public string PlanetOrColony = string.Empty;

    /// <summary>
    /// Адрес (однострочное).
    /// </summary>
    [DataField]
    public string Address = string.Empty;

    /// <summary>
    /// Образование (многострочное).
    /// </summary>
    [DataField]
    public string Education = string.Empty;

    /// <summary>
    /// Трудовой опыт (многострочное).
    /// </summary>
    [DataField]
    public string WorkExperience = string.Empty;

    /// <summary>
    /// Семейное положение (enum-string).
    /// </summary>
    [DataField]
    public string MaritalStatus = string.Empty;

    /// <summary>
    /// Близкие родственники (многострочное).
    /// </summary>
    [DataField]
    public string CloseRelatives = string.Empty;

    /// <summary>
    /// Экстренный контакт (многострочное).
    /// </summary>
    [DataField]
    public string EmergencyContact = string.Empty;

    /// <summary>
    /// Физиологические черты (многострочное).
    /// </summary>
    [DataField]
    public string PhysiologicalTraits = string.Empty;

    /// <summary>
    /// Психологические черты (многострочное).
    /// </summary>
    [DataField]
    public string PsychologicalTraits = string.Empty;

    /// <summary>
    /// История арестов (многострочное).
    /// </summary>
    [DataField]
    public string ArrestHistory = string.Empty;

    /// <summary>
    /// История судимостей (многострочное).
    /// </summary>
    [DataField]
    public string ConvictionHistory = string.Empty;

    public PortfolioProfile()
    {
    }

    public PortfolioProfile(PortfolioProfile other)
    {
        DistinguishingFeatures = other.DistinguishingFeatures;
        Region = other.Region;
        PlanetOrColony = other.PlanetOrColony;
        Address = other.Address;
        Education = other.Education;
        WorkExperience = other.WorkExperience;
        MaritalStatus = other.MaritalStatus;
        CloseRelatives = other.CloseRelatives;
        EmergencyContact = other.EmergencyContact;
        PhysiologicalTraits = other.PhysiologicalTraits;
        PsychologicalTraits = other.PsychologicalTraits;
        ArrestHistory = other.ArrestHistory;
        ConvictionHistory = other.ConvictionHistory;
    }

    /// <summary>
    /// Генерирует портфолио из пулов при первом создании персонажа.
    /// </summary>
    public static PortfolioProfile RandomForSpecies(ProtoId<SpeciesPrototype> species)
    {
        return new PortfolioProfile
        {
            DistinguishingFeatures = PortfolioGenerator.Generate(PortfolioPoolLust.DistinguishingFeatures, species),
            Region = PortfolioGenerator.Generate(PortfolioPoolLust.Region, species),
            PlanetOrColony = PortfolioGenerator.Generate(PortfolioPoolLust.PlanetOrColony, species),
            Address = PortfolioGenerator.Generate(PortfolioPoolLust.Address, species),
            Education = PortfolioGenerator.Generate(PortfolioPoolLust.Education, species),
            WorkExperience = PortfolioGenerator.Generate(PortfolioPoolLust.WorkExperience, species),
            MaritalStatus = PortfolioGenerator.PickEnum<MaritalStatus>().ToString(),
            CloseRelatives = PortfolioGenerator.Generate(PortfolioPoolLust.CloseRelatives, species),
            EmergencyContact = PortfolioGenerator.Generate(PortfolioPoolLust.EmergencyContact, species),
            PhysiologicalTraits = PortfolioGenerator.Generate(PortfolioPoolLust.PhysiologicalTraits, species),
            PsychologicalTraits = PortfolioGenerator.Generate(PortfolioPoolLust.PsychologicalTraits, species),
            ArrestHistory = PortfolioGenerator.Generate(PortfolioPoolLust.ArrestHistory, species),
            ConvictionHistory = PortfolioGenerator.Generate(PortfolioPoolLust.ConvictionHistory, species),
        };
    }

    public bool Equals(PortfolioProfile? other)
    {
        return other is not null &&
               DistinguishingFeatures == other.DistinguishingFeatures &&
               Region == other.Region &&
               PlanetOrColony == other.PlanetOrColony &&
               Address == other.Address &&
               Education == other.Education &&
               WorkExperience == other.WorkExperience &&
               MaritalStatus == other.MaritalStatus &&
               CloseRelatives == other.CloseRelatives &&
               EmergencyContact == other.EmergencyContact &&
               PhysiologicalTraits == other.PhysiologicalTraits &&
               PsychologicalTraits == other.PsychologicalTraits &&
               ArrestHistory == other.ArrestHistory &&
               ConvictionHistory == other.ConvictionHistory;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is PortfolioProfile other && Equals(other);
    }

    public override int GetHashCode()
    {
        var code = HashCode.Combine(
            DistinguishingFeatures,
            Region,
            PlanetOrColony,
            Address,
            Education,
            WorkExperience,
            MaritalStatus,
            CloseRelatives);
        return HashCode.Combine(
            code,
            EmergencyContact,
            PhysiologicalTraits,
            PsychologicalTraits,
            ArrestHistory,
            ConvictionHistory);
    }

    /// <summary>
    /// Санитизирует поля портфолио, вызывается из EnsureValid() профиля.
    /// </summary>
    public void EnsureValid()
    {
        const int maxText = 512;
        const int maxShortText = 128;
        const int maxEnum = 32;

        DistinguishingFeatures = ValidateEntry(DistinguishingFeatures, maxText);
        Education = ValidateEntry(Education, maxText);
        WorkExperience = ValidateEntry(WorkExperience, maxText);
        CloseRelatives = ValidateEntry(CloseRelatives, maxText);
        EmergencyContact = ValidateEntry(EmergencyContact, maxText);
        PhysiologicalTraits = ValidateEntry(PhysiologicalTraits, maxText);
        PsychologicalTraits = ValidateEntry(PsychologicalTraits, maxText);
        ArrestHistory = ValidateEntry(ArrestHistory, maxText);
        ConvictionHistory = ValidateEntry(ConvictionHistory, maxText);

        Region = ValidateEntry(Region, maxShortText);
        PlanetOrColony = ValidateEntry(PlanetOrColony, maxShortText);
        Address = ValidateEntry(Address, maxShortText);

        MaritalStatus = ValidateEntry(MaritalStatus, maxEnum);
    }

    private static string ValidateEntry(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }
}