using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Implants.Events;

[Serializable, NetSerializable]
public sealed partial class OrganImplanterDoAfterEvent : SimpleDoAfterEvent;
