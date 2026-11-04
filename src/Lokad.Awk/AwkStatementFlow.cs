namespace Lokad.Awk;

// Nonlocal requests crossing rule/file/host boundaries; branches and function returns
// are handled by the common interpreter's jumps and caller continuations.
internal enum AwkStatementFlow
{
    Continue,
    NextRecord,
    NextFile,
    ExitProgram
}
