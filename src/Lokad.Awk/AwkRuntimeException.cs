using System;

namespace Lokad.Awk;

internal sealed class AwkRuntimeException : Exception
{
    public AwkRuntimeException(ReadOnlyMemory<byte> message)
        : this(message, AwkDiagnosticId.RuntimeError)
    {
    }

    public AwkRuntimeException(ReadOnlyMemory<byte> message, AwkDiagnosticId id)
    {
        MessageBytes = message;
        Id = id;
    }

    public ReadOnlyMemory<byte> MessageBytes { get; }

    public AwkDiagnosticId Id { get; }
}
