namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task StandaloneArrayRootsKeepVisibleBindingsAtFullBudget(bool governed, bool hidden)
    {
        using var memory = new CooperativeTestMemory(4 << 20, enabled: governed);
        memory.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var program = Bind("function f(a,k){x=a[k][\"v\"]}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var function = Assert.Single(program.Functions).Value;
        var region = Assert.IsType<AwkRegion>(function.Region);
        var global = AwkArrayName.FromText("a");
        var local = AwkArrayName.FromText("b");
        var x = AwkVariableName.FromText("x");
        state.Variables.SetArrayValue(state.Arrays.GetSubarray(global, new("key"), default), new("v"), AwkValue.FromNumber(9));
        state.Variables.SetArrayValue(state.Arrays.GetSubarray(local, new("key"), default), new("v"), AwkValue.FromNumber(7));
        state.AssignVariableValue(AwkVariableName.FromText("k"), AwkValue.FromText("key"));
        state.AssignVariableValue(x, AwkValue.FromNumber(5));
        state.PushFunctionFrame(function.LocalLayout,
            [AwkFunctionLocalSlot.ArrayAlias(local), AwkFunctionLocalSlot.PreparedKey(new("key"))]);
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        storage.Initialize(region);
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            try
            {
                if (hidden)
                {
                    using var bindings = state.EnterGlobalBindings();
                    await region.ExecuteAsync(state, storage, regionOutput, regionIo);
                }
                else await region.ExecuteAsync(state, storage, regionOutput, regionIo);
                Assert.Equal(hidden ? 9 : 7, state.ResolveVariableValue(x).AsNumber());
                Assert.Equal(0, memory.Budget.Refusals);
            }
            finally { state.PopFunctionFrame(); }
        }
        storage.Dispose();
        regionOutput.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task PreparedArrayWritesRefuseBeforeMutation(int operation)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            var counters = AwkRuntimeCounters.CreateEnabled();
            var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner: owner);
            var name = AwkArrayName.FromText("a");
            state.Arrays.SetAwkValue(name, new("keep"), AwkValue.FromNumber(7));
            var body = operation switch
            {
                1 => "a[\"missing\"]+=1",
                2 => "a[\"missing\"]++",
                3 => "--a[\"missing\"]",
                _ => "a[\"missing\"]=99"
            };
            var syntax = AwkTestHarness.ParseSource("BEGIN{" + body + "}");
            Assert.True(AwkTestHarness.TryPrepareRule(Assert.Single(syntax.Syntax.Rules), AwkLoweringScope.Global, -1, out var region));
            using var storage = new AwkRegionStorage(state);
            using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
            var regionIo = AwkTestHarness.CreateRegionIo(state);
            using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
            storage.Initialize(region);
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await region.ExecuteAsync(state, storage, regionOutput, regionIo));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.True(budget.Refusals > 0);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
            Assert.False(state.Arrays.ContainsValue(name, new("missing")));
            Assert.Equal(7, state.Arrays.GetAwkValue(name, new("keep")).AsNumber());
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FieldArraySharedAdmissionRefusalStopsWithoutRetry(bool firstExists)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var program = Bind("BEGIN{total+=a[$1]+b[$1]}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = State(counters, limits, owner: owner);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var a = AwkArrayName.FromText("a"); var b = AwkArrayName.FromText("b");
        state.Arrays.SetAwkValue(a, new("hit"), AwkValue.FromNumber(7));
        state.Arrays.SetAwkValue(b, new("hit"), AwkValue.FromNumber(3));
        if (firstExists) _ = state.Arrays.GetAwkValue(a, new("missing"));
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(4));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("missing"));
        _ = state.RecordFields.GetFieldValue(new(1)); // Ensure the refused growth belongs to the array read.
        var host = new MockFileSystem(); var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting);
        var region = Assert.Single(program.BeginActions);
        using var scratch = new AwkRegionStorage(state);
        scratch.Prepare(region.Layout);
        using var execution = state.PushExecutionContext(output, io);
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
            await region.ExecuteAsync(state, scratch, output, io));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(4, state.ResolveVariableValue(total).AsNumber());
        Assert.Equal(firstExists, state.Arrays.ContainsValue(a, new("missing")));
        Assert.False(state.Arrays.ContainsValue(b, new("missing")));

        await io.CloseAsync();
    }

    [Theory]
    [InlineData("default")]
    [InlineData("pending")]
    [InlineData("captured-regex")]
    public async Task FieldArrayActionsKeepOriginalPolicyAndFieldBoundaries(string boundary)
    {
        var program = Bind("BEGIN{total+=a[$1]+b[$(1+0)]}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = State(counters, limits);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var fs = AwkVariableName.FromText("FS");
        if (boundary == "captured-regex") state.AssignVariable(fs, "[,;]");
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit"));
        state.AssignVariable(fs, " ");
        state.Arrays.SetAwkValue(AwkArrayName.FromText("a"), new("hit"), AwkValue.FromNumber(7));
        state.Arrays.SetAwkValue(AwkArrayName.FromText("b"), new("hit"), AwkValue.FromNumber(3));
        var host = new MockFileSystem();
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output);
        Assert.Equal(10, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());

        Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FieldArrayActionsPreserveState(bool missing)
    {
        var result = await RunFieldArrayAction(missing);
        Assert.Equal(missing ? 4 : 14, result.Total);
        Assert.Equal(missing, result.FirstMiss);
        Assert.Equal(missing, result.SecondMiss);
        Assert.Equal("", result.Error);
    }

    private static async Task<FieldArrayResult> RunFieldArrayAction(bool missing)
    {
        var program = Bind("BEGIN{total+=a[$1]+b[$1]}");
        var action = Assert.Single(program.BeginActions);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = State(counters, limits);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var a = AwkArrayName.FromText("a"); var b = AwkArrayName.FromText("b");
        state.Arrays.SetAwkValue(a, new("hit"), AwkValue.FromNumber(7));
        state.Arrays.SetAwkValue(b, new("hit"), AwkValue.FromNumber(3));
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(4));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(missing ? "missing" : "hit"));
        var host = new MockFileSystem(); var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting);
        var error = "";
        try { await action.ExecuteAsync(state, io, output); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        var result = new FieldArrayResult(state.ResolveVariableValue(total).AsNumber(), state.Arrays.ContainsValue(a, new("missing")), state.Arrays.ContainsValue(b, new("missing")), error);
        await io.CloseAsync();
        return result;
    }

    [Fact]
    public async Task FieldArrayActionMissingRefusalKeepsBothArraysAndScalar()
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var program = Bind("BEGIN{total+=a[$1]+b[$1]}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = State(counters, limits, owner: owner);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var a = AwkArrayName.FromText("a"); var b = AwkArrayName.FromText("b");
        state.Arrays.SetAwkValue(a, new("hit"), AwkValue.FromNumber(7));
        state.Arrays.SetAwkValue(b, new("hit"), AwkValue.FromNumber(3));
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(4));
        var record = AwkText.FromSourceLiteralString("missing");
        state.SetCurrentRecord(record);
        var host = new MockFileSystem(); var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting);
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
            await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(4, state.ResolveVariableValue(total).AsNumber());
        Assert.Equal(7, state.Arrays.GetAwkValue(a, new("hit")).AsNumber());
        Assert.Equal(3, state.Arrays.GetAwkValue(b, new("hit")).AsNumber());
        Assert.False(state.Arrays.ContainsValue(a, new("missing")));
        Assert.False(state.Arrays.ContainsValue(b, new("missing")));
        Assert.True(record.HasSameStorage(state.CurrentRecordByteText));
        await io.CloseAsync();
    }

    private readonly record struct FieldArrayResult(double Total, bool FirstMiss, bool SecondMiss, string Error);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CapturedArrayValueSurvivesPendingKeyOutputAndCancellation(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(4 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stdout = "";
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (_, bytes, _) =>
            {
                var captured = bytes.ToArray();
                stdout += AwkUtf8Codec.Decode(bytes.Span);
                if (stdout == "held\n")
                {
                    entered.TrySetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.True(bytes.Span.SequenceEqual(captured));
                }
                return AwkAppendResult.Accepted();
            }
        };
        var script = "function key(){print \"held\";original=\"changed\";return \"k\"}" +
            "BEGIN{a[key()]=original;print length(a[\"k\"])}";
        var invocation = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["-v", "original=" + new string('x', 8192), script], []);
        var run = AwkTestHarness.ParseAwk(invocation).ExecuteAsync(host,
            AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.True(budget.Reserved > 0);
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(0, (await run).Value);
            Assert.Equal(cancel ? "held\n" : "held\n8192\n", stdout);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task StandaloneKeyReadsKeepVisibleBindingsAtFullBudget(bool governed, bool hidden)
    {
        using var memory = new CooperativeTestMemory(4 << 20, enabled: governed);
        memory.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var program = Bind("function f(k){x=a[k]}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var function = Assert.Single(program.Functions).Value;
        var region = Assert.IsType<AwkRegion>(function.Region);
        var name = AwkArrayName.FromText("a");
        var x = AwkVariableName.FromText("x");
        state.Arrays.SetAwkValue(name, new("local"), AwkValue.FromNumber(7));
        state.Arrays.SetAwkValue(name, new("global"), AwkValue.FromNumber(9));
        state.AssignVariableValue(AwkVariableName.FromText("k"), AwkValue.FromText("global"));
        state.AssignVariableValue(x, AwkValue.FromNumber(5));
        state.PushFunctionFrame(function.LocalLayout, [AwkFunctionLocalSlot.PreparedKey(new("local"))]);
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        storage.Initialize(region);
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            try
            {
                if (hidden)
                {
                    using var bindings = state.EnterGlobalBindings();
                    await region.ExecuteAsync(state, storage, regionOutput, regionIo);
                }
                else await region.ExecuteAsync(state, storage, regionOutput, regionIo);
                Assert.Equal(hidden ? 9 : 7, state.ResolveVariableValue(x).AsNumber());
                Assert.Equal(0, memory.Budget.Refusals);
            }
            finally { state.PopFunctionFrame(); }
        }
        storage.Dispose();
        regionOutput.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task NumericArrayReadsAtFullBudgetPreserveOwnedSpelling(bool nested, bool returned, bool numericString)
    {
        await ReadFundedArraySpelling(nested, returned, numericString, owning: false);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwningNestedArrayReadsStillRefuseBeforeChangingTheirDestination(bool returned)
    {
        await ReadFundedArraySpelling(nested: true, returned, numericString: false, owning: true);
    }

    private static async Task ReadFundedArraySpelling(bool nested, bool returned, bool numericString, bool owning)
    {
        var budget = new CooperativeTestBudget(8L << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner: owner);
        var read = nested ? "a[k][\"v\"]" : "a[k]";
        var expression = owning ? read : read + "+0";
        var program = Bind(returned
            ? "function read(a,k){print \"ready\";return " + expression + "}BEGIN{x=read(a,\"k\")}"
            : "BEGIN{k=\"k\";print \"ready\";x=" + expression + "}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var x = AwkVariableName.FromText("x");
        state.AssignVariableValue(x, AwkValue.FromNumber(5));
        var spelling = numericString ? "0007" : "0007" + new string('x', 256);
        var original = numericString ? AwkTestHarness.NumericInput(spelling) : AwkValue.FromText(spelling);
        var root = AwkArrayName.FromText("a");
        var child = nested ? state.Arrays.GetSubarray(root, new("k"), default) : null;
        if (child is not null) state.Variables.SetArrayValue(child, new("v"), original);
        else state.Arrays.SetAwkValue(root, new("k"), original);
        var host = new MockFileSystem();
        var clamped = false;
        host.AfterAppend = () =>
        {
            if (clamped) return;
            Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
            budget.SetAvailableBytes(0);
            clamped = true;
        };
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting);
        try
        {
            if (owning)
            {
                var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                    await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
                Assert.Equal(5, state.ResolveVariableValue(x).AsNumber());
            }
            else
            {
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output);
                Assert.Equal(7, state.ResolveVariableValue(x).AsNumber());
            }
            Assert.True(clamped);
            Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        var stored = child is not null ? state.Variables.GetArrayValue(child, new AwkArrayKey("v"))
                : state.Arrays.GetAwkValue(root, new AwkArrayKey("k"), default);
            Assert.Equal(spelling, stored.AsString());
            Assert.Equal(numericString ? AwkRuntimeValueKind.NumericString : AwkRuntimeValueKind.String, stored.RuntimeKind);
            Assert.False(state.IsInsideFunction);
        }
        finally
        {
            host.AfterAppend = null;
            await io.CloseForCleanupAsync();
            output.Dispose();
            state.Dispose();
            await owner.DisposeAsync();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }
}
