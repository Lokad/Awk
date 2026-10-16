using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SignedPrintfAndSprintfUseIntegerDigits(bool sprintf)
    {
        var script = sprintf ? "{s=sprintf(\"%+06d:%06i\",$1,$2);print s}"
            : "{printf \"%+06d:%06i\\n\",$1,$2}";
        var result = await RunAsync([script], "7 -7\n12 0\n",
            AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("+00007:-00007\n+00012:000000\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(4, result.Counters.PrintfSignedIntegerFastPaths);
        Assert.Equal(2, result.Counters.FormattingFallbacks);
        Assert.Equal(1, result.Counters.StringDecodes);
    }

    [Fact]
    public async Task MultiFieldFilterUsesTheExistingBoundedCpuTree()
    {
        var result = await RunAsync(["$1==\"A\"&&$2==\"B\"&&$3==\"C\"{n++} END{print n+0}"],
            "A B C\nA B D\nD B C\n", AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(4, result.Counters.SynchronousExpressionRuns);
        Assert.Equal(0, result.Counters.StringDecodes);
    }

    [Fact]
    public async Task CompleteStringActionsUseBoundedSynchronousSubtrees()
    {
        var result = await RunAsync(["{s=substr($0,4,4);total+=length(s)+index($0,\"beta\")+length(tolower(s))} END{print total}"],
            "id:beta:tail\nid:beta:tail\n", AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("24\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(5, result.Counters.SynchronousExpressionRuns);
        Assert.Equal(0, result.Counters.StringDecodes);
    }

    [Fact]
    public async Task CompleteFieldLoopsUseTheExistingSynchronousRegion()
    {
        var result = await RunAsync(["{for(i=1;i<=NF;i++){nonempty+=($i!=\"\");chars+=length($i)}} END{print nonempty,chars}"],
            "aa b\né xyz\n", AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("4 7\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.SynchronousLoopRuns);
        Assert.Equal(0, result.Counters.StringDecodes);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    internal async Task PurePrintfFieldsBorrowBytesAndNumbersBeforeOwnedOutput(AwkExecutionEngineKind engine)
    {
        const string input = "sku 7\nother 8\n";
        var borrowed = await RunAsync(["-v", "i=1", "-v", "j=2", "{printf \"%s:%04d\\n\",$1,$2}"], input,
            AwkRegexEngineKind.DotNetCompatibility, engine);
        var owned = await RunAsync(["-v", "i=1", "-v", "j=2", "{printf \"%s:%04d\\n\",$i,$j}"], input,
            AwkRegexEngineKind.DotNetCompatibility, engine);
        Assert.Equal(0, borrowed.ExitCode);
        Assert.Equal(owned.ExitCode, borrowed.ExitCode);
        Assert.Equal("sku:0007\nother:0008\n", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(owned.FileSystem.GetOutput(AwkFileDescriptor.StdOut), borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(4, owned.Counters.AwkTextAllocations - borrowed.Counters.AwkTextAllocations);
        Assert.Equal(10, owned.Counters.AwkTextPayloadBytes - borrowed.Counters.AwkTextPayloadBytes);
        Assert.Equal(owned.Counters.FieldsMaterialized, borrowed.Counters.FieldsMaterialized);
        Assert.Equal(owned.Counters.ExecutionSteps, borrowed.Counters.ExecutionSteps);
        Assert.Equal(owned.Counters.Utf8Encodes, borrowed.Counters.Utf8Encodes);
        Assert.Equal(owned.Counters.StringDecodes, borrowed.Counters.StringDecodes);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    internal async Task PrintfBorrowsStagingInsteadOfAllocatingAnOwnedResult(AwkExecutionEngineKind engine)
    {
        const string input = "sku\t7\nsku\t8\n";
        var direct = await RunAsync(["-v", "i=1", "-v", "j=2", "BEGIN{ORS=\"\"}{printf \"%s:%04d\\n\",$i,$j}"], input,
            AwkRegexEngineKind.DotNetCompatibility, engine);
        var owned = await RunAsync(["-v", "i=1", "-v", "j=2", "BEGIN{ORS=\"\"}{print sprintf(\"%s:%04d\\n\",$i,$j)}"], input,
            AwkRegexEngineKind.DotNetCompatibility, engine);
        Assert.Equal(0, direct.ExitCode);
        Assert.Equal(0, owned.ExitCode);
        Assert.Equal("sku:0007\nsku:0008\n", direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut), owned.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", direct.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("", owned.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, owned.Counters.AwkTextAllocations - direct.Counters.AwkTextAllocations);
        Assert.Equal(18, owned.Counters.AwkTextPayloadBytes - direct.Counters.AwkTextPayloadBytes);
        Assert.Equal(owned.Counters.Utf8Encodes, direct.Counters.Utf8Encodes);
        Assert.Equal(owned.Counters.StringDecodes, direct.Counters.StringDecodes);
    }

    [Theory]
    [InlineData("{x=$1;for(i=0;i<8;i++)x=(x*3+1)%101;total+=x} END{print total}", "40\n")]
    [InlineData("function step(x){return (x*3+1)%101} {x=$1;for(i=0;i<8;i++)x=step(x);total+=x} END{print total}", "40\n")]
    [InlineData("function inc(x){return x+1} {x=$1;for(i=0;i<8;i++)x=(x*3+1)%101;total+=inc(inc(x))} END{print total}", "44\n")]
    public async Task PureForAndNumericReturnBodiesUsePreparedSynchronousRegions(string script, string expected)
    {
        var result = await RunAsync([script], "7\n7\n", AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(expected, result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.SynchronousLoopRuns);
        Assert.True(result.Counters.SynchronousExpressionRuns > 0);
        Assert.Equal(0, result.Counters.FunctionStackSuspensions);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData("a[$1]++", false)]
    [InlineData("a[$1]++", true)]
    [InlineData("a[$1]+=1", false)]
    [InlineData("a[$1]+=1", true)]
    public async Task ExistingUpdateTargetsReuseOwnedKeysWithoutFieldCopies(string mutation, bool miss)
    {
        var input = "hit payload\nhit other\n" + (miss ? "missing value\n" : "");
        var script = "BEGIN{a[\"hit\"]=7}{" + mutation + "}END{print a[\"hit\"],length(a)}";
        var direct = await RunAsync([script], input, AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var ownedScript = "BEGIN{a[\"hit\"]=7}{v=$1;" + mutation.Replace("$1", "v", StringComparison.Ordinal) + "}END{print a[\"hit\"],length(a)}";
        var owned = await RunAsync([ownedScript], input, AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        Assert.Equal(0, direct.ExitCode);
        Assert.Equal(0, owned.ExitCode);
        Assert.Equal(miss ? "9 2\n" : "9 1\n", direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut), owned.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", direct.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, owned.Counters.AwkTextAllocations - direct.Counters.AwkTextAllocations);
        Assert.Equal(0, direct.Counters.StringDecodes);
        Assert.Equal(0, direct.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingArrayReadsBorrowFieldKeysButMissesStillCreateElements(bool miss)
    {
        var input = "hit payload\nhit other\n" + (miss ? "missing value\n" : "");
        var borrowed = await RunAsync(["BEGIN{a[\"hit\"]=7}{sum+=a[$1]}END{print sum,length(a)}"], input,
            AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        var owned = await RunAsync(["BEGIN{a[\"hit\"]=7}{v=$1;sum+=a[v]}END{print sum,length(a)}"], input,
            AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        Assert.Equal(0, borrowed.ExitCode);
        Assert.Equal(0, owned.ExitCode);
        Assert.Equal(miss ? "14 2\n" : "14 1\n", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut), owned.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, owned.Counters.AwkTextAllocations - borrowed.Counters.AwkTextAllocations);
        Assert.Equal(0, borrowed.Counters.StringDecodes);
        Assert.Equal(0, borrowed.Counters.Utf8Encodes);
    }

    [Fact]
    public async Task FieldMembershipAvoidsCopiedKeysAndScalarCachePublication()
    {
        const string input = "hit payload\nmiss payload\nhit other\n";
        var borrowed = await RunAsync(["BEGIN{lookup[\"hit\"]=1} ($1 in lookup){n++} END{print n+0}"], input,
            AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        var owned = await RunAsync(["BEGIN{lookup[\"hit\"]=1} {v=$1;if(v in lookup)n++} END{print n+0}"], input,
            AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        Assert.Equal(0, borrowed.ExitCode);
        Assert.Equal(0, owned.ExitCode);
        Assert.Equal("2\n", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("2\n", owned.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(3, owned.Counters.AwkTextAllocations - borrowed.Counters.AwkTextAllocations);
        Assert.Equal(0, borrowed.Counters.StringDecodes);
        Assert.Equal(0, borrowed.Counters.Utf8Encodes);
        Assert.Equal(3, borrowed.Counters.FieldSplits);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    internal async Task WideEarlyFilteringPreparesOnlyTheReachedFields(AwkExecutionEngineKind engine)
    {
        var tail = string.Join(" ", Enumerable.Repeat("tail", 61));
        var input = string.Concat(Enumerable.Repeat("INFO worker ignored " + tail + "\n", 7)) +
            "ERROR worker payload " + tail + "\n";
        var result = await RunAsync(["$1==\"ERROR\"{print $3}"], input,
            AwkRegexEngineKind.DotNetCompatibility, engine);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("payload\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(8, result.Counters.FieldSplits);
        // The optional VM's field opcode still reads NF before loading a field.
        Assert.Equal(engine == AwkExecutionEngineKind.Interpreter ? 10 : 512, result.Counters.FieldsMaterialized);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    internal async Task OrdinalArrayKeysKeepMixedTextLookupsByteNative(AwkExecutionEngineKind engine)
    {
        var result = await RunAsync(
            ["BEGIN{a[0]=7;a[-1]=8;a[2147483647]=9;a[-2147483648]=10;a[\"01\"]=11;print a[\"0\"],a[\"-1\"],a[\"2147483647\"],a[\"-2147483648\"],a[1]+0,a[\"01\"],length(a)}"],
            "", AwkRegexEngineKind.DotNetCompatibility, engine);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("7 8 9 10 0 11 6\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.AwkTextAsStringCalls);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter, false)]
    [InlineData(AwkExecutionEngineKind.Interpreter, true)]
    [InlineData(AwkExecutionEngineKind.Executable, false)]
    [InlineData(AwkExecutionEngineKind.Executable, true)]
    internal async Task PlainPrintfTextAvoidsArgumentStringBridges(AwkExecutionEngineKind engine, bool sprintf)
    {
        string Script(bool width) => sprintf
            ? "{printf \"%s\",sprintf(\"<%" + (width ? "1" : "") + "s>|%" + (width ? "1" : "") + "s\\n\",$1,$2)}"
            : "{printf \"<%" + (width ? "1" : "") + "s>|%" + (width ? "1" : "") + "s\\n\",$1,$2}";
        var direct = await RunAsync([Script(false)], "café 😀\n", AwkRegexEngineKind.DotNetCompatibility, engine);
        var general = await RunAsync([Script(true)], "café 😀\n", AwkRegexEngineKind.DotNetCompatibility, engine);
        Assert.Equal(0, direct.ExitCode);
        Assert.Equal(0, general.ExitCode);
        Assert.Equal("<café>|😀\n", direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut), general.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", direct.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("", general.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, general.Counters.StringDecodes - direct.Counters.StringDecodes);
        Assert.Equal(2, general.Counters.FormattingStringInteropInputs - direct.Counters.FormattingStringInteropInputs);
        Assert.Equal(general.Counters.ExecutionSteps, direct.Counters.ExecutionSteps);
        Assert.Equal(general.Counters.OutputBytes, direct.Counters.OutputBytes);
        Assert.Equal(general.Counters.Utf8Encodes, direct.Counters.Utf8Encodes);
    }

    [Fact]
    public async Task Awk_NumericFieldConsumptionAvoidsCopiedTextWithoutStringInterop()
    {
        const string input = "k 007\nk 007\nk 007\n";
        var direct = await RunAsync(["{a[1]+=$2} END{print a[1]}"], input);
        var cached = await RunAsync(["{v=$2;a[1]+=v} END{print a[1]}"], input);
        Assert.Equal(0, direct.ExitCode);
        Assert.Equal(0, cached.ExitCode);
        Assert.Equal("21\n", direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("21\n", cached.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(3, cached.Counters.AwkTextAllocations - direct.Counters.AwkTextAllocations);
        Assert.Equal(0, direct.Counters.StringDecodes);
        Assert.Equal(0, direct.Counters.StringInteropFallbacks);
    }

    [Fact]
    public async Task Awk_FieldLiteralComparisonsKeepByteComparisonsAndAvoidFieldMaterialization()
    {
        const string input = "alpha tail\nalpha tail\nalpha tail\n";
        var borrowed = await RunAsync(["$1==\"alpha\"{n++} END{print n}"], input);
        var materialized = await RunAsync(["{v=$1;if(v==\"alpha\")n++} END{print n}"], input);
        Assert.Equal(0, borrowed.ExitCode);
        Assert.Equal(0, materialized.ExitCode);
        Assert.Equal("3\n", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("3\n", materialized.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(3, borrowed.Counters.ByteTextComparisons);
        Assert.Equal(3, materialized.Counters.ByteTextComparisons);
        Assert.Equal(3, borrowed.Counters.FieldSplits);
        Assert.Equal(3, materialized.Counters.AwkTextAllocations - borrowed.Counters.AwkTextAllocations);
        Assert.Equal(0, borrowed.Counters.StringInteropFallbacks);
        Assert.Equal(0, borrowed.Counters.StringDecodes);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkExecutionEngineKind.Interpreter, AwkRegexEngineKind.Utf8Adapter)]
    [InlineData(AwkExecutionEngineKind.Executable, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkExecutionEngineKind.Executable, AwkRegexEngineKind.Utf8Adapter)]
    internal async Task Awk_UnicodeCallersPreserveRuntimeTextBoundaries(AwkExecutionEngineKind engine,
        AwkRegexEngineKind backend)
    {
        foreach (var context in new[] { 0, 1, 2 })
        {
            string[] Arguments(string value) => context switch
            {
                0 => ["-v", "v=" + value, "BEGIN{print v;exit}"],
                1 => ["END{print v}", "v=" + value],
                _ => ["-F", value, "BEGIN{print FS;exit}"]
            };
            var control = await RunAsync(Arguments("A"), string.Empty, backend, engine);
            var escaped = await RunAsync(Arguments(@"\u0041"), string.Empty, backend, engine);
            Assert.Equal(0, control.ExitCode);
            Assert.Equal(0, escaped.ExitCode);
            Assert.Equal("A\n", escaped.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, escaped.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(control.Counters.StringDecodes, escaped.Counters.StringDecodes);
            Assert.Equal(control.Counters.Utf8Encodes, escaped.Counters.Utf8Encodes);
            Assert.Equal(control.Counters.AwkTextAsStringCalls, escaped.Counters.AwkTextAsStringCalls);
            Assert.Equal(control.Counters.AwkTextAllocations, escaped.Counters.AwkTextAllocations);
        }
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkExecutionEngineKind.Executable, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkExecutionEngineKind.Interpreter, AwkRegexEngineKind.Utf8Adapter)]
    [InlineData(AwkExecutionEngineKind.Executable, AwkRegexEngineKind.Utf8Adapter)]
    internal async Task Awk_UnicodeRegexEscapesPreserveRuntimeBoundaries(AwkExecutionEngineKind engine,
        AwkRegexEngineKind regex)
    {
        foreach (var literal in new[] { false, true })
        {
            string Script(string pattern) => literal
                ? "BEGIN{for(i=0;i<3;i++)v=(\"A\"~/" + pattern + "/);print v;exit}"
                : "BEGIN{p=\"" + pattern + "\";for(i=0;i<3;i++)v=(\"A\"~p);print v;exit}";
            var control = await RunAsync([Script("A")], "", regex, engine);
            var escaped = await RunAsync([Script(literal ? "\\u0041" : "\\\\u0041")], "", regex, engine);
            Assert.Equal(0, escaped.ExitCode);
            Assert.Equal("1\n", escaped.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, escaped.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(control.Counters.RegexCompiles, escaped.Counters.RegexCompiles);
            Assert.InRange(escaped.Counters.RegexCompiles, 0, 1);
            Assert.Equal(3, escaped.Counters.RegexMatches);
            Assert.Equal(control.Counters.StringDecodes, escaped.Counters.StringDecodes);
            Assert.Equal(control.Counters.Utf8Encodes, escaped.Counters.Utf8Encodes);
            Assert.Equal(control.Counters.StringInteropFallbacks, escaped.Counters.StringInteropFallbacks);
            Assert.Equal(control.Counters.AwkTextAllocations, escaped.Counters.AwkTextAllocations);
        }
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkExecutionEngineKind.Executable, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkExecutionEngineKind.Interpreter, AwkRegexEngineKind.Utf8Adapter)]
    [InlineData(AwkExecutionEngineKind.Executable, AwkRegexEngineKind.Utf8Adapter)]
    internal async Task Awk_UnicodeStringEscapesShareLiteralRuntimeCounters(AwkExecutionEngineKind engine, AwkRegexEngineKind regex)
    {
        var control = await RunAsync(["BEGIN{v=\"\U0001f600\";printf \"%s\",v;exit}"], "", regex, engine);
        // Plain %s keeps the value in UTF-8; only the source escape boundary decodes.
        Assert.Equal(1, control.Counters.StringDecodes);
        foreach (var literal in new[] { "\\u1f600", "\U0001f600" })
        {
            var (exit, host, counters) = await RunAsync(
                ["BEGIN{v=\"" + literal + "\";printf \"%s\",v;exit}"], "", regex, engine);
            Assert.Equal(0, exit);
            Assert.Equal("\U0001f600", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(control.Counters.StringDecodes, counters.StringDecodes);
            Assert.Equal(control.Counters.Utf8Encodes, counters.Utf8Encodes);
            Assert.Equal(control.Counters.StringInteropFallbacks, counters.StringInteropFallbacks);
            Assert.Equal(control.Counters.FormattingFallbacks, counters.FormattingFallbacks);
            Assert.Equal(control.Counters.AwkTextAllocations, counters.AwkTextAllocations);
        }
    }

    private const string InvalidUtf8Diagnostic = "awk: invalid UTF-8 input\n";
    private const string StepCalibrationInput =
        "sku1 region1 open 10\n" +
        "sku2 region2 closed 20\n" +
        "sku1 region2 open 30\n";

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    internal async Task Awk_AssignedFieldCountTextRemainsByteNative(AwkExecutionEngineKind engine)
    {
        var (exitCode, host, counters) = await RunAsync(
            ["BEGIN{$0=\"a b c\";NF=\"01.8\";$1=\"z\";print NF,$0}"],
            "", AwkRegexEngineKind.DotNetCompatibility, engine);

        Assert.Equal(0, exitCode);
        Assert.Equal("01.8 z\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, counters.FieldSplits);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringInteropFallbacks);
        Assert.Equal(0, counters.FormattingFallbacks);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    internal async Task Awk_IntegerPrintBypassesCustomFormatWithoutStringInterop(AwkExecutionEngineKind engine)
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{OFMT=\"%.2f\";print 1000000,9007199254740992,9223372036854775808,1e30,-0}"],
            string.Empty, AwkRegexEngineKind.DotNetCompatibility, engine);
        Assert.Equal(0, exitCode);
        Assert.Equal("1000000 9007199254740992 9223372036854775808 1000000000000000019884624838656 0\n",
            fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
    }

    [Fact]
    public void Awk_RuntimeCountersDoNotLoseConcurrentUpdates()
    {
        const int updateCount = 10_000;
        var counters = AwkRuntimeCounters.CreateEnabled();

        Parallel.For(0, updateCount, _ => counters.RecordExecutionStep());

        Assert.Equal(updateCount, counters.ExecutionSteps);
    }

    [Fact]
    public async Task Awk_BenchmarkCountersCoverDynamicRecordSeparators()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"|\"} {print $0; RS=\"([,;])+\"}"],
            input: "first|second,third;");

        Assert.Equal(0, exitCode);
        Assert.Equal("first\nsecond\nthird\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.RegexMatches);
        Assert.True(counters.RecordSeparatorResolutions > 0);
    }

    [Fact]
    public async Task Awk_NonAsciiRecordSeparatorDoesNotMoveTailPerRecord()
    {
        var input = new StringBuilder();
        for (var index = 0; index < 1_000; index++)
            input.Append("xé");

        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"é\"} {records++} END{print records}"],
            input.ToString());

        Assert.Equal(0, exitCode);
        Assert.Equal("1000\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.RecordSeparatorTailBytesMoved);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter)]
    [InlineData(AwkExecutionEngineKind.Executable)]
    internal async Task Awk_LiteralRecordSeparatorScanWorkScalesWithInput(AwkExecutionEngineKind engine)
    {
        var row = "<row><cell>" + new string('a', 1_000) + "</cell></row>";
        foreach (var recordCount in new[] { 100, 400, 1_600 })
        {
            var input = string.Concat(Enumerable.Repeat(row, recordCount));
            var (exitCode, fileSystem, counters) = await RunAsync(
                ["BEGIN{RS=\"</row>\"} {n++} END{print n}"],
                input, AwkRegexEngineKind.DotNetCompatibility, engine);

            Assert.Equal(0, exitCode);
            Assert.Equal(recordCount + "\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
            Assert.InRange(counters.RecordSeparatorBytesScanned, input.Length, input.Length * 2L);
            Assert.Equal(0, counters.RegexMatches);
            Assert.Equal(0, counters.RegexCompiles);
            Assert.Equal(0, counters.RecordSeparatorTailBytesMoved);
        }
    }

    [Fact]
    public async Task Awk_LiteralRecordSeparatorPreservesPartialMatchesAcrossBatches()
    {
        var prefix = string.Concat(Enumerable.Repeat("line\n",
            AwkRuntimeLimits.DefaultInputReadBatchLineCount - 1));
        var input = prefix + "one</\nrow>two</\nrow>";
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"</\\nrow>\"} {n++} END{print n}"], input);

        Assert.Equal(0, exitCode);
        Assert.Equal("2\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.RecordSeparatorBytesScanned, input.Length, input.Length * 2L);
        Assert.Equal(0, counters.RegexMatches);
    }

    [Fact]
    public async Task Awk_RegexEnumerationInspectsInputPolicyOnce()
    {
        var input = new string('x', 1_000) + "é\n";
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["-b", "{gsub(/x/, \"\"); print length($0)}"],
            input,
            AwkRegexEngineKind.DotNetCompatibility);

        Assert.Equal(0, exitCode);
        Assert.Equal("2\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1_001, counters.RegexPolicyInputBytesInspected);
        Assert.Equal(1_001, counters.RegexMatches);
    }

    [Fact]
    public async Task Awk_SubstitutionMaterializesOnlyObservableGroups()
    {
        var substitution = await RunAsync(
            ["BEGIN{s=\"xx\"; gsub(/(x)/,\"<&>\",s); print s}"]);
        var gensub = await RunAsync([
            "BEGIN{print gensub(/(x)(a)?(b)?(c)?(d)?(e)?(f)?(g)?(h)?(i)?(j)?/," +
            "\"<\\\\1>\",\"g\",\"xx\")}"
        ]);
        var wholeMatch = await RunAsync([
            "BEGIN{print gensub(/(x)(a)?(b)?(c)?(d)?(e)?(f)?(g)?(h)?(i)?(j)?/," +
            "\"<&>\",\"g\",\"xx\")}"
        ]);
        var escapedOnly = await RunAsync([
            "BEGIN{print gensub(/(x)(a)?(b)?(c)?(d)?(e)?(f)?(g)?(h)?(i)?(j)?/," +
            "\"<\\\\q>\",\"g\",\"xx\")}"
        ]);

        Assert.Equal(0, substitution.ExitCode);
        Assert.Equal(0, gensub.ExitCode);
        Assert.Equal(0, wholeMatch.ExitCode);
        Assert.Equal(0, escapedOnly.ExitCode);
        Assert.Equal("<x><x>\n", substitution.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("<x><x>\n", gensub.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("<x><x>\n", wholeMatch.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("<q><q>\n", escapedOnly.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, substitution.Counters.RegexGroupsMaterialized);
        Assert.Equal(4, gensub.Counters.RegexGroupsMaterialized);
        Assert.Equal(2, wholeMatch.Counters.RegexGroupsMaterialized);
        Assert.Equal(0, escapedOnly.Counters.RegexGroupsMaterialized);
    }

    [Fact]
    public async Task Awk_BenchmarkCountersCoverMatchCaptureMaterialization()
    {
        var result = await RunAsync([
            "BEGIN{if(match(\"abc123\",/([a-z]+)([0-9]+)/,m)) print m[1],m[2]}"
        ]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("abc 123\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, result.Counters.RegexMatches);
        Assert.Equal(3, result.Counters.RegexGroupsMaterialized);
    }

    [Fact]
    public async Task Awk_DynamicUtf8RegexCompilesOnlySelectedEngine()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{pattern=\"(x)+\"; print match(\"xx\", pattern)}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("1\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
    }

    [Fact]
    public async Task Awk_RegexCacheRetainsHotPatternDuringChurn()
    {
        var (exitCode, fileSystem, counters) = await RunAsync([
            "BEGIN{hot=\"^hot$\"; for(i=0;i<80;i++){match(\"hot\",hot); cold=\"^cold\" i \"$\"; match(\"none\",cold)} print match(\"hot\",hot)}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("1\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(81, counters.RegexCompiles);
    }

    [Fact]
    public async Task Awk_MultipartConcatenationCopiesFinalPayloadOnce()
    {
        var expression = new StringBuilder();
        for (var index = 0; index < 64; index++)
            expression.Append(index == 0 ? "\"x\"" : " \"x\"");

        var program = "BEGIN{s=" + expression + "; print s}";
        var interpreter = await RunAsync(
            [program],
            string.Empty,
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);
        var executable = await RunAsync(
            [program],
            string.Empty,
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Executable);

        Assert.Equal(0, interpreter.ExitCode);
        Assert.Equal(0, executable.ExitCode);
        var expectedOutput = new string('x', 64) + "\n";
        Assert.Equal(expectedOutput, interpreter.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(expectedOutput, executable.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(64, interpreter.Counters.ConcatenationBytesCopied);
        Assert.Equal(64, executable.Counters.ConcatenationBytesCopied);
        Assert.Equal(0, executable.Counters.VmFallbackCalls);
    }

    [Fact]
    public async Task Awk_RepeatedSelfConcatenationCopiesLinearPayloadBytes()
    {
        const int recordCount = 1_024;
        const int recordLength = 10;
        var input = new StringBuilder(recordCount * (recordLength + 1));
        for (var index = 0; index < recordCount; index++)
            input.Append("abcdefghij\n");

        var interpreter = await RunAsync(
            ["{s=s $0} END{print s}"],
            input.ToString(),
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);
        var executable = await RunAsync(
            ["{s=s $0} END{print s}"],
            input.ToString(),
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Executable);

        var finalLength = recordCount * recordLength;
        var expectedOutput = input.ToString().Replace("\n", string.Empty, StringComparison.Ordinal) + "\n";
        Assert.Equal(0, interpreter.ExitCode);
        Assert.Equal(0, executable.ExitCode);
        Assert.Equal(expectedOutput, interpreter.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(expectedOutput, executable.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.InRange(interpreter.Counters.ConcatenationBytesCopied, finalLength, finalLength * 2L);
        Assert.InRange(executable.Counters.ConcatenationBytesCopied, finalLength, finalLength * 2L);
        Assert.Equal(0, executable.Counters.VmFallbackCalls);
    }

    [Theory]
    [InlineData("BEGIN{FS=\"[,;]\"} {print NF}", "a,b;c\n", "3\n")]
    [InlineData("BEGIN{FIELDWIDTHS=\"2 3\"} {print NF}", "abcdef\n", "2\n")]
    [InlineData("BEGIN{FPAT=\"[A-Z]+|[0-9]+\"} {print NF}", "A-12-B\n", "3\n")]
    public async Task Awk_BenchmarkCountersCoverFieldSplittingModes(
        string program,
        string input,
        string expectedOutput)
    {
        var (exitCode, fileSystem, counters) = await RunAsync([program], input);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.FieldSplits > 0);
    }

    [Fact]
    public async Task Awk_BenchmarkCountersCoverSeparatorArraysSortingSubstitutionAndConcat()
    {
        var (exitCode, fileSystem, counters) = await RunAsync([
            "BEGIN{split(\"a,b\", parts, \",\", seps); a[\"b\"]=2; a[\"a\"]=1; PROCINFO[\"sorted_in\"]=\"@ind_str_asc\"; s=\"a1b22\"; gsub(/[0-9]+/,\"#\",s); x=\"caf\u00e9\" \"caf\u00e9\"; print length(seps), s, length(x), (\"caf\u00e9\" > \"caff\"); for(k in a) print k, a[k]}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("1 a#b# 8 1\na 1\nb 2\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.SeparatorCaptureFallbacks > 0);
        Assert.True(counters.RegexMatches > 0);
        Assert.True(counters.ByteTextComparisons > 0);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Awk_RecordSplitSnapshotsStayLazyAndByteNative(bool useExecutable, bool charactersAsBytes)
    {
        const int rows = 8;
        var shapes = new[]
        {
            ("BEGIN{FS=\",\"} {FS=\" \";print NF;FS=\",\"}", "a,b\n", false),
            ("BEGIN{FIELDWIDTHS=\"2 2\"} {FIELDWIDTHS=\"1 3\";print NF;FIELDWIDTHS=\"2 2\"}", "abcd\n", false),
            ("BEGIN{FPAT=\"[a-z]+\"} {FPAT=\"[0-9]+\";print NF;FPAT=\"[a-z]+\"}", "a1b2\n", true)
        };
        foreach (var (program, record, usesRegex) in shapes)
        {
            string[] arguments = charactersAsBytes ? ["--characters-as-bytes", program] : [program];
            var result = await RunAsync(
                arguments,
                string.Concat(Enumerable.Repeat(record, rows)),
                AwkRegexEngineKind.Utf8Adapter,
                useExecutable ? AwkExecutionEngineKind.Executable : AwkExecutionEngineKind.Interpreter);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Concat(Enumerable.Repeat("2\n", rows)),
                result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(rows, result.Counters.FieldSplits);
            Assert.Equal(0, result.Counters.Utf8Encodes);
            // Each FPAT assignment prepares both case variants, including the unused pattern.
            // The source-pattern boundary adds one decode; records stay byte-native.
            Assert.InRange(result.Counters.StringDecodes, 0, usesRegex ? 3 : 0);
            Assert.Equal(0, result.Counters.RegexDotNetTextMatches);
            Assert.Equal(usesRegex ? 4 : 0, result.Counters.RegexCompiles);
        }
    }

    [Fact]
    public async Task Awk_ArraySortProjectsTextOnlyForValueStringOrder()
    {
        const int entryCount = 32;

        static string Program(string order) =>
            "function cmp(i1,v1,i2,v2){return v1-v2} " +
            "BEGIN{for(i=32;i>=1;i--)a[i]=i; n=asort(a,b,\"" + order + "\"); print n}";

        var numeric = await RunAsync([Program("@val_num_asc")]);
        var valueString = await RunAsync([Program("@val_str_asc")]);
        var userFunction = await RunAsync([Program("cmp")]);
        var defaultOrder = await RunAsync([Program("")]);
        var typeAscending = await RunAsync([Program("@val_type_asc")]);
        var typeDescending = await RunAsync([Program("@val_type_desc")]);
        var unsorted = await RunAsync([Program("@unsorted")]);

        Assert.Equal("32\n", numeric.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("32\n", valueString.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("32\n", userFunction.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            numeric.Counters.AwkTextAllocations + entryCount,
            valueString.Counters.AwkTextAllocations);
        Assert.Equal(numeric.Counters.AwkTextAllocations, userFunction.Counters.AwkTextAllocations);
        foreach (var result in new[] { defaultOrder, typeAscending, typeDescending, unsorted })
        {
            Assert.Equal("32\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(numeric.Counters.AwkTextAllocations, result.Counters.AwkTextAllocations);
            Assert.InRange(result.Counters.StringDecodes, 0, numeric.Counters.StringDecodes);
        }
        Assert.Equal(0, unsorted.Counters.ByteTextComparisons);
        Assert.True(unsorted.Counters.ExecutionSteps < defaultOrder.Counters.ExecutionSteps);
    }

    [Theory]
    [InlineData("@ind_str_asc", "2", "1", 1)]
    [InlineData("@ind_num_asc", "2", "1", 1)]
    [InlineData("@val_str_asc", "2", "1", 1)]
    [InlineData("@val_str_asc", "1", "1", 2)]
    [InlineData("@val_num_asc", "2", "1", 0)]
    [InlineData("@val_num_asc", "1", "1", 2)]
    [InlineData("@val_num_asc", "\"02\"", "\"2\"", 1)]
    [InlineData("@val_type_asc", "2", "1", 0)]
    [InlineData("@val_type_asc", "1", "1", 1)]
    [InlineData("@val_type_asc", "1", "\"0\"", 0)]
    [InlineData("@val_type_asc", "\"aa\"", "\"b\"", 1)]
    [InlineData("@val_type_desc", "1", "1", 1)]
    public async Task Awk_ArraySortEvaluatesOnlyRequiredTextTieBreakers(
        string order,
        string firstValue,
        string secondValue,
        long expectedComparisons)
    {
        var result = await RunAsync([
            "BEGIN{a[\"b\"]=" + firstValue + "; a[\"a\"]=" + secondValue +
            "; n=asort(a,b,\"" + order + "\"); print n}"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("2\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(expectedComparisons, result.Counters.ByteTextComparisons);
    }

    [Fact]
    public async Task Awk_RegexReplacementEncodingDoesNotScaleWithLiteralLength()
    {
        var shortRun = await RunAsync(["BEGIN{s=\"x\"; gsub(/x/,\"é&\",s); print s}"]);
        var longRun = await RunAsync(
            ["BEGIN{s=\"x\"; gsub(/x/,\"" + new string('é', 64) + "&\",s); print s}"]);

        Assert.Equal(0, shortRun.ExitCode);
        Assert.Equal(0, longRun.ExitCode);
        Assert.Equal("éx\n", shortRun.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(new string('é', 64) + "x\n", longRun.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, shortRun.Counters.Utf8Encodes);
        Assert.Equal(0, longRun.Counters.Utf8Encodes);
        Assert.Equal(shortRun.Counters.AwkTextAllocations, longRun.Counters.AwkTextAllocations);
    }

    [Fact]
    public async Task Awk_BenchmarkCountersCoverNonAsciiByteModePolicy()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["--characters-as-bytes", "BEGIN{s=\"caf\u00e9\"; x=s s; print length(s), length(x)}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("5 10\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.StringDecodes);
    }

    [Fact]
    public async Task Awk_BenchmarkCountersCoverInvalidInputFailure()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = CreateOptions(
            counters,
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", "{print}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInputBytes([0xFF]);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(InvalidUtf8Diagnostic, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.RecordsRead);
    }

    public static TheoryData<string, string, string> DefaultRegexCounterCases => new()
    {
        {
            "BEGIN{FS=\"[,;]\"} {print NF}",
            "a,b;c\n",
            "3\n"
        },
        {
            "BEGIN{RS=\"(,+|;+)\"} {print $0}",
            "alpha,beta;",
            "alpha\nbeta\n"
        },
        {
            "BEGIN{FPAT=\"[A-Z]+|[0-9]+\"} {print NF}",
            "A-12-B\n",
            "3\n"
        },
        {
            "BEGIN{s=\"abc123\"; print match(s,/[0-9]+/)}",
            string.Empty,
            "4\n"
        },
        {
            "BEGIN{s=\"abc123\"; sub(/[0-9]+/,\"X\",s); print s}",
            string.Empty,
            "abcX\n"
        },
        {
            "BEGIN{s=\"a1b22\"; gsub(/[0-9]+/,\"#\",s); print s}",
            string.Empty,
            "a#b#\n"
        },
        {
            "BEGIN{print gensub(/[0-9]+/,\"#\",\"g\",\"a1b22\")}",
            string.Empty,
            "a#b#\n"
        },
        {
            "/[0-9]+/{print $0}",
            "abc\nx9\n",
            "x9\n"
        }
    };

    [Theory]
    [MemberData(nameof(DefaultRegexCounterCases))]
    public async Task Awk_DefaultRegexTextCountersCoverRepresentativePaths(
        string program,
        string input,
        string expectedOutput)
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            [program],
            input,
            AwkRegexEngineKind.DotNetCompatibility);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RegexMatches > 0);
        Assert.True(counters.RegexDotNetTextMatches > 0);
        Assert.True(counters.RegexStringAdapterMatches > 0);
        Assert.True(counters.AwkTextAsStringCalls > 0);
        Assert.True(counters.AwkTextAsStringDecodeMisses <= counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_SimpleByteSetRegexRecordSeparatorAvoidsRegexMatches()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"[,;]\"} {print $0}"],
            input: "alpha,beta;");

        Assert.Equal(0, exitCode);
        Assert.Equal("alpha\nbeta\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.RegexMatches);
    }

    [Fact]
    public async Task Awk_GroupedByteRunRecordSeparatorAvoidsRegexMatches()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"([|;])+\"} {print $0}"],
            "a|b;c|d;",
            AwkRegexEngineKind.DotNetCompatibility);

        Assert.Equal(0, exitCode);
        Assert.Equal("a\nb\nc\nd\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_GroupedAlternationByteRunRecordSeparatorAvoidsRegexMatches()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"([|]|;)+\"} {print $0}"],
            "a|b;c|d;",
            AwkRegexEngineKind.DotNetCompatibility);

        Assert.Equal(0, exitCode);
        Assert.Equal("a\nb\nc\nd\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_Utf8RegexPatternGapFallbacksAreCounted()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{s=\"abc\"; print match(s,/\\`a/)}"],
            input: string.Empty,
            AwkRegexEngineKind.Utf8Adapter);

        Assert.Equal(0, exitCode);
        Assert.Equal("1\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RegexDotNetTextMatches > 0);
        Assert.True(counters.RegexPatternGapFallbacks > 0);
        Assert.True(counters.RegexStringAdapterMatches > 0);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversArithmeticLoops()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{for(i=1;i<=100;i++) total += i * 2; print total}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("10100\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.ExecutionSteps > 0);
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    public static TheoryData<string, string, long> StreamingStepCalibrationCases => new()
    {
        {
            "{print $2}",
            "region1\nregion2\nregion2\n",
            19
        },
        {
            "$1 != \"\" && $2 != \"\" && $3 == \"open\" {matches++} END{print matches+0}",
            "2\n",
            61
        },
        {
            "{sum[$1,$2]+=$4; count[$3]++} END{print length(sum), length(count)}",
            "3 2\n",
            47
        }
    };

    [Theory]
    [MemberData(nameof(StreamingStepCalibrationCases))]
    public async Task Awk_RuntimeCounterEvidencePinsStreamingStepCalibration(
        string program,
        string expectedOutput,
        long expectedExecutionSteps)
    {
        var (exitCode, fileSystem, counters) = await RunAsync([program], StepCalibrationInput);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(expectedExecutionSteps, counters.ExecutionSteps);
    }

    [Fact]
    public async Task Awk_RegexLiteralPredicatesShortCircuitAndStayByteNative()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["{print (1 || /never/), (0 && /never/), /café/}"],
            "café\nother\n");

        Assert.Equal(0, exitCode);
        Assert.Equal("1 0 1\n1 0 0\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(2, counters.RegexMatches);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversPrintHeavyActions()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{for(i=1;i<=50;i++) print \"row\", i}"]);

        var output = fileSystem.GetOutput(AwkFileDescriptor.StdOut);
        Assert.Equal(0, exitCode);
        Assert.StartsWith("row 1\n", output, StringComparison.Ordinal);
        Assert.EndsWith("row 50\n", output, StringComparison.Ordinal);
        Assert.Equal(Encoding.UTF8.GetByteCount(output), counters.OutputBytes);
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
        Assert.Equal(0, counters.StringInteropFallbacks);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(1, counters.OutputSinkCreations);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceBoundsRepeatedUtf8RuneSlicing()
    {
        const int runeCount = 4_096;
        var input = new string('\u00e9', runeCount) + "\n";
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["{for(i=1;i<=length($0);i++) last=substr($0,i,1)} END{print last}"],
            input);

        Assert.Equal(0, exitCode);
        Assert.Equal("\u00e9\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, counters.RuneSliceIndexAllocations);
        Assert.True(counters.RuneSliceIndexBytes < Encoding.UTF8.GetByteCount(input) / 8);
        Assert.InRange(
            counters.RuneSliceBytesScanned,
            Encoding.UTF8.GetByteCount(input) - 1,
            (Encoding.UTF8.GetByteCount(input) - 1) * 80L);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversLargeSplitAndFieldWorkloads()
    {
        const int rowCount = 32;
        const int fieldCount = 24;

        var (exitCode, fileSystem, counters) = await RunAsync(
            ["{n=split($0,a,\" \"); total += n + length($1) + length($NF) + length(a[1]) + length(a[n])} END{print total}"],
            BuildRepeatedSeparatedRows(rowCount, fieldCount, ' '));

        Assert.Equal(0, exitCode);
        Assert.Equal("896\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(rowCount, counters.RecordsRead);
        Assert.Equal(rowCount, counters.FieldSplits);
        Assert.True(counters.FieldsMaterialized > 0);
        Assert.True(counters.AwkTextAllocations > 0);
        Assert.True(counters.AwkTextPayloadBytes > 0);
        Assert.Equal(0, counters.RegexStringAdapterMatches);
        Assert.True(counters.AwkTextAsStringCalls <= rowCount);
        Assert.True(counters.AwkTextAsStringDecodeMisses <= counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_BothRegexFieldSeparatorVariantsArePreparedOnceForRepeatedRecords()
    {
        const int rowCount = 32;
        var input = BuildRepeatedSeparatedRows(rowCount, fieldCount: 3, ',');
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{FS=\"[,;]\"} {total += NF} END{print total}"],
            input,
            AwkRegexEngineKind.Utf8Adapter);

        Assert.Equal(0, exitCode);
        Assert.Equal("96\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(rowCount, counters.FieldSplits);
        Assert.Equal(2, counters.RegexCompiles);
        Assert.Equal(1, counters.AwkTextAsStringCalls);
        Assert.Equal(1, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversSmallArrayWorkloads()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{for(i=1;i<=64;i++) a[i]=i; for(i=1;i<=64;i++) total += a[i]; print total}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("2080\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversUserFunctionLoops()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["function add(x,y){return x+y} BEGIN{for(i=1;i<=80;i++) total=add(total,i); print total}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("3240\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.ExecutionSteps > 0);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.FormattingFallbacks);
    }

    [Theory]
    [InlineData(AwkExecutionEngineKind.Interpreter, false)]
    [InlineData(AwkExecutionEngineKind.Interpreter, true)]
    [InlineData(AwkExecutionEngineKind.Executable, false)]
    [InlineData(AwkExecutionEngineKind.Executable, true)]
    internal async Task Awk_ShallowFunctionCallsDoNotForceStackSuspension(
        AwkExecutionEngineKind engine, bool nestedArguments)
    {
        var body = nestedArguments ? "return leaf(leaf(n-1))" : "return leaf(n)";
        var (exitCode, host, counters) = await RunAsync(
            ["function leaf(n){return n+1} function outer(n){" + body +
             "} BEGIN{for(i=0;i<512;i++)sum+=outer(1);print sum}"],
            string.Empty, AwkRegexEngineKind.DotNetCompatibility, engine);
        Assert.Equal(0, exitCode);
        Assert.Equal("1024\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.FunctionStackSuspensions);
        Assert.True(counters.ExecutionSteps > 0);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(counters.MemoryReservedBytes, counters.MemoryCommittedBytes + counters.MemoryReleasedReservedBytes);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversExecutableMixedFallbackPrograms()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["function inc(x){return x+1} {print inc($2)}"],
            "sku1 1\nsku2 2\n",
            AwkRegexEngineKind.Utf8Adapter,
            executionEngineKind: AwkExecutionEngineKind.Executable);

        Assert.Equal(0, exitCode);
        Assert.Equal("2\n3\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.VmInstructions > 0);
        Assert.True(counters.VmFallbackCalls > 0);
        Assert.True(counters.VmOpcodeKindsExecuted > 0);
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(1, counters.OutputSinkCreations);
    }

    private static Task<BenchmarkRunResult> RunAsync(
        string[] arguments) =>
        RunAsync(
            arguments,
            string.Empty,
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);

    private static Task<BenchmarkRunResult> RunAsync(
        string[] arguments,
        string input) =>
        RunAsync(
            arguments,
            input,
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);

    private static Task<BenchmarkRunResult> RunAsync(
        string[] arguments,
        string input,
        AwkRegexEngineKind regexEngineKind) =>
        RunAsync(arguments, input, regexEngineKind, AwkExecutionEngineKind.Interpreter);

    private static async Task<BenchmarkRunResult> RunAsync(
        string[] arguments,
        string input,
        AwkRegexEngineKind regexEngineKind,
        AwkExecutionEngineKind executionEngineKind)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = CreateOptions(counters, regexEngineKind, executionEngineKind);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", arguments),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        if (input.Length > 0)
            fileSystem.SetStandardInput(input);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);
        return new BenchmarkRunResult(exitCode.Value, fileSystem, counters);
    }

    private readonly record struct BenchmarkRunResult(
        int ExitCode,
        MockFileSystem FileSystem,
        AwkRuntimeCounters Counters);

    private static string BuildRepeatedSeparatedRows(int rowCount, int fieldCount, char separator)
    {
        var builder = new StringBuilder();
        for (var row = 0; row < rowCount; row++)
        {
            for (var field = 0; field < fieldCount; field++)
            {
                if (field > 0)
                    builder.Append(separator);

                builder.Append('a');
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static AwkRuntimeOptions CreateOptions(
        AwkRuntimeCounters counters,
        AwkRegexEngineKind regexEngineKind,
        AwkExecutionEngineKind executionEngineKind) =>
        new(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            regexEngineKind,
            executionEngineKind);
}
