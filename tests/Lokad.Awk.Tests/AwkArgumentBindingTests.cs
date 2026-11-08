namespace Lokad.Awk.Tests;

public sealed class AwkArgumentBindingTests
{
    [Fact]
    public void GlobalBindingKeepsUntouchedOriginsAndSymbolTableChanges()
    {
        using var fixture = State();
        var state = fixture.State;
        var name = AwkVariableName.FromText("x");
        state.InitializeGlobalReadSlots(1);
        Assert.False(state.HasScalar(name));
        Assert.True(state.TryResolveArrayArgument(name, out var untouched));
        Assert.Equal(AwkFunctionLocalSlotKind.PotentialArrayAlias, untouched.Kind);
        Assert.Equal(new AwkArrayName(name.Identifier), untouched.ArrayName);
        Assert.True(untouched.Value.IsUntyped);
        state.AssignVariableValue(name, AwkValue.FromText("0007"));
        state.ResolvePreparedGlobalValue(name, 0);
        Assert.False(state.TryResolveArrayArgument(name, out _));
        state.SetArrayString(AwkArrayName.FromText("SYMTAB"), new AwkArrayKey("x"), "0009");
        Assert.False(state.TryResolveArrayArgument(name, out _));
        Assert.Equal("0009", state.ResolvePreparedGlobalValue(name, 0).AsString());
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void HiddenCallerSlotFallsBackToTheGlobalBindingAndThenRestores()
    {
        using var fixture = State();
        var state = fixture.State;
        var name = AwkVariableName.FromText("p");
        var global = new AwkArrayName(name.Identifier);
        state.SetArrayString(global, new AwkArrayKey("key"), "global");
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(42))]);
        try
        {
            Assert.Equal(42, BindLocalArgument(state, new(0), name).Value.ToValue(AwkValue.FromNumericString, AwkValue.FromText).AsNumber());
            using (state.EnterGlobalBindings())
            {
                var argument = BindLocalArgument(state, new(0), name);
                Assert.Equal(AwkFunctionLocalSlotKind.ArrayAlias, argument.Kind);
                Assert.Equal(global, argument.ArrayName);
                Assert.Equal(0, state.Counters.IndexedLocalArrayArguments);
            }
            Assert.Equal(42, BindLocalArgument(state, new(0), name).Value.ToValue(AwkValue.FromNumericString, AwkValue.FromText).AsNumber());
            Assert.Equal(42, state.ResolveVariableValue(name).AsNumber());
        }
        finally { state.PopFunctionFrame(); }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void InvalidLocalOrdinalUsesNameFallbackWithoutChangingTheAlias()
    {
        using var fixture = State();
        var state = fixture.State;
        var name = AwkVariableName.FromText("p");
        var array = AwkArrayName.FromText("values");
        state.SetArrayString(array, new AwkArrayKey("key"), "kept");
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.ArrayAlias(array)]);
        try
        {
            var argument = BindLocalArgument(state, new(99), name);
            Assert.Equal(AwkFunctionLocalSlotKind.ArrayAlias, argument.Kind);
            Assert.Equal(array, argument.ArrayName);
            Assert.Equal("kept", state.GetArrayAwkValue(array, new AwkArrayKey("key")).AsString());
            Assert.Equal(0, state.Counters.IndexedLocalArrayArguments);
        }
        finally { state.PopFunctionFrame(); }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    private static AwkFunctionLocalSlot BindLocalArgument(AwkRuntimeState state, AwkFunctionLocalSlotId source,
        AwkVariableName name)
    {
        var incoming = state.PrepareFunctionFrame(new([AwkVariableName.FromText("argument")]));
        try
        {
            state.StageFunctionLocalArgument(source, name, incoming, 0, allowArray: true, prepareKey: false);
            return incoming.GetSlot(0);
        }
        finally { state.DiscardIncomingFunctionFrame(incoming); }
    }

    [Fact]
    public void ReferencedArgumentCapturesBeforeAdmissionCanChangeItsSource() =>
        VerifyArgumentCaptureBeforeAdmission(preparedLocal: false);

    [Fact]
    public void PreparedLocalArgumentCapturesBeforeAdmissionCanChangeItsSource() =>
        VerifyArgumentCaptureBeforeAdmission(preparedLocal: true);

    private static void VerifyArgumentCaptureBeforeAdmission(bool preparedLocal)
    {
        using var fixture = State();
        var state = fixture.State;
        var name = AwkVariableName.FromText("p");
        var spelling = new string('x', 4096);
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromText(spelling))]);
        var caller = state.CurrentFunctionFrame;
        var incoming = state.PrepareFunctionFrame(new([name]));
        try
        {
            var callbacks = 0;
            fixture.Budget.OnGrowth = _ =>
            {
                fixture.Budget.OnGrowth = null;
                callbacks++;
                // Keep the caller's funding until retirement. Change only its
                // already-funded cell, without reentering admission bookkeeping.
                caller.GetSlot(0) = AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(17));
            };
            ref var source = ref state.CurrentFunctionFrame.GetSlot(0);
            if (preparedLocal) state.StageFunctionLocalArgument(new(0), name, incoming, 0, allowArray: true, prepareKey: false);
            else state.StageFunctionArgument(incoming, 0, in source);
            Assert.Equal(1, callbacks);
            Assert.Equal(spelling, incoming.GetSlot(0).Value.ToValue(AwkValue.FromNumericString, AwkValue.FromText).AsString());
            Assert.Equal(17, state.ResolveVariableValue(name).AsNumber());
        }
        finally
        {
            fixture.Budget.OnGrowth = null;
            state.DiscardIncomingFunctionFrame(incoming);
            state.PopFunctionFrame();
        }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    public static IEnumerable<object[]> PendingReferenceBindings()
    {
        foreach (var shape in new[] { "global", "local", "array" })
        foreach (var cancel in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [shape, cancel, governed];
    }

    [Theory]
    [MemberData(nameof(PendingReferenceBindings))]
    public async Task EarlierBindingsSurviveActualPendingGetlineAndCancellation(string shape, bool cancel, bool governed)
    {
        var payload = new string('x', 4096);
        var scalar = "function leaf(a,b){switch(0){default:break}print a,b;return 1}";
        var source = shape switch
        {
            "global" => scalar + "BEGIN{x=\"" + payload + "\";print leaf(x,(getline x));print x}",
            "local" => scalar + "function wrap(x){return leaf(x,(getline x))}BEGIN{x=\"" + payload + "\";print wrap(x);print x}",
            _ => "function leaf(a,b){switch(0){default:break}print a[\"key\"],b;return 1}function wrap(a){return leaf(a,(getline a[\"key\"]))}BEGIN{values[\"key\"]=\"" + payload + "\";print wrap(values);print values[\"key\"]}"
        };
        var host = new MockFileSystem();
        host.SetStandardInput("replacement\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationSeen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            using var registration = token.Register(() => cancellationSeen.TrySetResult());
            entered.TrySetResult();
            await resume.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, token).ConfigureAwait(false);
        }
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var execution = awk.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(execution.IsCompleted);
            var held = budget.Reserved;
            if (governed) Assert.True(held >= payload.Length);
            if (cancel)
            {
                cancellation.Cancel();
                await cancellationSeen.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(execution.IsCompleted);
                Assert.Equal(held, budget.Reserved);
            }
            resume.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            else Assert.Equal(AwkExitCode.Success, await execution.WaitAsync(TimeSpan.FromSeconds(10)));
            var expected = shape switch
            {
                "global" => payload + " 1\n1\nreplacement\n",
                "local" => payload + " 1\n1\n" + payload + "\n",
                _ => "replacement 1\n1\nreplacement\n"
            };
            Assert.Equal(cancel ? "" : expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            resume.TrySetResult();
            try { await execution.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) { }
        }
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task PreparedFieldArgumentsSurvivePendingRecordReplacement(bool governed, bool cancel)
    {
        const string source = "function show(a,b,c,d){print a,b,c,d}BEGIN{print \"before\";$0=\"warm 0000\";warm=$2}{show($1,$2,getline,$2)}";
        var host = new MockFileSystem();
        host.SetStandardInput("old 0007\nnew 0011\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationSeen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            if (++reads == 1)
            {
                host.ReadAsyncOverride = null;
                var first = await host.ReadAsync(descriptor, 9, token).ConfigureAwait(false);
                host.ReadAsyncOverride = Read;
                return first;
            }
            using var registration = token.Register(() => cancellationSeen.TrySetResult());
            entered.TrySetResult();
            await resume.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        }
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var execution = awk.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(execution.IsCompleted);
            Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
            var held = budget.Reserved;
            if (governed) Assert.True(held > 0);
            if (cancel)
            {
                cancellation.Cancel();
                await cancellationSeen.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(execution.IsCompleted);
                Assert.Equal(held, budget.Reserved);
            }
            resume.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            else Assert.Equal(AwkExitCode.Success, await execution.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(cancel ? "before\n" : "before\nold 0007 1 0011\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            resume.TrySetResult();
            try { await execution.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) { }
        }
    }

    private static CooperativeTestMemory State()
    {
        var fixture = new CooperativeTestMemory(4 << 20);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault());
        return fixture.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, fixture.Configure(options)));
    }
}
