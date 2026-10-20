using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

public sealed class AwkPreparedGlobalReadTests
{
    [Fact]
    public void LayoutCopiesNamesAndExcludesSpecials()
    {
        var name = AwkVariableName.FromText("value");
        var names = new HashSet<AwkVariableName> { name, AwkVariableName.FromText("FS") };
        var layout = new AwkGlobalReadLayout(names, null);
        names.Clear();
        Assert.Equal(1, layout.Count);
        Assert.True(layout.TryGetIndex(name, out var index));
        Assert.Equal(0, index);
        Assert.False(layout.TryGetIndex(AwkVariableName.FromText("FS"), out _));
    }

    [Fact]
    public void SlotVectorIsAdmittedWithoutCreatingScalarsAndFirstReadStaysLazy()
    {
        using var memory = State(CancellationToken.None);
        var state = memory.State;
        var counters = state.Counters;
        var name = AwkVariableName.FromText("missing");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.Equal(4, Unsafe.SizeOf<int>());
        state.InitializeGlobalReadSlots(3);
        Assert.Equal(24 + 3 * 4, state.MemoryAccounting.CurrentCommittedBytes - before);
        Assert.False(state.HasScalar(name));
        var admitted = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.True(state.ResolvePreparedGlobalValue(name, 1).IsUnassignedValue);
        Assert.True(state.HasScalar(name));
        Assert.Equal(AwkRuntimeMemory.EstimateSymbolMetadataBytes(name.Text.Length) + 24,
            state.MemoryAccounting.CurrentCommittedBytes - admitted);
        var typed = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.True(state.ResolvePreparedGlobalValue(name, 1).IsUnassignedValue);
        Assert.Equal(typed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Fact]
    public void FailedVectorAdmissionPublishesNothing()
    {
        const int limit = 1 << 20;
        using var memory = State(CancellationToken.None);
        var state = memory.State;
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(limit - before))
        {
            var pressured = state.MemoryAccounting.CurrentCommittedBytes;
            Assert.Throws<AwkRuntimeException>(() => state.InitializeGlobalReadSlots(3));
            Assert.Equal(pressured, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        // With no vector published, legacy evaluation ignores an ordinal.
        Assert.True(state.ResolvePreparedGlobalValue(AwkVariableName.FromText("missing"), 99).IsUnassignedValue);
    }

    [Fact]
    public void CanceledVectorPreparationDoesNotChargeOrPublish()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(cancellation.Token);
        var state = memory.State;
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => state.InitializeGlobalReadSlots(3));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void ArrayReadFailureDoesNotCacheAReplacementScalar()
    {
        using var memory = State(CancellationToken.None);
        var state = memory.State;
        var name = AwkVariableName.FromText("values");
        var array = new AwkArrayName(name.Identifier);
        state.SetArrayString(array, AwkArrayKey.FromOrdinal(1), "kept");
        state.InitializeGlobalReadSlots(1);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.Throws<AwkRuntimeException>(() => state.ResolvePreparedGlobalValue(name, 0));
        Assert.Throws<AwkRuntimeException>(() => state.ResolvePreparedGlobalValue(name, 0));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal("kept", state.GetArrayAwkValue(array, AwkArrayKey.FromOrdinal(1)).AsString());
        Assert.False(state.HasScalar(name));
    }

    [Fact]
    public async Task CompiledReadsOwnSlotsAcrossPendingConcurrentAndRepeatedExecutions()
    {
        var source = new MockFileSystem();
        source.AddFile("/script", "{SYMTAB[$1]=$2;seen+=x+0} END{print seen+0,x+0,y+0}");
        var compiled = await AwkExecutor.CompileAsync(source,
            AwkTestHarness.BuildInvocation("awk", "-f", "/script", "/data"), CancellationToken.None);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        async Task RunAsync(string input, string expected, bool pending)
        {
            var host = new MockFileSystem();
            host.AddFile("/data", input);
            if (pending) host.ReadLinesAsyncOverride = ReadAsync;
            async Task<AwkReadResult> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
            {
                if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
                await ready.Task.WaitAsync(token).ConfigureAwait(false);
                host.ReadLinesAsyncOverride = null;
                try { return await host.ReadLinesAsync(descriptor, count, token).ConfigureAwait(false); }
                finally { host.ReadLinesAsyncOverride = ReadAsync; }
            }
            var exit = await compiled.ExecuteAsync(host, deadline.Token);
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal(expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
        }
        await Task.WhenAll(RunAsync("x 10\ny 20\n", "20 10 20\n", true),
            RunAsync("padding 99\ny 40\nx 30\n", "30 30 40\n", true));
        await RunAsync("y 7\nx 9\n", "9 9 7\n", false);
    }

    private static CooperativeTestMemory State(CancellationToken token)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault());
        var memory = new CooperativeTestMemory(1 << 20, counters);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, memory.Configure(options)));
    }
}
