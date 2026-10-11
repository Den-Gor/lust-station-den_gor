using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public partial class Profile
{
    public ProfilePortfolio? PortfolioData { get; set; }
}

public class ProfilePortfolio
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;

    [Required, MaxLength(512)]
    public string DistinguishingFeatures { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Region { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string PlanetOrColony { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Address { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string Education { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string WorkExperience { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string MaritalStatus { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string CloseRelatives { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string EmergencyContact { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string PhysiologicalTraits { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string PsychologicalTraits { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string ArrestHistory { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string ConvictionHistory { get; set; } = string.Empty;
}
