namespace Lokad.Awk;

// Core AWK syntax validation, independent of provider compilation failures.
internal sealed class AwkRegexSyntaxException()
    : AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
