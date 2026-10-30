using System;

namespace Lokad.Awk;

internal class AwkRuntimeException : Exception
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

internal sealed class AwkHostReadException : AwkRuntimeException
{
    public AwkHostReadException(AwkFileDescriptor descriptor, AwkOperationError error)
        : base(
            AwkMessages.FormatDescriptorReadError(descriptor, error.Message),
            AwkDiagnosticId.HostIoFailure)
    {
        Descriptor = descriptor;
        Error = error;
    }

    public AwkFileDescriptor Descriptor { get; }

    public AwkOperationError Error { get; }
}

internal sealed class AwkHostAppendException(
    AwkFileDescriptor descriptor, ReadOnlyMemory<byte> message)
    : AwkRuntimeException(message, AwkDiagnosticId.HostIoFailure)
{
    public AwkFileDescriptor Descriptor { get; } = descriptor;
}
