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
        Assert.Equal(AwkFunctionArgumentKind.PotentialArray, untouched.Kind);
        Assert.Equal(new AwkArrayName(name.Identifier), untouched.ArrayName);
        Assert.True(untouched.Value.IsUntypedValue);
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
        state.PushFunctionFrame(new([name]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        try
        {
            Assert.False(state.TryResolveArrayArgument(new(0), name, out _));
            using (state.EnterGlobalBindings())
            {
                Assert.True(state.TryResolveArrayArgument(new(0), name, out var argument));
                Assert.Equal(AwkFunctionArgumentKind.Array, argument.Kind);
                Assert.Equal(global, argument.ArrayName);
                Assert.Equal(0, state.Counters.IndexedLocalArrayArguments);
            }
            Assert.False(state.TryResolveArrayArgument(new(0), name, out _));
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
        state.PushFunctionFrame(new([name]), [AwkFunctionArgument.Array(array)]);
        try
        {
            Assert.True(state.TryResolveArrayArgument(new(99), name, out var argument));
            Assert.Equal(AwkFunctionArgumentKind.Array, argument.Kind);
            Assert.Equal(array, argument.ArrayName);
            Assert.Equal("kept", state.GetArrayAwkValue(array, new AwkArrayKey("key")).AsString());
            Assert.Equal(0, state.Counters.IndexedLocalArrayArguments);
        }
        finally { state.PopFunctionFrame(); }
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

    private static CooperativeTestMemory State()
    {
        var fixture = new CooperativeTestMemory(4 << 20);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault());
        return fixture.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, fixture.Configure(options)));
    }
}
