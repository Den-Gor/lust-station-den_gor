using System.Linq;
using Content.Shared.Body;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;

namespace Content.Shared._Lust.CyberImplants.Systems;

/// <summary>
/// Система старта do-after события для иплантера органов
/// </summary>
public sealed partial class SharedOrganImplanterSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MarkingManager _markings = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedSurgerySystem _surgery = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Инициализация слота компонента при создании
        SubscribeLocalEvent<Components.OrganImplanterComponent, ComponentInit>(OnComponentInit);

        // Обработка использования предмета в руке (after interact)
        SubscribeLocalEvent<Components.OrganImplanterComponent, AfterInteractEvent>(OnAfterInteract);
    }

    /// <summary>
    /// Маппинг категории органа на слот тела для прикрепления конечностей.
    /// </summary>
    /// <param name="category">ID категории органа.</param>
    /// <returns>Имя слота или null, если категория не является конечностью.</returns>
    public static string? GetSlotFromCategory(string category) => category switch
    {
        "ArmLeft" => "left arm",
        "ArmRight" => "right arm",
        "HandLeft" => "left hand",
        "HandRight" => "right hand",
        "LegLeft" => "left leg",
        "LegRight" => "right leg",
        "FootLeft" => "left foot",
        "FootRight" => "right foot",
        _ => null,
    };

    /// <summary>
    /// Проверяет, является ли категория конечностью (Arm/Hand/Leg/Foot)
    /// </summary>
    public static bool IsLimbCategory(string category)
        => GetSlotFromCategory(category) is not null;

    /// <summary>
    /// Проверяет, использован ли имплантатор (и не бесконечный ли он).
    /// вот эту штуку && !comp.Infinite оставил как защита  от  кривого прототипа
    /// </summary>
    /// <param name="comp">Компонент имплантатор.</param>
    /// <returns>True, если использован и не бесконечный.</returns>
    public static bool IsUsed(Components.OrganImplanterComponent comp) => comp.Used && !comp.Infinite;

    /// <summary>
    /// Попытка найти орган, хранящийся в слоте имплантантор.
    /// </summary>
    /// <param name="uid">имплантор.</param>
    /// <param name="organ">Найденный орган.</param>
    /// <returns>True, если орган найден в слоте.</returns>
    public bool TryGetStoredOrgan(EntityUid uid, out EntityUid organ)
    {
        organ = default;

        // Получаем слот по id слота
        if (!_itemSlots.TryGetSlot(uid, Components.OrganImplanterComponent.SlotId, out var slot))
            return false;

        // Проверяем есть ли предмет в этом слоте
        if (slot.Item is not { } item)
            return false;

        // Является ли придмет органом
        if (!HasComp<OrganComponent>(item))
            return false;

        organ = item;
        return true;
    }
    /// <summary>
    /// Проверка можно ли использовать имплантер органов на цели с заданным органом.
    /// </summary>
    /// <param name="ent">Имплантер.</param>
    /// <param name="target">тело.</param>
    /// <param name="user">Тот кто использует имплантер.</param>
    /// <param name="silent">Если true не показывать pop-up сообщения об ошибках.</param>
    /// <returns>True, если использование разрешено.</returns>
    public bool CanUse(Entity<Components.OrganImplanterComponent> ent, EntityUid target, EntityUid user, bool silent)
    {
        // Проверка есть ли у тела место для органов
        if (!_surgery.TryGetBodyContainer(target, out _))
            return false;

        // Проверка есть ли в экстракторе предмет
        if (!TryGetStoredOrgan(ent.Owner, out var newOrgan))
        {
            if (!silent)
                _popup.PopupEntity(Loc.GetString("lust-organ-extractor-empty"), ent.Owner, user);
            return false;
        }

        // Проверка является ли предмет органом
        if (!TryComp<OrganComponent>(newOrgan, out var organComp))
            return false;

        var category = organComp.Category?.Id;

        // проверка если нет категории у органа но он  функциональный и это спесис тогда колим орган
        if (string.IsNullOrEmpty(category) && HasComp<FunctionalOrganComponent>(newOrgan))
        {
            if (!TryComp<HumanoidProfileComponent>(target, out _))
            {
                if (!silent)
                    _popup.PopupEntity(Loc.GetString("lust-organ-extractor-wrong-type"), ent.Owner, user);
                return false;
            }
            return true;
        }

        //  проверка есть ли котегория у органа
        if (string.IsNullOrEmpty(category))
            return false;

        // Проверка может ли тело принять  эту категорию
        if (!BodySupportsCategory(target, category))
        {
            if (!silent)
                _popup.PopupEntity(Loc.GetString("lust-organ-extractor-wrong-type"), ent.Owner, user);
            return false;
        }

        // Если орган у нас конечность проверяем есть ли у неё анчёр
        if (IsLimbCategory(category) && !CanAttachLimbTo(target, category))
        {
            if (!silent)
                _popup.PopupEntity(Loc.GetString("lust-organ-extractor-no-anchor"), ent.Owner, user);
            return false;
        }
        return true;

    }

    /// <summary>
    /// Публичный API: можно ли начать операцию имплантер на цели.
    /// </summary>
    /// <param name="ent">Имплантер.</param>
    /// <param name="target">Тело.</param>
    /// <param name="user">Пользователь.</param>
    /// <param name="silent">Не показывать попапы при отказе.</param>
    /// <returns>True, если операцию можно начать.</returns>
    public bool CanStartExtraction(Entity<Components.OrganImplanterComponent> ent, EntityUid target, EntityUid user, bool silent)
    {
        if (IsUsed(ent.Comp))
        {
            if (!silent)
                _popup.PopupEntity(Loc.GetString("lust-organ-extractor-used"), ent.Owner, user);
            return false;
        }

        return CanUse(ent, target, user, silent);
    }

    /// <summary>
    /// Публичный API пытается начать операцию имплантер (запускает do-after).
    /// </summary>
    /// <param name="ent">имплантер</param>
    /// <param name="target">тело</param>
    /// <param name="user">пользователь имлпантера </param>
    /// <param name="silent">Не показывать попапы при отказе.</param>
    /// <returns> True, если do-after успешно запущен. </returns>
    public bool TryStartExtraction(Entity<Components.OrganImplanterComponent> ent, EntityUid target, EntityUid user, bool silent)
    {
        if (!CanStartExtraction(ent, target, user, silent))
            return false;

        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            ent.Comp.Duration,
            new Events.OrganImplanterDoAfterEvent(),
            ent.Owner,
            target: target,
            used: ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    /// <summary>
    /// Проверяет, можно ли прикрепить конечность данной категории к цели присутсвует ли у неё родитель (кисть к руке)
    /// </summary>
    /// <param name="target">Тело.</param>
    /// <param name="category">Категория конечностм.</param>
    /// <returns>True, если есть подходящий анкор для категории.</returns>
    private bool CanAttachLimbTo(EntityUid target, string category)
    {
        return GetSlotFromCategory(category) is { } slot
               && SharedSurgerySystem.GetAttachmentAnchorCategory(slot) is { } anchor
               && _surgery.HasBodyOrganCategory(target, anchor);
    }

    /// <summary>
    /// Проверяет, поддерживает ли тело данную категорию органов (по профилю вида и разметке).
    /// </summary>
    /// <param name="target">Тело.</param>
    /// <param name="category">ID категории органа.</param>
    /// <returns>True, если тело поддерживает категорию.</returns>
    private bool BodySupportsCategory(EntityUid target, string category)
    {
        // Получаем хуманоди профиль компонент
        if (!TryComp<HumanoidProfileComponent>(target, out var appearance))
            return false;

        // получаем орган и проверяем его на соответсвие категории
        var organs = _markings.GetOrgans(appearance.Species);
        return organs.Keys.Any(k => k.Id == category);
    }

    /// <summary>
    /// Иницилизация имлантав в имлантер органов при создании
    /// </summary>
    /// <param name="ent">имплантер</param>
    /// <param name="args"></param>
    private void OnComponentInit(Entity<Components.OrganImplanterComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.StartingOrgan != null)
            ent.Comp.Slot.StartingItem = ent.Comp.StartingOrgan;

        _itemSlots.AddItemSlot(ent.Owner, Components.OrganImplanterComponent.SlotId, ent.Comp.Slot);
    }

    /// <summary>
    /// Начинает do-after операцию имплантер при взаимодействии с целью.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnAfterInteract(Entity<Components.OrganImplanterComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { Valid: true } target || !args.CanReach || args.Handled)
            return;

        if (!TryStartExtraction(ent, target, args.User, false))
            return;

        args.Handled = true;
    }
}
