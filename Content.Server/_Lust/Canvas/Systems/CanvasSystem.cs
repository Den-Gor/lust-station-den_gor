using System.Buffers.Binary;
using Content.Server._Sunrise;
using Content.Server.Administration.Logs;
using Content.Shared._Lust.Canvas;
using Content.Shared._Lust.Canvas.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Tag;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Canvas.Systems;

/// <summary>
/// Приём рисунков холста от клиентов, их проверка и запись.
/// </summary>
public sealed partial class CanvasSystem : EntitySystem
{
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private NetTexturesManager _netTextures = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private ISawmill _sawmill = default!;

    /// <summary>Минимальный интервал между сохранениями одной сессии</summary>
    private const float CooldownSeconds = 1f;

    /// <summary>Сколько рисунков сервер принял за текущий раунд</summary>
    private const int MaxSavesPerRound = 400;

    /// <summary>Метка кисти для рисования.</summary>
    private static readonly ProtoId<TagPrototype> BrushTag = "LustDraw";

    private readonly Dictionary<ICommonSession, TimeSpan> _lastSave = new();

    /// <summary>
    /// Счётчик рисунков за раунд
    /// </summary>
    private int _savesThisRound;
    private bool _roundCleanupStarted;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = LogManager.GetSawmill("canvas");
        _netManager.RegisterNetMessage<CanvasSaveMessage>(OnSave, accept: NetMessageAccept.Server);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<CanvasComponent, ComponentShutdown>(OnCanvasShutdown);
    }

    /// <summary>
    /// Очистка счёчика при рестарте раунда
    /// </summary>
    /// <param name="ev"></param>
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _savesThisRound = 0;
        _roundCleanupStarted = true;
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        _roundCleanupStarted = false;
    }

    private void OnCanvasShutdown(Entity<CanvasComponent> ent, ref ComponentShutdown args)
    {
        UnregisterImageIfUnused(ent.Comp.ImageId, ent.Owner);
    }

    /// <summary>
    /// Сохранение картинки
    /// </summary>
    /// <param name="msg"></param>
    private void OnSave(CanvasSaveMessage msg)
    {
        if (!_playerManager.TryGetSessionByChannel(msg.MsgChannel, out var session))
            return;

        var now = _timing.CurTime;
        if (_lastSave.TryGetValue(session, out var last) && (now - last).TotalSeconds < CooldownSeconds)
            return;

        if (!msg.IsValid)
        {
            _lastSave[session] = now;
            _sawmill.Warning($"Canvas save rejected from {session.Name}: invalid image payload length");
            return;
        }

        var data = msg.Data;

        if (data.Length > CanvasSaveMessage.MaxDataLength)
        {
            _sawmill.Warning($"Canvas save rejected from {session.Name}: too large ({data.Length} bytes)");
            return;
        }

        if (!HasPngSignature(data))
        {
            _sawmill.Warning($"Canvas save rejected from {session.Name}: not a PNG");
            return;
        }

        // Размеры читаем из заголовка самого файла
        if (!TryReadPngSize(data, out var width, out var height)
            || width != CanvasComponent.Size
            || height != CanvasComponent.Size)
        {
            _sawmill.Warning($"Canvas save rejected from {session.Name}: bad size {width}x{height}");
            return;
        }

        var actor = session.AttachedEntity;
        if (actor is null || !CheckPaperExists(msg.PaperUid, session))
            return;

        var paper = GetEntity(msg.PaperUid);

        if (!CheckPaperIsUnstamped(paper, session))
            return;

        if (!CheckRoundLimit(session))
            return;

        if (!CheckDistance(actor.Value, paper, session))
            return;

        if (!CheckBrushOwned(actor.Value, session))
            return;

        SaveDrawing(paper, actor.Value, session, data);
        _lastSave[session] = now;
    }

    /// <summary>
    /// Отправляем инфу игроку о том что картинка сохранена
    /// </summary>
    private void NotifySaved(EntityUid paper, EntityUid actor)
    {
        _userInterface.CloseUi(paper, CanvasUiKey.Key, actor);
        _userInterface.OpenUi(paper, CanvasUiKey.View, actor);
    }

    /// <summary>
    /// Проверка является ли бумага холстом
    /// </summary>
    private bool CheckPaperExists(NetEntity netPaper, ICommonSession session)
    {
        var paper = GetEntity(netPaper);
        if (paper == EntityUid.Invalid || !HasComp<CanvasComponent>(paper))
        {
            _sawmill.Warning($"Canvas save rejected from {session.Name}: no canvas on paper");
            return false;
        }

        return true;
    }

    private bool CheckPaperIsUnstamped(EntityUid paper, ICommonSession session)
    {
        if (!TryComp<PaperComponent>(paper, out var paperComponent))
        {
            _sawmill.Warning($"Canvas save rejected from {session.Name}: entity is not paper");
            return false;
        }

        if (paperComponent.StampedBy.Count == 0)
            return true;

        _sawmill.Warning($"Canvas save rejected from {session.Name}: paper is stamped");
        return false;
    }

    /// <summary>
    /// Проверка не привышает ли ресунок сумарный лимит
    /// </summary>
    private bool CheckRoundLimit(ICommonSession session)
    {
        if (_savesThisRound >= MaxSavesPerRound)
        {
            _sawmill.Warning($"Canvas save rejected from {session.Name}: round limit {MaxSavesPerRound} reached");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Достаёт ли игрок до бумашки
    /// </summary>
    private bool CheckDistance(EntityUid actor, EntityUid paper, ICommonSession session)
    {
        if (_interaction.InRangeAndAccessible(actor, paper))
            return true;

        _sawmill.Warning($"Canvas save rejected from {session.Name}: too far from {ToPrettyString(paper)}");
        return false;
    }

    /// <summary>
    /// Проверка рук на наличие кисти
    /// </summary>
    private bool CheckBrushOwned(EntityUid actor, ICommonSession session)
    {
        if (HasBrush(actor))
            return true;

        if (TryComp<HandsComponent>(actor, out var hands))
        {
            foreach (var handId in hands.SortedHands)
            {
                var held = _hands.GetHeldItem(actor, handId);
                if (held is { } heldBrush && HasBrush(heldBrush))
                    return true;
            }
        }
        _sawmill.Warning($"Canvas save rejected from {session.Name}: no brush owned by {ToPrettyString(actor)}");
        return false;
    }

    private bool HasBrush(EntityUid uid)
    {
        return _tag.HasTag(uid, BrushTag);
    }

    /// <summary>
    /// Регистрирует текстуру и записывает её в компонент бумаги
    /// </summary>
    private void SaveDrawing(EntityUid paper, EntityUid actor, ICommonSession session, byte[] data)
    {
        var canvas = Comp<CanvasComponent>(paper);
        var previousImageId = canvas.ImageId;

        var imageId = Guid.NewGuid().ToString();
        var path = $"/NetTextures/Canvas/{imageId}.png";
        _netTextures.RegisterDynamicResource(path, data);

        _savesThisRound++;

        canvas.ImageId = imageId;
        canvas.HasDrawing = true;
        canvas.AuthorAccount = session.Name;
        Dirty(paper, canvas);
        _appearance.SetData(paper, CanvasVisuals.HasDrawing, true);

        if (!string.Equals(previousImageId, imageId, StringComparison.Ordinal))
            UnregisterImageIfUnused(previousImageId, paper);

        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(actor):player} created a canvas drawing on {ToPrettyString(paper):entity} (account {session.Name}); resource: {path}; size: {data.Length} bytes.");

        NotifySaved(paper, actor);
    }

    /// <summary>Удаляет PNG, если после удаления или замены холста на него больше никто не ссылается.</summary>
    private void UnregisterImageIfUnused(string? imageId, EntityUid excludedEntity)
    {
        if (_roundCleanupStarted
            || !Guid.TryParseExact(imageId, "D", out _))
            return;

        var query = EntityQueryEnumerator<CanvasComponent>();
        while (query.MoveNext(out var uid, out var canvas))
        {
            if (uid != excludedEntity && string.Equals(canvas.ImageId, imageId, StringComparison.Ordinal))
                return;
        }

        _netTextures.UnregisterDynamicResource($"/NetTextures/Canvas/{imageId}.png");
    }

    /// <summary>
    /// Получение первых 8 байт пнг
    /// </summary>
    private static bool HasPngSignature(byte[] data)
    {
        return data.Length >= 8
               && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
               && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A;
    }

    /// <summary>
    /// Читаемр размер картинкииз байт
    /// </summary>
    private static bool TryReadPngSize(byte[] data, out int width, out int height)
    {
        width = height = 0;

        if (data.Length < 24)
            return false;

        width = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(16, 4));
        height = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(20, 4));
        return true;
    }
}
