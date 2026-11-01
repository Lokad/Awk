namespace Lokad.Awk.Tests;

public sealed partial class AwkBuiltinArgumentMemoryTests
{
    [Fact]
    public void InactiveStorageClearsPayloadAndRemainsFundedUntilOwnerCompletion()
    {
        var fixture = State(observed: true, CancellationToken.None);
        var budget = fixture.Budget;
        try
        {
            var state = fixture.State;
            var baseline = state.MemoryAccounting.CurrentCommittedBytes;
            var text = AwkValue.FromText(AwkText.FromString(new string('x', 4096)));
            AwkValue[] values;
            using (var buffer = AwkValueBuffer.Rent(2, state))
            {
                values = buffer.Values;
                buffer.Set(0, text);
                Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= baseline + text.RetainedTextMemoryBytes);
            }
            Assert.All(values, value => Assert.Equal(default, value));
            Assert.Equal(baseline + AwkRuntimeMemory.EstimateAwkValueArrayBytes(2), state.MemoryAccounting.CurrentCommittedBytes);
            using (var reused = AwkValueBuffer.Rent(2, state))
            {
                Assert.Same(values, reused.Values);
                reused.Set(0, AwkValue.FromNumber(7));
                using var nested = AwkValueBuffer.Rent(2, state);
                Assert.NotSame(values, nested.Values);
                nested.Set(0, AwkValue.FromNumber(9));
                Assert.Equal(7, reused.Values[0].AsNumber());
            }
            Assert.All(values, value => Assert.Equal(default, value));
            Assert.Equal(baseline + AwkRuntimeMemory.EstimateAwkValueArrayBytes(2), state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(2, state.Counters.GovernedArgumentBufferAllocations);
            Assert.Equal(1, state.Counters.GovernedArgumentBufferReuses);
        }
        finally { fixture.Dispose(); }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("fault")]
    [InlineData("cancel")]
    public async Task RetainedArgumentsSurviveUntilPendingOperandActuallyCompletes(string outcome)
    {
        using var cancellation = new CancellationTokenSource();
        var fixture = State(observed: true, cancellation.Token);
        var budget = fixture.Budget;
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<AwkValue>? pending = null;
        try
        {
            var state = fixture.State;
            var baseline = state.MemoryAccounting.CurrentCommittedBytes;
            var text = AwkValue.FromText(AwkText.FromString(new string('x', 4096)));
            var evaluations = 0;
            var builtin = AwkBuiltinImplementation.Sync(context =>
            {
                evaluations++;
                Assert.Equal(text, context.GetValue(0));
                return context.GetValue(1);
            });
            pending = builtin.EvaluateAsync([
                new ObservedOperand(() => ValueTask.FromResult(text)),
                new ObservedOperand(() => new(completion.Task))], new(state)).AsTask();
            Assert.False(pending.IsCompleted);
            var held = state.MemoryAccounting.CurrentCommittedBytes;
            Assert.True(held >= baseline + text.RetainedTextMemoryBytes);
            using (var nested = AwkValueBuffer.Rent(2, state))
                nested.Set(0, AwkValue.FromNumber(99));
            // A cancellation request is not completion of a non-cooperating operand.
            if (outcome == "cancel") cancellation.Cancel();
            Assert.False(pending.IsCompleted);
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= held);

            if (outcome == "success")
            {
                completion.SetResult(AwkValue.FromNumber(7));
                Assert.Equal(7, (await pending.WaitAsync(TimeSpan.FromSeconds(10))).AsNumber());
                Assert.Equal(1, evaluations);
            }
            else if (outcome == "fault")
            {
                completion.SetException(new InvalidOperationException("operand failed"));
                await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(0, evaluations);
            }
            else
            {
                completion.SetCanceled(cancellation.Token);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(0, evaluations);
            }
            Assert.Equal(baseline + AwkRuntimeMemory.EstimateAwkValueArrayBytes(2), state.MemoryAccounting.CurrentCommittedBytes);
        }
        finally
        {
            completion.TrySetCanceled();
            if (pending is not null)
            {
                try { await pending.WaitAsync(TimeSpan.FromSeconds(10)); }
                catch (InvalidOperationException) when (outcome == "fault") { }
                catch (OperationCanceledException) { }
            }
            fixture.Dispose();
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task RefusalAfterArityChangeFailsPublicCommandBeforeOperandEffects()
    {
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(8 << 20);
        host.AfterAppend = () =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) == "1\n" && budget.Refusals == 0)
                budget.RefuseNextAdmissionMinimumGrowth = 1;
        };
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "function effect(){print \"operand\";return 1}BEGIN{print int(1);print atan2(effect(),1);print \"later\"}END{print \"end\"}"));
        var exit = await awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
