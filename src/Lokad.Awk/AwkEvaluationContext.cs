namespace Lokad.Awk;

internal readonly struct AwkEvaluationContext
{
    private readonly bool _hasExecutionContext;
    private readonly AwkExecutionContext _executionContext;

    public AwkEvaluationContext(
        int recordNumber,
        AwkRuntimeState state)
    {
        RecordNumber = recordNumber;
        State = state;
        _hasExecutionContext = false;
        _executionContext = default;
    }

    public AwkEvaluationContext(
        int recordNumber,
        AwkRuntimeState state,
        AwkOutputSink output,
        AwkIo io)
        : this(recordNumber, state)
    {
        _hasExecutionContext = true;
        _executionContext = new AwkExecutionContext(output, io);
    }

    public int RecordNumber { get; }

    public AwkRuntimeState State { get; }

    public bool TryGetExecutionContext(out AwkExecutionContext context)
    {
        if (_hasExecutionContext)
        {
            context = _executionContext;
            return true;
        }

        return State.TryGetExecutionContext(out context);
    }
}
