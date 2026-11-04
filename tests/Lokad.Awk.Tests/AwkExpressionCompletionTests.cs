using System.Threading.Tasks.Sources;

namespace Lokad.Awk.Tests;

public sealed class AwkExpressionCompletionTests
{
    public static IEnumerable<object[]> Programs()
    {
        var cases = new (string Body, string First, string Second, string Expected, int Reads)[]
        {
            ("result=first()+second();print result,order", "2", "3", "5 LR\n", 2),
            ("print (first()<second()),order", "2", "3", "1 LR\n", 2),
            ("if(first()<second())result=1;else result=0;print result,order", "4", "3", "0 LR\n", 2),
            ("a[2]=\"kept\";result=(a[second()]=first());print result,a[2],order", "7", "2", "7 7 LR\n", 2),
            ("a[2]=\"007\";result=(a[first()]+=mutating());print result,a[2],order", "2", "3", "10 10 LR\n", 2),
            ("a[1]=\"007\";$0=\"key 3\";result=(a[first()]+=$(mutating()));print result,a[1],order", "1", "2", "10 10 LR\n", 2),
            ("a[2]=\"007\";result=++a[first()];print result,a[2],typeof(result),order", "2", "", "8 8 number L\n", 1),
            ("a[2]=\"007\";result=a[first()]++;print result,a[2],typeof(result),order", "2", "", "7 8 number L\n", 1),
            ("a[2]=\"007\";result=--a[first()];print result,a[2],typeof(result),order", "2", "", "6 6 number L\n", 1),
            ("a[2]=\"007\";result=a[first()]--;print result,a[2],typeof(result),order", "2", "", "7 6 number L\n", 1),
            ("$0=\"one two\";result=$(first());print \"[\" result \"]\",order", "0", "", "[one two] L\n", 1),
            ("$0=\"one two\";result=$(first());print \"[\" result \"]\",order", "1", "", "[one] L\n", 1),
            ("$0=\"one two\";result=$(first());print \"[\" result \"]\",order", "3", "", "[] L\n", 1),
            ("$0=\"one two\";print ($(first())==\"one\"),order", "1", "", "1 L\n", 1),
            ("$0=\"one two\";if($(first())==\"one\")result=1;else result=0;print result,order", "3", "", "0 L\n", 1),
            ("result=(arg(\"G\",0)&&arg(\"S\",1))||((first()&&arg(\"T\",1))||arg(\"F\",0));print result,order", "1", "", "1 GLT\n", 1),
            ("result=(arg(\"G\",0)&&arg(\"S\",1))||((first()&&arg(\"T\",1))||arg(\"F\",0));print result,order", "0", "", "0 GLF\n", 1),
            ("result=first()?second():arg(\"F\",5);print result,order", "1", "9", "9 LR\n", 2),
            ("result=first()?second():arg(\"F\",5);print result,order", "0", "", "5 LF\n", 1),
            ("$0=\"new 0008\";print ($(first())~/^0008$/),order", "2", "", "1 L\n", 1)
        };
        foreach (var item in cases)
        for (var mask = 0; mask < (1 << item.Reads); mask++)
        foreach (var governed in new[] { false, true })
            yield return [item.Body, item.First, item.Second, item.Expected, item.Reads, mask, governed];
    }

    [Theory]
    [MemberData(nameof(Programs))]
    public Task NativeOperandsKeepOrderAndConsumeActualHostReadsOnce(
        string body, string first, string second, string expected, int reads, int pendingMask, bool governed) =>
        Run(body, first, second, expected, reads, pendingMask, governed, "success");

    public static IEnumerable<object[]> FailedPrograms()
    {
        foreach (var body in new[]
        {
            "result=bad()+late()", "result=(bad()<late())", "if(bad()&&late())print \"bad\"",
            "$(late())=bad()", "a[2]=7;a[2]+=$(bad())", "$0=\"one two\";result=($(bad())==\"one\")",
            "result=($(bad())~/^one$/)", "result=1+bad()", "result=(1==bad())"
        })
        foreach (var outcome in new[] { "runtime-fault", "host-fault", "cancel" })
        foreach (var pending in new[] { false, true })
        foreach (var governed in new[] { false, true })
            yield return [body, outcome, pending, governed];
    }

