using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal readonly struct AwkExecutionContext(AwkOutputSink? output, AwkIo? io)
{
    public AwkOutputSink Output => output ??
        throw new InvalidOperationException("The default execution context has no output sink.");

    public AwkIo Io => io ??
        throw new InvalidOperationException("The default execution context has no I/O runtime.");
}

internal sealed class AwkExecutionContextStack
{
    private readonly Stack<AwkExecutionContext> _contexts = new();

    public AwkExecutionContextScope Push(AwkOutputSink output, AwkIo io)
    {
        // The command already owns these services. Reentrant actions need a new
        // scope only when they actually override a service.
        if (_contexts.TryPeek(out var active) && ReferenceEquals(active.Output, output) && ReferenceEquals(active.Io, io))
            return default;
        _contexts.Push(new AwkExecutionContext(output, io));
        return new AwkExecutionContextScope(this);
    }

    public bool TryGet(out AwkExecutionContext context) => _contexts.TryPeek(out context);

    internal void Pop()
    {
        _contexts.Pop();
    }
}

internal readonly struct AwkExecutionContextScope(AwkExecutionContextStack? stack) : IDisposable
{
    public void Dispose() => stack?.Pop();
}
