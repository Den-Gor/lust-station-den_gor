using Content.Shared._Lust.MedicalRecords;
using Robust.Client.UserInterface;

namespace Content.Client._Lust.MedicalRecords;

public sealed class LustMedicalRecordsBoundUserInterface : BoundUserInterface
{
    private LustMedicalRecordsWindow? _window;

    public LustMedicalRecordsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LustMedicalRecordsWindow>();
        _window.OnRecordSelected += selectedKey =>
            SendMessage(new LustMedicalRecordsSelectRecordMessage(selectedKey));
        _window.OnSaveRequested += (recordId, gender, species, physiologicalTraits, psychologicalTraits, notes, fingerprint, dna) =>
            SendMessage(new LustMedicalRecordsSaveMessage(
                recordId,
                gender,
                species,
                physiologicalTraits,
                psychologicalTraits,
                notes,
                fingerprint,
                dna));
        _window.OnPrintRequested += recordId =>
            SendMessage(new LustMedicalRecordsPrintMessage(recordId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not LustMedicalRecordsUiState medicalState)
            return;

        _window?.UpdateState(medicalState);
    }
}
