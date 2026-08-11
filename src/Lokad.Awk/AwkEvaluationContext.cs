namespace Lokad.Awk;

internal readonly struct AwkEvaluationContext
{
    private readonly AwkRuntimeState? _state;
    private readonly bool _hasExecutionContext;
    private readonly AwkExecutionContext _executionContext;

    public AwkEvaluationContext(AwkRuntimeState state)
    {
        _state = state;
        _hasExecutionContext = false;
        _executionContext = default;
    }

    public AwkEvaluationContext(
        AwkRuntimeState state,
        AwkOutputSink output,
        AwkIo io)
        : this(state)
    {
        _hasExecutionContext = true;
        _executionContext = new AwkExecutionContext(output, io);
    }

    public AwkRuntimeState State => _state ??
        throw new InvalidOperationException("The default evaluation context has no runtime state.");

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
