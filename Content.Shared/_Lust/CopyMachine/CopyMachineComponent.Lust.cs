using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130
namespace Content.Shared._Sunrise.CopyMachine;

public sealed partial class CopyMachineComponent
{

    /// <summary>
    /// Шанс поломки принтера
    /// </summary>
    [DataField]
    public float JamChance = 0.07f;

    /// <summary>
    /// Флаг поломон ли принтер датефилд оставил для маперов
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsJammed;

    /// <summary>
    /// Шанс на расклинивание
    /// </summary>
    [DataField]
    public float UnjamChance = 0.2f;
    /// <summary>
    /// Предмет который будет выдовать принтер если заклинет
    /// </summary>
    [DataField]
    public EntProtoId JammedPaperProtoId = "PaperScrap";

    /// <summary>
    /// Звук заклинивания принтера
    /// </summary>
    [DataField]
    public SoundSpecifier JamSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    /// <summary>
    /// Звук расклинивания принтера
    /// </summary>
    [DataField]
    public SoundSpecifier UnjamSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");

}
