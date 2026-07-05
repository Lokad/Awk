namespace Lokad.Awk;

internal enum AwkIoEventKind
{
    Write,
    Close,
    Diagnostic
}

internal readonly struct AwkIoEvent
{
    private AwkIoEvent(
        AwkIoEventKind kind,
        AwkRedirectedOutput output,
        string path,
        AwkDiagnostic diagnostic)
    {
        Kind = kind;
        Output = output;
        Path = path;
        Diagnostic = diagnostic;
    }

    public AwkIoEventKind Kind { get; }

    public AwkRedirectedOutput Output { get; }

    public string Path { get; }

    public AwkDiagnostic Diagnostic { get; }

    public static AwkIoEvent Write(AwkRedirectedOutput output) =>
        new(AwkIoEventKind.Write, output, string.Empty, default);

    public static AwkIoEvent Close(string path) =>
        new(AwkIoEventKind.Close, default, path, default);

    public static AwkIoEvent DiagnosticWarning(AwkDiagnostic diagnostic) =>
        new(AwkIoEventKind.Diagnostic, default, string.Empty, diagnostic);
}
