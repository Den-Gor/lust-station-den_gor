using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Lust.Implants.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OrganicImmunityComponent : Component
{
    [DataField]
    public float Max = 100f;

    [DataField, AutoNetworkedField]
    public float Current = 100f;

    [DataField]
    public float BaseRegen = 1.0f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSymptomCheck;

    [DataField, AutoNetworkedField,ViewVariables(VVAccess.ReadOnly)]
    public OrganicImmunityTier CurrentTier = OrganicImmunityTier.Healthy;

    [ViewVariables] public float CachedDrain;
    [ViewVariables] public bool DrainDirty = true;

    public float Net => BaseRegen - CachedDrain;
}

public enum OrganicImmunityTier : byte
{
    Healthy, Mild, Moderate, Severe, Critical,
}
