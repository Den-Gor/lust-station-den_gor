using Content.Server.Database;
using Content.Shared._Lust.Preferences;
using Content.Shared.Preferences;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Preferences.Managers;

public sealed partial class ServerPreferencesManager
{
    private static HumanoidCharacterProfile ApplyLustPortfolioData(
        HumanoidCharacterProfile humanoid,
        Profile profile)
    {
        if (profile.PortfolioData is not { } portfolioData)
            return humanoid.WithAgeIsUnknown(profile.AgeIsUnknown);

        var portfolio = new PortfolioProfile
        {
            DistinguishingFeatures = portfolioData.DistinguishingFeatures,
            Region = portfolioData.Region,
            PlanetOrColony = portfolioData.PlanetOrColony,
            Address = portfolioData.Address,
            Education = portfolioData.Education,
            WorkExperience = portfolioData.WorkExperience,
            MaritalStatus = portfolioData.MaritalStatus,
            CloseRelatives = portfolioData.CloseRelatives,
            EmergencyContact = portfolioData.EmergencyContact,
            PhysiologicalTraits = portfolioData.PhysiologicalTraits,
            PsychologicalTraits = portfolioData.PsychologicalTraits,
            ArrestHistory = portfolioData.ArrestHistory,
            ConvictionHistory = portfolioData.ConvictionHistory,
        };

        return humanoid
            .WithAgeIsUnknown(profile.AgeIsUnknown)
            .WithPortfolio(portfolio);
    }
}
