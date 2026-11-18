namespace Lokad.Awk;

// A fault from application code, rather than an AWK error or a library invariant.
// Keep it separate from AwkRuntimeException, which carries script diagnostics.
internal sealed class AwkHostException(Exception cause) : Exception("Application callback failed.", cause);
