using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal readonly struct AwkExecutionContext(AwkOutputSink output, AwkIo io)
{
    public AwkOutputSink Output { get; } = output;

    public AwkIo Io { get; } = io;
}

internal sealed class AwkExecutionContextStack
{
    private readonly Stack<AwkExecutionContext> _contexts = new();

    public AwkExecutionContextScope Push(AwkOutputSink output, AwkIo io)
    {
        _contexts.Push(new AwkExecutionContext(output, io));
        return new AwkExecutionContextScope(this);
    }

    public bool TryGet(out AwkExecutionContext context)
    {
        if (_contexts.Count > 0)
        {
            context = _contexts.Peek();
            return true;
        }

        context = default;
        return false;
    }

    internal void Pop()
    {
        _contexts.Pop();
    }
}

internal readonly struct AwkExecutionContextScope(AwkExecutionContextStack stack) : IDisposable
{
    public void Dispose()
    {
        stack.Pop();
    }
}
