using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Lust.Sheetlets;

[CommonSheetlet]
public sealed class LustPortfolioReadOnlyFieldSheetlet : Sheetlet<PalettedStylesheet>
{
    public const string ReadOnlyFieldStyleClass = "LustReadOnlyPortfolioField";

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<Label>()
                .Class(ReadOnlyFieldStyleClass)
                .FontColor(new Color(192, 192, 192)),
            E<TextEdit>()
                .Class(ReadOnlyFieldStyleClass)
                .Pseudo(TextEdit.StylePseudoClassNotEditable)
                .Prop("font-color", new Color(192, 192, 192)),
        ];
    }
}
