namespace Lokad.Awk.Tests;

public sealed class AwkGetlineDestinationTests
{
    [Fact]
    public async Task CancellationDuringRecordAdmissionPreservesThePreviousRecordAndTerminator()
    {
        using var cancellation = new CancellationTokenSource();
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memory = new CooperativeTestMemory(1 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, cancellation.Token,
            memory.Configure(CreateOptions(limits, AwkRuntimeCounters.Disabled)))).State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("keep"));
        Assert.Equal(1, state.FieldCount);
        state.RecordNumber = 4;
        state.FileRecordNumber = 3;
        state.SetRecordTerminator(AwkRecordTerminator.FromText(AwkText.FromSourceLiteralString(",")));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.FieldWidths,
            AwkValue.FromText(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("1", 4096)))));
        var host = new MockFileSystem();
        host.AddFile("/data", "new\n");
        using var output = AwkTestHarness.CreateOutputSink();
        var io = CreateIo(host, state, limits, cancellation.Token);
        var region = Bind(state, "getline <\"data\"");
        var canceledAdmission = false;
        memory.Budget.OnGrowth = bytes =>
        {
            // Tiny input/descriptor storage is admitted first; the large retained
            // width plan makes the later current-record admission observable.
            if (bytes < 16 << 10) return;
            canceledAdmission = true;
            cancellation.Cancel();
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await region.ExecuteAsync(state, io, output));
        memory.Budget.OnGrowth = null;
        Assert.True(canceledAdmission);
        Assert.Equal("keep", state.CurrentRecordByteText.AsString());
        Assert.Equal(4, state.RecordNumber);
        Assert.Equal(3, state.FileRecordNumber);
        Assert.Equal(",", state.ResolveSpecialVariableText(AwkSpecialVariableId.RecordTerminator).AsString());
        Assert.Equal(1, state.FieldCount);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseForCleanupAsync();
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RefusedStoragePrecedesOpeningInputAndDestinationMutation(bool field, bool memory)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = CreateOptions(limits, AwkRuntimeCounters.Disabled);
        using var ownedMemory = new CooperativeTestMemory(1 << 20);
        var state = ownedMemory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, ownedMemory.Configure(options))).State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed"));
        Assert.Equal(1, state.FieldCount);
        var array = new AwkArrayName(AwkVariableName.FromText("a").Identifier);
        if (!field && !memory)
            state.SetArrayAwkValue(array, new AwkArrayKey("kept"), AwkValue.FromNumber(7));
        var key = memory ? new string('k', 600) : "new";
        var destination = field ? "$30" : "a[\"" + key + "\"]";
        var region = Bind(state, "status=(getline " + destination + "<\"data\")");
        var host = new MockFileSystem();
        host.AddFile("/data", "record\n");
        using var output = AwkTestHarness.CreateOutputSink();
        var io = CreateIo(host, state, limits, CancellationToken.None);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;

        ownedMemory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await region.ExecuteAsync(state, io, output));

        Assert.Equal("awk: memory limit exceeded\n",
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
        host.ReadAsyncOverride = (_, _, token) =>
        {
            readEntered = true;
            Assert.True(state.TryPeekArrayAwkValue(array, key, out var value));
            Assert.Equal(AwkRuntimeValueKind.Untyped, value.RuntimeKind);
            cancellation.Cancel();
            return ValueTask.FromCanceled<AwkReadBuffer>(token);
        };
        using var output = AwkTestHarness.CreateOutputSink();
        var io = CreateIo(host, state, limits, cancellation.Token);
        var region = Bind(state, "status=(getline a[\"key\"]<\"data\")");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await region.ExecuteAsync(state, io, output));


        Assert.True(readEntered);
        Assert.True(state.TryPeekArrayAwkValue(array, key, out _));
        Assert.Equal(0, state.RecordNumber);
        await io.CloseForCleanupAsync();
        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public async Task RepeatedEofDestinationResolutionKeepsArrayKeysByteNative()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            CreateOptions(AwkRuntimeLimits.CreateDefault(), counters));
        using var ownedState = state;
        var array = AwkArrayName.FromText("a");
        var region = Bind(state, "for(i=0;i<200;i++)status=(getline a[\"κ\"]<\"data\")");
        var host = new MockFileSystem();
        host.AddFile("/data", "");
        var io = CreateIo(host, state, state.Limits, CancellationToken.None);
        using var output = AwkTestHarness.CreateOutputSink();
        await region.ExecuteAsync(state, io, output);
        var decodes = counters.StringDecodes; // The filename crosses the host boundary once.
        await region.ExecuteAsync(state, io, output);
        Assert.Equal(decodes, counters.StringDecodes);
        Assert.Equal(0, state.ResolveVariableValue(AwkVariableName.FromText("status")).AsNumber());
        Assert.True(state.TryGetExistingArrayCount(array, out var count));
        Assert.Equal(1, count);
        Assert.True(state.TryPeekArrayAwkValue(array, new AwkArrayKey("κ"), out var value));
        Assert.Equal(AwkRuntimeValueKind.Untyped, value.RuntimeKind);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
        Assert.Equal(0, host.OpenFileCount);
    }

    private static AwkRegion Bind(AwkRuntimeState state, string body)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("{" + body + "}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        return Assert.Single(program.MainActions);
    }

    private static AwkRuntimeOptions CreateOptions(AwkRuntimeLimits limits, AwkRuntimeCounters counters) =>
        new(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            limits, regexEngine: AwkUtf8RegexEngine.Instance);

    private static AwkIo CreateIo(MockFileSystem host, AwkRuntimeState state,
        AwkRuntimeLimits limits, CancellationToken cancellation) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            cancellation, state.Counters, limits, state, static _ => []);
}
