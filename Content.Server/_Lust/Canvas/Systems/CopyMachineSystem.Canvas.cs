using Content.Server._Sunrise.CopyMachine;
using Content.Shared._Lust.Canvas.Components;

#pragma warning disable IDE0130 // Пространство имён соответствует исходной системе копировального аппарата.
namespace Content.Server._Sunrise.CopyMachine;

public sealed partial class CopyMachineSystem
{
    private void CopyCanvasDrawing(EntityUid sourcePaper, EntityUid copiedPaper)
    {
        if (!TryComp<CanvasComponent>(sourcePaper, out var sourceCanvas)
            || !sourceCanvas.HasDrawing
            || !Guid.TryParseExact(sourceCanvas.ImageId, "D", out _))
        {
            return;
        }

        var copiedCanvas = EnsureComp<CanvasComponent>(copiedPaper);
        copiedCanvas.ImageId = sourceCanvas.ImageId;
        copiedCanvas.HasDrawing = true;
        copiedCanvas.AuthorAccount = sourceCanvas.AuthorAccount;
        Dirty(copiedPaper, copiedCanvas);
        _appearance.SetData(copiedPaper, CanvasVisuals.HasDrawing, true);
    }
}
