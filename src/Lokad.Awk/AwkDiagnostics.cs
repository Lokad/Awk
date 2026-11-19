using System.Globalization;

namespace Lokad.Awk;

internal readonly record struct AwkFailureDiagnostic(ReadOnlyMemory<byte> Message, AwkDiagnosticId Id);

internal static class AwkDiagnostics
{
    public static AwkFailureDiagnostic Parse(IReadOnlyList<AwkDiagnostic> diagnostics)
    {
        AwkDiagnostic? first = null;
        for (var index = 0; index < diagnostics.Count; index++)
        {
            var diagnostic = diagnostics[index];
            if (diagnostic.IsWarning)
                continue;
            if (diagnostic.Id == AwkDiagnosticId.ParseInvalidRegex)
                return new(AwkMessages.ErrorInvalidRegex, diagnostic.Id);
            first ??= diagnostic;
        }

        return first is { } failure
            ? new(FormatDiagnosticMessage(failure), failure.Id)
            : new(AwkMessages.ErrorInvalidScript, AwkDiagnosticId.ParseSyntax);
    }

    public static ReadOnlyMemory<byte> Warning(AwkDiagnostic diagnostic) =>
        FormatDiagnosticMessage(diagnostic);

    private static ReadOnlyMemory<byte> FormatDiagnosticMessage(AwkDiagnostic diagnostic)
    {
        var message = diagnostic.Message.Trim();
        if (message.Length == 0)
            return AwkMessages.ErrorInvalidScript;

        if (message[0] is >= 'A' and <= 'Z')
            message = char.ToLowerInvariant(message[0]) + message[1..];

        var severity = diagnostic.IsWarning ? "warning: " : string.Empty;
        var formatted = diagnostic.SourceLocation is { } location
            ? string.Create(CultureInfo.InvariantCulture,
                $"awk: {location.SourceName.Text}:{location.LineNumber.Value}:{location.ColumnNumber.Value}: {severity}{message}\n")
            : string.Concat("awk: ", severity, message, "\n");
        return AwkUtf8Codec.Encode(formatted);
    }
}
