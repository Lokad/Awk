namespace Lokad.Awk.Tests;

public sealed class AwkGetlineDestinationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DestinationAdmissionFailurePrecedesOpeningTheInput(bool field, bool memory)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        limits = memory ? limits.WithMaxExecutionMemoryBytes(500) :
            field ? limits.WithMaxFields(2) : limits.WithMaxArrayElements(1);
        var options = CreateOptions(limits, AwkRuntimeCounters.Disabled);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed"));
        Assert.Equal(1, state.FieldCount);
        var array = new AwkArrayName(AwkVariableName.FromText("a").Identifier);
        if (!field && !memory)
            state.SetArrayAwkValue(array, new AwkArrayKey("kept"), AwkValue.FromNumber(7));
        var key = memory ? new string('k', 600) : "new";
        var reference = field ? AwkReference.Field(AwkExpression.Number("30", 30)) :
            AwkReference.ArrayElement(array, AwkArrayIndex.Simple(AwkExpression.Literal(key)));
        var host = new MockFileSystem();
        host.AddFile("/data", "record\n");
        using var output = AwkTestHarness.CreateOutputSink();
        var io = CreateIo(host, state, limits, CancellationToken.None);
        var context = new AwkEvaluationContext(state, output, io);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;

        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => AwkGetlineRuntime.ExecuteAsync(
            AwkGetlineTarget.ReferenceTarget(reference),
            AwkGetlineSource.Redirection(AwkExpression.Literal("data")),
            context, new AwkExecutionContext(output, io), AwkPendingDiagnosticPolicy.Preserve).AsTask());

        Assert.Equal(memory ? "awk: memory limit exceeded\n" : "awk: array element limit exceeded\n",
            AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal("seed", state.CurrentRecordByteText.AsString());
        Assert.Equal(1, state.FieldCount);
        if (memory || field)
            Assert.False(state.TryGetExistingArrayCount(array, out _));
        else
        {
            Assert.True(state.TryGetExistingArrayCount(array, out var count));
            Assert.Equal(1, count);
            Assert.Equal(7, state.GetArrayAwkValue(array, new AwkArrayKey("kept")).AsNumber());
        }
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task DestinationIsAdmittedBeforeAHostReadObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token);
        var array = new AwkArrayName(AwkVariableName.FromText("a").Identifier);
        var key = new AwkArrayKey("key");
        var host = new MockFileSystem();
        host.AddFile("/data", "record\n");
        var readEntered = false;
        host.ReadLinesAsyncOverride = (_, _, token) =>
        {
            readEntered = true;
            Assert.True(state.TryPeekArrayAwkValue(array, key, out var value));
            Assert.Equal(AwkRuntimeValueKind.Untyped, value.RuntimeKind);
            cancellation.Cancel();
            return Task.FromCanceled<AwkReadResult>(token);
        };
        using var output = AwkTestHarness.CreateOutputSink();
        var io = CreateIo(host, state, limits, cancellation.Token);
        var context = new AwkEvaluationContext(state, output, io);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AwkGetlineRuntime.ExecuteAsync(
            AwkGetlineTarget.ReferenceTarget(AwkReference.ArrayElement(array,
                AwkArrayIndex.Simple(AwkExpression.Literal("key")))),
            AwkGetlineSource.Redirection(AwkExpression.Literal("data")),
            context, new AwkExecutionContext(output, io), AwkPendingDiagnosticPolicy.Preserve).AsTask());

        Assert.True(readEntered);
        Assert.True(state.TryPeekArrayAwkValue(array, key, out _));
        Assert.Equal(0, state.RecordNumber);
    }

    [Fact]
    public async Task RepeatedEofDestinationResolutionKeepsArrayKeysByteNative()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            CreateOptions(AwkRuntimeLimits.CreateDefault(), counters));
        var array = new AwkArrayName(AwkVariableName.FromText("a").Identifier);
        var target = AwkGetlineTarget.ReferenceTarget(AwkReference.ArrayElement(array,
            AwkArrayIndex.Simple(AwkExpression.Literal("κ"))));
        var context = new AwkEvaluationContext(state);

        for (var index = 0; index < 200; index++)
            _ = await target.ResolveAsync(context);

        Assert.Equal(0, counters.StringDecodes);
        Assert.True(state.TryGetExistingArrayCount(array, out var count));
        Assert.Equal(1, count);
        Assert.True(state.TryPeekArrayAwkValue(array, new AwkArrayKey("κ"), out var value));
        Assert.Equal(AwkRuntimeValueKind.Untyped, value.RuntimeKind);
    }

    private static AwkRuntimeOptions CreateOptions(AwkRuntimeLimits limits, AwkRuntimeCounters counters) =>
        new(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            limits, AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);

    private static AwkIo CreateIo(MockFileSystem host, AwkRuntimeState state,
        AwkRuntimeLimits limits, CancellationToken cancellation) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            cancellation, state.Counters, limits, state, static () => []);
}
