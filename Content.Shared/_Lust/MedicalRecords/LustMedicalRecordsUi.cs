using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.MedicalRecords;

/// <summary>
/// Ключ интерфейса медицинских записей.
/// </summary>
[Serializable, NetSerializable]
public enum LustMedicalRecordsUiKey : byte
{
    Key
}

/// <summary>
/// Данные медицинской консоли, которые сервер отправляет клиенту.
/// </summary>
[Serializable, NetSerializable]
public sealed class LustMedicalRecordsUiState : BoundUserInterfaceState
{
    public readonly Dictionary<uint, string>? RecordListing;
    public readonly uint? SelectedKey;

    public readonly string? Name;
    public readonly int? Age;
    public readonly Gender? Gender;
    public readonly string? Species;
    public readonly string? JobTitle;
    public readonly string? Fingerprint;
    public readonly string? DNA;

    public readonly string? Notes;
    public readonly bool CanEditBiometrics;
    public readonly HumanoidCharacterProfile? HumanoidProfile;


    /// <summary>
    /// Создаёт пустое состояние, например, когда у станции нет записей.
    /// </summary>
    public LustMedicalRecordsUiState()
    {
        RecordListing = null;
        SelectedKey = null;

        Name = null;
        Age = null;
        Gender = null;
        Species = null;
        JobTitle = null;
        Fingerprint = null;
        DNA = null;

        Notes = null;
        CanEditBiometrics = false;
        HumanoidProfile = null;
    }

    /// <summary>
    /// Создаёт состояние со списком и текущим выбором.
    /// </summary>
    public LustMedicalRecordsUiState(Dictionary<uint, string> recordListing, uint? selectedKey)
    {
        RecordListing = recordListing;
        SelectedKey = selectedKey;
        Name = null;
        Age = null;
        Gender = null;
        Species = null;
        JobTitle = null;
        Fingerprint = null;
        DNA = null;
        Notes = null;
        CanEditBiometrics = false;
        HumanoidProfile = null;
    }

    public LustMedicalRecordsUiState(
        Dictionary<uint, string> recordListing,
        uint? selectedKey,
        Content.Shared.StationRecords.GeneralStationRecord? generalRecord,
        MedicalRecord? medicalRecord,
        bool canEditBiometrics)
    {
        RecordListing = recordListing;
        SelectedKey = selectedKey;

        Name = generalRecord?.Name;
        Age = generalRecord?.Age;
        Gender = generalRecord?.Gender;
        Species = generalRecord?.Species;
        JobTitle = generalRecord?.JobTitle;
        Fingerprint = generalRecord?.Fingerprint;
        DNA = generalRecord?.DNA;

        Notes = medicalRecord?.Notes;
        CanEditBiometrics = canEditBiometrics;
        HumanoidProfile = generalRecord?.HumanoidProfile;
    }
}

/// <summary>
/// Клиент просит сервер выбрать запись сотрудника.
/// </summary>
[Serializable, NetSerializable]
public sealed class LustMedicalRecordsSelectRecordMessage : BoundUserInterfaceMessage
{
    public readonly uint? SelectedKey;

    public LustMedicalRecordsSelectRecordMessage(uint? selectedKey)
    {
        SelectedKey = selectedKey;
    }
}

/// <summary>
/// Сохраняет редактируемые поля выбранной медицинской карты.
/// </summary>
[Serializable, NetSerializable]
public sealed class LustMedicalRecordsSaveMessage : BoundUserInterfaceMessage
{
    public readonly uint RecordId;
    public readonly Gender Gender;
    public readonly string Species;
    public readonly string PhysiologicalTraits;
    public readonly string PsychologicalTraits;
    public readonly string Notes;
    public readonly string Fingerprint;
    public readonly string DNA;

    public LustMedicalRecordsSaveMessage(
        uint recordId,
        Gender gender,
        string species,
        string physiologicalTraits,
        string psychologicalTraits,
        string notes,
        string fingerprint,
        string dna)
    {
        RecordId = recordId;
        Gender = gender;
        Species = species;
        PhysiologicalTraits = physiologicalTraits;
        PsychologicalTraits = psychologicalTraits;
        Notes = notes;
        Fingerprint = fingerprint;
        DNA = dna;
    }
}

/// <summary>
/// Просит сервер распечатать медицинскую карту выбранного сотрудника.
/// </summary>
[Serializable, NetSerializable]
public sealed class LustMedicalRecordsPrintMessage(uint recordId) : BoundUserInterfaceMessage
{
    public readonly uint RecordId = recordId;
}
