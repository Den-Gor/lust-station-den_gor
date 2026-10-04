using Content.Shared._Lust.Preferences;
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

        var portfolio = humanoid.Portfolio;
        profile.PortfolioData.DistinguishingFeatures = portfolio.DistinguishingFeatures;
        profile.PortfolioData.Region = portfolio.Region;
        profile.PortfolioData.PlanetOrColony = portfolio.PlanetOrColony;
        profile.PortfolioData.Address = portfolio.Address;
        profile.PortfolioData.Education = portfolio.Education;
        profile.PortfolioData.WorkExperience = portfolio.WorkExperience;
        profile.PortfolioData.MaritalStatus = portfolio.MaritalStatus;
        profile.PortfolioData.CloseRelatives = portfolio.CloseRelatives;
        profile.PortfolioData.EmergencyContact = portfolio.EmergencyContact;
        profile.PortfolioData.PhysiologicalTraits = portfolio.PhysiologicalTraits;
        profile.PortfolioData.PsychologicalTraits = portfolio.PsychologicalTraits;
        profile.PortfolioData.ArrestHistory = portfolio.ArrestHistory;
        profile.PortfolioData.ConvictionHistory = portfolio.ConvictionHistory;
    }
}
