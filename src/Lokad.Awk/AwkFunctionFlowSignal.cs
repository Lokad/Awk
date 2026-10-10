namespace Lokad.Awk;

/// <summary>Unwinds value evaluation to the owning action without publishing a partial value.</summary>
internal sealed class AwkFunctionFlowSignal(AwkStatementFlow flow) : Exception
{
    internal AwkStatementFlow Flow { get; } = flow;
}
