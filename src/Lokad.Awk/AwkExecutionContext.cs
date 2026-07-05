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

    public IDisposable Push(AwkOutputSink output, AwkIo io)
    {
        _contexts.Push(new AwkExecutionContext(output, io));
        return new Scope(this);
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

    private void Pop()
    {
        _contexts.Pop();
    }

    private sealed class Scope(AwkExecutionContextStack stack) : IDisposable
    {
        public void Dispose()
        {
            stack.Pop();
        }
    }
}
