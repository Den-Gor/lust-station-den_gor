using Content.Shared._Sunrise.CopyMachine;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

#pragma warning disable IDE0130
namespace Content.Server._Sunrise.CopyMachine;

public sealed partial class CopyMachineSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private void OnAttacked(Entity<CopyMachineComponent> ent, ref AttackedEvent args)
    {
        TryUnjam(ent, ref args);
    }

    /// <summary>
    /// Функция проверки сломан ли принтер
    /// </summary>
    /// <param name="ent"></param>
    /// <returns></returns>
    public bool CanUnjam(Entity<CopyMachineComponent> ent)
    {
        if (!ent.Comp.IsJammed)
            return false;

        return true;
    }

    /// <summary>
    /// Функция вызываемая для попытки починки принтера
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public bool TryUnjam(Entity<CopyMachineComponent> ent, ref AttackedEvent args)
    {
        if (!CanUnjam(ent))
            return false;

        if (!_random.Prob(ent.Comp.UnjamChance))
            return false;

        ent.Comp.IsJammed = false;
        Dirty(ent);
        QueueUIUpdate(ent);
        UpdateJamAppearance(ent, false);
        _popup.PopupEntity(Loc.GetString("copy-machine-lust-unjammed"), ent, args.User, PopupType.Medium);
        _audioSystem.PlayPvs(ent.Comp.UnjamSound, ent);
        return true;
    }

    /// <summary>
    /// Перенесение функции и её почти полное изменение из санрайза в ласт
    /// Функция занимается созданием напечатаного предмета
    /// </summary>
    /// <param name="ent"></param>
    private void CompleteCurrentJob(Entity<CopyMachineComponent> ent)
    {
        var jobView = ent.Comp.CurrentJobView;
        if (jobView != null)
        {
            if (ent.Comp.IsJammed)
            {
                Spawn(ent.Comp.JammedPaperProtoId, Transform(ent).Coordinates);
            }
            else if (_random.Prob(ent.Comp.JamChance))
            {
                // Заклинило прямо во время этого задания — лист испорчен, выдаём макулатуру вместо документа.
                ent.Comp.IsJammed = true;
                Dirty(ent);
                QueueUIUpdate(ent);
                UpdateJamAppearance(ent, true);
                _popup.PopupEntity(Loc.GetString("copy-machine-lust-jammed"), ent, PopupType.Medium);
                _audioSystem.PlayPvs(ent.Comp.JamSound, ent);
                Spawn(ent.Comp.JammedPaperProtoId, Transform(ent).Coordinates);
            }
            else
            {
                var jobType = jobView.Type;
                var templateId = jobView.TemplateId;
                _ = jobType switch
                {
                    CopyMachineJobType.Print when templateId != null => TryPrintFromTemplate(ent, templateId),
                    CopyMachineJobType.Copy => TryCopyFromSlotOrButtScan(ent),
                    _ => false
                };
            }
        }

        ent.Comp.IsProcessing = false;
        ent.Comp.CurrentJobView = null;
        ent.Comp.NextPrintTime = TimeSpan.Zero;
        UpdateRunningAppearance(ent, false);

        QueueUIUpdate(ent);
    }

    private void UpdateJamAppearance(EntityUid uid, bool isJammed)
    {
        _appearance.SetData(uid, CopyMachineVisuals.IsJammed, isJammed);
    }
}
