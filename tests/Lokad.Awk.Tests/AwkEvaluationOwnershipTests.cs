using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkEvaluationOwnershipTests
{
    public static IEnumerable<object[]> Captures()
    {
        foreach (var expression in new[]
        {
            "f(x,wipe())", "print index(x,wipe())", "print substr(x,wipe()+1,1)",
            "print sprintf(\"%s\",x,wipe())==\"\"", "print x==wipe()",
            "if(x==wipe())print 1", "print x==$(wipe()+1)", "if(x==$(wipe()+1))print 1",
            "a[wipe()]=x", "$(wipe()+1)=x",
            "print x~wipe()", "print match(x,wipe())", "print split(x,a,wipe()+1)",
            "print patsplit(x,a,wipe()+1)", "sub(/z/,x,a[wipe()])", "gsub(/z/,x,a[wipe()])",
            "print close(x,substr(\"to\"wipe(),1,2))",
            "print x,wipe()", "print x,wipe(),1", "print x,1,wipe(),2", "print x,wipe(),1,2,3",
            "printf \"%s%s\\n\",x,wipe()", "printf x,wipe()",
            "print length(x wipe())",
            "print length(x wipe() \"a\" \"b\" \"c\" \"d\" \"e\" \"f\" \"g\")"
        }) yield return [expression];
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CompositeSeparatorAndCompoundKeySurviveLaterMutation(bool compound)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        long held = 0;
        var host = new OwnershipHost { OnHeld = () => held = budget.Reserved };
        var script = compound
            ? "function wipe(){delete a;x=\"\";print \"held\";return 1}" +
              "BEGIN{x=sprintf(\"%01000000d\",0);a[x]=2;a[x]+=wipe();print a[sprintf(\"%01000000d\",0)]}"
            : "function wipe(){SUBSEP=\"\";print \"held\";return 0}" +
              "BEGIN{SUBSEP=sprintf(\"%01000000d\",0);a[wipe(),1]=1;print length(a)}";
        var exit = await ExecuteAsync(script, host, budget);
        Assert.Equal(0, exit.Value);
        Assert.Equal(compound ? "held\n3\n" : "held\n1\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.True(held >= 1_000_000);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [MemberData(nameof(Captures))]
    public async Task CapturedTextStaysFundedAfterItsGlobalIsCleared(string expression)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        long held = 0;
        var host = new OwnershipHost { OnHeld = () => held = budget.Reserved };
        var exit = await ExecuteAsync("function wipe(){x=\"\";print \"held\";return 0}" +
            "function f(a,b){print length(a)} BEGIN{x=sprintf(\"%01000000d\",0);" + expression + "}", host, budget);
        Assert.Equal(0, exit.Value);
        Assert.True(held >= 1_000_000, "Captured text was refunded before its last use: " + held);
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(16, false)] [InlineData(128, true)]
    public async Task NestedArgumentsCannotHideTheirRetainedStrings(int megabytes, bool succeeds)
    {
        var budget = new CooperativeTestBudget(megabytes << 20);
        var host = new OwnershipHost();
        var exit = await ExecuteAsync("""
            function hold(a,b){return length(a)+b}
            function fill(n){
                if(!n){x="";print "held";return 0}
                x=sprintf("%01000000d",0)
                return hold(x,fill(n-1))
            }
            BEGIN{print "base";print fill(32);print "done"}
            """, host, budget);
        Assert.Equal(succeeds ? 0 : 2, exit.Value);
        Assert.Equal(succeeds ? "base\nheld\n32000000\ndone\n" : "base\n", host.Output);
        Assert.Equal(succeeds ? "" : "awk: memory limit exceeded\n", host.Error);
        if (succeeds) Assert.True(budget.Peak >= 32_000_000);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(16, false)] [InlineData(128, true)]
    public async Task NestedCloseNamesStayFundedDuringDirectionEvaluation(int megabytes, bool succeeds)
    {
        var budget = new CooperativeTestBudget(megabytes << 20);
        var host = new OwnershipHost();
        var exit = await ExecuteAsync("""
            function fill(n){
                if(!n){x="";print "held";return "to"}
                x=sprintf("%01000000d",0)
                if(close(x,fill(n-1))<0)failures++
                return "to"
            }
            BEGIN{print "base";fill(32);print failures;print "done"}
            """, host, budget);
        Assert.Equal(succeeds ? 0 : 2, exit.Value);
        Assert.Equal(succeeds ? "base\nheld\n32\ndone\n" : "base\n", host.Output);
        Assert.Equal(succeeds ? "" : "awk: memory limit exceeded\n", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, "arguments")] [InlineData(true, "arguments")]
    [InlineData(false, "snapshot")] [InlineData(true, "snapshot")]
    [InlineData(false, "concat")] [InlineData(true, "concat")]
    [InlineData(false, "assignment")] [InlineData(true, "assignment")]
    [InlineData(false, "compound")] [InlineData(true, "compound")]
    [InlineData(false, "pattern")] [InlineData(true, "pattern")]
    [InlineData(false, "print")] [InlineData(true, "print")]
    [InlineData(false, "printf-values")] [InlineData(true, "printf-values")]
    [InlineData(false, "printf-format")] [InlineData(true, "printf-format")]
    [InlineData(false, "close")] [InlineData(true, "close")]
    public async Task PendingOperandKeepsTextFundedUntilHostCompletion(bool cancel, string kind)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(16 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new OwnershipHost { PendingHeld = () => { entered.SetResult(); return completed.Task; } };
        var script = kind == "snapshot"
            ? "BEGIN{for(i=0;i<64;i++)a[sprintf(\"%016384d\",i)]=i;for(k in a){delete a;k=\"\";print \"held\";break}}"
            : "function wipe(){delete a;x=\"\";print \"held\";return 0}" +
              "function f(a,b){print length(a)} BEGIN{x=sprintf(\"%01000000d\",0);" +
              (kind switch
              {
                  "arguments" => "f(x,wipe())",
                  "concat" => "print length(x wipe())",
                  "assignment" => "a[wipe()]=x",
                  "compound" => "a[x]=2;a[x]+=wipe()",
                  "pattern" => "print match(x,wipe())",
                  "print" => "print x,wipe()",
                  "printf-values" => "printf \"%s%s\\n\",x,wipe()",
                  "printf-format" => "printf x,wipe()",
                  "close" => "print close(x,substr(\"to\"wipe(),1,2))",
                  _ => throw new ArgumentOutOfRangeException(nameof(kind))
              }) + "}";
        var execution = ExecuteAsync(script, host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(budget.Reserved >= 1_000_000);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 1_000_000);
        }
        finally { completed.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else Assert.Equal(0, (await execution).Value);
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task IterationRetainsKeyFundingAcrossSourceReplacement(bool sorted, bool replace)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        long held = 0;
        var host = new OwnershipHost { OnHeld = () => held = budget.Reserved };
        var script = "BEGIN{for(i=0;i<64;i++)a[sprintf(\"%04096d\",i)]=i;" +
            (sorted ? "PROCINFO[\"sorted_in\"]=\"@ind_str_asc\";" : "") +
            "for(k in a){if(!n++){" + (replace ? "split(\"z\",a)" : "delete a") +
            ";k=\"\";print \"held\"};total+=length(k)};print n,total}";
        var exit = await ExecuteAsync(script, host, budget);
        Assert.Equal(0, exit.Value);
        Assert.Equal("held\n64 258048\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.True(held >= 64 * 4096);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SnapshotRefusesBeforeItsBodyChangesTheSource(bool sorted)
    {
        var budget = new CooperativeTestBudget(1_200_000);
        var host = new OwnershipHost();
        var exit = await ExecuteAsync("BEGIN{for(i=0;i<64;i++)a[sprintf(\"%04096d\",i)]=i;" +
            (sorted ? "PROCINFO[\"sorted_in\"]=\"@ind_str_asc\";" : "") +
            "print \"before\";for(k in a){delete a;print \"held\"}}", host, budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task SortRetainsValuesAfterTheComparisonClearsItsSource()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        long held = 0;
        var host = new OwnershipHost { OnHeld = () => held = budget.Reserved };
        var exit = await ExecuteAsync("function cmp(i1,v1,i2,v2){if(!n++){delete a;print \"held\"};return i1-i2}" +
            "BEGIN{for(i=0;i<64;i++)a[i]=sprintf(\"%04096d\",i);print asort(a,b,\"cmp\");print length(b[1])}", host, budget);
        Assert.Equal(0, exit.Value);
        Assert.Equal("held\n64\n4096\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.True(held >= 64 * 4096);
        Assert.Equal(0, budget.Reserved);
    }

    private static Task<AwkExitCode> ExecuteAsync(string source, IAwkHost host, IMemoryBudget budget,
        CancellationToken token = default) =>
        AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []))
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), token);

    private sealed class OwnershipHost : IAwkHost
    {
        private readonly StringBuilder _output = new();
        private readonly StringBuilder _error = new();
        public string Output => _output.ToString();
        public string Error => _error.ToString();
        public Action? OnHeld { get; init; }
        public Func<Task>? PendingHeld { get; init; }
        public async Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (descriptor == AwkFileDescriptor.StdErr) _error.Append(Encoding.UTF8.GetString(content.Span));
            else if (descriptor == AwkFileDescriptor.StdOut)
            {
                _output.Append(Encoding.UTF8.GetString(content.Span));
                if (content.Span.SequenceEqual("held\n"u8))
                {
                    OnHeld?.Invoke();
                    if (PendingHeld is { } pending) await pending().ConfigureAwait(false);
                }
            }
            return AwkExitCode.Success;
        }
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => Task.FromResult(AwkReadResult.EndOfFile);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) => Task.FromResult(AwkOpenedFile.MissingFile());
        public Task<AwkOperationStatus> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Task.FromResult(AwkOperationStatus.Success);
        public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands, CancellationToken token) => Task.FromResult<IReadOnlyList<AwkExitCode>>([AwkExitCode.Success]);
        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken token) => Task.FromResult(AwkPipeCreationResult.NotSupported(default));
    }
}
