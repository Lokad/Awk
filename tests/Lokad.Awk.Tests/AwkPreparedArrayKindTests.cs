namespace Lokad.Awk.Tests;

public sealed class AwkPreparedArrayKindTests
{
    private static readonly AwkArrayName Array = AwkArrayName.FromText("a");
    private static readonly AwkVariableName Variable = AwkVariableName.FromText("a");
    private static readonly AwkArrayKey Key = new("k");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SuccessfulAccessesBindOnlyArrayKinds(bool slots)
    {
        var state = State(slots: slots);
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), 0);
        Assert.Equal(0, state.Counters.BoundGlobalArrayAccesses);
        Assert.False(state.HasBoundGlobalScalar(0));
        Assert.False(state.TryResolvePreparedGlobalNumber(0, out _));
        Assert.Equal(7, state.GetArrayAwkValue(Array, Key, 0).AsNumber());
        Assert.True(state.ContainsArrayValue(Array, Key, 0));
        state.SetCurrentRecord(AwkText.FromString("k tail"));
        Assert.Equal(7, state.GetArrayFieldValue(Array, 1, 0).AsNumber());
        Assert.True(state.ContainsArrayField(Array, 1, 0));
        Assert.Equal(slots ? 4 : 0, state.Counters.BoundGlobalArrayAccesses);
    }

    [Fact]
    public void BindingUsesTheCurrentReplacementDictionary()
    {
        var state = State();
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), 0);
        state.ClearArray(Array);
        Assert.False(state.ContainsArrayValue(Array, Key, 0));
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(9));
        Assert.Equal(9, state.GetArrayAwkValue(Array, Key, 0).AsNumber());
        state.SplitIntoArray(AwkText.FromString("new words"), Array, AwkFieldSeparatorSpec.Default,
            AwkOptionalIdentifier.None);
        Assert.False(state.ContainsArrayValue(Array, Key, 0));
        Assert.Equal("new", state.GetArrayAwkValue(Array, AwkArrayKey.FromOrdinal(1), 0).AsString());
    }

    [Fact]
    public void FailedScalarUseDoesNotPublishAnArrayBinding()
    {
        var state = State();
        state.AssignVariableValue(Variable, AwkValue.FromNumber(3));
        var error = Assert.Throws<AwkRuntimeException>(() =>
            state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), 0));
        Assert.Equal("awk: attempt to use scalar `a' as an array\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.False(state.TryUseBoundGlobalArray(0));
        Assert.Equal(3, state.ResolveVariableValue(Variable).AsNumber());
    }

    [Fact]
    public void BoundArrayCannotBecomeAScalarOrANumericSlot()
    {
        var state = State();
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), 0);
        Assert.False(state.HasBoundGlobalScalar(0));
        Assert.False(state.TryResolvePreparedGlobalNumber(0, out _));
        Assert.True(state.TryResolveArrayArgument(Variable, out _));
        var read = Assert.Throws<AwkRuntimeException>(() => state.ResolvePreparedGlobalValue(Variable, 0));
        var write = Assert.Throws<AwkRuntimeException>(() =>
            state.AssignPreparedGlobalValue(Variable, 0, AwkValue.FromNumber(9)));
        const string expected = "awk: attempt to use array `a' in a scalar context\n";
        Assert.Equal(expected, AwkUtf8Codec.Decode(read.MessageBytes));
        Assert.Equal(expected, AwkUtf8Codec.Decode(write.MessageBytes));
        Assert.Equal(7, state.GetArrayAwkValue(Array, Key, 0).AsNumber());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveFunctionFramesKeepOriginalNameResolution(bool alias)
    {
        var state = State();
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), 0);
        var other = AwkArrayName.FromText("b");
        state.SetArrayAwkValue(other, Key, AwkValue.FromNumber(9));
        var argument = alias ? AwkFunctionArgument.Array(other) : AwkFunctionArgument.Scalar(AwkValue.FromNumber(3));
        state.PushFunctionFrame(new([Variable]), [argument]);
        try
        {
            if (alias)
                Assert.Equal(9, state.GetArrayAwkValue(Array, Key, 0).AsNumber());
            else
            {
                var error = Assert.Throws<AwkRuntimeException>(() => state.GetArrayAwkValue(Array, Key, 0));
                Assert.Equal("awk: attempt to use scalar `a' as an array\n", AwkUtf8Codec.Decode(error.MessageBytes));
                Assert.Equal(3, state.ResolvePreparedGlobalValue(Variable, 0).AsNumber());
                state.AssignPreparedGlobalValue(Variable, 0, AwkValue.FromNumber(4));
                Assert.Equal(4, state.ResolvePreparedGlobalValue(Variable, 0).AsNumber());
            }
            Assert.Equal(0, state.Counters.BoundGlobalArrayAccesses);
        }
        finally { state.PopFunctionFrame(); }
        Assert.False(state.HasBoundGlobalScalar(0));
        Assert.Equal(7, state.GetArrayAwkValue(Array, Key, 0).AsNumber());
    }

    [Fact]
    public void BoundWritesKeepEntryPreflights()
    {
        var state = State(limits: AwkRuntimeLimits.CreateDefault().WithMaxArrayElements(1));
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), 0);
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        var error = Assert.Throws<AwkRuntimeException>(() =>
            state.SetArrayAwkValue(Array, new("new"), AwkValue.FromNumber(9), 0));
        Assert.Equal("awk: array element limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(7, state.GetArrayAwkValue(Array, Key, 0).AsNumber());
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void UnboundFunctionAccessCannotPublishGlobalArrayKind()
    {
        var state = State();
        state.AssignVariableValue(Variable, AwkValue.FromNumber(3));
        var other = AwkArrayName.FromText("b");
        state.SetArrayAwkValue(other, Key, AwkValue.FromNumber(9));
        state.PushFunctionFrame(new([Variable]), [AwkFunctionArgument.Array(other)]);
        try { Assert.Equal(9, state.GetArrayAwkValue(Array, Key, 0).AsNumber()); }
        finally { state.PopFunctionFrame(); }
        Assert.False(state.TryUseBoundGlobalArray(0));
        var error = Assert.Throws<AwkRuntimeException>(() =>
            state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), 0));
        Assert.Equal("awk: attempt to use scalar `a' as an array\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(3, state.ResolveVariableValue(Variable).AsNumber());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task PendingKeysPublishOnlyAfterSuccess(int outcome)
    {
        var state = State();
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reference = AwkReference.ArrayElement(Array, AwkArrayIndex.Simple(new PendingKey(completion.Task)), 0);
        var pending = reference.AssignValueAsync(new(state), AwkValue.FromNumber(7));
        Assert.False(pending.IsCompleted);
        Assert.False(state.TryUseBoundGlobalArray(0));
        if (outcome == 0)
        {
            completion.SetResult(AwkValue.FromText("k"));
            await pending;
            Assert.True(state.TryUseBoundGlobalArray(0));
            Assert.Equal(7, state.GetArrayAwkValue(Array, Key, 0).AsNumber());
        }
        else
        {
            if (outcome == 1)
            {
                completion.SetException(new AwkRuntimeException(Lokad.Awk.AwkMessages.ErrorMemoryLimit));
                await Assert.ThrowsAsync<AwkRuntimeException>(() => pending.AsTask());
            }
            else
            {
                completion.SetCanceled(new CancellationToken(true));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.AsTask());
            }
            Assert.False(state.TryUseBoundGlobalArray(0));
        }
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task ParsedArrayArgumentsAndSpecialArraysKeepTheirOriginalPaths()
    {
        const string source = "function size(x){return length(x)} BEGIN{a[1]=7;print size(a),length(a);ARGV[1]=\"x\";print ARGV[1]}";
        var host = new MockFileSystem();
        var options = Options();
        var program = await AwkExecutor.CompileAsync(host, AwkTestHarness.BuildInvocation("awk", [source]), CancellationToken.None);
        var result = await program.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(0, result.Value);
        Assert.Equal("1 1\nx\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, options.Counters.BoundGlobalArrayAccesses);
    }

    private static AwkRuntimeOptions Options(AwkRuntimeLimits? limits = null) =>
        new(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(),
            (limits ?? AwkRuntimeLimits.CreateDefault()),
            AwkRegexEngineKind.DotNetCompatibility);

    private static AwkRuntimeState State(bool slots = true, AwkRuntimeLimits? limits = null)
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, Options(limits));
        if (slots) state.InitializeGlobalReadSlots(1);
        return state;
    }

    private sealed class PendingKey(Task<AwkValue> completion) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => new(completion);
    }
}
