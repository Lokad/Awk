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

        if (message.StartsWith("awk:", StringComparison.Ordinal))
        {
            if (diagnostic.SourceLocation.HasValue)
            {
                return AwkUtf8Codec.Encode(
                    "awk: " +
                    FormatLocation(diagnostic.SourceLocation.Value) +
                    ": " +
                    EnsureTrailingNewLine(message[4..].TrimStart()));
            }

            return AwkUtf8Codec.Encode(EnsureTrailingNewLine(message));
        }

        if (diagnostic.SourceLocation.HasValue)
        {
            return AwkUtf8Codec.Encode(
                "awk: " +
                FormatLocation(diagnostic.SourceLocation.Value) +
                ": " +
                EnsureTrailingNewLine(LowercaseAsciiInitial(message)));
        }

        return AwkUtf8Codec.Encode("awk: " + LowercaseAsciiInitial(message) + "\n");

        static string FormatLocation(AwkSourceLocation location) =>
            location.SourceName.Text +
            ":" +
            location.LineNumber.Value.ToString(CultureInfo.InvariantCulture) +
            ":" +
            location.ColumnNumber.Value.ToString(CultureInfo.InvariantCulture);

        static string EnsureTrailingNewLine(string message) =>
            message.EndsWith('\n') ? message : message + "\n";

        static string LowercaseAsciiInitial(string message)
        {
            if (message.Length == 0 ||
                message[0] < 'A' ||
                message[0] > 'Z')
            {
                return message;
            }

            return (char)(message[0] - 'A' + 'a') + message[1..];
        }
    }

}
