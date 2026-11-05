using Content.Server._Starlight.Medical.Limbs;
using Content.Server.Chat.Systems;
using Content.Server.Starlight.Medical.Surgery;
using Content.Shared._Lust.CyberImplants.Components;
using Content.Shared._Lust.CyberImplants.Events;
using Content.Shared._Lust.CyberImplants.Systems;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Robust.Shared.Containers;

namespace Content.Server._Lust.CyberImplants.Systems;

/// <summary>
/// Серверная система занимается выниманием прошлых органов и дабовлением новых
/// </summary>
public sealed partial class OrganImplanterSystem : EntitySystem
{
    [Dependency] private SharedOrganImplanterSystem _organImplanter = default!;
    [Dependency] private LimbSystem _limb = default!;
    [Dependency] private SurgerySystem _surgery = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private ChatSystem _chat = default!;

    /// <summary>
    /// Обработчик завершения do-after операции экстрактора: извлекает старый орган и вставляет новый.
    /// </summary>
    /// <param name="ent">Имплантер.</param>
    /// <param name="args">Аргументы события do-after.</param>
    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<OrganImplanterComponent> ent, ref OrganImplanterDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { Valid: true } target)
            return;

        //  Если имлпантер уже использовали и он не бесконечный отклонить
        if (SharedOrganImplanterSystem.IsUsed(ent.Comp))
            return;

        // Возможно ли использовать имплантер
        if(!_organImplanter.CanUse(ent, target,  args.User,true))
            return;

        // Проверка еслить орган в иплантере
        if(!_organImplanter.TryGetStoredOrgan(ent.Owner, out var newOrgan))
            return;

        // Получаем контейнер слота имплантера
        if(ent.Comp.Slot.ContainerSlot is not {} slotContainer)
            return;

        // пытаемся извлечь орган и слота имплантера
        if(!_container.Remove(newOrgan,slotContainer))
            return;

        // Пытаемя заменить орган. При неудаче — вернуть орган обратно в слот
        if (!TryReplaceOrgan(target, newOrgan))
        {
            _container.Insert(newOrgan, slotContainer);
            return;
        }

        _chat.TryEmoteWithChat(target, "Scream");

        if (ent.Comp.Damage.DamageDict.Count > 0)
            _damageable.TryChangeDamage(target, ent.Comp.Damage, origin: ent.Owner);

        if (ent.Comp.BleedAmount > 0f)
            _bloodstream.TryModifyBleedAmount(target, ent.Comp.BleedAmount);

        // Пометить экстрактор использованным (если не бесконечный) и заблокировать слот
        if (!ent.Comp.Infinite)
        {
            ent.Comp.Used = true;
            Dirty(ent.Owner, ent.Comp);
            _itemSlots.SetLock(ent.Owner, OrganImplanterComponent.SlotId, true);
        }

    }

    /// <summary>
    /// Очищает прогресс хирургии на органе
    /// </summary>
    private void ClearSurgeryProgress(EntityUid organ)
    {
        if (HasComp<SurgeryProgressComponent>(organ))
            RemComp<SurgeryProgressComponent>(organ);
    }

    /// <summary>
    /// Извлекает орган из тела через хирургию и поднимает SurgeryOrganExtracted.
    /// Используется для не-конечностей.
    /// </summary>
    private bool TryExtractOrgan(EntityUid target, EntityUid organ)
    {
        if (!_surgery.TryRemoveFromBody(target, organ, Transform(target).Coordinates))
            return false;

        var extracted = new SurgeryOrganExtracted(target, organ, organ);
        RaiseLocalEvent(organ, ref extracted);

        return true;
    }

    /// <summary>
    /// Вставляет орган в тело через хирургию и поднимает SurgeryOrganImplantationCompleted.
    /// Используется для не-конечностей
    /// </summary>
    private bool TryInsertOrgan(EntityUid target, EntityUid organ)
    {
        if (!_surgery.TryInsertIntoBody(target, organ))
            return false;

        var implanted = new SurgeryOrganImplantationCompleted(target, organ, organ);
        RaiseLocalEvent(organ, ref implanted);

        return true;
    }

    /// <summary>
    /// Вставляет новый орган в тело выбрасывая  старый
    /// </summary>
    /// <param name="target">тело</param>
    /// <param name="newOrgan">новый орган</param>
    /// <param name="newOrganComp">его компонент</param>
    /// <returns></returns>
    private bool TryReplaceBodyOrgan(EntityUid target, EntityUid newOrgan, OrganComponent newOrganComp)
    {
        //  Получение категории органа и проверяем не нул ли она
        var category = newOrganComp.Category?.Id;
        if (string.IsNullOrEmpty(category))
            return false;

        // Получаем bodyComponent
        if (!TryComp<BodyComponent>(target, out var body))
            return false;

        // Проверяем имет ли орган новые анчёры
        var isLimb = SharedOrganImplanterSystem.IsLimbCategory(category);

        // Ищим и удалем старый орган
        if (_surgery.TryFindBodyOrganByCategory(target, category, out var oldOrgan))
        {
            ClearSurgeryProgress(oldOrgan);

            // пытаемя отрезать канечность
            if (isLimb)
            {
                if (!_limb.Amputate((target, body), oldOrgan))
                    return false;
            }
            // Пытаемся выризать орган
            else
            {
                if (!TryExtractOrgan(target, oldOrgan))
                    return false;
            }
        }

        // Пытаемся вставить новый орган
        if (isLimb)  // пришить конечность
        {
            var slot = SharedOrganImplanterSystem.GetSlotFromCategory(category);
            if (string.IsNullOrEmpty(slot))
                return false;

            return _limb.AttachLimb((target, body), slot, (newOrgan, newOrganComp));
        }

        return TryInsertOrgan(target, newOrgan); // пришить орган
    }

    private bool TryReplaceFunctionalOrgan(EntityUid target, EntityUid newOrgan)
    {
        // Ищим и удаляем орган
        if (_surgery.TryFindBodyEntityWithComponent(target, typeof(FunctionalOrganComponent), null, out var oldOrgan)
            && oldOrgan != EntityUid.Invalid)
        {
            ClearSurgeryProgress(oldOrgan);

            if (!TryExtractOrgan(target, oldOrgan))
                return false;
        }

        // вставляем новый
        return TryInsertOrgan(target, newOrgan);
    }

    /// <summary>
    /// Заменяет орган в теле цели на новый орган.
    /// </summary>
    /// <param name="target">Тело.</param>
    /// <param name="newOrgan">орган для вставки.</param>
    /// <returns>True, если замена прошла успешно.</returns>
    private bool TryReplaceOrgan(EntityUid target, EntityUid newOrgan)
    {
        // Получам копанент органа
        if (!TryComp<OrganComponent>(newOrgan, out var newOrganComp))
            return false;

        // Пропускам функциональные импланты без категории
        if (string.IsNullOrEmpty(newOrganComp.Category?.Id)
            && HasComp<FunctionalOrganComponent>(newOrgan))
        {
            return TryReplaceFunctionalOrgan(target, newOrgan);
        }

        //  Тробуем иплантировать обычные органы
        return TryReplaceBodyOrgan(target, newOrgan, newOrganComp);
    }
}
