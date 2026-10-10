using Content.Client._Lust.Preference;
using Content.Shared.Preferences;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private PortfolioEditor? _portfolio;

    private void UpdatePortfolioControls()
    {
        // Вкладка портфолио создаётся здесь, до проверки Profile,
        // чтобы она не зависела ни от наличия описания, ни от ic.flavor_text.
        EnsurePortfolioTab();

        if (Profile is null)
            return;

        _portfolio?.SetProfile(Profile);
    }

    /// <summary>
    /// Создаёт собственную вкладку портфолио.
    /// Не зависит от вкладки описания и от CVar ic.flavor_text, вызывается повторно безопасно.
    /// </summary>
    private void EnsurePortfolioTab()
    {
        if (_portfolio is not null)
            return;

        _portfolio = new PortfolioEditor();
        _portfolio.OnProfileChanged += OnPortfolioChange;
        TabContainer.AddChild(_portfolio);
        TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-portfolio-tab"));
    }

    private void OnPortfolioChange(HumanoidCharacterProfile profile)
    {
        Profile = profile;
        SetDirty();
    }
}
