using System.Linq;
using Content.Server.Hands.Systems;
using Content.Server._Lust.MedicalRecords.Components;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared._Lust.Preferences;
using Content.Shared._Lust.MedicalRecords;
using Content.Shared._Sunrise.Helpers;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Preferences;
using Content.Shared.StationRecords;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Lust.MedicalRecords.Systems;

public sealed class LustMedicalRecordsSystem : EntitySystem
{
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
        SubscribeLocalEvent<LustMedicalRecordsConsoleComponent, RecordModifiedEvent>(OnRecordModified);
        SubscribeLocalEvent<LustMedicalRecordsConsoleComponent, AfterInteractUsingEvent>(OnCardUsed);
        SubscribeLocalEvent<LustMedicalRecordsConsoleComponent, GotEmaggedEvent>(OnEmagged);

        Subs.BuiEvents<LustMedicalRecordsConsoleComponent>(LustMedicalRecordsUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOpened);
            subs.Event<LustMedicalRecordsSelectRecordMessage>(OnSelectRecord);
            subs.Event<LustMedicalRecordsSaveMessage>(OnSaveRecord);
            subs.Event<LustMedicalRecordsPrintMessage>(OnPrintRecord);

        });
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        _records.AddRecordEntry(ev.Key, new MedicalRecord());
        _records.Synchronize(ev.Key);
    }

    private void OnSelectRecord(Entity<LustMedicalRecordsConsoleComponent> ent, ref LustMedicalRecordsSelectRecordMessage msg)
    {
        if (!_access.IsAllowed(msg.Actor, ent.Owner))
            return;

        if (_station.GetOwningStation(ent.Owner) is not { } station)
            return;

        if (msg.SelectedKey is { } id
            && !_records.TryGetRecord<GeneralStationRecord>(
                new StationRecordKey(id, station),
                out _))
        {
            return;
        }

        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
    }
    private void OnOpened(EntityUid uid, LustMedicalRecordsConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_access.IsAllowed(args.Actor, uid))
            return;

        UpdateUserInterface(uid, component, component.BiometricsUnlocked);
    }

    private void OnRecordModified(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        ref RecordModifiedEvent args)
    {
        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
    }

    private void OnSaveRecord(Entity<LustMedicalRecordsConsoleComponent> ent, ref LustMedicalRecordsSaveMessage msg)
    {
        if (!_access.IsAllowed(msg.Actor, ent.Owner)
            || ent.Comp.ActiveKey != msg.RecordId
            || !Enum.IsDefined(typeof(Gender), msg.Gender)
            || _station.GetOwningStation(ent.Owner) is not { } station
            || !_prototype.TryIndex<SpeciesPrototype>(msg.Species, out _))
            return;

        var key = new StationRecordKey(msg.RecordId, station);
        if (!_records.TryGetRecord<GeneralStationRecord>(key, out var record) || record is null)
            return;

        _records.TryGetRecord<MedicalRecord>(key, out var medicalRecord);
        medicalRecord ??= new MedicalRecord();

        var portfolio = new PortfolioProfile(record.HumanoidProfile?.Portfolio ?? new PortfolioProfile())
        {
            PhysiologicalTraits = msg.PhysiologicalTraits.SanitizeInput(512),
            PsychologicalTraits = msg.PsychologicalTraits.SanitizeInput(512),
        };

        var profile = (record.HumanoidProfile ?? new HumanoidCharacterProfile()).WithPortfolio(portfolio);
        var updatedRecord = GeneralStationRecord.SanitizeRecord(record with
        {
            Gender = msg.Gender,
            Species = msg.Species,
            HumanoidProfile = profile,
        }, in _prototype);

        var canEditBiometrics = ent.Comp.BiometricsUnlocked;
        if (canEditBiometrics)
        {
            updatedRecord = GeneralStationRecord.SanitizeRecord(updatedRecord with
            {
                Fingerprint = SanitizeBiometricCode(msg.Fingerprint, 32),
                DNA = SanitizeBiometricCode(msg.DNA, 16),
            }, in _prototype);
        }

        medicalRecord.Notes = msg.Notes.SanitizeInput(2048);
        _records.AddRecordEntry(key, updatedRecord);
        _records.AddRecordEntry(key, medicalRecord);
        _records.Synchronize(key);
        _audio.PlayPvs(ent.Comp.SuccessfulSound, ent.Owner);
    }

    private void OnPrintRecord(Entity<LustMedicalRecordsConsoleComponent> ent, ref LustMedicalRecordsPrintMessage msg)
    {
        if (!_access.IsAllowed(msg.Actor, ent.Owner)
            || ent.Comp.ActiveKey != msg.RecordId)
            return;

        if (_timing.CurTime < ent.Comp.NextPrintTime)
        {
            _popup.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), ent.Owner, msg.Actor);
            _audio.PlayPvs(ent.Comp.FailedSound, ent.Owner);
            return;
        }

        if (_station.GetOwningStation(ent.Owner) is not { } station)
            return;

        var key = new StationRecordKey(msg.RecordId, station);
        if (!_records.TryGetRecord<GeneralStationRecord>(key, out var record) || record is null)
            return;

        _records.TryGetRecord<MedicalRecord>(key, out var medicalRecord);

        var printed = Spawn(ent.Comp.Paper, Transform(ent).Coordinates);
        _hands.PickupOrDrop(msg.Actor, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paper))
        {
            _audio.PlayPvs(ent.Comp.FailedSound, ent.Owner);
            return;
        }

        var unrecognized = Loc.GetString("printed-station-records-unrecognized");
        string Printable(string? value) => string.IsNullOrWhiteSpace(value)
            ? unrecognized
            : FormattedMessage.EscapeText(value);

        var speciesName = _prototype.TryIndex<SpeciesPrototype>(record.Species, out var species)
            ? Loc.GetString(species.Name)
            : unrecognized;
        var age = record.Age > 0 && record.HumanoidProfile?.AgeIsUnknown != true
            ? record.Age.ToString()
            : unrecognized;
        var gender = Loc.GetString("station-records-gender", ("gender", record.Gender.ToString()));
        var portfolio = record.HumanoidProfile?.Portfolio;

        var content = Loc.GetString(
            "lust-medical-records-print-content",
            ("name", Printable(record.Name)),
            ("age", age),
            ("gender", gender),
            ("species", Printable(speciesName)),
            ("job", Printable(record.JobTitle)),
            ("fingerprint", Printable(record.Fingerprint)),
            ("dna", Printable(record.DNA)),
            ("closeRelatives", Printable(portfolio?.CloseRelatives)),
            ("emergencyContact", Printable(portfolio?.EmergencyContact)),
            ("physiologicalTraits", Printable(portfolio?.PhysiologicalTraits)),
            ("psychologicalTraits", Printable(portfolio?.PsychologicalTraits)),
            ("notes", Printable(medicalRecord?.Notes)));

        _metaData.SetEntityName(printed, Loc.GetString("lust-medical-records-print-name", ("name", record.Name)));
        _paper.SetContent((printed, paper), content);
        _audio.PlayPvs(ent.Comp.SoundPrint, ent);
        ent.Comp.NextPrintTime = _timing.CurTime + ent.Comp.PrintCooldown;
    }

    private void UpdateUserInterface(EntityUid uid, LustMedicalRecordsConsoleComponent component, bool canEditBiometrics)
    {
        if (_station.GetOwningStation(uid) is not { } station
            || !TryComp<StationRecordsComponent>(station, out var stationRecords))
        {
            _ui.SetUiState(uid, LustMedicalRecordsUiKey.Key, new LustMedicalRecordsUiState());
            return;
        }

        var listing = _records.BuildListing((station, stationRecords), null);

        if (listing.Count == 0)
        {
            _ui.SetUiState(
                uid,
                LustMedicalRecordsUiKey.Key,
                new LustMedicalRecordsUiState(listing, null));
            return;
        }

        if (component.ActiveKey is not { } selectedKey || !listing.ContainsKey(selectedKey))
            component.ActiveKey = listing.Keys.First();

        var recordKey = new StationRecordKey(component.ActiveKey.Value, station);
        _records.TryGetRecord<GeneralStationRecord>(recordKey, out var generalRecord);
        _records.TryGetRecord<MedicalRecord>(recordKey, out var medicalRecord);

        _ui.SetUiState(
            uid,
            LustMedicalRecordsUiKey.Key,
            new LustMedicalRecordsUiState(
                listing,
                component.ActiveKey,
                generalRecord,
                medicalRecord,
                canEditBiometrics));
    }


    private static string SanitizeBiometricCode(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var result = new System.Text.StringBuilder(Math.Min(value.Length, maxLength));
        foreach (var character in value)
        {
            if (result.Length >= maxLength)
                break;

            if (character is >= 'a' and <= 'z')
                result.Append((char) (character - ('a' - 'A')));
            else if (character is >= 'A' and <= 'Z' or >= '0' and <= '9')
                result.Append(character);
        }

        return result.ToString();
    }

    private void OnCardUsed(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        ref AfterInteractUsingEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || !this.IsPowered(ent.Owner, EntityManager)
            || !TryComp<IdCardComponent>(args.Used, out _)
            || !_access.FindAccessTags(args.Used)
                .Contains(new ProtoId<AccessLevelPrototype>("Captain")))
        {
            return;
        }

        ent.Comp.BiometricsUnlocked = !ent.Comp.BiometricsUnlocked;
        _audio.PlayPvs(ent.Comp.CardInsertSound, ent.Owner);
        var message = ent.Comp.BiometricsUnlocked
            ? "lust-medical-records-biometrics-unlocked"
            : "lust-medical-records-biometrics-locked";

        _popup.PopupEntity(Loc.GetString(message), ent.Owner, args.User);

        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
        args.Handled = true;
    }

    private void OnEmagged(
        Entity<LustMedicalRecordsConsoleComponent> ent,
        ref GotEmaggedEvent args)
    {
        if (args.Handled || ent.Comp.BiometricsUnlocked)
            return;

        ent.Comp.BiometricsUnlocked = true;
        UpdateUserInterface(ent.Owner, ent.Comp, ent.Comp.BiometricsUnlocked);
        args.Handled = true;
    }
}
