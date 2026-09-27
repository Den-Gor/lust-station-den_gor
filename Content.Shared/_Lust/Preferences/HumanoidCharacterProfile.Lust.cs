using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

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

    [DataField]
    public string PortfolioDistinguishingFeatures { get; set; } = string.Empty;

    [DataField]
    public string PortfolioRegion { get; set; } = string.Empty;

    [DataField]
    public string PortfolioPlanetOrColony { get; set; } = string.Empty;

    [DataField]
    public string PortfolioStreetOrBlock { get; set; } = string.Empty;

    [DataField]
    public string PortfolioApartment { get; set; } = string.Empty;

    [DataField]
    public string PortfolioEducation { get; set; } = string.Empty;

    [DataField]
    public string PortfolioMaritalStatus { get; set; } = string.Empty;

    [DataField]
    public string PortfolioCloseRelatives { get; set; } = string.Empty;

    [DataField]
    public string PortfolioEmergencyContact { get; set; } = string.Empty;

    [DataField]
    public string PortfolioPhysiologicalTraits { get; set; } = string.Empty;

    [DataField]
    public string PortfolioPsychologicalTraits { get; set; } = string.Empty;

    [DataField]
    public string PortfolioArrestHistory { get; set; } = string.Empty;

    [DataField]
    public string PortfolioConvictionHistory { get; set; } = string.Empty;

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

    public HumanoidCharacterProfile WithPortfolio(
        string distinguishingFeatures,
        string region,
        string planetOrColony,
        string streetOrBlock,
        string apartment,
        string education,
        string maritalStatus,
        string closeRelatives,
        string emergencyContact,
        string physiologicalTraits,
        string psychologicalTraits,
        string arrestHistory,
        string convictionHistory)
    {
        return new(this)
        {
            PortfolioDistinguishingFeatures = distinguishingFeatures,
            PortfolioRegion = region,
            PortfolioPlanetOrColony = planetOrColony,
            PortfolioStreetOrBlock = streetOrBlock,
            PortfolioApartment = apartment,
            PortfolioEducation = education,
            PortfolioMaritalStatus = maritalStatus,
            PortfolioCloseRelatives = closeRelatives,
            PortfolioEmergencyContact = emergencyContact,
            PortfolioPhysiologicalTraits = physiologicalTraits,
            PortfolioPsychologicalTraits = psychologicalTraits,
            PortfolioArrestHistory = arrestHistory,
            PortfolioConvictionHistory = convictionHistory,
        };
    }

    private void CopyLustProfile(HumanoidCharacterProfile other)
    {
        Erp = other.Erp;
        Virginity = other.Virginity;
        AnalVirginity = other.AnalVirginity;
        // профиль
        AgeIsUnknown = other.AgeIsUnknown;
        PortfolioDistinguishingFeatures = other.PortfolioDistinguishingFeatures;
        PortfolioRegion = other.PortfolioRegion;
        PortfolioPlanetOrColony = other.PortfolioPlanetOrColony;
        PortfolioStreetOrBlock = other.PortfolioStreetOrBlock;
        PortfolioApartment = other.PortfolioApartment;
        PortfolioEducation = other.PortfolioEducation;
        PortfolioMaritalStatus = other.PortfolioMaritalStatus;
        PortfolioCloseRelatives = other.PortfolioCloseRelatives;
        PortfolioEmergencyContact = other.PortfolioEmergencyContact;
        PortfolioPhysiologicalTraits = other.PortfolioPhysiologicalTraits;
        PortfolioPsychologicalTraits = other.PortfolioPsychologicalTraits;
        PortfolioArrestHistory = other.PortfolioArrestHistory;
        PortfolioConvictionHistory = other.PortfolioConvictionHistory;
    }

    private bool LustProfileEquals(HumanoidCharacterProfile other)
    {
        return Erp == other.Erp &&
               Virginity == other.Virginity &&
               AnalVirginity == other.AnalVirginity &&
                // профиль
                AgeIsUnknown == other.AgeIsUnknown &&
                PortfolioDistinguishingFeatures == other.PortfolioDistinguishingFeatures &&
                PortfolioRegion == other.PortfolioRegion &&
                PortfolioPlanetOrColony == other.PortfolioPlanetOrColony &&
                PortfolioStreetOrBlock == other.PortfolioStreetOrBlock &&
                PortfolioApartment == other.PortfolioApartment &&
                PortfolioEducation == other.PortfolioEducation &&
                PortfolioMaritalStatus == other.PortfolioMaritalStatus &&
                PortfolioCloseRelatives == other.PortfolioCloseRelatives &&
                PortfolioEmergencyContact == other.PortfolioEmergencyContact &&
                PortfolioPhysiologicalTraits == other.PortfolioPhysiologicalTraits &&
                PortfolioPsychologicalTraits == other.PortfolioPsychologicalTraits &&
                PortfolioArrestHistory == other.PortfolioArrestHistory &&
                PortfolioConvictionHistory == other.PortfolioConvictionHistory;


    }

    private void AddLustHashCode(ref HashCode hashCode)
    {
        hashCode.Add((int) Erp);
        hashCode.Add((int) Virginity);
        hashCode.Add((int) AnalVirginity);
        // профиль
        hashCode.Add(AgeIsUnknown);
        hashCode.Add(PortfolioDistinguishingFeatures);
        hashCode.Add(PortfolioRegion);
        hashCode.Add(PortfolioPlanetOrColony);
        hashCode.Add(PortfolioStreetOrBlock);
        hashCode.Add(PortfolioApartment);
        hashCode.Add(PortfolioEducation);
        hashCode.Add(PortfolioMaritalStatus);
        hashCode.Add(PortfolioCloseRelatives);
        hashCode.Add(PortfolioEmergencyContact);
        hashCode.Add(PortfolioPhysiologicalTraits);
        hashCode.Add(PortfolioPsychologicalTraits);
        hashCode.Add(PortfolioArrestHistory);
        hashCode.Add(PortfolioConvictionHistory);
    }
}
