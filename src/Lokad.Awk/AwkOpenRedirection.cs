namespace Lokad.Awk;

internal enum AwkOpenRedirectionKind
{
    None,
    OutputFile,
    InputFile,
    OutputPipe,
    InputPipe,
    StandardInput
}

// Lookup without a direction selects the most recently opened logical stream.
// Closing a physical output descriptor to flush it does not change this order.
internal readonly record struct AwkOpenRedirection(AwkOpenRedirectionKind Kind, long Order)
{
    public AwkOpenRedirection Newer(AwkOpenRedirection other) =>
        other.Order > Order ? other : this;
}

internal enum AwkFlushStatus
{
    Success,
    Unopened,
    InputFile,
    InputPipe
}
