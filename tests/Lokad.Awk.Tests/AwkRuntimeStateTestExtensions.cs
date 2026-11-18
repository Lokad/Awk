namespace Lokad.Awk.Tests;

internal static class AwkRuntimeStateTestExtensions
{
    extension(AwkRuntimeState state)
    {
        public double RecordNumber
        {
            get => state.ResolveSpecialVariableValue(AwkSpecialVariableId.RecordNumber).AsNumber(state.CancellationCheckpoint);
            set => state.AssignSpecialVariableValue(AwkSpecialVariableId.RecordNumber, AwkValue.FromNumber(value));
        }

        public double FileRecordNumber
        {
            get => state.ResolveSpecialVariableValue(AwkSpecialVariableId.FileRecordNumber).AsNumber(state.CancellationCheckpoint);
            set => state.AssignSpecialVariableValue(AwkSpecialVariableId.FileRecordNumber, AwkValue.FromNumber(value));
        }

        public AwkText CurrentRecordByteText => state.RecordFields.RecordAwkText;

        public AwkValue GetGlobalScalarValue(AwkGlobalSlotId id) =>
            state.Variables.GetScalarValue(state.Variables.GetScalarCell(id));

        public void AssignField(int index, string value) => state.AssignFieldValue(index, state.CreateStringValue(value));

        public AwkText GetArrayByteText(AwkArrayName name, AwkArrayKey index) =>
            state.FormatStringByteText(state.GetArrayAwkValue(name, index));
    }
}
