namespace Lokad.Awk.Tests;

// A borrowed view of actual invocation services. Ungoverned standalone cases own
// their runtime; shared-memory cases leave final retirement with the memory fixture.
internal sealed class AwkFieldTestContext : IDisposable
{
    private readonly CooperativeTestMemory _memory;
    private readonly bool _ownsMemory;

    public AwkRuntimeState State => _memory.State;
    public AwkFieldFrame Frame => State.RecordFields;
    public AwkFieldSplitter Splitter => State.FieldSplitter;
    public long BaselineBytes { get; }
    public long BaselineBudget { get; }

    public AwkFieldTestContext(CooperativeTestMemory memory, string separator = " ", string outputSeparator = " ")
    {
        _memory = memory;
        if (separator != " ") State.AssignSpecialVariableValue(AwkSpecialVariableId.FieldSeparator,
            AwkValue.FromText(AwkText.FromSourceLiteralString(separator)));
        if (outputSeparator != " ") State.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator,
            AwkValue.FromText(AwkText.FromSourceLiteralString(outputSeparator)));
        BaselineBytes = memory.Accounting.LiveBytes;
        BaselineBudget = memory.Budget.Reserved;
    }

    public AwkFieldTestContext(AwkRuntimeCounters counters, string separator = " ", string outputSeparator = " ")
        : this(new CooperativeTestMemory(8 << 20, enabled: false).InitializeRuntime(counters), separator, outputSeparator)
    {
        _ownsMemory = true;
    }

    public void Dispose()
    {
        if (_ownsMemory) _memory.Dispose();
        else Frame.ResetRecord();
    }
}
