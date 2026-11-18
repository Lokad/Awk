namespace Lokad.Awk.Tests;

public sealed class AwkComputedFieldIndexTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScalarCompoundTargetSurvivesAPendingIndex(bool governed)
    {
        var host = new MockFileSystem();
        host.AddFile("/side", "1\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = async (descriptor, maximum, token) =>
        {
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, token).ConfigureAwait(false);
        };
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["function choose(v){getline v<\"/side\";sum=100;$0=\"0003 tail\";return v}BEGIN{sum=7;$0=\"old\";r=(sum+=$(choose()));print sum,r,$1}"], []);
        Assert.True(Awk.TryParse(command, out var awk));
        var run = awk.ExecuteAsync(host, policy, CancellationToken.None);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
            release.TrySetResult();
            Assert.Equal(0, (await run.WaitAsync(TimeSpan.FromSeconds(10))).Value);
            Assert.Equal("10 10 0003\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            release.TrySetResult();
            await run;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompoundTargetSurvivesAPendingRightHandSide(bool governed)
    {
        var host = new MockFileSystem();
        host.AddFile("/side", "5\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = async (descriptor, maximum, token) =>
        {
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, token).ConfigureAwait(false);
        };
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["function rhs(v){getline v<\"/side\";i=3;$1=100;return v}BEGIN{$0=\"10 20 30\";i=2;print($i+=rhs()),i,$0}"], []);
        Assert.True(Awk.TryParse(command, out var awk));
        var run = awk.ExecuteAsync(host, policy, CancellationToken.None);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
            release.TrySetResult();
            Assert.Equal(0, (await run.WaitAsync(TimeSpan.FromSeconds(10))).Value);
            Assert.Equal("25 3 100 25 30\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            release.TrySetResult();
            await run;
        }
    }

    public static IEnumerable<object[]> PublicCases()
    {
        foreach (var test in AwkGoldenCorpus.DirectCases.Where(c => c.Name.StartsWith("computed-field-", StringComparison.Ordinal)))
        foreach (var governed in new[] { false, true })
            yield return [test, governed];
    }

    [Theory]
    [MemberData(nameof(PublicCases))]
    public async Task ComputedFieldsRespectThePublicExecutionBoundary(AwkGoldenCase test, bool governed)
    {
        var host = new MockFileSystem();
        host.SetStandardInput(test.StandardInput);
        var reads = 0;
        host.AfterRead = () => reads++;
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", test.Arguments, []);
        Assert.True(Awk.TryParse(command, out var awk));

        Assert.Equal(test.ExpectedExitCode, (await awk.ExecuteAsync(host, policy, CancellationToken.None)).Value);
        Assert.Equal(test.ExpectedStdOut, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(test.ExpectedStdErr, host.GetOutput(AwkFileDescriptor.StdErr));
        if (test.StandardInput is "" or "unread\n") Assert.Equal(0, reads);
        else Assert.True(reads > 0);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(double.NaN, true)]
    [InlineData(double.PositiveInfinity, false)]
    [InlineData(double.PositiveInfinity, true)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(double.NegativeInfinity, true)]
    public async Task PendingInvalidTargetsPreserveTheRecordAndEvaluateTheIndexOnce(double number, bool governed)
    {
        using var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(AwkTestHarness.CreateRuntimeOptions()))).State;
        var record = AwkText.FromSourceLiteralString("kept record");
        state.SetCurrentRecord(record);
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("function index_value(v){effects++;getline v<\"/side\";return bad}" +
            "BEGIN{$(index_value())=7}"));
        Assert.True(binding.Success);
        var program = binding.Value.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        state.AssignVariableValue(AwkVariableName.FromText("bad"), AwkValue.FromNumber(number));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/side", "ready\n");
        var reads = 0;
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            reads++;
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, CancellationToken.None, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting);
        var run = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.True(record.HasSameStorage(state.CurrentRecordByteText));
            release.TrySetResult();
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal("awk: non-finite field index\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
            Assert.Equal(1, reads);
            Assert.True(record.HasSameStorage(state.CurrentRecordByteText));
            Assert.Equal(2, state.FieldCount);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (AwkRuntimeException) { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }
}