    [Theory]
    [MemberData(nameof(FailedPrograms))]
    public Task FailedOrCanceledChildStopsLaterEffectsAndUnwinds(
        string body, string outcome, bool pending, bool governed) =>
        Run(body, "2", "", "", 1, pending ? 1 : 0, governed, outcome);

    private static async Task Run(string body, string first, string second, string expected,
        int reads, int pendingMask, bool governed, string outcome)
    {
        using var cancellation = new CancellationTokenSource();
        var host = new MockFileSystem();
        host.AddFile("/first", first + "\n");
        host.AddFile("/second", second + "\n");
        var operations = Enumerable.Range(0, reads).Select(_ => new SingleConsumptionRead()).ToArray();
        var readCount = 0;
        host.ReadAsyncOverride = Read;
        ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            host.ReadAsyncOverride = null;
            try
            {
                var value = host.ReadAsync(descriptor, maximum, token);
                if (descriptor == AwkFileDescriptor.StdIn) return value;
                Assert.True(value.IsCompletedSuccessfully);
                var index = readCount++;
                var operation = operations[index];
                operation.Begin(value.Result);
                if ((pendingMask & (1 << index)) == 0) Complete(operation);
                return operation.Value;
            }
            finally { host.ReadAsyncOverride = Read; }
        }
        void Complete(SingleConsumptionRead operation)
        {
            if (outcome == "cancel") cancellation.Cancel();
            operation.Complete(outcome == "host-fault");
        }
        var source = "function first(v){order=order \"L\";getline v<\"/first\";return v}" +
            "function second(v){order=order \"R\";getline v<\"/second\";return v}" +
            "function mutating(v){order=order \"R\";a[1]=100;a[2]=100;getline v<\"/second\";return v}" +
            "function arg(tag,v){order=order tag;return v}" +
            "function bad(v){getline v<\"/first\";return 1/0}" +
            "function late(){print \"late\";return 1}" +
            "BEGIN{print \"prefix\";" + body + ";print \"suffix\"}";
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var run = tool.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            foreach (var operation in operations)
            {
                await operation.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                if (operation.IsPending)
                {
                    Assert.False(run.IsCompleted);
                    Assert.Equal("prefix\n", host.GetOutput(AwkFileDescriptor.StdOut));
                    if (governed) Assert.True(budget.Reserved > 0);
                    if (outcome == "cancel")
                    {
                        cancellation.Cancel();
                        Assert.False(run.IsCompleted);
                    }
                    Complete(operation);
                }
            }
            if (outcome == "cancel")
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else
                Assert.Equal(outcome == "success" ? AwkExitCode.Success : AwkExitCode.FatalError,
                    await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal("prefix\n" + (outcome == "success" ? expected + "suffix\n" : ""), host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(outcome switch
            {
                "host-fault" => "awk: execution failed without exposing host exception details\n",
                "runtime-fault" => "awk: division by zero attempted\n",
                _ => ""
            }, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(reads, readCount);
            Assert.All(operations, operation => Assert.Equal(1, operation.Consumptions));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            foreach (var operation in operations)
                if (operation.IsPending) Complete(operation);
            try { await run.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) when (outcome == "cancel") { }
        }
    }

    private sealed class SingleConsumptionRead : IValueTaskSource<AwkReadBuffer>
    {
        private ManualResetValueTaskSourceCore<AwkReadBuffer> _completion = new() { RunContinuationsAsynchronously = true };
        private AwkReadBuffer? _buffer;
        private bool _completed;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsPending => _buffer is not null && !_completed;
        public int Consumptions { get; private set; }
        public ValueTask<AwkReadBuffer> Value => new(this, _completion.Version);
        public void Begin(AwkReadBuffer buffer)
        {
            Assert.Null(_buffer);
            _buffer = buffer;
            Entered.TrySetResult();
        }
        public void Complete(bool fail)
        {
            Assert.False(_completed);
            _completed = true;
            var buffer = _buffer ?? throw new InvalidOperationException("No host read entered.");
            if (fail)
            {
                buffer.Dispose();
                _completion.SetException(new IOException("private host failure"));
            }
            else _completion.SetResult(buffer);
        }
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
