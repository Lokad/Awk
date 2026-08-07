using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkCurrentSurfaceRecordsFieldsTests : AwkCurrentSurfaceTestBase
{
    private const string InvalidUtf8Diagnostic = "awk: invalid UTF-8 input\n";

    [Fact]
    public void AwkRecordLine_DecodesUtf8DefaultNewlineRecords()
    {
        var line = AwkRecordLine.ReadUtf8DefaultNewline("caf\u00e9\n"u8.ToArray());

        Assert.Equal("caf\u00e9", line.Record.AsString());
        Assert.Equal("\n", line.Terminator.ByteText.AsString());
    }

    [Fact]
    public void AwkRecordLine_DecodesDefaultNewlineCrLfAsSingleRecordTerminator()
    {
        var line = AwkRecordLine.ReadUtf8DefaultNewline("alpha\r\n"u8.ToArray());

        Assert.Equal("alpha", line.Record.AsString());
        Assert.Equal("\n", line.Terminator.ByteText.AsString());
    }

    [Fact]
    public void AwkRecordLine_DecodesUnterminatedUtf8Record()
    {
        var line = AwkRecordLine.ReadUtf8DefaultNewline("caf\u00e9"u8.ToArray());

        Assert.Equal("caf\u00e9", line.Record.AsString());
        Assert.Equal(string.Empty, line.Terminator.ByteText.AsString());
    }

    [Fact]
    public async Task Awk_InvalidUtf8FromStandardInputFailsWithDiagnostic()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInputBytes([0x61, 0x80, 0x0A]);

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "{print $0}")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        AssertInvalidUtf8Failure(exitCode, fileSystem);
    }

    [Fact]
    public async Task Awk_LcAllCDoesNotRelaxStrictUtf8Ingress()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInputBytes([0x63, 0x61, 0x66, 0xE9, 0x0A]);
        var invocation = AwkTestHarness.BuildInvocation(
            "awk",
            ["{print $0}"],
            [new AwkEnvironmentVariable("LC_ALL", "C")]);

        var tool = Assert.IsType<Awk>(Awk.TryParse(invocation));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        AssertInvalidUtf8Failure(exitCode, fileSystem);
    }

    [Fact]
    public async Task Awk_LcAllCStillAcceptsValidNonAsciiUtf8()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput("café\n");
        var invocation = AwkTestHarness.BuildInvocation(
            "awk",
            ["{print $0}"],
            [new AwkEnvironmentVariable("LC_ALL", "C")]);

        var tool = Assert.IsType<Awk>(Awk.TryParse(invocation));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal("café\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_InvalidUtf8FromExplicitInputFileFailsWithDiagnostic()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/bad.txt", [0xC3, 0x28, 0x0A]);

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "{print $0}", "/bad.txt")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        AssertInvalidUtf8Failure(exitCode, fileSystem);
    }

    [Fact]
    public async Task Awk_InvalidUtf8DefaultRecordFailsBeforeAdvancingRecordState()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInputBytes([0x61, 0x80, 0x0A]);
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var io = new AwkIo(
            fileSystem,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            CancellationToken.None,
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            state,
            static () => []);
        var counters = AwkRuntimeCounters.Disabled;
        var reader = new AsyncLineReader(
            fileSystem,
            AwkFileDescriptor.StdIn,
            CancellationToken.None,
            AwkIo.InputReadBatchLineCount,
            counters,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters));
        var descriptorReader = new AwkRecordDescriptorReader(
            fileSystem,
            CancellationToken.None,
            counters,
            state.Resources);
        using var cursor = new AwkRecordCursor(state, descriptorReader, reader);
        var recordReader = new AwkRecordReader(fileSystem, state, io, CancellationToken.None);
        var invoked = false;

        var exception = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await recordReader.ProcessCursorAsync(
            cursor,
            _ =>
            {
                invoked = true;
                return ValueTask.FromResult(AwkStatementFlow.Continue);
            }));

        Assert.Equal(InvalidUtf8Diagnostic, AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.False(invoked);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
    }

    [Fact]
    public async Task Awk_InvalidUtf8FromGetlineFailsWithDiagnostic()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInputBytes([0xC3, 0x28, 0x0A]);

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{ if ((getline line) > 0) print line }")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        AssertInvalidUtf8Failure(exitCode, fileSystem);
    }

    [Fact]
    public async Task Awk_GetlineTargetVariableStoresAwkTextWithoutRecordDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{getline line; print line}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput("caf\u00e9\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("caf\u00e9\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public async Task Awk_InvalidUtf8WithCustomRecordSeparatorFailsWithDiagnostic()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInputBytes([0x61, 0xC3, 0x28, 0x7C]);

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{RS=\"|\"} {print $0}")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        AssertInvalidUtf8Failure(exitCode, fileSystem);
    }

    [Fact]
    public async Task Awk_DefaultRecordProcessingReadsInputInBatches()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("first\n"u8.ToArray(), [], reachedEof: false));
        host.EnqueueAppendExitCode(0);

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "{print; exit}")));
        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal([AwkIo.InputReadBatchLineCount], host.ReadLineBatchLineCounts);
    }

    [Fact]
    public async Task Awk_DynamicCustomRecordSeparatorKeepsBufferedAndUnreadInput()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("a\nb|"u8.ToArray(), [2], reachedEof: false));
        host.EnqueueAppendExitCode(0);
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("c\n"u8.ToArray(), [], reachedEof: true));
        host.EnqueueAppendExitCode(0);

        var program = "{record=$0; terminator=RT; gsub(/\\n/,\"N\",record); gsub(/\\n/,\"N\",terminator); " +
            "print NR \":\" record \"[\" terminator \"]\"; if(NR==1) RS=\"|\"}";
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", program)));
        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(
            [AwkIo.InputReadBatchLineCount, AwkIo.InputReadBatchLineCount],
            host.ReadLineBatchLineCounts);
        Assert.Equal("1:a[N]\n2:b[|]\n3:cN[]\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_SingleCharacterCustomRecordSeparatorReadsInputInBatches()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("first|second|"u8.ToArray(), [], reachedEof: false));
        host.EnqueueAppendExitCode(0);

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "BEGIN{RS=\"|\"} {print; exit}")));
        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal([AwkIo.InputReadBatchLineCount], host.ReadLineBatchLineCounts);
    }

    [Fact]
    public async Task Awk_SingleByteRecordSeparatorUsesAwkTextRecords()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{RS=\"|\"} {print $0}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput("alpha|beta|");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("alpha\nbeta\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(2, counters.RecordsRead);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Fact]
    public async Task Awk_RecordSeparatorResolutionIsBoundedAcrossManyRecords()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{RS=\",\"} {count++} END{print count}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput(string.Join(",", Enumerable.Range(0, 64).Select(static index => "r" + index)));

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("64\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RecordSeparatorResolutions < 12);
    }

    [Fact]
    public async Task Awk_UnchangedRecordSeparatorAssignmentsReuseTheCompiledPlan()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{RS=1} {RS=(NR%2 ? \"1\" : 1); count++} END{print count}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput(string.Join("1", Enumerable.Repeat("record", 64)));

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("64\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, counters.RecordSeparatorResolutions);
    }

    [Fact]
    public async Task Awk_FieldSplittingIsLazyWhenOnlyCurrentRecordIsUsed()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "{print length($0)}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput("alpha beta\ncarrot delta\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("10\n12\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.FieldSplits);
        Assert.Equal(0, counters.FieldsMaterialized);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public async Task Awk_FieldCountDoesNotAllocateTextForEveryField()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "{print NF}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        var fieldCount = 128;
        fileSystem.SetStandardInput(string.Join(" ", Enumerable.Range(0, fieldCount).Select(index => "f" + index)) + "\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal(fieldCount + "\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, counters.FieldSplits);
        Assert.Equal(fieldCount, counters.FieldsMaterialized);
        Assert.True(counters.AwkTextAllocations < fieldCount / 2);
    }

    [Fact]
    public async Task Awk_FieldCountKeepsSpecialFieldModesAsRecordSlices()
    {
        const int fieldCount = 128;
        var fieldWidths = string.Join(' ', Enumerable.Repeat("1", fieldCount));
        var scenarios = new[]
        {
            (Program: "BEGIN{FPAT=\".\"} {print NF}", Input: new string('a', fieldCount) + "\n"),
            (Program: $"BEGIN{{FIELDWIDTHS=\"{fieldWidths}\"}} {{print NF}}", Input: new string('a', fieldCount) + "\n"),
            (Program: "BEGIN{FS=\"\"} {print NF}", Input: new string('a', fieldCount) + "\n")
        };

        foreach (var scenario in scenarios)
        {
            var counters = AwkRuntimeCounters.CreateEnabled();
            var options = new AwkRuntimeOptions(
                () => new AwkUnixTimestampSeconds(0),
                TimeZoneInfo.Utc,
                new AwkRandomGenerator(),
                counters,
                AwkRuntimeLimits.CreateDefault(),
                AwkRegexEngineKind.DotNetCompatibility,
                AwkExecutionEngineKind.Interpreter);
            var compiled = await AwkExecutor.CompileAsync(
                new MockFileSystem(),
                BuildInvocation("awk", scenario.Program),
                CancellationToken.None);
            var fileSystem = new MockFileSystem();
            fileSystem.SetStandardInput(scenario.Input);

            var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

            Assert.Equal(0, exitCode);
            Assert.Equal(fieldCount + "\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(1, counters.FieldSplits);
            Assert.Equal(fieldCount, counters.FieldsMaterialized);
            Assert.True(counters.AwkTextAllocations < fieldCount / 2);
        }
    }

    [Theory]
    [InlineData(false, "{print length($0), index($0,\"é\"), substr($0,4,1)}", "4 4 é\n")]
    [InlineData(true, "{print length($0), index($0,\"é\"), substr($0,4,2)}", "5 4 é\n")]
    public async Task Awk_StringBuiltinsUseAwkTextWithoutRecordDecodes(
        bool charactersAsBytes,
        string program,
        string expectedOutput)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            charactersAsBytes
                ? BuildInvocation("awk", "--characters-as-bytes", program, "/file.txt")
                : BuildInvocation("awk", program, "/file.txt"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "café\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public async Task Awk_SplitSingleByteSeparatorStoresAwkTextWithoutValueDecodes()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{n=split(\"café,bravo\", a, \",\"); print n, a[1], a[2]}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("2 café bravo\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, counters.StringDecodes);
        Assert.Equal(1, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public async Task Awk_EmptyFieldSeparatorSplitsAsciiBytesInByteMode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "--characters-as-bytes", "BEGIN{FS=\"\"} {print NF, $1, $2, $3}", "/file.txt"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "abc\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("3 a b c\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public async Task Awk_EmptyFieldSeparatorSplitsUtf8RunesWithoutStringDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{FS=\"\"} {print NF, $1, $2, $3}", "/file.txt"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "éA😀\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("3 é A 😀\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, counters.FieldSplits);
        Assert.Equal(3, counters.FieldsMaterialized);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public async Task Awk_EmptyFieldSeparatorRejectsNonAsciiByteModeFields()
    {
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "--characters-as-bytes",
            "BEGIN{FS=\"\"} {print NF}",
            "/file.txt")));
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "é\n");

        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: byte-mode FS=\"\" over non-ASCII text is unsupported in Lokad awk\n",
            fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    public static TheoryData<string, string, string, int, int> ByteNativeFieldCountCases => new()
    {
        {
            "{print NF}",
            "alpha beta\ncarrot delta\n",
            "2\n2\n",
            2,
            4
        },
        {
            "BEGIN{FS=\",\"} {print NF}",
            "alpha,beta\ncarrot,delta\n",
            "2\n2\n",
            2,
            4
        },
        {
            "BEGIN{FS=\"\"} {print NF}",
            "éa\n",
            "2\n",
            1,
            2
        }
    };

    [Theory]
    [MemberData(nameof(ByteNativeFieldCountCases))]
    public async Task Awk_FieldCountUsesAwkTextSplittingWithoutFieldValueDecodes(
        string program,
        string input,
        string expectedOutput,
        int expectedRecords,
        int expectedFields)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", program),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput(input);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(expectedRecords, counters.FieldSplits);
        Assert.Equal(expectedFields, counters.FieldsMaterialized);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    public static TheoryData<string, string, string, int, int> FieldRecompositionCounterCases => new()
    {
        {
            "{$2=\"É\"; print $0}",
            "café beta\n",
            "café É\n",
            1,
            2
        },
        {
            "BEGIN{OFS=\"|\"} {$2=\"é\"; print $0}",
            "alpha beta gamma\n",
            "alpha|é|gamma\n",
            1,
            3
        },
        {
            "{NF=4; print $0}",
            "a b\n",
            "a b  \n",
            1,
            4
        },
        {
            "{for(i=1;i<=NF;i++) $i=$i; print $0}",
            BuildLargeNonAsciiRecord() + "\n",
            BuildLargeNonAsciiRecord() + "\n",
            1,
            200
        }
    };

    [Theory]
    [MemberData(nameof(FieldRecompositionCounterCases))]
    public async Task Awk_FieldRecompositionStaysByteNative(
        string program,
        string input,
        string expectedOutput,
        int expectedFieldSplits,
        int expectedFieldsMaterialized)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", program),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput(input);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(expectedFieldSplits, counters.FieldSplits);
        Assert.Equal(expectedFieldsMaterialized, counters.FieldsMaterialized);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    public static TheoryData<string[], string, string> FieldWidthTextCases => new()
    {
        {
            ["BEGIN{FIELDWIDTHS=\"2 2\"} {print $1, $2, NF}", "/file.txt"],
            "abcd\n",
            "ab cd 2\n"
        },
        {
            ["BEGIN{FIELDWIDTHS=\"4 5\"} {print $1, $2, NF}", "/file.txt"],
            "caf\u00e9bravo\n",
            "caf\u00e9 bravo 2\n"
        },
        {
            ["BEGIN{FIELDWIDTHS=\"1:2 0 2\"} {print \"[\" $1 \"]\", \"[\" $2 \"]\", \"[\" $3 \"]\", NF}", "/file.txt"],
            "\u00e9ABCDE\n",
            "[AB] [] [CD] 3\n"
        },
        {
            ["NR==1{FIELDWIDTHS=\"2 2\"} NR==2{FIELDWIDTHS=\"3\"} {print NF, \"[\" $1 \"]\", \"[\" $2 \"]\"}", "/file.txt"],
            "abcd\nwxyz\n",
            "2 [ab] [cd]\n1 [wxy] []\n"
        },
        {
            ["--characters-as-bytes", "BEGIN{FIELDWIDTHS=\"2 2\"} {print $1, $2, NF}", "/file.txt"],
            "abcd\n",
            "ab cd 2\n"
        },
        {
            ["--characters-as-bytes", "BEGIN{FIELDWIDTHS=\"2 1\"} {print $1, $2, NF}", "/file.txt"],
            "\u00e9x\n",
            "\u00e9 x 2\n"
        }
    };

    [Theory]
    [MemberData(nameof(FieldWidthTextCases))]
    public async Task Awk_FieldWidthsUseAwkTextWithoutRecordDecodes(
        string[] arguments,
        string input,
        string expectedOutput)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", arguments),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", input);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.FieldSplits > 0);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public async Task Awk_FieldWidthsRejectByteModeSplitInsideUtf8Character()
    {
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "--characters-as-bytes",
            "BEGIN{FIELDWIDTHS=\"1 1\"} {print NF}",
            "/file.txt")));
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "\u00e9\n");

        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: byte-mode FIELDWIDTHS cannot split a UTF-8 character in Lokad awk\n",
            fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    public static TheoryData<string, string, string> RegexFieldSplittingCounterCases => new()
    {
        {
            "BEGIN{FS=\"[,;]\"} {print NF, $1, $2, $3}",
            "alpha,beta;gamma\n",
            "3 alpha beta gamma\n"
        },
        {
            "BEGIN{FPAT=\"[A-Z]+|[0-9]+\"} {print NF, $1, $2, $3}",
            "A-12-B\n",
            "3 A 12 B\n"
        }
    };

    [Theory]
    [MemberData(nameof(RegexFieldSplittingCounterCases))]
    public async Task Awk_RegexFieldSplittingUsesByteRegexAdapter(
        string program,
        string input,
        string expectedOutput)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", program),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput(input);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RegexMatches > 0);
    }

    [Fact]
    public async Task Awk_SeparatorCaptureFallbacksAreCounted()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{split(\"a,b\", a, \",\", s); print length(s)}"),
            CancellationToken.None);

        var exitCode = await compiled.ExecuteAsync(new MockFileSystem(), CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.True(counters.SeparatorCaptureFallbacks > 0);
    }

    [Fact]
    public async Task Awk_SingleByteRecordSeparatorTransfersRemainderWhenSeparatorChanges()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput("first|second,third,");

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{RS=\"|\"} {print $0; RS=\",\"}")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal("first\nsecond\nthird\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Theory]
    [InlineData("BEGIN{RS=\"|\"} {print $0; RS=\",\"}", "first|second,third,", "first\nsecond\nthird\n", 0)]
    [InlineData("BEGIN{RS=\"é\"} {print $0; RS=\",\"}", "firstésecond,third,", "first\nsecond\nthird\n", 0)]
    [InlineData("BEGIN{RS=\"\"} {print $0; RS=\"|\"}", "first\n\nsecond|third|", "first\nsecond\nthird\n", 0)]
    public async Task Awk_ByteNativeRecordSeparatorsKeepRemainderAsAwkTextWhenSeparatorChanges(
        string program,
        string input,
        string expectedOutput,
        int expectedStringDecodes)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", program),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput(input);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(expectedStringDecodes, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    public static TheoryData<string, string[], int[], string> CustomRecordSeparatorBatchCases => new()
    {
        {
            "BEGIN{RS=\"|\"} {print $0}",
            ["alpha", "beta|gamma", "|"],
            [0, 1, 1],
            "alphabeta\ngamma\n"
        },
        {
            "BEGIN{RS=\"é\"} {print $0}",
            ["alpha", "betaégamma", "é"],
            [0, 1, 1],
            "alphabeta\ngamma\n"
        },
        {
            "BEGIN{RS=\"[,;]\"} {print $0}",
            ["alpha", "beta,gamma", ";"],
            [0, 1, 1],
            "alphabeta\ngamma\n"
        },
        {
            "BEGIN{RS=\"-+\"} {print $0 \"[\" RT \"]\"}",
            ["a-", "-b"],
            [0, 2],
            "a[--]\nb[]\n"
        },
        {
            "BEGIN{RS=\"\"} {print $0}",
            ["alpha", "beta\n\n", "gamma\n\n"],
            [0, 0, 2],
            "alphabeta\ngamma\n"
        },
        {
            "BEGIN{RS=\"\"} {print $0 \"[\" length(RT) \"]\"}",
            ["a\n\n", "\nb"],
            [0, 2],
            "a[3]\nb[0]\n"
        }
    };

    [Theory]
    [MemberData(nameof(CustomRecordSeparatorBatchCases))]
    public async Task Awk_CustomRecordSeparatorsKeepRecordsAcrossHostBatches(
        string program,
        string[] batches,
        int[] appendsAfterBatch,
        string expectedOutput)
    {
        Assert.Equal(batches.Length, appendsAfterBatch.Length);
        var host = new TestOperationVisitor([]);
        for (var index = 0; index < batches.Length; index++)
        {
            host.EnqueueAwkReadResult(AwkReadResult.FromContent(
                Encoding.UTF8.GetBytes(batches[index]),
                [],
                reachedEof: index == batches.Length - 1));
            for (var appendIndex = 0; appendIndex < appendsAfterBatch[index]; appendIndex++)
                host.EnqueueAppendExitCode(0);
        }

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", program)));
        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(Enumerable.Repeat(AwkIo.InputReadBatchLineCount, batches.Length), host.ReadLineBatchLineCounts);
        Assert.Equal(expectedOutput, host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Theory]
    [InlineData("|")]
    [InlineData("é")]
    [InlineData("[,;]")]
    [InlineData("-+")]
    [InlineData("([,;])+")]
    [InlineData("(?:[,;])+")]
    [InlineData("")]
    public async Task Awk_BufferedSeparatorScanningIsLinearAcrossHostBatches(string separator)
    {
        const int batchCount = 32;
        const int batchLength = 1024;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var program = "BEGIN{RS=\"" + separator + "\"} {records++} END{print length($0), records}";
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", program),
            CancellationToken.None);
        var host = new TestOperationVisitor([]);
        var batch = Encoding.UTF8.GetBytes(new string('a', batchLength));
        for (var index = 0; index < batchCount; index++)
        {
            host.EnqueueAwkReadResult(AwkReadResult.FromContent(
                batch,
                [],
                reachedEof: index == batchCount - 1));
        }

        host.EnqueueAppendExitCode(0);

        var exitCode = await compiled.ExecuteAsync(host, CancellationToken.None, options);

        var inputLength = batchCount * batchLength;
        Assert.Equal(0, exitCode);
        Assert.Equal(inputLength + " 1\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RecordSeparatorBytesScanned <= inputLength + (batchCount * 4));
    }

    [Fact]
    public async Task Awk_GroupedAlternationRecordSeparatorScansEachInputByteOnce()
    {
        const int batchCount = 32;
        const int batchLength = 1024;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{RS=\"([,]|;)+\"} {print length($0)}"),
            CancellationToken.None);
        var host = new TestOperationVisitor([]);
        var batch = Encoding.UTF8.GetBytes(new string('a', batchLength));
        for (var index = 0; index < batchCount; index++)
        {
            host.EnqueueAwkReadResult(AwkReadResult.FromContent(
                batch,
                [],
                reachedEof: index == batchCount - 1));
        }

        host.EnqueueAppendExitCode(0);

        var exitCode = await compiled.ExecuteAsync(host, CancellationToken.None, options);

        var inputLength = batchCount * batchLength;
        Assert.Equal(0, exitCode);
        Assert.Equal(inputLength + "\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.AwkTextPayloadBytes < inputLength * 2);
        Assert.Equal(inputLength, counters.RecordSeparatorBytesScanned);
        Assert.Equal(0, counters.RegexMatches);
    }

    [Fact]
    public async Task Awk_RegexRecordSeparatorPreservesInputStartAcrossBufferCompaction()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("xfirst,"u8.ToArray(), [], reachedEof: false));
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("xsecond"u8.ToArray(), [], reachedEof: true));
        host.EnqueueAppendExitCode(0);
        host.EnqueueAppendExitCode(0);

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{RS=\"(^x|,)\"} {print $0 \"[\" RT \"]\"}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(
            "[x]\nfirst[,]\nxsecond[]\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Theory]
    [InlineData("-+", "record--", "-")]
    [InlineData("(?:-)+", "record--", "-")]
    [InlineData("", "record\n\n", "\n")]
    public async Task Awk_GreedySeparatorContinuationIsLinearAcrossHostBatches(
        string separator,
        string firstBatch,
        string continuationBatch)
    {
        const int continuationCount = 32;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var program = "BEGIN{RS=\"" + separator + "\"} {print $0 \"[\" length(RT) \"]\"}";
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", program),
            CancellationToken.None);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(
            Encoding.UTF8.GetBytes(firstBatch),
            [],
            reachedEof: false));
        for (var index = 0; index < continuationCount; index++)
        {
            host.EnqueueAwkReadResult(AwkReadResult.FromContent(
                Encoding.UTF8.GetBytes(continuationBatch),
                [],
                reachedEof: false));
        }

        host.EnqueueAwkReadResult(AwkReadResult.FromContent("next"u8.ToArray(), [], reachedEof: true));
        host.EnqueueAppendExitCode(0);
        host.EnqueueAppendExitCode(0);

        var exitCode = await compiled.ExecuteAsync(host, CancellationToken.None, options);

        var inputLength = Encoding.UTF8.GetByteCount(firstBatch) +
            (continuationCount * Encoding.UTF8.GetByteCount(continuationBatch)) +
            4;
        Assert.Equal(0, exitCode);
        Assert.Equal(
            "record[" + (continuationCount + 2) + "]\nnext[0]\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RecordSeparatorBytesScanned <= inputLength * 2);
    }

    [Fact]
    public async Task Awk_RegexRecordSeparatorReadsInputInBatches()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("first,second;"u8.ToArray(), [], reachedEof: false));
        host.EnqueueAppendExitCode(0);

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "BEGIN{RS=\"[,;]\"} {print; exit}")));
        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal([AwkIo.InputReadBatchLineCount], host.ReadLineBatchLineCounts);
    }

    [Fact]
    public async Task Awk_RegexRecordSeparatorUsesByteRegexAdapter()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{RS=\"(,+|;+)\"} {print $0}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput("alpha,beta;gamma");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("alpha\nbeta\ngamma\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RegexMatches > 0);
    }

    [Fact]
    public async Task Awk_ParagraphRecordSeparatorReadsInputInBatches()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(
            "first paragraph\n\nsecond paragraph\n"u8.ToArray(),
            [],
            reachedEof: false));
        host.EnqueueAppendExitCode(0);

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "BEGIN{RS=\"\"} {print $0; exit}")));
        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal([AwkIo.InputReadBatchLineCount], host.ReadLineBatchLineCounts);
    }

    [Fact]
    public async Task Awk_ParagraphRecordSeparatorUsesAwkTextRecords()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{RS=\"\"} {print $0}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput("one\n\ntwo\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("one\ntwo\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(2, counters.RecordsRead);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    private static void AssertInvalidUtf8Failure(int exitCode, MockFileSystem fileSystem)
    {
        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        var stderr = fileSystem.GetOutput(AwkFileDescriptor.StdErr);
        Assert.Equal(InvalidUtf8Diagnostic, stderr);
        AwkTestHarness.AssertNoClrExceptionLeak(stderr);
    }

    private static string BuildLargeNonAsciiRecord() =>
        string.Join(" ", Enumerable.Range(1, 200).Select(index => "é" + index));
}
