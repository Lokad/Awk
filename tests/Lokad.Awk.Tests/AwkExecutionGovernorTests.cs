using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk.Tests;

public sealed class AwkExecutionGovernorTests
{
    [Theory]
    [InlineData("BEGIN{x=1+2*3; print x; print (x ? x+1 : 0)}", "")]
    [InlineData("BEGIN{x=(1 && 2) || 3; if(x) print x; else print 0}", "")]
    [InlineData("BEGIN{a[1]=2; a[2]=3; print a[1]+a[2], (1 in a); delete a[1]}", "")]
    [InlineData("BEGIN{print \"before\"; x=length(\"abcd\"); printf \"%d\\n\",x; print x+1}", "")]
    [InlineData("function f(x){return x+1} BEGIN{print \"before\"; print f(1)+f(2)}", "")]
    [InlineData("BEGIN{for(i=0;i<3;i++) {print i; x+=i} print x}", "")]
    [InlineData("{print $0, $(1), $2, NF, NR, FNR} END{print \"end\"}", "a b\nc d\n")]
    [InlineData("/a/ {print $1} END{print NR}", "a b\nc d\n")]
    [InlineData("BEGIN{s=\"aaa\"; gsub(/a/,\"b\",s); print s}", "")]
    [InlineData("BEGIN{a[1]=3; a[2]=1; a[3]=2; asort(a,b); print b[1],b[3]}", "")]
    [InlineData("BEGIN{x=\"0\"; print !+ +x,!!x; print + + + + + + + +1}", "")]
    [InlineData("BEGIN{i=1;a[1]=2;print -++a[i++]^2,i,a[1];print !!a[1]}", "")]
    [InlineData("BEGIN{$0=\"2 3\";i=1;print $- -i^2,$!0^2;print !++$i,$0}", "")]
    [InlineData("BEGIN{i=0;print and(i++,i++,i++),or(i++,i++,i++),xor(i++,i++,i++),i}", "")]
    [InlineData("function f(x){print x;return x}BEGIN{print and(f(0),f(1),f(-0.1));print \"bad\"}", "")]
    [InlineData("BEGIN{print compl(0),lshift(1,63),rshift(2^63,1);print lshift(1,64)}", "")]
    [InlineData("BEGIN{a[\"x\"]=10;a[\"y\"]=2;a[\"z\"]=\"0\";n=asort(a,b);for(i=1;i<=n;i++)print b[i]}", "")]
    [InlineData("BEGIN{a[\"x\"]=10;a[\"y\"]=2;a[\"z\"]=\"0\";PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)print k,a[k]}", "")]
    [InlineData("BEGIN{a[1]=1;a[2]=2;PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a)n++;print n}", "")]
    public async Task EnginesAndObservationalCountersAdmitTheSameWork(string program, string input)
    {
        var compiled = await CompileAsync(program);
        for (var limit = 1; limit <= 80; limit++)
        {
            var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(limit);
            var interpreter = await RunAsync(compiled, input, AwkExecutionEngineKind.Interpreter, limits, true);
            var executable = await RunAsync(compiled, input, AwkExecutionEngineKind.Executable, limits, true);
            Assert.Equal(interpreter, executable);
            foreach (var engine in new[] { AwkExecutionEngineKind.Interpreter, AwkExecutionEngineKind.Executable })
            {
                var unobserved = await RunAsync(compiled, input, engine, limits, false);
                Assert.Equal(interpreter with { Steps = 0 }, unobserved);
            }
        }
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter, false)]
    [InlineData(AwkExecutionEngineKind.Executable, false)]
    [InlineData(AwkExecutionEngineKind.Interpreter, true)]
    [InlineData(AwkExecutionEngineKind.Executable, true)]
    public async Task DefaultDepthAdmitsOneThousandCallsAndRejectsTheNext(
        object engineValue, bool asynchronousHost)
    {
        var engine = Assert.IsType<AwkExecutionEngineKind>(engineValue);
        var read = asynchronousHost ? "getline value;" : "";
        // Include multiple expression and statement frames between recursive calls.
        var program = "function f(n, value){" + read +
            "if(n>1) return 1+(2+(3+(f(n-1)-5))); return 1}" +
            "BEGIN{print f(1000); print f(1001)}";
        var compiled = await CompileAsync(program);
        var host = new MockFileSystem();
        var reads = 0;
        if (asynchronousHost)
        {
            host.ReadLinesAsyncOverride = async (_, _, token) =>
            {
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
                token.ThrowIfCancellationRequested();
                reads++;
                return AwkReadResult.FromContent("x\n"u8.ToArray(), [], AwkReadCompletion.MoreData);
            };
        }

        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = Options(engine, AwkRuntimeLimits.CreateDefault(), counters);
        var exitCode = await compiled.ExecuteAsync(host, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("1000\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: function recursion limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.ExecutionSteps, 1, 50_000_000);
        if (asynchronousHost)
            Assert.True(reads >= 2_000);
    }

    [Theory]
    [InlineData(AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkRegexEngineKind.Utf8Adapter)]
    public void RegexSearchAdmitsItsInputWorkBeforeMatching(object regexEngineValue)
    {
        var regexEngine = Assert.IsType<AwkRegexEngineKind>(regexEngineValue);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(3), regexEngine,
            AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var regex = state.CreateRegex(new AwkRegexPattern("z"));

        var error = Assert.Throws<AwkRuntimeException>(() =>
            state.IsRegexMatch(regex, AwkText.FromString(new string('a', 32_768))));

        Assert.Equal("awk: execution step limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, counters.ExecutionSteps);
    }

    [Fact]
    public async Task CommonBudgetStopsSortingBeforeReplacingTheDestination()
    {
        var options = Options(AwkExecutionEngineKind.Interpreter,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(2).WithMaxExecutionMemoryBytes(1 << 20),
            AwkRuntimeCounters.Disabled);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var source = AwkArrayName.FromText("source");
        var destination = AwkArrayName.FromText("destination");
        var key = new AwkArrayKey("1");
        for (var index = 1; index <= 32; index++)
            state.SetArrayString(source, new AwkArrayKey(index.ToString()), (33-index).ToString());
        state.SetArrayString(destination, key, "keep");
        var committedBefore = state.MemoryGovernor.CurrentCommittedBytes;

        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
            state.SortArrayValuesAsync(source, destination, AwkArraySortOrder.Default,
                new AwkEvaluationContext(state)).AsTask());

        Assert.Equal("awk: execution step limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("keep", state.GetArrayByteText(destination, key).AsString(state.Counters));
        Assert.Equal(committedBefore, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    public async Task CommonBudgetStopsSubstitutionLoops(object engineValue)
    {
        var engine = Assert.IsType<AwkExecutionEngineKind>(engineValue);
        var compiled = await CompileAsync("{gsub(/a/,\"b\"); print $0}");
        var result = await RunAsync(compiled, new string('a', 10_000)+"\n", engine,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(100), true);
        Assert.Equal(AwkExitCode.FatalError.Value, result.ExitCode);
        Assert.Equal("", result.StdOut);
        Assert.Equal("awk: execution step limit exceeded\n", result.StdErr);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    public async Task ExplicitSecondaryCapStillAppliesToUserSortComparators(object engineValue)
    {
        var engine = Assert.IsType<AwkExecutionEngineKind>(engineValue);
        var compiled = await CompileAsync("function cmp(k1,v1,k2,v2){return v1-v2}" +
            "BEGIN{a[1]=3; a[2]=2; a[3]=1; print asort(a,b,\"cmp\")}");
        var result = await RunAsync(compiled, "", engine,
            AwkRuntimeLimits.CreateDefault().WithMaxSortComparisons(1), true);
        Assert.Equal(AwkExitCode.FatalError.Value, result.ExitCode);
        Assert.Equal("", result.StdOut);
        Assert.Equal("awk: array sort comparison limit exceeded\n", result.StdErr);
    }

    private static Task<AwkCompiledProgram> CompileAsync(string program) =>
        AwkExecutor.CompileAsync(new MockFileSystem(), AwkTestHarness.BuildInvocation("awk", program),
            CancellationToken.None);

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    internal async Task NestedArgumentsKeepTheClrStackBounded(AwkExecutionEngineKind engine)
    {
        var argument = "1";
        for (var index = 0; index < 120; index++)
            argument = "f(" + argument + ")";
        var compiled = await CompileAsync("function f(x){return x} BEGIN{print " + argument + "}");
        var result = await RunAsync(compiled, "", engine,
            AwkRuntimeLimits.CreateDefault().WithMaxFunctionCallDepth(1), true);
        Assert.Equal(AwkExitCode.Success.Value, result.ExitCode);
        Assert.Equal("1\n", result.StdOut);
        Assert.Equal("", result.StdErr);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    internal async Task RegexSeparatorRescansConsumeTheCommonAllowance(AwkExecutionEngineKind engine)
    {
        var compiled = await CompileAsync("BEGIN{RS=\"a+b\"} {print $0}");
        var host = new MockFileSystem();
        var reads = 0;
        var content = AwkUtf8Codec.Encode(new string('a', 4_096));
        host.ReadLinesOverride = (_, _) => ++reads <= 12
            ? AwkReadResult.FromContent(content, [], AwkReadCompletion.MoreData)
            : AwkReadResult.EndOfFile;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var exitCode = await compiled.ExecuteAsync(host, CancellationToken.None,
            Options(engine, AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(50), counters));

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: execution step limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(reads, 2, 11);
        Assert.True(counters.RecordSeparatorBytesScanned > reads * content.Length);
    }

    [Fact]
    public void CommonBudgetPreflightsLiteralSplittingBeforeArrayReplacement()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            Options(AwkExecutionEngineKind.Interpreter,
                AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(3).WithMaxExecutionMemoryBytes(1 << 20),
                AwkRuntimeCounters.Disabled));
        var destination = AwkArrayName.FromText("destination");
        var key = new AwkArrayKey("1");
        state.SetArrayString(destination, key, "keep");
        var committedBefore = state.MemoryGovernor.CurrentCommittedBytes;

        var error = Assert.Throws<AwkRuntimeException>(() => state.SplitIntoArray(
            AwkText.FromString(new string('a', 32_768)), destination,
            AwkFieldSeparatorSpec.FromText(","), AwkOptionalIdentifier.None));

        Assert.Equal("awk: execution step limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("keep", state.GetArrayByteText(destination, key).AsString(state.Counters));
        Assert.Equal(committedBefore, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void DefaultHostCapAndCancellationAreIndependentOfCounters()
    {
        var resources = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled);
        for (var index = 0; index < 100_000; index++)
            resources.RecordHostCall();
        var error = Assert.Throws<AwkRuntimeException>(resources.RecordHostCall);
        Assert.Equal("awk: host call limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var cancelled = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters, cancellation.Token);
        Assert.Throws<OperationCanceledException>(cancelled.RecordHostCall);
        Assert.Equal(0, counters.ExecutionSteps);
        Assert.Equal(0, counters.HostCalls);
        cancelled.RecordCleanupHostCall();
        Assert.Equal(1, counters.HostCalls);
        Assert.Equal(0, counters.ExecutionSteps);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    internal async Task CallerCancellationUnwindsDeepAsynchronousCalls(AwkExecutionEngineKind engine)
    {
        var compiled = await CompileAsync("function f(n){getline x; if(n>1) return f(n-1); return 1} BEGIN{print f(1000)}");
        using var cancellation = new CancellationTokenSource();
        var host = new MockFileSystem();
        var reads = 0;
        host.ReadLinesAsyncOverride = async (_, _, token) =>
        {
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            if (++reads == 500)
                cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return AwkReadResult.FromContent("x\n"u8.ToArray(), [], AwkReadCompletion.MoreData);
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => compiled.ExecuteAsync(host, cancellation.Token,
            Options(engine, AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled)));
        Assert.Equal(500, reads);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    private static AwkRuntimeOptions Options(
        AwkExecutionEngineKind engine, AwkRuntimeLimits limits, AwkRuntimeCounters counters) =>
        new(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, limits, AwkRegexEngineKind.DotNetCompatibility, engine);

    private static async Task<Result> RunAsync(AwkCompiledProgram compiled, string input,
        AwkExecutionEngineKind engine, AwkRuntimeLimits limits, bool observed)
    {
        var host = new MockFileSystem();
        host.SetStandardInput(input);
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var exitCode = await compiled.ExecuteAsync(host, CancellationToken.None, Options(engine, limits, counters));
        return new Result(exitCode.Value, host.GetOutput(AwkFileDescriptor.StdOut),
            host.GetOutput(AwkFileDescriptor.StdErr), counters.ExecutionSteps);
    }

    private readonly record struct Result(int ExitCode, string StdOut, string StdErr, long Steps);
}
