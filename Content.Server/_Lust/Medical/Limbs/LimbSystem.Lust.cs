using Content.Shared.Body;
using Content.Shared.Humanoid;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server._Starlight.Medical.Limbs;

/// <summary>
/// Фикс бага c неккоректным отоброжением руки при опперации если у неё подругому устроен  визуал
/// </summary>
public sealed partial class LimbSystem
{
    private void ApplyVisualOrganData(EntityUid body, HumanoidVisualLayers layer, EntityUid limb)
    {
        if (TryComp<VisualOrganComponent>(limb, out var visualOrgan))
        {
            _sunriseBody.SetBaseLayerData(body, layer, visualOrgan.Data);
        }
    }
}
