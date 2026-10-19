namespace Lokad.Awk.Tests;

public sealed class AwkFieldLoopRegionTests
{
    private const string Script = "{for(i=1;i<=NF;i++){nonempty+=($i!=\"\");chars+=length($i)}}";

    [Fact]
    public async Task CompleteFieldLoopPreservesState()
    {
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(2 << 20);
        var prepared = await RunAsync(false, limits);
        var ordinary = await RunAsync(true, limits);
        Assert.Equal(ordinary, prepared with { Loops = 0 });
        Assert.Equal(1, prepared.Loops);
        {
            Assert.Equal(3, prepared.Nonempty);
            Assert.Equal(7, prepared.Chars);
            Assert.Equal("", prepared.Error);
        }
    }

    [Theory]
    [InlineData("fields")] [InlineData("memory")] [InlineData("unicode")]
    public async Task FieldAdmissionAndUnicodeMatchOrdinaryExecution(string mode)
    {
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(mode == "memory" ? 64 << 10 : 2 << 20);
        if (mode == "fields") limits = limits.WithMaxFields(1);
        var record = mode == "memory" ? string.Join(' ', Enumerable.Repeat(new string('x', 4096), 3)) : "é xyz aa";
        var prepared = await RunAsync(false, limits, record);
        Assert.Equal(await RunAsync(true, limits, record), prepared with { Loops = 0 });
        Assert.Equal(1, prepared.Loops);
        Assert.Equal(mode == "unicode", prepared.Error.Length == 0);
    }

    [Theory]
    [InlineData("pending")] [InlineData("lint")]
    public async Task OutputAndLintRetainOrdinaryLoopBoundaries(string mode)
    {
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(2 << 20);
        Assert.Equal(await RunAsync(true, limits, boundary: mode), await RunAsync(false, limits, boundary: mode));
    }

    [Theory]
    [InlineData(14, true)] [InlineData(15, false)]
    public void FieldPreparationKeepsTheExistingSixteenNodeDepthBound(int depth, bool eligible)
    {
        AwkExpression index = AwkExpression.Number("1", 1);
        for (var current = 1; current < depth; current++)
            index = AwkExpression.Add(index, AwkExpression.Number("0", 0));
        foreach (var expression in new[]
        {
            AwkExpression.Length(AwkExpression.FieldIndex(index), AwkOptionalIdentifier.None),
            AwkExpression.Compare(AwkExpression.FieldIndex(index), AwkExpression.Literal("a"), AwkComparison.Equal)
        })
        {
            var condition = new AwkCondition([[AwkConditionClause.Truthy(expression)]]);
            Assert.Equal(eligible, condition.TryGetSynchronousExpression(out _));
        }
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task PendingFieldIndicesRemainOrdinaryAndNeverReplay(bool compare, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(2 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, cancellation.Token);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner, cancellation.Token);
        state.InitializeGlobalReadSlots(2);
        var i = AwkVariableName.FromText("i"); var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(5));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("a bb"));
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var index = new PendingIndex(completion.Task);
        var field = AwkExpression.FieldIndex(index);
        var value = compare ? AwkExpression.Compare(field, AwkExpression.Literal("bb"), AwkComparison.Equal)
            : AwkExpression.Length(field, AwkOptionalIdentifier.None);
        var reference = AwkReference.PreparedGlobalScalar(i, 0);
        var loop = AwkStatement.For(AwkStatement.Assignment(reference, AwkExpression.Number("1", 1)),
            new([[AwkConditionClause.Comparison(AwkExpression.PreparedGlobalVariable(i, 0), AwkExpression.Number("2", 2), AwkComparison.LessThan)]]),
            AwkStatement.PostIncrement(reference), [AwkStatement.CompoundAssignment(AwkReference.PreparedGlobalScalar(total, 1),
                value, AwkCompoundAssignmentOperation.Add)]);
        var host = new MockFileSystem(); var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        var run = loop.ExecuteAsync(new(state, output, io)).AsTask();
        try
        {
            Assert.False(run.IsCompleted);
            Assert.Equal(5, state.ResolveVariableValue(total).AsNumber());
            Assert.Equal(0, counters.SynchronousLoopRuns);
            if (cancel) cancellation.Cancel();
            completion.SetResult(AwkValue.FromNumber(2));
            if (cancel)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else
            {
                Assert.Equal(AwkStatementFlow.Continue, await run);
                Assert.Equal(compare ? 6 : 7, state.ResolveVariableValue(total).AsNumber());
            }
            Assert.Equal(1, index.Entries);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            completion.TrySetResult(AwkValue.FromNumber(2));
            try { await run; } catch (OperationCanceledException) when (cancel) { }
            await io.CloseAsync(); output.Dispose(); await owner.DisposeAsync();
            Assert.Equal(0, budget.Reserved);
        }
    }

    private static async Task<Result> RunAsync(bool ordinary, AwkRuntimeLimits limits, string record = "aa b cdef", string boundary = "")
    {
        var budget = new CooperativeTestBudget(2 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, limits, owner, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        var names = new HashSet<AwkVariableName>(new[] { "i", "nonempty", "chars" }.Select(AwkVariableName.FromText));
        var layout = new AwkGlobalReadLayout(names, null);
        state.InitializeGlobalReadSlots(layout.Count);
        foreach (var name in names) state.AssignVariableValue(name, AwkValue.FromNumber(0));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(record));
        var parsed = AwkParser.Parse(Script);
        var scope = AwkLoweringScope.GlobalFor(AwkCompatibilityMode.GnuDefault, globalReads: ordinary ? null : layout);
        var lowered = AwkStatementLowerer.LowerList(parsed.Syntax.Rules.Single().ActionStatements, scope);
        Assert.True(lowered.Succeeded);
        var statement = Assert.Single(lowered.Value);
        var host = new MockFileSystem(); var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        var error = "";
        try { await statement.ExecuteAsync(new(state, output, io)); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        var result = new Result(state.ResolveVariableValue(AwkVariableName.FromText("i")).AsNumber(), state.ResolveVariableValue(AwkVariableName.FromText("nonempty")).AsNumber(), state.ResolveVariableValue(AwkVariableName.FromText("chars")).AsNumber(), counters.MemoryPeakLiveBytes, counters.SynchronousLoopRuns, counters.FieldsMaterialized, host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync(); output.Dispose(); await owner.DisposeAsync(); Assert.Equal(0, budget.Reserved);
        return result;
    }

    private static AwkRuntimeState State(AwkRuntimeCounters counters, AwkRuntimeLimits limits, AwkExecutionMemoryOwner owner, CancellationToken token = default, AwkLintMode lint = AwkLintMode.Off)
    {
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility) { MemoryOwner = owner };
        return new(AwkFieldSeparatorSpec.Default, token, options, new(limits, counters, token), AwkCompatibilityProfile.Default.WithLintMode(lint));
    }

    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host, AwkRuntimeCounters counters) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            CancellationToken.None, counters, state.Limits, state, static () => []);

    private sealed class PendingIndex(Task<AwkValue> value) : AwkExpression
    {
        public int Entries { get; private set; }
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) { Entries++; return new(value); }
    }

    private readonly record struct Result(double Index, double Nonempty, double Chars, long Peak, long Loops, long Descriptors, string Output, string Error);
}
