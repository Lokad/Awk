using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkBenchmarkCounterTests
{
    private const string InvalidUtf8Diagnostic = "awk: invalid UTF-8 input\n";
    private const string StepCalibrationInput =
        "sku1 region1 open 10\n" +
        "sku2 region2 closed 20\n" +
        "sku1 region2 open 30\n";

    [Fact]
    public async Task Awk_BenchmarkCountersCoverDynamicRecordSeparators()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"|\"} {print $0; RS=\"[,;]+\"}"],
            input: "first|second,third;");

        Assert.Equal(0, exitCode);
        Assert.Equal("first\nsecond\nthird\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RegexMatches > 0);
        Assert.Equal(0, counters.RegexRecordSeparatorFallbacks);
    }

    [Theory]
    [InlineData("BEGIN{FS=\"[,;]\"} {print NF}", "a,b;c\n", "3\n", nameof(AwkRuntimeCounters.RegexFieldSeparatorFallbacks))]
    [InlineData("BEGIN{FIELDWIDTHS=\"2 3\"} {print NF}", "abcdef\n", "2\n", nameof(AwkRuntimeCounters.FieldWidthFallbacks))]
    [InlineData("BEGIN{FPAT=\"[A-Z]+|[0-9]+\"} {print NF}", "A-12-B\n", "3\n", nameof(AwkRuntimeCounters.FieldPatternFallbacks))]
    public async Task Awk_BenchmarkCountersCoverFieldSplittingModes(
        string program,
        string input,
        string expectedOutput,
        string counterName)
    {
        var (exitCode, fileSystem, counters) = await RunAsync([program], input);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        if (counterName == nameof(AwkRuntimeCounters.FieldWidthFallbacks))
            Assert.Equal(0, counters.FieldWidthFallbacks);
        else if (counterName == nameof(AwkRuntimeCounters.RegexFieldSeparatorFallbacks))
            Assert.Equal(0, counters.RegexFieldSeparatorFallbacks);
        else
            Assert.Equal(0, counters.FieldPatternFallbacks);
    }

    [Fact]
    public async Task Awk_BenchmarkCountersCoverSeparatorArraysSortingSubstitutionAndConcat()
    {
        var (exitCode, fileSystem, counters) = await RunAsync([
            "BEGIN{split(\"a,b\", parts, \",\", seps); a[\"b\"]=2; a[\"a\"]=1; PROCINFO[\"sorted_in\"]=\"@ind_str_asc\"; s=\"a1b22\"; gsub(/[0-9]+/,\"#\",s); x=\"caf\u00e9\" \"caf\u00e9\"; print length(seps), s, length(x), (\"caf\u00e9\" > \"caff\"); for(k in a) print k, a[k]}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("3 a#b# 8 1\na 1\nb 2\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.SeparatorCaptureFallbacks > 0);
        Assert.True(counters.RegexMatches > 0);
        Assert.True(counters.ByteTextComparisons > 0);
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
        Assert.Equal(shortRun.Counters.Utf8Encodes, longRun.Counters.Utf8Encodes);
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
        var options = CreateOptions(counters);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", "{print}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInputBytes([0xFF]);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
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
            "BEGIN{RS=\"[,;]+\"} {print $0}",
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
    public async Task Awk_DefaultRegexFallbackCountersCoverRepresentativePaths(
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
        Assert.True(counters.RegexDotNetTextFallbacks > 0);
        Assert.True(counters.RegexStringAdapterFallbacks > 0);
        Assert.True(counters.AwkTextAsStringCalls >= counters.RegexDotNetTextFallbacks);
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
    public async Task Awk_Utf8RegexPatternGapFallbacksAreCounted()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{s=\"abc\"; print match(s,/\\`a/)}"],
            input: string.Empty,
            AwkRegexEngineKind.Utf8Adapter);

        Assert.Equal(0, exitCode);
        Assert.Equal("1\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RegexDotNetTextFallbacks > 0);
        Assert.True(counters.RegexPatternGapFallbacks > 0);
        Assert.True(counters.RegexStringAdapterFallbacks > 0);
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
            9
        },
        {
            "$1 != \"\" && $2 != \"\" && $3 == \"open\" {matches++} END{print matches+0}",
            "2\n",
            53
        },
        {
            "{sum[$1,$2]+=$4; count[$3]++} END{print length(sum), length(count)}",
            "3 2\n",
            39
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
        Assert.Equal(0, counters.RegexStringAdapterFallbacks);
        Assert.True(counters.AwkTextAsStringCalls <= rowCount);
        Assert.True(counters.AwkTextAsStringDecodeMisses <= counters.AwkTextAsStringCalls);
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

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversExecutableMixedFallbackPrograms()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["function inc(x){return x+1} {print inc($2)}"],
            "sku1 1\nsku2 2\n",
            executionEngineKind: AwkExecutionEngineKind.Executable);

        Assert.Equal(0, exitCode);
        Assert.Equal("2\n3\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.VmInstructions > 0);
        Assert.True(counters.VmFallbackCalls > 0);
        Assert.True(counters.VmOpcodeKindsExecuted > 0);
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    private static async Task<(int ExitCode, MockFileSystem FileSystem, AwkRuntimeCounters Counters)> RunAsync(
        string[] arguments,
        string input = "",
        AwkRegexEngineKind regexEngineKind = AwkRegexEngineKind.Utf8Adapter,
        AwkExecutionEngineKind executionEngineKind = AwkExecutionEngineKind.Interpreter)
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
        return (exitCode, fileSystem, counters);
    }

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
        AwkRegexEngineKind regexEngineKind = AwkRegexEngineKind.Utf8Adapter,
        AwkExecutionEngineKind executionEngineKind = AwkExecutionEngineKind.Interpreter) =>
        new(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            regexEngineKind,
            executionEngineKind);
}
