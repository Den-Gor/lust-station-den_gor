using Robust.Shared.Serialization;

namespace Content.Shared._Lust.MedicalRecords;

[Serializable, NetSerializable,  DataRecord]
public sealed partial class MedicalRecord
{
    [DataField]
    public string Notes = string.Empty;
}
