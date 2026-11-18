using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkCurrentSurfaceCancellationResourceTests : AwkCurrentSurfaceTestBase
{
    [Fact]
    public async Task Awk_ClassicLoopObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();

        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token);
        var fileSystem = new MockFileSystem();
        var io = new AwkIo(
            fileSystem,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            cancellation.Token,
            AwkRuntimeLimits.CreateDefault(),
            state,
            static _ => []);
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("BEGIN{for(i=0;1;i++)x++}"));
        Assert.True(binding.Success);
        var program = binding.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        using var output = AwkTestHarness.CreateOutputSink();
        cancellation.Cancel();
        try
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output));
            Assert.Equal(0, state.ResolveVariableValue(AwkVariableName.FromText("x")).AsNumber());
        }
        finally { await io.CloseForCleanupAsync(); }

    }

    [Fact]
    public async Task Awk_InputRecordProcessingObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var fileSystem = new MockFileSystem
        {
            AfterRead = cancellation.Cancel
        };
        fileSystem.AddFile("/file.txt", "one\ntwo\n");

        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "{print $0}", "/file.txt"));

        await Assert.ThrowsAsync<OperationCanceledException>(() => tool.ExecuteAsync(fileSystem, AwkTestHarness.CreatePolicy(), cancellation.Token));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, fileSystem.OpenFileCount);
    }

    [Fact]
    public async Task Awk_BeginExitClosesThePreparedInputDescriptor()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "unread\n");
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{exit}",
            "/file.txt"));

        var exitCode = await tool.ExecuteAsync(fileSystem, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal(0, fileSystem.OpenFileCount);
    }

    [Fact]
    public async Task Awk_CleanupFailureDoesNotReplaceCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var fileSystem = new MockFileSystem
        {
            AfterRead = cancellation.Cancel,
            AfterCloseDescriptor = static () => throw new InvalidOperationException("cleanup failed")
        };
        fileSystem.AddFile("/file.txt", "one\ntwo\n");
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "{print}",
            "/file.txt"));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tool.ExecuteAsync(fileSystem, AwkTestHarness.CreatePolicy(), cancellation.Token));

        Assert.Equal(0, fileSystem.OpenFileCount);
    }

    [Fact]
    public async Task Awk_CancellationClosesRedirectedOutputWithAnUncancelledToken()
    {
        using var cancellation = new CancellationTokenSource();
        var fileSystem = new MockFileSystem
        {
            AfterAppend = cancellation.Cancel
        };
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"one\" > \"/out.txt\"; print \"unreached\"}"));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tool.ExecuteAsync(fileSystem, AwkTestHarness.CreatePolicy(), cancellation.Token));

        Assert.Equal(0, fileSystem.OpenFileCount);
        Assert.Equal("one\n", fileSystem.ReadFile("/out.txt"));
    }

    [Fact]
    public async Task Awk_RuntimeFailureClosesRedirectedOutputBeforeReportingTheDiagnostic()
    {
        var fileSystem = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"one\" > \"/out.txt\"; values[1]=1; print values}"));

        var exitCode = await tool.ExecuteAsync(fileSystem, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(0, fileSystem.OpenFileCount);
        Assert.Equal("one\n", fileSystem.ReadFile("/out.txt"));
        Assert.Equal(
            "awk: attempt to use array `values' in a scalar context\n",
            fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Theory]
    [InlineData("BEGIN{RS=\"|\"} {print $0}", "one|two|")]
    [InlineData("BEGIN{RS=\"\"} {print $0}", "one\n\ntwo\n\n")]
    [InlineData("BEGIN{RS=\"[,;]\"} {print $0}", "one,two;")]
    public async Task Awk_CustomRecordSeparatorProcessingObservesCancellation(
        string program,
        string input)
    {
        using var cancellation = new CancellationTokenSource();
        var fileSystem = new MockFileSystem
        {
            AfterRead = cancellation.Cancel
        };
        fileSystem.SetStandardInput(input);

        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", program));

        await Assert.ThrowsAsync<OperationCanceledException>(() => tool.ExecuteAsync(fileSystem, AwkTestHarness.CreatePolicy(), cancellation.Token));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_OutputWriteObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var host = new TestOperationVisitor([])
        {
            BeforeAppend = cancellation.Cancel
        };

        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "BEGIN{print sprintf(\"%10000s\", \"x\")}"));

        await Assert.ThrowsAsync<OperationCanceledException>(() => tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), cancellation.Token));
    }

    [Fact]
    public async Task Awk_OutputFlushedBeforeCancellationRemainsVisible()
    {
        using var cancellation = new CancellationTokenSource();
        var appendCount = 0;
        var fileSystem = new MockFileSystem
        {
            AfterAppend = () =>
            {
                appendCount++;
                if (appendCount == 1)
                    cancellation.Cancel();
            }
        };

        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"before\"; print \"after\"}"));

        await Assert.ThrowsAsync<OperationCanceledException>(() => tool.ExecuteAsync(fileSystem, AwkTestHarness.CreatePolicy(), cancellation.Token));

        Assert.Equal("before\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_StandardOutputAppendFailureFailsExecution()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendResult(AwkAppendResult.Failure(new(AwkOperationErrorKind.IoFailure, "destination failed")));
        host.EnqueueAppendResult(AwkAppendResult.Accepted());

        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "BEGIN{print \"x\"}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: standard output: write failed: destination failed\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_RedirectedAppendFailureThrowsHostIoFailure()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueOpenAwkFileDescriptor(3);
        host.EnqueueAppendResult(AwkAppendResult.Failure(new(AwkOperationErrorKind.IoFailure, "destination failed")));
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var io = new AwkIo(
            host,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            CancellationToken.None,
            AwkRuntimeLimits.CreateDefault(),
            state,
            static _ => []);

        var exception = await Assert.ThrowsAnyAsync<AwkRuntimeException>(
            () => io.WriteAsync(AwkTestHarness.CreateRedirectedOutput(
                "/out.txt",
                AwkOutputRedirectionKind.Write,
                "x\n")));

        Assert.Equal(
            "awk: file descriptor 3: write failed: destination failed\n",
            AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(AwkDiagnosticId.HostIoFailure, exception.Id);
    }

    [Fact]
    public void Awk_Utf8RegexAdapterEnumerationObservesCancellationChecks()
    {
        var checks = 0;
        var enumerating = false;
        var runtime = new AwkRegexRuntime(
            static () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune,
            () =>
            {
                if (enumerating) checks++;
                if (checks > 5)
                    throw new OperationCanceledException();
            },
            AwkTestHarness.CreateRuntimeOptions());
        var regex = runtime.Create(new AwkRegexPattern("a"));
        var input = AwkText.FromString(new string('a', 32));
        enumerating = true;

        using var matches = runtime.EnumerateMatchValues(
            regex,
            input,
            AwkRegexCapturePlan.All);
        Assert.Throws<OperationCanceledException>(() =>
        {
            while (matches.MoveNext(out _))
            {
            }
        });
        Assert.True(checks > 5);
    }

    [Fact]
    public async Task Awk_RegexReplacementLoopPreservesResultAndMatchObservations()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var input = new string('a', 64);
        var command = BuildInvocation("awk", "BEGIN{s=\"" + input + "\"; gsub(/a/,\"b\",s); print length(s)}");
        var fileSystem = new MockFileSystem();

        var exitCode = await AwkExecutor.ExecuteAsync(fileSystem, command, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("64\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));

    }

    public static TheoryData<string, string, int> GovernedAwkTextAllocationCases => new()
    {
        {
            "BEGIN{x=\"" + new string('a', 5000) + "\"; print x}",
            string.Empty,
            4096
        },
        {
            "BEGIN{x=\"" + new string('a', 9000) + "\" \"" + new string('b', 9000) + "\"; print x}",
            string.Empty,
            16384
        },
        {
            "{print substr($0, 1, 5000)}",
            new string('a', 5000) + "\n",
            4096
        },
        {
            "BEGIN{x=sprintf(\"%05000d\", 1); print x}",
            string.Empty,
            4096
        },
        {
            "{$2=$2; print $0}",
            new string('a', 5000) + " " + new string('b', 5000) + "\n",
            8192
        },
        {
            "function cmp(i1,v1,i2,v2){return 0} BEGIN{a[\"" + new string('a', 9000) + "\"]=1; a[\"" + new string('b', 9000) + "\"]=2; asorti(a,b,\"cmp\"); print \"done\"}",
            string.Empty,
            8192
        }
    };

    [Theory]
    [MemberData(nameof(GovernedAwkTextAllocationCases))]
    public async Task Awk_MemoryAccountingCoversRuntimeAwkTextAllocations(
        string program,
        string standardInput,
        int memoryBudgetBytes)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance) with
        { MemoryBudget = new CooperativeTestBudget(memoryBudgetBytes) };
        var command = BuildInvocation("awk", program);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput(standardInput);

        var exitCode = await AwkExecutor.ExecuteAsync(fileSystem, command, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_RecursiveFunctionLimitFailsWithAwkDiagnostic()
    {
        var options = CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxFunctionCallDepth(2));
        var command = BuildInvocation("awk", "function f(){return f()} BEGIN{print f()}");
        var fileSystem = new MockFileSystem();

        var exitCode = await AwkExecutor.ExecuteAsync(fileSystem, command, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: function recursion limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public void Awk_MatchAdmissionFailurePreservesCaptureAndPositionState()
    {
        using var memoryForState = CreateGovernedState(1_000);
        var state = memoryForState.State;
        var arrayName = AwkArrayName.FromText("a");
        state.SetArrayString(arrayName, AwkArrayKey.FromOrdinal(1), "keep");
        SetMatchState(state, start: 41, length: 42);
        var exception = Assert.Throws<AwkRuntimeException>(() => AwkMatchRuntime.Execute(state,
            AwkText.FromSourceLiteralString("abcd"), new AwkRegexPattern("(ab)(cd)"), new AwkArrayTarget(arrayName)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", FormatArrayValue(state, arrayName, AwkArrayKey.FromOrdinal(1)));
        Assert.True(state.TryGetExistingArrayCount(arrayName, out var count));
        Assert.Equal(1, count);
        AssertMatchState(state, start: 41, length: 42);
    }

    [Fact]
    public async Task Awk_RegexEvaluationObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{print (\"abc\"~/a/)}"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), cancellation.Token));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public async Task Awk_RecursiveFunctionEntryObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token);
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("function f(){return f()}BEGIN{f()}"));
        Assert.True(binding.Success);
        var program = binding.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var fileSystem = new MockFileSystem();
        var io = new AwkIo(
            fileSystem,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            cancellation.Token,
            AwkRuntimeLimits.CreateDefault(),
            state,
            static _ => []);
        using var output = AwkTestHarness.CreateOutputSink();
        cancellation.Cancel();
        try
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output));
            Assert.False(state.IsInsideFunction);
        }
        finally { await io.CloseForCleanupAsync(); }

    }

    [Fact]
    public void Awk_ArrayMemoryLimitFailsBeforeAddingElement()
    {
        var arrayName = AwkArrayName.FromText("a");
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        using var memory = new CooperativeTestMemory(1 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            memory.Configure(options))).State;

        state.SetArrayString(arrayName, new AwkArrayKey("k"), "v");
        var before = state.MemoryAccounting.LiveBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.SetArrayString(arrayName, new AwkArrayKey("long"), "value"));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.False(state.Arrays.ContainsValue(arrayName, new AwkArrayKey("long")));
        Assert.Equal(
            before,
            state.MemoryAccounting.LiveBytes);
    }

    [Fact]
    public void Awk_ArrayMemoryAccountingReleasesDeletedAndShrunkElements()
    {
        var arrayName = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("abc");
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            options);

        state.SetArrayString(arrayName, key, "defg");
        state.SetArrayString(arrayName, key, "x");
        state.Arrays.DeleteValue(arrayName, key);

        Assert.Equal(0, state.MemoryAccounting.LiveBytes);
    }

    [Fact]
    public void Awk_ArraySortMemoryLimitFailsBeforeReplacingDestination()
    {
        var sourceArrayName = AwkArrayName.FromText("source");
        var destinationArrayName = AwkArrayName.FromText("destination");
        var ordinalOne = new AwkArrayKey("1");
        using var memoryForState = CreateGovernedState(1 << 20);
        var state = memoryForState.State;
        state.SetArrayString(sourceArrayName, new AwkArrayKey("k"), "v");
        state.SetArrayString(destinationArrayName, ordinalOne, "keep");

        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(
            () => AwkArrayStorageTestOperations.Sort(state, sourceArrayName, destinationArrayName, AwkArraySortOrder.Default, keys: false));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", FormatArrayValue(state, destinationArrayName, ordinalOne));
    }

    [Fact]
    public void Awk_SplitMemoryLimitFailsBeforeReplacingArray()
    {
        var arrayName = AwkArrayName.FromText("a");
        var ordinalOne = new AwkArrayKey("1");
        using var memoryForState = CreateGovernedState(8 << 20);
        var state = memoryForState.State;
        state.SetArrayString(arrayName, ordinalOne, "keep");
        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.SplitIntoArray(
                AwkText.FromString(new string('x', 80)),
                arrayName,
                AwkFieldSeparatorSpec.FromText(" "),
                AwkOptionalIdentifier.None));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", FormatArrayValue(state, arrayName, ordinalOne));
    }

    [Fact]
    public void Awk_BuiltinArraySortDoesNotReserveUserComparatorBuffers()
    {
        var sourceArrayName = AwkArrayName.FromText("source");
        var destinationArrayName = AwkArrayName.FromText("destination");
        var ordinalOne = new AwkArrayKey("1");
        using var memoryForState = CreateGovernedState(8_192);
        var state = memoryForState.State;
        state.SetArrayString(sourceArrayName, new AwkArrayKey("k"), "v");
        state.SetArrayString(destinationArrayName, ordinalOne, "keep");

        var count = AwkArrayStorageTestOperations.Sort(state, sourceArrayName, destinationArrayName, AwkArraySortOrder.Default, keys: false);

        Assert.Equal(1, count);
        Assert.Equal("v", FormatArrayValue(state, destinationArrayName, ordinalOne));
    }

    [Fact]
    public void Awk_BuiltinArrayKeySortWritesDirectlyToDestination()
    {
        var sourceArrayName = AwkArrayName.FromText("source");
        var destinationArrayName = AwkArrayName.FromText("destination");
        var ordinalOne = new AwkArrayKey("1");
        using var memoryForState = CreateGovernedState(8_192);
        var state = memoryForState.State;
        state.SetArrayString(sourceArrayName, new AwkArrayKey("k"), "v");
        state.SetArrayString(destinationArrayName, ordinalOne, "keep");

        var count = AwkArrayStorageTestOperations.Sort(state, sourceArrayName, destinationArrayName, AwkArraySortOrder.Default, keys: true);

        Assert.Equal(1, count);
        Assert.Equal("k", FormatArrayValue(state, destinationArrayName, ordinalOne));
    }

    [Fact]
    public void Awk_SplitMemoryLimitFailsBeforeReplacingEitherCaptureArray()
    {
        var fieldArrayName = AwkArrayName.FromText("fields");
        var separatorArrayName = AwkArrayName.FromText("separators");
        var ordinalOne = new AwkArrayKey("1");
        using var memoryForState = CreateGovernedState(1 << 20);
        var state = memoryForState.State;
        state.SetArrayString(fieldArrayName, ordinalOne, "keep-field");
        state.SetArrayString(separatorArrayName, ordinalOne, "keep-separator");

        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.SplitIntoArray(
                AwkText.FromString("a b"),
                fieldArrayName,
                AwkFieldSeparatorSpec.FromText(" "),
                AwkOptionalIdentifier.FromText(separatorArrayName.Text)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep-field", FormatArrayValue(state, fieldArrayName, ordinalOne));
        Assert.Equal("keep-separator", FormatArrayValue(state, separatorArrayName, ordinalOne));
    }

    [Fact]
    public void Awk_PatsplitMemoryLimitFailsBeforeReplacingEitherCaptureArray()
    {
        var fieldArrayName = AwkArrayName.FromText("fields");
        var separatorArrayName = AwkArrayName.FromText("separators");
        var ordinalOne = new AwkArrayKey("1");
        using var memoryForState = CreateGovernedState(1 << 20);
        var state = memoryForState.State;
        state.SetArrayString(fieldArrayName, ordinalOne, "keep-field");
        state.SetArrayString(separatorArrayName, ordinalOne, "keep-separator");

        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.PatsplitIntoArray(
                AwkText.FromString("a b"),
                fieldArrayName,
                new AwkRegexPattern("[^ ]+"),
                AwkOptionalIdentifier.FromText(separatorArrayName.Text)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep-field", FormatArrayValue(state, fieldArrayName, ordinalOne));
        Assert.Equal("keep-separator", FormatArrayValue(state, separatorArrayName, ordinalOne));
    }

    [Fact]
    public void Awk_FieldSplitMemoryLimitFailsBeforeMaterializingFields()
    {
        using var memoryForState = CreateGovernedState(600);
        var state = memoryForState.State;
        var record = new string('x', 100);
        state.SetCurrentRecord(AwkText.FromString(record));
        // Pressure starts after the record's real storage is admitted; it must not
        // depend on a previous text-wrapper size leaving an accidental shortfall.
        memoryForState.Budget.SetAvailableBytes(0);

        var exception = Assert.Throws<AwkRuntimeException>(() => state.FieldCount);

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(record, state.CurrentRecordByteText.AsString());
    }

    [Fact]
    public void Awk_RegexEnumerationHoldsOnlyOneMatchBuffer()
    {
        using var memoryForState = CreateGovernedState(32 << 10);
        var state = memoryForState.State;
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var retained = state.MemoryAccounting.LiveBytes;
        var input = AwkText.FromString(new string('a', 1000));

        var matchCount = 0;
        using (var matches = state.EnumerateRegexMatches(regex, input, AwkRegexCapturePlan.All))
        {
            while (matches.MoveNext(out _))
            {
                matchCount++;
            }

            Assert.Equal(retained, state.MemoryAccounting.LiveBytes);
        }

        Assert.Equal(1000, matchCount);
        Assert.Equal(retained, state.MemoryAccounting.LiveBytes);
    }

    [Fact]
    public void Awk_RegexFirstMatchMemoryLimitDoesNotReserveEnumerationList()
    {
        using var memoryForState = CreateGovernedState(64 << 10);
        var state = memoryForState.State;
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var retained = state.MemoryAccounting.LiveBytes;
        var input = AwkText.FromString(new string('a', 10000));

        using (var leasedMatch = state.MatchRegexValue(regex, input, AwkRegexCapturePlan.All))
        {
            var match = leasedMatch.Match;

            Assert.True(match.Success);
            Assert.Equal("a", match.Value.AsString());
        }
        Assert.Equal(retained, state.MemoryAccounting.LiveBytes);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("😀")]
    public void Awk_RegexFirstMatchMemoryLimitIncludesGroupPayload(string text)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            memory.Configure(new AwkRuntimeOptions(
                () => new AwkUnixTimestampSeconds(0),
                TimeZoneInfo.Utc,
                new AwkRandomGenerator(),
                AwkRuntimeCounters.Disabled,
                AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance)))).State;
        // Prepare outside the capture-only budget. Runtime scalar preparation has
        // its own admission; the literal isolates capture publication from scalar matching.
        var preparation = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, static () => { }, AwkTestHarness.CreateRuntimeOptions());
        var regex = preparation.Create(new AwkRegexPattern(text));
        var input = AwkText.FromString(text);

        var before = state.MemoryAccounting.LiveBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(() =>
            state.MatchRegexValue(regex, input, AwkRegexCapturePlan.All));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(before, state.MemoryAccounting.LiveBytes);
    }

    [Fact]
    public void Awk_SubFirstReplacementSucceedsWithBoundedSearchScratch()
    {
        using var memoryForState = CreateGovernedState(32 << 10);
        var state = memoryForState.State;
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var retained = state.MemoryAccounting.LiveBytes;
        var input = AwkText.FromString("aaaa");
        Assert.True(memoryForState.Budget.TryReserve(
            memoryForState.Budget.Limit - memoryForState.Budget.Reserved - 600, out var pressure));
        using var heldPressure = pressure;

        var replaced = AwkRegexSubstitution.ReplaceAll(
            state,
            regex,
            input,
            AwkText.FromString("X"),
            AwkReplacementMode.Substitution,
            maxReplacements: 1,
            out var replacementCount);

        Assert.Equal(1, replacementCount);
        Assert.Equal("Xaaa", replaced.AsString());
        Assert.Equal(retained, state.MemoryAccounting.LiveBytes);
    }

    [Fact]
    public async Task Awk_StandardOutputFlushAvoidsCopyingBuffer()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(300);
        var governor = memoryForGovernor.Accounting;
        using var output = new AwkOutputSink(governor);
        output.Append(AwkText.FromString(new string('x', 80)));
        var fileSystem = new MockFileSystem();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var io = new AwkIo(
            fileSystem,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            CancellationToken.None,
            AwkRuntimeLimits.CreateDefault(),
            state,
            static _ => []);

        await output.FlushAsync(io);

        Assert.Equal(new string('x', 80), fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public async Task Awk_RedirectedPrintAvoidsCopyingBuffer()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance) with
        { MemoryBudget = new CooperativeTestBudget(1 << 20) };
        var content = new string('x', 80);
        var command = BuildInvocation("awk", $"BEGIN{{print \"{content}\" > \"/out.txt\"}}");
        var fileSystem = new MockFileSystem();

        var exitCode = await AwkExecutor.ExecuteAsync(fileSystem, command, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal(content + "\n", fileSystem.ReadFile("/out.txt"));

    }

    [Fact]
    public void Awk_ScalarMemoryLimitFailsBeforeReplacingValue()
    {
        var variableName = AwkVariableName.FromText("x");
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        using var memory = new CooperativeTestMemory(1 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            memory.Configure(options))).State;
        state.AssignVariable(variableName, "keep");

        var before = state.MemoryAccounting.LiveBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.AssignVariable(variableName, new string('v', 400)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", FormatVariableValue(state, variableName));
    }

    [Fact]
    public void Awk_FunctionLocalMemoryLimitFailsBeforeReplacingValue()
    {
        var variableName = AwkVariableName.FromText("x");
        using var memoryForState = CreateGovernedState(1_024);
        var state = memoryForState.State;
        state.PushFunctionFrame(
            new AwkFunctionLocalLayout([variableName]),
            [AwkFunctionLocalSlot.Scalar(AwkValue.FromText("keep"))]);

        try
        {
            memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            var exception = Assert.Throws<AwkRuntimeException>(
                () => state.AssignVariable(variableName, new string('v', 400)));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
            Assert.Equal("keep", FormatVariableValue(state, variableName));
            Assert.True(state.MemoryAccounting.LiveBytes > 0);
        }
        finally
        {
            state.PopFunctionFrame();
        }

        Assert.True(state.MemoryAccounting.LiveBytes > 0); // The frame cache remains owned.
        memoryForState.Dispose();
        Assert.Equal(0, memoryForState.Budget.Reserved);
    }

    [Fact]
    public void Awk_ConcatenationTreeMemoryLimitFailsBeforeReplacingVariable()
    {
        var variableName = AwkVariableName.FromText("value");
        using var memoryForState = CreateGovernedState(50_000);
        var state = memoryForState.State;
        state.AssignVariable(variableName, "keep");
        var segment = AwkText.FromString("x");
        var concatenated = segment;
        for (var index = 0; index < 1_024; index++)
        {
            concatenated = state.ConcatText([concatenated, segment]);
        }

        var exception = Assert.Throws<AwkRuntimeException>(() =>
            state.AssignVariableValue(
                variableName,
                AwkValue.FromDeferredText(concatenated, AwkNumberParsingMode.Decimal)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", FormatVariableValue(state, variableName));
    }

    [Fact]
    public void Awk_OmittedFunctionLocalArrayReleasesStorageWithFrame()
    {
        var localName = AwkVariableName.FromText("a");
        var arrayName = new AwkArrayName(localName.Identifier);
        using var memoryForState = CreateGovernedState(2_048);
        var state = memoryForState.State;
        state.PushFunctionFrame(new AwkFunctionLocalLayout([localName]), []);

        state.Arrays.SetAwkValue(arrayName, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));

        Assert.True(state.IsArray(arrayName));
        var active = state.MemoryAccounting.LiveBytes;
        state.PopFunctionFrame();

        Assert.False(state.IsArray(arrayName));
        Assert.InRange(state.MemoryAccounting.LiveBytes, 1, active - 1);
        memoryForState.Dispose();
        Assert.Equal(0, memoryForState.Budget.Reserved);
    }

    [Fact]
    public void Awk_CurrentRecordMemoryLimitFailsBeforeReplacingValue()
    {
        using var memoryForState = CreateGovernedState(200);
        var state = memoryForState.State;
        state.SetCurrentRecord(AwkText.FromString("keep"));

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.AssignField(0, new string('v', 400)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", state.CurrentRecordByteText.AsString());
    }

    [Fact]
    public void Awk_FieldAssignmentMemoryLimitFailsBeforeRecomposingRecord()
    {
        using var memoryForState = CreateGovernedState(650);
        var state = memoryForState.State;
        state.SetCurrentRecord(AwkText.FromString("a b"));
        Assert.Equal(2, state.FieldCount);

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.AssignField(2, new string('v', 400)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("a b", state.CurrentRecordByteText.AsString());
        Assert.Equal(2, state.FieldCount);
    }

    [Fact]
    public void Awk_FieldCountMemoryLimitFailsBeforeExpandingFields()
    {
        // Admit the original field's value-type buffer while rejecting 30 fields.
        using var memoryForState = CreateGovernedState(500);
        // Admit the original field's value-type buffer while rejecting 30 fields.
        var state = memoryForState.State;
        state.SetCurrentRecord(AwkText.FromString("a"));

        var exception = Assert.Throws<AwkRuntimeException>(() =>
            state.AssignSpecialVariableValue(AwkSpecialVariableId.FieldCount, AwkValue.FromNumber(30)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("a", state.CurrentRecordByteText.AsString());
        Assert.Equal(1, state.FieldCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Awk_RecordPlanMemoryFailurePrecedesCounterAndTerminatorMutation(bool paragraph)
    {
        using var memoryForState = CreateGovernedState(1 << 20);
        var state = memoryForState.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("keep"));
        Assert.Equal(1, state.FieldCount);
        state.RecordNumber = 4;
        state.FileRecordNumber = 3;
        state.SetRecordTerminator(AwkRecordTerminator.FromText(AwkText.FromSourceLiteralString(",")));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.FieldSeparator,
            AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 300))));
        var committedBefore = state.MemoryAccounting.LiveBytes;
        var record = AwkInputRecordResult.Record(AwkText.FromSourceLiteralString("new"), AwkRecordTerminator.NewLine,
            paragraph ? AwkRecordFieldSplittingMode.Paragraph : AwkRecordFieldSplittingMode.Ordinary);

        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(() => state.PublishInputRecord(record, readsMainInput: true));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", state.CurrentRecordByteText.AsString());
        Assert.Equal(4, state.RecordNumber);
        Assert.Equal(3, state.FileRecordNumber);
        Assert.Equal(",", state.ResolveSpecialVariableText(AwkSpecialVariableId.RecordTerminator).AsString());
        Assert.Equal(committedBefore, state.MemoryAccounting.LiveBytes);
        Assert.Equal(1, state.FieldCount);

    }

    [Fact]
    public void Awk_CancellationDuringRecordHeaderAdmissionPreservesPublishedState()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        using var cancellation = new CancellationTokenSource();
        var options = memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(),
            regexEngine: AwkUtf8RegexEngine.Instance));
        var state = memory.Initialize(AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, cancellation.Token, options)).State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("keep"));
        Assert.Equal(1, state.FieldCount);
        state.RecordNumber = 4;
        state.FileRecordNumber = 3;
        state.SetRecordTerminator(AwkRecordTerminator.FromText(AwkText.FromSourceLiteralString(",")));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.FieldSeparator,
            AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 300))));
        var record = AwkInputRecordResult.Record(AwkText.FromSourceLiteralString("new"),
            AwkRecordTerminator.NewLine, AwkRecordFieldSplittingMode.Ordinary);
        var reserved = memory.Budget.Reserved;
        memory.Budget.OnGrowth = _ => cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => state.PublishInputRecord(record, readsMainInput: true));
        memory.Budget.OnGrowth = null;
        Assert.Equal("keep", state.CurrentRecordByteText.AsString());
        Assert.Equal(4, state.RecordNumber);
        Assert.Equal(3, state.FileRecordNumber);
        Assert.Equal(",", state.ResolveSpecialVariableText(AwkSpecialVariableId.RecordTerminator).AsString());
        Assert.Equal(1, state.FieldCount);
        Assert.Equal(reserved, memory.Budget.Reserved);
    }

    [Fact]
    public void Awk_SubstitutionFieldReferenceMemoryLimitFailsBeforeExtendingFields()
    {
        // Admit the original field's value-type buffer while rejecting 30 fields.
        using var memoryForState = CreateGovernedState(500);
        // Admit the original field's value-type buffer while rejecting 30 fields.
        var state = memoryForState.State;
        state.SetCurrentRecord(AwkText.FromString("a"));
        Assert.Equal(1, state.FieldCount);

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.ExtendFieldCountForReference(new AwkFieldIndex(30)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("a", state.CurrentRecordByteText.AsString());
        Assert.Equal(1, state.FieldCount);
    }

    [Fact]
    public void Awk_RuntimeLimitsExposeCompatibleDefaults()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        Assert.Equal(AwkRuntimeLimits.DefaultMaxOutputFiles, limits.MaxOutputFiles);
        Assert.Equal(AwkRuntimeLimits.DefaultMaxInputFiles, limits.MaxInputFiles);
        Assert.Equal(AwkRuntimeLimits.DefaultMaxPipes, limits.MaxPipes);
        Assert.Equal(AwkRuntimeLimits.DefaultMaxIncludeDepth, limits.MaxIncludeDepth);
        Assert.Equal(AwkRuntimeLimits.DefaultMaxFunctionCallDepth, limits.MaxFunctionCallDepth);
        Assert.Equal(AwkRuntimeLimits.DefaultMaxSyntaxNestingDepth, limits.MaxSyntaxNestingDepth);
    }

    [Fact]
    public async Task Awk_DefaultSyntaxLimitRejectsDeeplyNestedCallsBeforeExecution()
    {
        var program = new StringBuilder("function f(x){return x} BEGIN{x=");
        for (var index = 0; index <= AwkRuntimeLimits.DefaultMaxSyntaxNestingDepth; index++)
            program.Append("f(");

        program.Append('0');
        for (var index = 0; index <= AwkRuntimeLimits.DefaultMaxSyntaxNestingDepth; index++)
            program.Append(')');

        program.Append('}');
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", program.ToString()));
        var fileSystem = new MockFileSystem();

        var exitCode = await tool.ExecuteAsync(fileSystem, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: program nesting limit exceeded.\n",
            fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Theory]
    [InlineData("sum", false)]
    [InlineData("sum", true)]
    [InlineData("concatenation", false)]
    [InlineData("concatenation", true)]
    [InlineData("else-if", false)]
    [InlineData("else-if", true)]
    [InlineData("fields", false)]
    [InlineData("fields", true)]
    public async Task Awk_SyntaxLimitsAllowFlatPrograms(string shape, bool restricted)
    {
        var source = shape switch
        {
            "sum" => "BEGIN{print " + string.Join('+', Enumerable.Repeat("1", 10_000)) + "}",
            "concatenation" => "BEGIN{print length(" + string.Concat(Enumerable.Repeat("\"a\" ", 10_000)) + ")}",
            "fields" => "BEGIN{$0=\"" + string.Join(' ', Enumerable.Range(1, 32)) + "\";print (" +
                string.Join(' ', Enumerable.Range(1, 32).Select(index => "$" + index)) + ")}",
            _ => "BEGIN{x=999;" + string.Concat(Enumerable.Range(0, 1_000).Select(index =>
                (index == 0 ? "if(x==" : "else if(x==") + index + ")print " + index + ";")) + "else print -1}"
        };
        var expected = shape == "else-if" ? "999\n" : shape == "fields"
            ? string.Concat(Enumerable.Range(1, 32)) + "\n" : "10000\n";
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(128 << 20);
        var policy = (restricted ? AwkExecutionPolicy.CreateRestricted(AwkMemoryBudget.CreateFixed(64L << 20)) : AwkExecutionPolicy.CreateDefault())
            .WithMemoryBudget(budget);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", source));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, policy, cancellation.Token));
        Assert.Equal(expected, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData("unary", false)]
    [InlineData("unary", true)]
    [InlineData("power", false)]
    [InlineData("power", true)]
    [InlineData("ternary", false)]
    [InlineData("ternary", true)]
    public async Task Awk_SyntaxLimitsStillRejectStructuralExpressionNestingBeforeEffects(string shape, bool restricted)
    {
        const int count = 140;
        var expression = shape switch
        {
            "unary" => new string('!', count) + "1",
            "power" => string.Join('^', Enumerable.Repeat("1", count)),
            _ => string.Concat(Enumerable.Repeat("0?0:", count)) + "1"
        };
        var source = "BEGIN{print \"before\";print " + expression + "}";
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = (restricted ? AwkExecutionPolicy.CreateRestricted(AwkMemoryBudget.CreateFixed(64L << 20)) : AwkExecutionPolicy.CreateDefault())
            .WithMemoryBudget(budget);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", source));
        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: program nesting limit exceeded.\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task Awk_PublicInputFileLimitFailsBeforeOpeningAnotherRedirection()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/one.txt", "one\n");
        fileSystem.AddFile("/two.txt", "two\n");
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{getline one < \"/one.txt\"; getline two < \"/two.txt\"}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMaxInputFiles(1);

        var exitCode = await tool.ExecuteAsync(fileSystem, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: input file limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, fileSystem.OpenFileCount);
    }

    [Fact]
    public async Task Awk_PublicIncludeDepthLimitFailsBeforeReadingDeeperSource()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/one.awk", "@include \"/two.awk\"\n");
        fileSystem.AddFile("/two.awk", "@include \"/three.awk\"\n");
        fileSystem.AddFile("/three.awk", "BEGIN{print \"unreachable\"}\n");
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "-f", "/one.awk"));
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMaxIncludeDepth(1);

        var exitCode = await tool.ExecuteAsync(fileSystem, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: include depth limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, fileSystem.OpenFileCount);
    }

    [Fact]
    public void Awk_DisabledMemoryAccountingIgnoresChargesAndRefunds()
    {
        var accounting = AwkMemoryAccounting.Disabled;
        accounting.Charge(long.MaxValue);
        accounting.Charge(1);
        accounting.Refund(long.MaxValue);

        Assert.False(accounting.IsEnabled);
        Assert.Equal(0, accounting.LiveBytes);
    }

    [Fact]
    public async Task Awk_OutputFileLimitFailsBeforeOpeningAnotherFile()
    {
        var fileSystem = new MockFileSystem();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var io = new AwkIo(
            fileSystem,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            CancellationToken.None,
            AwkRuntimeLimits.CreateDefault().WithMaxOutputFiles(2),
            state,
            static _ => []);

        await io.WriteAsync(AwkTestHarness.CreateRedirectedOutput(
            "/one.txt",
            AwkOutputRedirectionKind.Write,
            "one\n"));
        await io.WriteAsync(AwkTestHarness.CreateRedirectedOutput(
            "/two.txt",
            AwkOutputRedirectionKind.Write,
            "two\n"));
        var exception = await Assert.ThrowsAsync<AwkRuntimeException>(
            () => io.WriteAsync(AwkTestHarness.CreateRedirectedOutput(
                "/three.txt",
                AwkOutputRedirectionKind.Write,
                "three\n")));

        Assert.Equal("awk: output file limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.False(fileSystem.FileExists("/three.txt"));
    }

    [Fact]
    public async Task Awk_RuntimeOptionsOutputFileLimitFailsBeforeOpeningAnotherFile()
    {
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxOutputFiles(1), regexEngine: AwkUtf8RegexEngine.Instance);
        var command = BuildInvocation(
                "awk",
                "BEGIN{print \"one\" > \"/one.txt\"; print \"two\" > \"/two.txt\"}");
        var fileSystem = new MockFileSystem();

        var exitCode = await AwkExecutor.ExecuteAsync(fileSystem, command, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: output file limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(fileSystem.FileExists("/one.txt"));
        Assert.False(fileSystem.FileExists("/two.txt"));
    }

    private static AwkRuntimeOptions CreateOptions(AwkRuntimeLimits limits) =>
        CreateOptions(limits, AwkRuntimeCounters.Disabled);

    private static string FormatArrayValue(
        AwkRuntimeState state,
        AwkArrayName arrayName,
        AwkArrayKey key) =>
        state.GetArrayByteText(arrayName, key).AsString();

    private static string FormatVariableValue(AwkRuntimeState state, AwkVariableName name) =>
        state.FormatStringByteText(state.ResolveVariableValue(name)).AsString();

    private static void SetMatchState(AwkRuntimeState state, int start, int length)
    {
        state.AssignVariableValue(
            new AwkSpecialVariableName(AwkSpecialVariableId.RegexStart),
            AwkValue.FromNumber(start));
        state.AssignVariableValue(
            new AwkSpecialVariableName(AwkSpecialVariableId.RegexLength),
            AwkValue.FromNumber(length));
    }

    private static void AssertMatchState(AwkRuntimeState state, int start, int length)
    {
        Assert.Equal(
            start,
            state.ResolveVariableValue(new AwkSpecialVariableName(AwkSpecialVariableId.RegexStart)).AsNumber());
        Assert.Equal(
            length,
            state.ResolveVariableValue(new AwkSpecialVariableName(AwkSpecialVariableId.RegexLength)).AsNumber());
    }

    private static AwkRuntimeOptions CreateOptions(
        AwkRuntimeLimits limits,
        AwkRuntimeCounters counters) =>
        new(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            limits, regexEngine: AwkUtf8RegexEngine.Instance);

    private static CooperativeTestMemory CreateGovernedState(int memoryBudgetBytes)
    {
        var testMemory = new CooperativeTestMemory(16L << 20);
        testMemory.Initialize(AwkTestHarness.CreateRuntimeState(
                    AwkFieldSeparatorSpec.Default,
                    CancellationToken.None,
                    testMemory.Configure(new AwkRuntimeOptions(
                        () => new AwkUnixTimestampSeconds(0),
                        TimeZoneInfo.Utc,
                        new AwkRandomGenerator(),
                        AwkRuntimeCounters.Disabled,
                        AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance))));
        testMemory.Budget.SetAvailableBytes(memoryBudgetBytes);
        return testMemory;
    }
}
