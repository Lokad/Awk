namespace Lokad.Awk.Tests;

public sealed class AwkGlobalUpdateOwnershipTests
{
    private const int Limit = 1 << 20;

    [Fact]
    public void ResolvedTargetRetainsItsManagedLayout() =>
        Assert.Equal(72, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkResolvedReference>());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FirstAssignmentWaitsForRhsAndFailedAdmissionCreatesNothing(bool vector)
    {
        var state = State(vector);
        var name = AwkVariableName.FromText("value");
        var source = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var expression = AwkExpression.Assignment(Reference(name), new PendingExpression(source.Task));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var result = expression.EvaluateAsync(new AwkEvaluationContext(state)).AsTask();
        Assert.False(result.IsCompleted);
        Assert.False(state.HasScalar(name));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - before))
        {
            source.SetResult(AwkValue.FromNumber(7));
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => result);
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.False(state.HasScalar(name));
            Assert.Equal(Limit, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        await Reference(name).AssignValueAsync(new AwkEvaluationContext(state), AwkValue.FromNumber(9));
        Assert.Equal(9, state.ResolveVariableValue(name).AsNumber());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompoundReadPrecedesPendingRhsAndSymbolTableEffects(bool vector)
    {
        var state = State(vector);
        var name = AwkVariableName.FromText("value");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var reference = Reference(name);
        var context = new AwkEvaluationContext(state);
        var source = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = reference.ApplyCompoundAssignmentAsync(context, new PendingExpression(source.Task), AwkCompoundAssignmentOperation.Add).AsTask();
        Assert.False(result.IsCompleted);
        state.SetArrayAwkValue(AwkArrayName.FromText("SYMTAB"), new AwkArrayKey("value"), AwkValue.FromNumber(100));
        source.SetResult(AwkValue.FromNumber(3));
        Assert.Equal(10, (await result).AsNumber());
        Assert.Equal(10, (await reference.EvaluateAsync(context)).AsNumber());
        var slot = state.GetOrCreateGlobalScalarSlot(name);
        Assert.Equal(10, state.GetGlobalScalarValue(slot).AsNumber());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedExistingSlotWriteRetainsValueAndCharge(bool vector)
    {
        var state = State(vector);
        var name = AwkVariableName.FromText("value");
        var reference = Reference(name);
        var context = new AwkEvaluationContext(state);
        await reference.AssignValueAsync(context, AwkValue.FromNumber(7));
        Assert.Equal(7, (await reference.EvaluateAsync(context)).AsNumber());
        var slot = state.GetOrCreateGlobalScalarSlot(name);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - before))
        {
            var text = AwkValue.FromText(new string('x', 1024));
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => reference.AssignValueAsync(context, text).AsTask());
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(7, state.GetGlobalScalarValue(slot).AsNumber());
            Assert.Equal(Limit, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Throws<AwkRuntimeException>(() => state.AssignVariableValue(slot, text));
            Assert.Equal(7, state.GetGlobalScalarValue(slot).AsNumber());
        }
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        await reference.AssignValueAsync(context, AwkValue.FromNumber(9));
        Assert.Equal(9, state.GetGlobalScalarValue(slot).AsNumber());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ArrayAdmissionBeforeFirstUseRetainsArrayAndRejectsUpdates(bool vector)
    {
        var state = State(vector);
        var name = AwkVariableName.FromText("value");
        var reference = Reference(name);
        var array = new AwkArrayName(name.Identifier);
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayString(array, key, "kept");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var context = new AwkEvaluationContext(state);
        var read = await Assert.ThrowsAsync<AwkRuntimeException>(() => reference.EvaluateAsync(context).AsTask());
        var write = await Assert.ThrowsAsync<AwkRuntimeException>(() => reference.AssignValueAsync(context, AwkValue.FromNumber(7)).AsTask());
        Assert.Equal("awk: attempt to use array `value' in a scalar context\n", AwkUtf8Codec.Decode(read.MessageBytes));
        Assert.Equal(AwkUtf8Codec.Decode(read.MessageBytes), AwkUtf8Codec.Decode(write.MessageBytes));
        Assert.Equal("kept", state.GetArrayByteText(array, key).AsString());
        Assert.False(state.HasScalar(name));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Fact]
    public async Task LoweredLocalStaysLocalWithGlobalSlotsPresent()
    {
        var state = State(true);
        var name = AwkVariableName.FromText("value");
        var reference = Reference(name, local: true);
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        state.PushFunctionFrame(new AwkFunctionLocalLayout([name]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(20))]);
        try
        {
            var result = await reference.ApplyIncrementAsync(new AwkEvaluationContext(state));
            Assert.Equal(21, result.AsNumber());
        }
        finally
        {
            state.PopFunctionFrame();
        }
        Assert.Equal(7, state.ResolveVariableValue(name).AsNumber());
    }

    [Fact]
    public async Task CompoundReadPromotesUntypedOriginBeforeCanceledRhs()
    {
        var state = State(true);
        var name = AwkVariableName.FromText("value");
        Assert.True(state.TryResolveArrayArgument(name, out _));
        Assert.False(state.HasScalar(name));
        var source = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = Reference(name).ApplyCompoundAssignmentAsync(new AwkEvaluationContext(state), new PendingExpression(source.Task), AwkCompoundAssignmentOperation.Add).AsTask();
        Assert.True(state.HasScalar(name));
        source.SetCanceled();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => result);
        Assert.True(state.HasScalar(name));
        Assert.False(state.IsArray(new AwkArrayName(name.Identifier)));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task CompiledUpdatesRetainExecutionOwnershipWithPendingHostsAndLocalShadowing()
    {
        var source = new MockFileSystem();
        source.AddFile("/script", "function bump(value){value+=1;total+=value;return value} {SYMTAB[$1]=$2;value=$2+0;value+=bump(1);total+=value} END{print total,value}");
        var compiled = await AwkExecutor.CompileAsync(source, AwkTestHarness.BuildInvocation("awk", "-f", "/script", "/data"), CancellationToken.None);
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
        await Task.WhenAll(RunAsync("x 10\ny 20\n", "38 22\n", true), RunAsync("padding 99\ny 40\nx 30\n", "181 32\n", true));
        await RunAsync("y 7\nx 9\n", "24 11\n", false);
    }

    private static AwkReference Reference(AwkVariableName name, bool local = false)
    {
        var layout = new AwkGlobalReadLayout(new HashSet<AwkVariableName> { name }, null);
        var scope = local ? AwkLoweringScope.Function(new AwkFunctionLocalLayout([name]), globalReads: layout) : AwkLoweringScope.GlobalFor(AwkCompatibilityMode.GnuDefault, globalReads: layout);
        var token = new AwkToken(AwkTokenKind.Identifier, name.Text, new AwkTextSpan(0, name.Text.Length));
        var result = AwkLValueLowerer.Lower(new AwkExpressionSyntax(AwkExpressionKind.Variable, [token], [], token.Span), scope);
        Assert.True(result.Succeeded);
        return result.Reference;
    }

    private static AwkRuntimeState State(bool vector)
    {
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(Limit), AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        if (vector) state.InitializeGlobalReadSlots(1);
        return state;
    }

    private sealed class PendingExpression(Task<AwkValue> value) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => new(value);
    }
}
