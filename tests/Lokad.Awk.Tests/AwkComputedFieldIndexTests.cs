namespace Lokad.Awk.Tests;

public sealed class AwkComputedFieldIndexTests
{
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
        host.AfterReadLines = () => reads++;
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", test.Arguments, []);
        Assert.True(Awk.TryParse(command, out var awk));

        Assert.Equal(test.ExpectedExitCode, (await awk.ExecuteAsync(host, policy, CancellationToken.None)).Value);
        Assert.Equal(test.ExpectedStdOut, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(test.ExpectedStdErr, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, reads);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(double.NaN, false)] [InlineData(double.NaN, true)]
    [InlineData(double.PositiveInfinity, false)] [InlineData(double.PositiveInfinity, true)]
    [InlineData(double.NegativeInfinity, false)] [InlineData(double.NegativeInfinity, true)]
    public async Task PendingInvalidTargetsPreserveTheRecordAndEvaluateTheIndexOnce(double number, bool governed)
    {
        using var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None, memory.Configure(AwkRuntimeOptions.CreateSystemDefault()))).State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("kept record"));
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var index = new PendingIndex(completion.Task);
        var target = AwkReference.Field(index);
        var pending = target.AssignValueAsync(new(state), AwkValue.FromNumber(7)).AsTask();
        Assert.False(pending.IsCompleted);
        completion.SetResult(AwkValue.FromNumber(number));

        var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => pending);
        Assert.Equal("awk: non-finite field index\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(1, index.Entries);
        Assert.Equal("kept record", state.CurrentRecordByteText.AsString());
        Assert.Equal(2, state.FieldCount);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    private sealed class PendingIndex(Task<AwkValue> completion) : AwkExpression
    {
        public int Entries { get; private set; }
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            Entries++;
            return new(completion);
        }
    }
}
