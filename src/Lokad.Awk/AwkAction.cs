using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkAction
{
    private readonly AwkPattern? _pattern;
    private readonly AwkStatement[]? _statements;
    private readonly AwkRegion? _region;

    // A prepared rule already contains its condition.
    internal AwkAction(AwkRegion region) => _region = region;

    internal AwkRegion? Region => _region;

    public AwkAction(AwkPattern? pattern, IReadOnlyList<AwkStatement> statements)
    {
        _pattern = pattern;
        var snapshot = AwkCollectionSnapshots.Copy(statements);
        _statements = snapshot;
    }

    internal AwkStatement[] Statements => _statements ?? [];

    public ValueTask<bool> MatchesAsync(AwkRuntimeState state) =>
        _pattern == null
            ? ValueTask.FromResult(true)
            : _pattern.MatchesAsync(state);

    public async ValueTask<AwkStatementFlow> ExecuteAsync(
        AwkRuntimeState state,
        AwkIo io,
        AwkOutputSink output)
    {
        if (_region is { } region)
        {
            using var header = state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.ScopedRegionStorageBytes);
            state.ThrowIfCancellationRequested();
            using var storage = new AwkRegionStorage(state);
            using var context = state.PushExecutionContext(output, io);
            return await region.ExecuteAsync(state, storage, output, io).ConfigureAwait(false);
        }
        var statements = Statements;
        if (statements.Length == 0)
            return AwkStatementFlow.Continue;

        using var executionContext = state.PushExecutionContext(output, io);
        var evaluationContext = new AwkEvaluationContext(state, output, io);
        var flow = await AwkStatementBlockExecutor
            .ExecuteAsync(statements, evaluationContext)
            .ConfigureAwait(false);

        if (flow == AwkStatementFlow.BreakLoop)
            throw new AwkRuntimeException(AwkMessages.ErrorBreakOutsideLoop);

        if (flow == AwkStatementFlow.ContinueLoop)
            throw new AwkRuntimeException(AwkMessages.ErrorContinueOutsideLoop);

        return flow;
    }

}
