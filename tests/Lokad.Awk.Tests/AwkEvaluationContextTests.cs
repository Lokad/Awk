namespace Lokad.Awk.Tests;

public sealed class AwkEvaluationContextTests
{
    public static IEnumerable<object[]> NestedBindingCases()
    {
        foreach (var finite in new[] { false, true })
        foreach (var canceled in new[] { false, true })
        foreach (var sameOutput in new[] { false, true })
        foreach (var sameIo in new[] { false, true })
            yield return [finite, canceled, sameOutput, sameIo];
    }

    [Theory]
    [MemberData(nameof(NestedBindingCases))]
    public async Task PendingFailureRestoresTheEnclosingContext(bool finite, bool canceled, bool sameOutput, bool sameIo)
    {
        using var memoryForState = CreateState(finite);
        var state = memoryForState.State;
        var firstIo = CreateIo(state);
        var secondIo = CreateIo(state);
        using var firstOutput = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        using var secondOutput = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        using (state.PushExecutionContext(firstOutput, firstIo))
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var innerOutput = sameOutput ? firstOutput : secondOutput;
            var innerIo = sameIo ? firstIo : secondIo;
            var pending = AwaitInScopeAsync(state, innerOutput, innerIo, completion.Task);
            Assert.False(pending.IsCompleted);
            Assert.True(state.TryGetExecutionContext(out var active));
            Assert.Same(innerOutput, active.Output);
            Assert.Same(innerIo, active.Io);
            using (state.PushExecutionContext(firstOutput, firstIo))
            {
                Assert.True(state.TryGetExecutionContext(out var nested));
                Assert.Same(firstOutput, nested.Output);
                Assert.Same(firstIo, nested.Io);
            }
            Assert.True(state.TryGetExecutionContext(out var resumed));
            Assert.Same(innerOutput, resumed.Output);
            Assert.Same(innerIo, resumed.Io);
            if (canceled)
            {
                completion.SetCanceled();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
                Assert.True(pending.IsCanceled);
            }
            else
            {
                completion.SetException(new InvalidOperationException("pending failure"));
                var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
                Assert.Equal("pending failure", failure.Message);
            }
            Assert.True(state.TryGetExecutionContext(out var restored));
            Assert.Same(firstOutput, restored.Output);
            Assert.Same(firstIo, restored.Io);
        }
        Assert.False(state.TryGetExecutionContext(out _));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await firstIo.CloseAsync();
        await secondIo.CloseAsync();
    }

    private static async Task AwaitInScopeAsync(AwkRuntimeState state, AwkOutputSink output, AwkIo io, Task completion)
    {
        using var scope = state.PushExecutionContext(output, io);
        await completion.ConfigureAwait(false);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExplicitBindingsAreSnapshotsWhileAmbientBindingsFollowScopes(bool explicitBindings, bool finite)
    {
        using var memoryForState = CreateState(finite);
        var state = memoryForState.State;
        var firstIo = CreateIo(state);
        var secondIo = CreateIo(state);
        using var firstOutput = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        using var secondOutput = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        AwkEvaluationContext context;
        using (state.PushExecutionContext(firstOutput, firstIo))
        {
            context = explicitBindings ? new(state, firstOutput, firstIo) : new(state);
            Assert.Same(state, context.State);
            Assert.True(context.TryGetExecutionContext(out var before));
            Assert.Same(firstOutput, before.Output);
            Assert.Same(firstIo, before.Io);
            var allow = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = ResolveAfterAsync(context, allow.Task);
            Assert.False(pending.IsCompleted);
            using (state.PushExecutionContext(secondOutput, secondIo))
            {
                allow.SetResult();
                var observed = await pending.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Same(explicitBindings ? firstOutput : secondOutput, observed.Output);
                Assert.Same(explicitBindings ? firstIo : secondIo, observed.Io);
            }
            Assert.True(context.TryGetExecutionContext(out var restored));
            Assert.Same(firstOutput, restored.Output);
            Assert.Same(firstIo, restored.Io);
        }
        Assert.Equal(explicitBindings, context.TryGetExecutionContext(out var after));
        if (explicitBindings)
        {
            Assert.Same(firstOutput, after.Output);
            Assert.Same(firstIo, after.Io);
        }
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await firstIo.CloseAsync();
        await secondIo.CloseAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StateOnlyContextWithoutAnActiveScopeRemainsEmpty(bool finite)
    {
        using var memoryForState = CreateState(finite);
        var state = memoryForState.State;
        var context = new AwkEvaluationContext(state);
        Assert.Same(state, context.State);
        Assert.False(context.TryGetExecutionContext(out _));
    }

    [Fact]
    public void DefaultContextRetainsItsLocatedInternalFailure()
    {
        var context = default(AwkEvaluationContext);
        var stateError = Assert.Throws<InvalidOperationException>(() => context.State);
        Assert.Equal("The default evaluation context has no runtime state.", stateError.Message);
        var lookupError = Assert.Throws<InvalidOperationException>(() => context.TryGetExecutionContext(out _));
        Assert.Equal(stateError.Message, lookupError.Message);
    }

    private static async Task<AwkExecutionContext> ResolveAfterAsync(AwkEvaluationContext context, Task allow)
    {
        await allow.ConfigureAwait(false);
        if (context.TryGetExecutionContext(out var execution)) return execution;
        throw new InvalidOperationException("Expected a live test execution context.");
    }

    private static CooperativeTestMemory CreateState(bool finite)
    {

        var testMemory = new CooperativeTestMemory(1 << 20, enabled: finite);
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), limits, regexEngine: AwkUtf8RegexEngine.Instance);
        return testMemory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options)));
    }

    private static AwkIo CreateIo(AwkRuntimeState state) =>
        new(new MockFileSystem(), AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, CancellationToken.None, state.Counters, state.Limits, state, static _ => []);
}
