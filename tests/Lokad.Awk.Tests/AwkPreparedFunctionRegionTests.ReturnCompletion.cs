namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public void ReturnedScratchKeepsItsFundingAfterCalleeRetirement(bool governed, bool textSource, bool overwrite)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters, enabled: governed);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        using var source = new AwkRegionStorage(memory.State);
        using var caller = new AwkRegionStorage(memory.State);
        source.Prepare(new(0, Values: 1, Texts: 1));
        caller.Prepare(new(0, Values: 1));
        var spelling = "0007" + new string(' ', 32 << 10);
        var text = AwkText.FromSourceLiteralString(spelling);
        var value = textSource ? memory.State.CreateStringValue(text) : AwkValue.FromNumericString(text);
        if (textSource) source.StoreText(0, text);
        else source.StoreValue(0, value);
        if (overwrite) caller.StoreValue(0, AwkValue.FromText("previous"));
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            source.MoveRetiringReturnValueTo(caller, 0, in value);
            source.Dispose();
            Assert.Equal(spelling, caller.Values[0].ByteText.AsString());
            Assert.Equal(textSource ? AwkRuntimeValueKind.String : AwkRuntimeValueKind.NumericString, caller.Values[0].RuntimeKind);
            Assert.Equal(7, caller.Values[0].AsNumber());
            if (governed) Assert.True(memory.State.MemoryAccounting.CurrentCommittedBytes >= value.RetainedTextMemoryBytes);
            Assert.Equal(0, memory.Budget.Refusals);
        }
        caller.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task TextReturnConvertsWithinItsFundedCalleeAfterActualFlush(bool cancel) =>
        CheckReturnedValueAsync(numeric: false, pending: true, cancel);

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public Task ReadyReturnKeepsItsFundedCalleeThroughHostCompletion(bool numeric, bool cancel) =>
        CheckReturnedValueAsync(numeric, pending: false, cancel);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NumericReturnKeepsItsFundedCalleeAfterActualFlush(bool cancel) =>
        CheckReturnedValueAsync(numeric: true, pending: true, cancel);

    private static async Task CheckReturnedValueAsync(bool numeric, bool pending, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        var program = Bind("function part(s){return " + (numeric ? "s+0" : "substr(s,1)") +
            "}BEGIN{total+=part(\"0007\")}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(0));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IMemoryReservation? pressure = null;
        var visitor = new TestOperationVisitor([])
        {
            AppendOverride = async (_, bytes, _) =>
            {
                Assert.True(state.IsInsideFunction);
                Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var reserved));
                pressure = reserved;
                entered.SetResult();
                if (pending) await release.Task.ConfigureAwait(false);
                else if (cancel) cancellation.Cancel();
                Assert.True(bytes.Span.SequenceEqual("before\n"u8));
                return AwkAppendResult.Accepted();
            }
        };
        var io = new AwkIo(visitor, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, cancellation.Token, counters, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        output.Append(AwkText.FromSourceLiteralString("before\n"));
        var run = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (pending)
            {
                Assert.False(run.IsCompleted);
                Assert.True(state.IsInsideFunction);
                if (cancel) cancellation.Cancel();
                Assert.False(run.IsCompleted);
                release.SetResult();
            }
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(AwkStatementFlow.Continue, await run);
            Assert.Equal(cancel ? 0 : 7, state.ResolveVariableValue(total).AsNumber());
            Assert.Equal(0, memory.Budget.Refusals);
            Assert.False(state.IsInsideFunction);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
            pressure?.Dispose();
            await io.CloseForCleanupAsync();
        }
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PublicTextReturnKeepsFundingAcrossPendingDiagnostics(bool cancel)
    {
        const string warning = "awk: warning: sqrt: received negative argument -1\n";
        const string source = "function part(s){return sqrt(-1)?substr(s,1):substr(s,1)}" +
            "BEGIN{print \"ready\";total=0;total+=part(\"0007\");print total}";
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IMemoryReservation? pressure = null;
        var stdout = "";
        var stderr = "";
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                if (descriptor == AwkFileDescriptor.StdOut) stdout += AwkUtf8Codec.Decode(bytes.Span);
                else
                {
                    stderr += AwkUtf8Codec.Decode(bytes.Span);
                    Assert.True(budget.TryReserve(budget.Limit - budget.Reserved, out var reserved));
                    pressure = reserved;
                    entered.SetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.True(bytes.Span.SequenceEqual(System.Text.Encoding.UTF8.GetBytes(warning)));
                }
                return AwkAppendResult.Accepted();
            }
        };
        var run = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("ready\n", stdout);
            Assert.False(run.IsCompleted);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            release.SetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(0, (await run).Value);
            Assert.Equal(cancel ? "ready\n" : "ready\n7\n", stdout);
            Assert.Equal(warning, stderr);
            Assert.Equal(0, budget.Refusals);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
            pressure?.Dispose();
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public async Task NumericReturnDoesNotAdmitUnreachableWideFields()
    {
        var budget = new CooperativeTestBudget(2L << 20);
        var host = new MockFileSystem();
        host.AddFile("/input", "007" + string.Concat(Enumerable.Repeat(" a", 50_000)) + "\n");
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["function f(){return +$1;print $50000}{print f();exit}", "/input"], []);
        var awk = AwkTestHarness.ParseAwk(command);

        var exit = await awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget),
            CancellationToken.None);

        Assert.Equal(0, exit.Value);
        Assert.Equal("7\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public Task CapturedReturnSurvivesActualFlushCompletion(bool governed, bool numeric, bool cancel) =>
        VerifyCapturedReturnCompletion(governed, numeric, cancel, assignment: false);

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public Task NumericCallAssignmentWaitsForActualFlushCompletion(bool governed, bool cancel) =>
        VerifyCapturedReturnCompletion(governed, numeric: true, cancel, assignment: true);

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public Task StoredNumberReturnSurvivesActualFlushCompletion(bool governed, bool cancel) =>
        VerifyCapturedReturnCompletion(governed, numeric: true, cancel, assignment: false, storedNumber: true);

    private async Task VerifyCapturedReturnCompletion(bool governed, bool numeric, bool cancel, bool assignment,
        bool storedNumber = false)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        {
            await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, cancellation.Token);
            var counters = AwkRuntimeCounters.CreateEnabled();
            using var state = State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, owner);
            var source = storedNumber ? "function f(x){return a[\"r\"]}BEGIN{total+=f(7)}"
                : numeric ? "function f(x){return x+0}BEGIN{total" + (assignment ? "=" : "+=") + "f(7)}"
                : "function f(x){return x}BEGIN{saved=f(\"0007\")}";
            var program = Bind(source);
            state.InitializeGlobalNames(program.GlobalNames);
            state.InitializeGlobalReadSlots(program.GlobalReadCount);
            state.InitializeFunctions(program.Functions);
            if (storedNumber)
                state.SetArrayAwkValue(AwkArrayName.FromText("a"), new AwkArrayKey("r"), AwkValue.FromNumber(7));
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var host = new TestOperationVisitor([]);
            var writes = new List<byte[]>();
            host.AppendOverride = async (descriptor, bytes, _) =>
            {
                Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                writes.Add(bytes.ToArray());
                entered.TrySetResult();
                await release.Task.ConfigureAwait(false);
                // The host may use the borrow until actual completion.
                Assert.True(bytes.Span.SequenceEqual("before\n"u8));
                return AwkAppendResult.Accepted();
            };
            var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
                AwkFileDescriptor.StdErr, cancellation.Token, counters, state.Limits, state, static _ => []);
            using var output = new AwkOutputSink(state.MemoryAccounting, counters);
            output.Append(AwkText.FromSourceLiteralString("before\n"));
            var pending = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(pending.IsCompleted);
                Assert.True(state.IsInsideFunction);
                // Return evaluation precedes the flush, including an owned text capture.
                if (storedNumber)
                    state.SetArrayAwkValue(AwkArrayName.FromText("a"), new AwkArrayKey("r"), AwkValue.FromText("changed"));
                else state.AssignVariableValue(AwkVariableName.FromText("x"), AwkValue.FromText("changed"));
                if (governed) Assert.True(budget.Reserved > 0);
                if (cancel) cancellation.Cancel();
                Assert.False(pending.IsCompleted);
                Assert.True(state.IsInsideFunction);
                if (governed) Assert.True(budget.Reserved > 0);
                release.TrySetResult();
                if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
                else
                {
                    Assert.Equal(AwkStatementFlow.Continue, await pending);
                    if (numeric) Assert.Equal(7, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
                    else Assert.True(state.ResolveVariableValue(AwkVariableName.FromText("saved")).ByteText.AsSpan().SequenceEqual("0007"u8));
                    if (assignment) Assert.True(counters.CallNumericScratchBypasses > 0);
                }
                Assert.True(Assert.Single(writes).AsSpan().SequenceEqual("before\n"u8));
                Assert.False(state.IsInsideFunction);
                Assert.Equal(1, counters.BoundFunctionRuns);
                Assert.Equal(0, counters.Utf8Encodes);
                Assert.Equal(0, counters.StringDecodes);
            }
            finally
            {
                release.TrySetResult();
                try { await pending; } catch (OperationCanceledException) when (cancel) { }
                await io.CloseForCleanupAsync();
            }
        }
        Assert.Equal(0, budget.Reserved);
    }
}
