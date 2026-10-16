namespace Lokad.Awk;

internal readonly struct AwkEvaluationContext
{
    private readonly AwkRuntimeState? _state;
    private readonly AwkOutputSink? _output;
    private readonly AwkIo? _io;

    public AwkEvaluationContext(AwkRuntimeState state)
    {
        _state = state;
        _output = null;
        _io = null;
    }

    public AwkEvaluationContext(
        AwkRuntimeState state,
        AwkOutputSink output,
        AwkIo io)
        : this(state)
    {
        _output = output;
        _io = io;
    }

    public AwkRuntimeState State => _state ??
        throw new InvalidOperationException("The default evaluation context has no runtime state.");

    public bool TryGetExecutionContext(out AwkExecutionContext context)
    {
        if (_output is { } output)
        {
            context = new AwkExecutionContext(output, _io);
            return true;
        }

        return State.TryGetExecutionContext(out context);
    }
}
