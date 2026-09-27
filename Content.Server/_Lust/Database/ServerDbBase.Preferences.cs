using Content.Shared.Preferences;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    private static void StoreLustProfileData(Profile profile, HumanoidCharacterProfile humanoid)
    {
        profile.ErpData ??= new ProfileErp
        {
            ProfileId = profile.Id,
            Profile = profile,
        };

        profile.ErpData.Erp = humanoid.Erp.ToString();
        profile.ErpData.Virginity = humanoid.Virginity.ToString();
        profile.ErpData.AnalVirginity = humanoid.AnalVirginity.ToString();
    }

    private static void StoreLustPortfolioData(Profile profile, HumanoidCharacterProfile humanoid)
    {
        profile.PortfolioData ??= new ProfilePortfolio
        {
            ProfileId = profile.Id,
            Profile = profile,
        };

        profile.AgeIsUnknown = humanoid.AgeIsUnknown;

        profile.PortfolioData.DistinguishingFeatures = humanoid.PortfolioDistinguishingFeatures;
        profile.PortfolioData.Region = humanoid.PortfolioRegion;
        profile.PortfolioData.PlanetOrColony = humanoid.PortfolioPlanetOrColony;
        profile.PortfolioData.StreetOrBlock = humanoid.PortfolioStreetOrBlock;
        profile.PortfolioData.Apartment = humanoid.PortfolioApartment;
        profile.PortfolioData.Education = humanoid.PortfolioEducation;
        profile.PortfolioData.MaritalStatus = humanoid.PortfolioMaritalStatus;
        profile.PortfolioData.CloseRelatives = humanoid.PortfolioCloseRelatives;
        profile.PortfolioData.EmergencyContact = humanoid.PortfolioEmergencyContact;
        profile.PortfolioData.PhysiologicalTraits = humanoid.PortfolioPhysiologicalTraits;
        profile.PortfolioData.PsychologicalTraits = humanoid.PortfolioPsychologicalTraits;
        profile.PortfolioData.ArrestHistory = humanoid.PortfolioArrestHistory;
        profile.PortfolioData.ConvictionHistory = humanoid.PortfolioConvictionHistory;
    }
}
