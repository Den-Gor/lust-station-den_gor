using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Roles.Jobs;
using Content.Server.StationRecords.Components;
using Content.Shared._Lust.Preferences;
using Content.Shared._Sunrise.StationRecords;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Paper;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.StationRecords.Systems;

public sealed partial class GeneralStationRecordConsoleSystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private JobSystem _job = default!;
    [Dependency] private PaperSystem _paper = default!;

    private void InitializeSunrise()
    {
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, GotEmaggedEvent>(OnEmagged);

        Subs.BuiEvents<GeneralStationRecordConsoleComponent>(GeneralStationRecordConsoleKey.Key, subs =>
        {
            subs.Event<SaveStationRecord>(OnSave);
            subs.Event<PrintStationRecord>(Print);
        });
    }

    private void OnSave(Entity<GeneralStationRecordConsoleComponent> ent, ref SaveStationRecord args)
    {
        var owning = _station.GetOwningStation(ent.Owner);

        if (owning == null)
        {
            _audio.PlayPvs(ent.Comp.FailedSound, ent);
            return;
        }

        // Дополнительная серверная проверка на случай педиков с читами
        if (!HasAccess(ent, args.Actor))
        {
            _audio.PlayPvs(ent.Comp.FailedSound, ent);
            return;
        }

        // Lust-edit-start
        // Раньше тут был RemoveRecord + AddRecordEntry. RemoveRecord внутри зовёт
        // StationRecordSet.RemoveAllRecords, который чистит ВСЕ таблицы на ключе —
        // вместе с общей записью стирался CriminalRecordStatus с историей нарушений.
        // Плюс AddRecordEntry выдавал новый id, из-за чего StationRecordKeyStorageComponent
        // на ID-картах оставался со старым ключом. Теперь обновляем запись по месту.
        var key = new StationRecordKey(args.Id, owning.Value);

        // Проверку существования раньше неявно делал RemoveRecord, возвращая false.
        // Без неё AddRecordEntry(key, ...) создал бы запись с произвольным id от клиента.
        if (!_stationRecords.TryGetRecord<GeneralStationRecord>(key, out _))
        {
            _audio.PlayPvs(ent.Comp.FailedSound, ent);
            return;
        }

        var record = GeneralStationRecord.SanitizeRecord(args.Record, in _prototype);
        _stationRecords.AddRecordEntry(key, record);
        ent.Comp.ActiveKey = key.Id;
        // Lust-edit-end

        var message = Loc.GetString("station-record-updated", ("name", args.Record.Name));
        var popup = Loc.GetString("station-record-updated-successfully");

        DoFeedback(ent, message, popup);

        // Lust-edit-start
        // Synchronize поднимает RecordModifiedEvent, на который подписаны манифест экипажа,
        // кримконсоли и сам станучёт. Раньше событие не поднималось вовсе
        // из-за чего манифест и кримконсоль показывали устаревшие данные.
        _stationRecords.Synchronize(key);
        // Lust-edit-end
    }

    private void OnEmagged(Entity<GeneralStationRecordConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (ent.Comp.CanRedactSensitiveData
            && ent.Comp.CanDeleteEntries
            && ent.Comp.Silent
            && ent.Comp.SkipAccessCheck)
            return;

        if (args.Handled)
            return;

        ent.Comp.CanDeleteEntries = true;
        ent.Comp.CanRedactSensitiveData = true;
        ent.Comp.Silent = true;
        ent.Comp.SkipAccessCheck = true;

        UpdateUserInterface(ent);
        args.Handled = true;
    }

    private void Print(Entity<GeneralStationRecordConsoleComponent> ent, ref PrintStationRecord args)
    {
        var user = args.Actor;

        if (_timing.CurTime < ent.Comp.NextPrintTime)
        {
            _popup.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), ent, user);
            _audio.PlayPvs(ent.Comp.FailedSound, ent);
            return;
        }

        var owning = _station.GetOwningStation(ent.Owner);

        if (owning == null)
        {
            _audio.PlayPvs(ent.Comp.FailedSound, ent);
            return;
        }

        if (!_stationRecords.TryGetRecord<GeneralStationRecord>(new StationRecordKey(args.Id, owning.Value), out var record))
        {
            _audio.PlayPvs(ent.Comp.FailedSound, ent);
            return;
        }

        // Spawn a piece of paper.
        var printed = Spawn(ent.Comp.Paper, Transform(ent).Coordinates);
        _hands.PickupOrDrop(args.Actor, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
        {
            _audio.PlayPvs(ent.Comp.FailedSound, ent);
            return;
        }

        var documentName = Loc.GetString("printed-station-records-document-name", ("name", record.Name));
        _metaData.SetEntityName(printed, documentName);
        // Lust-edit-start
        var portfolio = record.HumanoidProfile?.Portfolio;
        var missing = Loc.GetString("printed-station-records-unrecognized");

        string PrintableField(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? missing
                : FormattedMessage.EscapeText(value);
        }

        var maritalStatus = Enum.TryParse<MaritalStatus>(
            portfolio?.MaritalStatus,
            true,
            out var status)
            ? Loc.GetString(
                $"humanoid-profile-editor-portfolio-marital-{status.ToString().ToLowerInvariant()}-text")
            : missing;

        var age = record.Age > 0
            ? record.Age.ToString()
            : missing;
        // Lust-edit-end

        var text = Loc.GetString(
            "printed-station-records-content",
            ("name", record.Name),
            ("job", GetJobName(record.JobPrototype)),
            ("department", GetDepartmentName(record.JobPrototype)),
            ("age", age), // Lust-edit
            ("gender", GetGenderName(record.Gender)),
            ("species", GetSpeciesName(record.Species)),
            ("dna", record.DNA ?? Loc.GetString("printed-station-records-unrecognized")),
            ("fingerprint", record.Fingerprint ?? Loc.GetString("printed-station-records-unrecognized")),
            ("personality", GetPersonality(record.Personality)),
            // Lust-edit-start
            ("distinguishingFeatures", PrintableField(portfolio?.DistinguishingFeatures)),
            ("education", PrintableField(portfolio?.Education)),
            ("workExperience", PrintableField(portfolio?.WorkExperience)),
            ("region", PrintableField(portfolio?.Region)),
            ("planetOrColony", PrintableField(portfolio?.PlanetOrColony)),
            ("address", PrintableField(portfolio?.Address)),
            ("maritalStatus", maritalStatus),
            ("closeRelatives", PrintableField(portfolio?.CloseRelatives)),
            ("emergencyContact", PrintableField(portfolio?.EmergencyContact))
            // Lust-edit-end
        );

        _paper.SetContent((printed, paperComp), text);
        _audio.PlayPvs(ent.Comp.SoundPrint, ent,
            AudioParams.Default
            .WithVariation(0.25f)
            .WithVolume(4f)
            .WithRolloffFactor(2.8f)
            .WithMaxDistance(4.5f));

        ent.Comp.NextPrintTime = _timing.CurTime + ent.Comp.PrintCooldown;
    }

    private void DoFeedback(Entity<GeneralStationRecordConsoleComponent> ent, string message, string popup)
    {
        _popup.PopupEntity(popup, ent);

        if (ent.Comp.Silent)
            return;

        // Sunrise-Start
        var server = _messenger.GetServerEntity(_station.GetOwningStation(ent));

        foreach (var channel in ent.Comp.AnnouncementChannels)
        {
            //_radio.SendRadioMessage(ent, message, channel, ent);
            if (_messenger.GetGroupIdByRadioChannel(channel) is { } groupId && server != null)
            {
                _messenger.SendSystemMessageToGroup(server.Value.Item1, groupId, message);
            }
        }
        // Sunrise-End

        _audio.PlayPvs(ent.Comp.SuccessfulSound, ent);
    }

    private void OnOpened(Entity<GeneralStationRecordConsoleComponent> ent, ref BoundUIOpenedEvent msg)
    {
        ent.Comp.HasAccess = HasAccess(ent, msg.Actor);
        UpdateUserInterface(ent);
    }

    /// <summary>
    /// Проверяет наличие у персонажа доступа к консоли.
    /// </summary>
    private bool HasAccess(Entity<GeneralStationRecordConsoleComponent> ent, EntityUid actor)
    {
        var allowed = _access.IsAllowed(actor, ent);
        return allowed || ent.Comp.SkipAccessCheck;
    }

    #region Helpers

    private string GetJobName(ProtoId<JobPrototype> job)
    {
        if (!_prototype.TryIndex(job, out var jobPrototype))
            return Loc.GetString("printed-station-records-unrecognized");

        return jobPrototype.LocalizedName;
    }

    private string GetDepartmentName(ProtoId<JobPrototype> job)
    {
        if (!_job.TryGetDepartment(job, out var department))
            return Loc.GetString("printed-station-records-unrecognized");

        return Loc.GetString(department.Name);
    }

    private string GetGenderName(Gender gender)
    {
        return Loc.GetString("station-records-gender", ("gender", gender.ToString()));
    }

    private string GetSpeciesName(ProtoId<SpeciesPrototype> species)
    {
        if (!_prototype.TryIndex(species, out var speciesPrototype))
            return Loc.GetString("printed-station-records-unrecognized");

        return Loc.GetString(speciesPrototype.Name);
    }

    private string GetPersonality(string personality)
    {
        if (string.IsNullOrEmpty(personality))
            return Loc.GetString("printed-station-records-unrecognized");

        return personality;
    }

    #endregion
}
