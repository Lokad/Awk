namespace Lokad.Awk.Tests;

public sealed partial class AwkPrintStagingMemoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SmallPrintGrowthRefusalKeepsEarlierOutput(bool observed)
    {
        using var memoryForState = State(observed, "default");
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var earlier = new string('p', 60_000);
        output.Append(AwkText.FromSourceLiteralString(earlier));
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes - 131_071);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var io = Io(state, new MockFileSystem());
        var binding = AwkBinder.Bind(AwkParser.Parse("{print \"" + new string('x', 10_000) + "\",\"" + new string('y', 10_000) + "\"}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
            await program.MainProgram.ExecuteAsync(state, io, output));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(earlier, output.ToString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    public static IEnumerable<object[]> PendingPrintArguments()
    {
        foreach (var governed in new[] { false, true })
        foreach (var cancel in new[] { false, true })
        foreach (var count in new[] { 1, 4 })
        for (var position = 0; position < count; position++) yield return [governed, cancel, count, position];
    }

    [Theory]
    [MemberData(nameof(PendingPrintArguments))]
    public async Task PrintWaitsForEachArgumentWithoutPublishingOrReplaying(bool governed, bool cancel, int count, int position)
    {
        var arguments = string.Join(',', Enumerable.Range(0, count).Select(i => i == position ? "operand()" : "\"x\""));
        var source = "function operand(v){evaluations++;getline v<\"/side\";return v}" +
            "BEGIN{print \"prior\";print " + arguments + ";print \"after\",evaluations}";
        using var cancellation = new CancellationTokenSource();
        var host = new MockFileSystem();
        var value = count == 1 ? "1.25" : "é";
        host.AddFile("/side", value + "\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            reads++;
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var budget = new CooperativeTestBudget(Limit);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var run = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source)).ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal("prior\n", host.GetOutput(AwkFileDescriptor.StdOut));
            if (cancel)
            {
                cancellation.Cancel();
                Assert.False(run.IsCompleted);
                if (governed) Assert.True(budget.Reserved > 0);
            }
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkExitCode.Success, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            var line = string.Join(" ", Enumerable.Range(0, count).Select(i => i == position ? value : "x"));
            Assert.Equal("prior\n" + (cancel ? "" : line + "\nafter 1\n"), host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(1, reads);
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            release.TrySetResult();
            try { await run.WaitAsync(TimeSpan.FromSeconds(10)); } catch (OperationCanceledException) when (cancel) { }
        }
    }
}
