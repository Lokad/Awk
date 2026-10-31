namespace Lokad.Awk;

// Construction-only metadata. Execution retains the bound statements themselves,
// without an additional delegate graph or a result object for each executed step.
internal readonly struct AwkStatementPreparation(AwkStatement statement, int depth, int calls)
{
    private readonly AwkStatement? _statement = statement;
    internal AwkStatement Statement => _statement ??
        throw new InvalidOperationException("No computational statement was prepared.");
    internal int Depth { get; } = depth;
    internal int Calls { get; } = calls;
}
