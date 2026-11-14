namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkGlobalUpdateOwnershipTests
{
    private const int Limit = 1 << 20;

    [Fact]
    public void ResolvedReadTargetsFitTheirAdmittedBacking()
    {
        GC.KeepAlive(new AwkResolvedGetlineTarget[1]);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var targets = new AwkResolvedGetlineTarget[257];
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.InRange(bytes, 0, AwkRuntimeMemory.EstimateGetlineTargetArrayBytes(targets.Length));
        GC.KeepAlive(targets);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedExistingSlotWriteRetainsValueAndCharge(bool vector)
    {
        using var memoryForState = State(vector);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("value");
        using var reference = AwkResolvedReference.PreparedGlobalScalar(name, 0);
        reference.AssignValue(state, AwkValue.FromNumber(7));
        Assert.Equal(7, reference.Evaluate(state).AsNumber());
        var slot = state.GetOrCreateGlobalScalarSlot(name);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - before))
        {
            var text = AwkValue.FromText(new string('x', 1024));
            var error = Assert.Throws<AwkRuntimeException>(() => reference.AssignValue(state, text));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(7, state.GetGlobalScalarValue(slot).AsNumber());
            Assert.Equal(Limit, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Throws<AwkRuntimeException>(() => state.AssignVariableValue(slot, text));
            Assert.Equal(7, state.GetGlobalScalarValue(slot).AsNumber());
        }
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        reference.AssignValue(state, AwkValue.FromNumber(9));
        Assert.Equal(9, state.GetGlobalScalarValue(slot).AsNumber());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArrayAdmissionBeforeFirstUseRetainsArrayAndRejectsUpdates(bool vector)
    {
        using var memoryForState = State(vector);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("value");
        using var reference = AwkResolvedReference.PreparedGlobalScalar(name, 0);
        var array = new AwkArrayName(name.Identifier);
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayString(array, key, "kept");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var read = Assert.Throws<AwkRuntimeException>(() => reference.Evaluate(state));
        var write = Assert.Throws<AwkRuntimeException>(() => reference.AssignValue(state, AwkValue.FromNumber(7)));
        Assert.Equal("awk: attempt to use array `value' in a scalar context\n", AwkUtf8Codec.Decode(read.MessageBytes));
        Assert.Equal(AwkUtf8Codec.Decode(read.MessageBytes), AwkUtf8Codec.Decode(write.MessageBytes));
        Assert.Equal("kept", state.GetArrayByteText(array, key).AsString());
        Assert.False(state.HasScalar(name));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Fact]
    public async Task LocalUpdatePreservesSameNamedGlobalThroughPublicExecution()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "function bump(value){return ++value}BEGIN{value=7;print bump(20),value}"));
        Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None));
        Assert.Equal("21 7\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public async Task CompiledUpdatesRetainExecutionOwnershipWithPendingHostsAndLocalShadowing()
    {
        var source = new MockFileSystem();
        source.AddFile("/script", "function bump(value){value+=1;total+=value;return value} {SYMTAB[$1]=$2;value=$2+0;value+=bump(1);total+=value} END{print total,value}");
        var compiled = await AwkExecutor.CompileAsync(source, AwkTestHarness.BuildInvocation("awk", "-f", "/script", "/data"), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        async Task RunAsync(string input, string expected, bool pending)
        {
            var host = new MockFileSystem();
            host.AddFile("/data", input);
            if (pending) host.ReadAsyncOverride = ReadAsync;
            async ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
            {
                if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
                await ready.Task.WaitAsync(token).ConfigureAwait(false);
                host.ReadAsyncOverride = null;
                try { return await host.ReadAsync(descriptor, count, token).ConfigureAwait(false); }
                finally { host.ReadAsyncOverride = ReadAsync; }
            }
            var exit = await compiled.ExecuteAsync(host, deadline.Token, AwkTestHarness.CreateRuntimeOptions());
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal(expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
        }
        await Task.WhenAll(RunAsync("x 10\ny 20\n", "38 22\n", true), RunAsync("padding 99\ny 40\nx 30\n", "181 32\n", true));
        await RunAsync("y 7\nx 9\n", "24 11\n", false);
    }

    private static CooperativeTestMemory State(bool vector)
    {

        var testMemory = new CooperativeTestMemory(Limit);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options));
        if (vector) state.InitializeGlobalReadSlots(1);
        return testMemory.Initialize(() => state);
    }

}
