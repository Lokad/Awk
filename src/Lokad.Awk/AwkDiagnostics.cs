using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lokad.Awk;

internal readonly struct AwkFailureDiagnostic
{
    public AwkFailureDiagnostic(ReadOnlyMemory<byte> message, AwkExitCode exitCode, AwkDiagnosticId id)
    {
        Message = message;
        ExitCode = exitCode;
        Id = id;
        Category = AwkDiagnosticFacts.GetCategory(id);
    }

    public ReadOnlyMemory<byte> Message { get; }

    public AwkExitCode ExitCode { get; }

    public AwkDiagnosticId Id { get; }

    public AwkDiagnosticCategory Category { get; }
}

internal static class AwkDiagnostics
{
    public static AwkExitCode SuccessExitCode => AwkExitCode.Success;
    public static AwkExitCode FatalExitCode => AwkExitCode.FatalError;

    public static AwkFailureDiagnostic Invocation(ReadOnlyMemory<byte> message, AwkDiagnosticId id) =>
        new(message, FatalExitCode, id);

    public static AwkFailureDiagnostic SourceLoad(ReadOnlyMemory<byte> message, AwkDiagnosticId id) =>
        new(message, FatalExitCode, id);

    public static AwkFailureDiagnostic Parse(IReadOnlyList<AwkDiagnostic> diagnostics)
    {
        static AwkDiagnostic? ResolveFirstFailure(IReadOnlyList<AwkDiagnostic> diagnostics)
        {
            for (var index = 0; index < diagnostics.Count; index++)
            {
                if (!diagnostics[index].IsWarning)
                    return diagnostics[index];
            }

            return null;
        }

        for (var index = 0; index < diagnostics.Count; index++)
        {
            if (!diagnostics[index].IsWarning &&
                diagnostics[index].Id == AwkDiagnosticId.ParseInvalidRegex)
            {
                return new AwkFailureDiagnostic(AwkMessages.ErrorInvalidRegex, FatalExitCode, diagnostics[index].Id);
            }
        }

        var diagnostic = ResolveFirstFailure(diagnostics);
        return new AwkFailureDiagnostic(
            diagnostic.HasValue ? FormatDiagnosticMessage(diagnostic.Value) : AwkMessages.ErrorInvalidScript,
            FatalExitCode,
            diagnostic.HasValue ? diagnostic.Value.Id : AwkDiagnosticId.ParseSyntax);
    }

    public static AwkFailureDiagnostic Runtime(AwkRuntimeException exception) =>
        new(exception.MessageBytes, FatalExitCode, exception.Id);

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
