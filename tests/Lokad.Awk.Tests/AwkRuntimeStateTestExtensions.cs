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

        public bool IsInsideFunction => state.FunctionCalls.IsInsideFunction;

        public AwkFunctionFrame CurrentFunctionFrame => state.FunctionCalls.CurrentFrame;

        public void PushFunctionFrame(AwkFunctionLocalLayout layout, ReadOnlySpan<AwkFunctionLocalSlot> arguments) =>
            state.FunctionCalls.PushFrame(layout, arguments);

        public AwkValue GetGlobalScalarValue(AwkGlobalSlotId id) =>
            state.Variables.GetScalarValue(state.Variables.GetScalarCell(id));

        public void AssignVariableValue(AwkGlobalSlotId id, AwkValue value) => state.Variables.SetScalarValue(id, value);

        public void AssignField(int index, string value) => state.AssignFieldValue(index, state.CreateStringValue(value));

        public AwkText GetArrayByteText(AwkArrayName name, AwkArrayKey index) =>
            state.FormatStringByteText(state.GetArrayAwkValue(name, index));

        public void SetArrayString(AwkArrayName name, AwkArrayKey index, string text)
        {
            var bytes = !name.IsFunctionLocal && state.Variables.IsSymbolTable(name.Text)
                ? state.EncodeText(text) : AwkText.FromString(text);
            state.SetArrayAwkValue(name, index, state.CreateStringValue(bytes));
        }

        public int SplitIntoArray(AwkText input, AwkArrayName arrayName, AwkFieldSeparatorSpec separator,
            AwkOptionalIdentifier separatorArrayName, bool csv = false) =>
            state.SplitIntoArray(input, new AwkArrayTarget(arrayName), separator, Target(separatorArrayName), csv);

        public int PatsplitIntoArray(AwkText input, AwkArrayName arrayName, AwkRegexPattern pattern,
            AwkOptionalIdentifier separatorArrayName) =>
            state.PatsplitIntoArray(input, new AwkArrayTarget(arrayName), pattern, Target(separatorArrayName));

        public void ReplaceMatchCaptureArray(AwkArrayName name, AwkLeasedRegexMatch match) =>
            state.ReplaceMatchCaptureArray(new AwkArrayTarget(name), match);
    }

    private static AwkArrayTarget? Target(AwkOptionalIdentifier name) => name.HasValue
        ? new AwkArrayTarget(new AwkArrayName(name.Value)) : null;
}
