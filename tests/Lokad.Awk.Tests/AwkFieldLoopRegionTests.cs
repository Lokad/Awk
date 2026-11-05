namespace Lokad.Awk.Tests;

public sealed class AwkFieldLoopRegionTests
{
    private const string Script = "{for(i=1;i<=NF;i++){nonempty+=($i!=\"\");chars+=length($i)}}";

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CompleteFieldLoopPreservesState(bool governed)
    {
        var result = await RunAsync(AwkRuntimeLimits.CreateDefault(), governed);
        Assert.Equal(4, result.Index);
        Assert.Equal(3, result.Nonempty);
        Assert.Equal(7, result.Chars);
        Assert.Empty(result.Output);
        Assert.Empty(result.Error);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UnicodeFieldsKeepScalarLengths(bool governed)
    {
        var result = await RunAsync(AwkRuntimeLimits.CreateDefault(), governed, "é xyz aa");
        Assert.Equal(4, result.Index);
        Assert.Equal(3, result.Nonempty);
        Assert.Equal(6, result.Chars);
        Assert.Empty(result.Output);
        Assert.Empty(result.Error);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("lint")]
    public async Task OutputAndLintKeepFieldProcessingBoundaries(string mode)
    {
        var result = await RunAsync(AwkRuntimeLimits.CreateDefault(), true, boundary: mode);
        Assert.Equal(4, result.Index);
        Assert.Equal(3, result.Nonempty);
        Assert.Equal(7, result.Chars);
        Assert.Equal(mode == "pending" ? "prior" : "", result.Output);
        Assert.Empty(result.Error);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task PendingFieldIndicesKeepTheirCallerAndNeverReplay(bool compare, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(2 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, cancellation.Token);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner, cancellation.Token);
        var value = compare ? "($(index_value())==\"bb\")" : "length($(index_value()))";
        var binding = AwkBinder.Bind(AwkParser.Parse("function index_value(v){getline v<\"/side\";return v}" +
            "{for(i=1;i<2;i++)total+=" + value + "}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(5));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("a bb"));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entries = 0;
        var host = new MockFileSystem();
        host.AddFile("/side", "2\n");
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            entries++;
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var run = program.MainProgram.ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal(5, state.ResolveVariableValue(total).AsNumber());
            if (cancel)
            {
                cancellation.Cancel();
                Assert.False(run.IsCompleted);
                Assert.True(budget.Reserved > 0);
            }
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(cancel ? 5 : compare ? 6 : 7, state.ResolveVariableValue(total).AsNumber());
            Assert.Equal(1, entries);
            Assert.False(state.IsInsideFunction);
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            await owner.DisposeAsync();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }

    private static async Task<Result> RunAsync(AwkRuntimeLimits limits, bool governed, string record = "aa b cdef", string boundary = "")
    {
        var budget = new CooperativeTestBudget(2 << 20);
        await using var owner = governed ? AwkExecutionMemoryOwner.Create(budget, CancellationToken.None) : AwkExecutionMemoryOwner.Disabled;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, limits, owner, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        var binding = AwkBinder.Bind(AwkParser.Parse(Script));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        foreach (var name in new[] { "i", "nonempty", "chars" }) state.AssignVariableValue(AwkVariableName.FromText(name), AwkValue.FromNumber(0));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(record));
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        var error = "";
        try { await program.MainProgram.ExecuteAsync(state, io, output); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        var result = new Result(state.ResolveVariableValue(AwkVariableName.FromText("i")).AsNumber(),
            state.ResolveVariableValue(AwkVariableName.FromText("nonempty")).AsNumber(),
            state.ResolveVariableValue(AwkVariableName.FromText("chars")).AsNumber(), host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync();
        output.Dispose();
        await owner.DisposeAsync();
        Assert.Equal(0, budget.Reserved);
        return result;
    }

    private static AwkRuntimeState State(AwkRuntimeCounters counters, AwkRuntimeLimits limits, AwkExecutionMemoryOwner owner,
        CancellationToken token = default, AwkLintMode lint = AwkLintMode.Off)
    {
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits,
            regexEngine: AwkUtf8RegexEngine.Instance) { MemoryOwner = owner };
        return new(AwkFieldSeparatorSpec.Default, token, options, new(limits, counters, token), AwkCompatibilityProfile.Default.WithLintMode(lint));
    }

    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host, AwkRuntimeCounters counters) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            CancellationToken.None, counters, state.Limits, state, static _ => []);

    private readonly record struct Result(double Index, double Nonempty, double Chars, string Output, string Error);
}
