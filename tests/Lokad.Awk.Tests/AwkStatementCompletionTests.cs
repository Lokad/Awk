using System.Threading.Tasks.Sources;

namespace Lokad.Awk.Tests;

public sealed class AwkStatementCompletionTests
{
    [Theory]
    [InlineData("continue", false)] [InlineData("continue", true)]
    [InlineData("break", false)] [InlineData("break", true)]
    [InlineData("continue-loop", false)] [InlineData("continue-loop", true)]
    [InlineData("return", false)] [InlineData("return", true)]
    [InlineData("next", false)] [InlineData("next", true)]
    [InlineData("nextfile", false)] [InlineData("nextfile", true)]
    [InlineData("exit", false)] [InlineData("exit", true)]
    public async Task ReadCompletionIsConsumedOnceBeforeFollowingControlFlow(string flow, bool suspend)
    {
        var stop = flow switch { "continue" => "", "continue-loop" => "continue;", _ => flow + ";" };
        var body = "print \"prefix\";getline v<\"/side\";print \"completed\",v;" + stop + "print \"suffix\"";
        var source = flow switch
        {
            "break" or "continue-loop" => "{for(i=0;i<1;i++){" + body + "}}",
            "return" => "function worker(){" + body + "}{worker()}",
            _ => "{" + body + "}"
        };
        await Run(source, suspend, failRead: false,
            "prefix\ncompleted 7\n" + (flow == "continue" ? "suffix\n" : ""));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task FailedReadIsConsumedOnceAndPreservesRecoverableGetline(bool suspend) => Run(
        "{print \"prefix\";status=(getline v<\"/side\");if(status<0){print status,(ERRNO!=\"\");exit};print \"suffix\"}",
        suspend, failRead: true, "prefix\n-1 1\n");

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task FaultedReadIsConsumedOnceWithoutReplayingOrLeakingHostDetails(bool suspend) => Run(
        "{print \"prefix\";getline v<\"/side\";print \"suffix\"}",
        suspend, failRead: true, "prefix\n", throwRead: true);

    private static async Task Run(string source, bool suspend, bool failRead, string expected, bool throwRead = false)
    {
        var host = new MockFileSystem();
        host.SetStandardInput("record\n");
        host.AddFile("/side", "7\n");
        var operation = new SingleConsumptionRead();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        AwkReadBuffer? buffer = null;
        host.ReadAsyncOverride = Read;
        ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            host.ReadAsyncOverride = null;
            try
            {
                var value = host.ReadAsync(descriptor, maximum, token);
                if (descriptor == AwkFileDescriptor.StdIn) return value;
                buffer = value.Result;
                reads++;
                if (!suspend) Complete();
                entered.TrySetResult();
                return operation.Value;
            }
            finally { host.ReadAsyncOverride = Read; }
        }
        void Complete()
        {
            if (failRead)
            {
                buffer?.Dispose();
                if (throwRead) operation.Fail(new IOException("private read failure"));
                else operation.Complete(AwkReadBuffer.Failure(new(AwkOperationErrorKind.IoFailure, "read failed")));
            }
            else operation.Complete(buffer ?? throw new InvalidOperationException("No read entered."));
        }
        var budget = new CooperativeTestBudget(8 << 20);
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var run = tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), CancellationToken.None);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (suspend)
            {
                Assert.False(run.IsCompleted);
                Assert.Equal("prefix\n", host.GetOutput(AwkFileDescriptor.StdOut));
                Assert.True(budget.Reserved > 0);
                Complete();
            }
            Assert.Equal(throwRead ? new AwkExitCode(2) : AwkExitCode.Success, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(throwRead ? "awk: execution failed without exposing host exception details\n" : "", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(1, reads);
            Assert.Equal(1, operation.Consumptions);
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            if (buffer is not null && operation.Consumptions == 0 && suspend) Complete();
            await run.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    private sealed class SingleConsumptionRead : IValueTaskSource<AwkReadBuffer>
    {
        private ManualResetValueTaskSourceCore<AwkReadBuffer> _completion = new() { RunContinuationsAsynchronously = true };
        public int Consumptions { get; private set; }
        public ValueTask<AwkReadBuffer> Value => new(this, _completion.Version);
        public void Complete(AwkReadBuffer value) => _completion.SetResult(value);
        public void Fail(Exception error) => _completion.SetException(error);
        public AwkReadBuffer GetResult(short token)
        {
            Assert.Equal(0, Consumptions++);
            return _completion.GetResult(token);
        }
        public ValueTaskSourceStatus GetStatus(short token) => _completion.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            _completion.OnCompleted(continuation, state, token, flags);
    }
}
