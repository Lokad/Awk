namespace Lokad.Awk;

internal enum AwkActionPhase
{
    Begin,
    BeginFile,
    EndFile,
    End,
    None,
    Main,
    SortComparison
}
