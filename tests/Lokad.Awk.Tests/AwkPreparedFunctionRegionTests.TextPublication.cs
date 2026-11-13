namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UsedTextAssignmentsKeepTheirSnapshotsAndPlainStringOrigins(bool governed)
    {
        const string source = "function f(s,t){a=(t=tolower(s));b=(t=substr(s,2,2));" +
            "print a,b,t,typeof(a),typeof(b),a<10,b<10;return t}" +
            "BEGIN{print f(\"0007\");saved=tolower(\"AB\");other=saved=substr(\"XYZ\",2,2);print saved,other}";
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var exit = await AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []))
            .ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("0007 00 00 string string 1 1\n00\nYZ YZ\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public Task TextPublicationAdmissionFollowsProductionAndStopsBeforeMutation(bool substring, bool cancel) =>
        AssertTextPublicationStopsBeforeMutation(substring ? "substr(\"ABCD\",2,2)" : "tolower(\"ABC\")", cancel);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task ConcatenationPublicationAdmissionFollowsProductionAndStopsBeforeMutation(bool cancel) =>
        AssertTextPublicationStopsBeforeMutation("\"AB\" \"CD\" \"EF\"", cancel);

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public Task PublishedConcatenationStaysFundedUntilPendingOutputCompletes(bool local, bool cancel) =>
        VerifyPendingPublishedText(cancel, local
            ? "function f(s,t){t=s \"07\";print \"held\";return t}BEGIN{print f(\"00\")}"
            : "function f(s){saved=s \"07\";print \"held\";return saved}BEGIN{print f(\"00\")}", "0007\n");

    private static async Task AssertTextPublicationStopsBeforeMutation(string expression, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        var program = Bind("function f(){print \"before\";saved=" + expression + ";print \"after\"}BEGIN{f()}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var saved = AwkVariableName.FromText("saved");
        var observed = false;
        var host = new MockFileSystem();
        host.AfterAppend = () =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) != "before\n") return;
            var allocations = counters.AwkTextAllocations;
            memory.Budget.OnGrowth = _ =>
            {
                if (counters.AwkTextAllocations <= allocations) return;
                memory.Budget.OnGrowth = null;
                observed = true;
                Assert.False(state.HasScalar(saved));
                Assert.True(state.IsInsideFunction);
                if (cancel) cancellation.Cancel();
                else memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            };
        };
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters, state.CancellationCheckpoint);
        try
        {
            if (cancel)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output));
            else
            {
                var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                    await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
                Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
            }
            Assert.True(observed);
            Assert.False(state.HasScalar(saved));
            Assert.False(state.IsInsideFunction);
            Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        }
        finally
        {
            memory.Budget.OnGrowth = null;
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
        }
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }
}
