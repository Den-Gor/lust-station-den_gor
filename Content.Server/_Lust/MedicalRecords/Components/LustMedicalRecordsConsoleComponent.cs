using Content.Server._Lust.MedicalRecords.Systems;
using Content.Shared.Access;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.MedicalRecords.Components;

/// <summary>
/// Хранит выбранную запись в медицинской консоли.
/// </summary>
[RegisterComponent, Access(typeof(LustMedicalRecordsSystem))]
public sealed partial class LustMedicalRecordsConsoleComponent : Component
{
    /// <summary>
    /// ID выбранной записи сотрудника.
    /// </summary>
    [DataField]
    public uint? ActiveKey;

    /// <summary>
    /// Разрешено ли редактирование ДНК и отпечатков на этой консоли.
    /// </summary>
    [DataField]
    public bool BiometricsUnlocked;

    /// <summary>
    /// Доступ, открывающий редактирование биометрии. Настраивается на прототипе консоли.
    /// </summary>
    [DataField]
    public ProtoId<AccessLevelPrototype> BiometricsAccess = "Captain";

    /// <summary>
    /// Время, когда консоль снова сможет печатать карту.
    /// </summary>
    public TimeSpan NextPrintTime;

    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public EntProtoId Paper = "Paper";

    [DataField]
    public SoundSpecifier FailedSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField]
    public SoundSpecifier SuccessfulSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField]
    public SoundSpecifier CardInsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/batrifle_magin.ogg");

    [DataField]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");
}
