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
        cancellation.Cancel();

        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token);
        var fileSystem = new MockFileSystem();
        var io = new AwkIo(
            fileSystem,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            cancellation.Token,
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            state,
            static () => []);
        var statement = AwkStatement.For(
            AwkStatement.Assignment(AwkReference.Scalar(AwkVariableName.FromText("i")), AwkExpression.Number("0", 0)),
            new AwkCondition([[AwkConditionClause.Truthy(AwkExpression.Number("1", 1))]]),
            AwkStatement.PostIncrement(AwkReference.Scalar(AwkVariableName.FromText("i"))),
            [AwkStatement.PostIncrement(AwkReference.Scalar(AwkVariableName.FromText("x")))]);

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await statement.ExecuteAsync(state, AwkTestHarness.CreateOutputSink(), io));
    }

    [Fact]
    public async Task Awk_InputRecordProcessingObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var fileSystem = new MockFileSystem
        {
            AfterReadLines = cancellation.Cancel
        };
        fileSystem.AddFile("/file.txt", "one\ntwo\n");

        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "{print $0}", "/file.txt"));

        await Assert.ThrowsAsync<OperationCanceledException>(() => tool.ExecuteAsync(fileSystem, cancellation.Token));
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

        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal(0, fileSystem.OpenFileCount);
    }

    [Fact]
    public async Task Awk_CleanupFailureDoesNotReplaceCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var fileSystem = new MockFileSystem
        {
            AfterReadLines = cancellation.Cancel,
            AfterCloseDescriptor = static () => throw new InvalidOperationException("cleanup failed")
        };
        fileSystem.AddFile("/file.txt", "one\ntwo\n");
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "{print}",
            "/file.txt"));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tool.ExecuteAsync(fileSystem, cancellation.Token));

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
            () => tool.ExecuteAsync(fileSystem, cancellation.Token));

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

        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

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
            AfterReadLines = cancellation.Cancel
        };
        fileSystem.SetStandardInput(input);

        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", program));

        await Assert.ThrowsAsync<OperationCanceledException>(() => tool.ExecuteAsync(fileSystem, cancellation.Token));
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

        await Assert.ThrowsAsync<OperationCanceledException>(() => tool.ExecuteAsync(host, cancellation.Token));
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

        await Assert.ThrowsAsync<OperationCanceledException>(() => tool.ExecuteAsync(fileSystem, cancellation.Token));

        Assert.Equal("before\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_StandardOutputAppendFailureFailsExecution()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendExitCode(7);
        host.EnqueueAppendExitCode(0);

        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "BEGIN{print \"x\"}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: file descriptor 1: append failed with status 7\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_RedirectedAppendFailureThrowsHostIoFailure()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueOpenAwkFileDescriptor(3);
        host.EnqueueAppendExitCode(9);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var io = new AwkIo(
            host,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            CancellationToken.None,
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            state,
            static () => []);

        var exception = await Assert.ThrowsAsync<AwkRuntimeException>(
            () => io.WriteAsync(AwkTestHarness.CreateRedirectedOutput(
                "/out.txt",
                AwkOutputRedirectionKind.Write,
                "x\n")));

        Assert.Equal(
            "awk: file descriptor 3: append failed with status 9\n",
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
            AwkRuntimeCounters.Disabled, regexEngine: AwkUtf8RegexEngine.Instance);
        var regex = runtime.Create(new AwkRegexPattern("a"));
        var input = AwkText.FromString(new string('a', 32));
        enumerating = true;

        using var matches = runtime.EnumerateMatchValues(
            regex,
            input,
            AwkMemoryAccounting.Disabled,
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
    public async Task Awk_RegexReplacementHeavyLoopRecordsCancellationCheckpoints()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var input = new string('a', 64);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{s=\"" + input + "\"; gsub(/a/,\"b\",s); print length(s)}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("64\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RegexMatches >= 64);
        Assert.True(counters.CancellationChecks >= 64);
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
            AwkRuntimeLimits.CreateDefault()) with
        { MemoryBudget = new CooperativeTestBudget(memoryBudgetBytes) };
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", program),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput(standardInput);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_RecursiveFunctionLimitFailsWithAwkDiagnostic()
    {
        var options = CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxFunctionCallDepth(2));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "function f(){return f()} BEGIN{print f()}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: function recursion limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_RegexCaptureMemoryLimitFailsBeforeChangingMatchState()
    {
        using var memoryForState = CreateGovernedState(1_000);
        var state = memoryForState.State;
        var arrayName = AwkArrayName.FromText("a");
        state.SetArrayString(arrayName, AwkArrayKey.FromOrdinal(1), "keep");
        SetMatchState(state, start: 41, length: 42);
        var expression = CreateCaptureMatchExpression(arrayName);

        var exception = await Assert.ThrowsAsync<AwkRuntimeException>(
            () => expression.EvaluateAsync(new AwkEvaluationContext(state)).AsTask());

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", FormatArrayValue(state, arrayName, AwkArrayKey.FromOrdinal(1)));
        Assert.True(state.TryGetExistingArrayCount(arrayName, out var count));
        Assert.Equal(1, count);
        AssertMatchState(state, start: 41, length: 42);
        var retainedTextBytes = AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.EstimateTextBytes(byteLength: 4),
            AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.EstimateTextBytes(byteLength: 2),
                AwkRuntimeMemory.EstimateTextBytes(byteLength: 2)));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task Awk_RegexEvaluationObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token);
        var expression = AwkExpression.RegexMatch(
            AwkExpression.Literal("abc"),
            AwkExpression.Literal("a"),
            AwkRegexMatchPolarity.Positive);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => expression.EvaluateAsync(new AwkEvaluationContext(state)).AsTask());
    }

    [Fact]
    public void Awk_RegexFactoryAppliesMatchTimeout()
    {
        var regex = AwkRegex.Create(
            new AwkRegexPattern("a+"),
            AwkRegexCaseSensitivity.CaseSensitive);

        var backend = Assert.IsType<AwkDotNetRegexBackend>(regex.Backend);
        Assert.Equal(AwkRegexCaseSensitivity.CaseSensitive, backend.SeedCaseSensitivity);
        Assert.NotNull(backend.Value);
        Assert.Equal(TimeSpan.FromSeconds(2), backend.Value.MatchTimeout);
    }

    [Fact]
    public async Task Awk_RecursiveFunctionEntryObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token);
        var functionName = AwkFunctionName.FromText("f");
        var function = new AwkUserFunction(
            functionName,
            AwkFunctionLocalLayout.Empty,
            [AwkStatement.Return(AwkOptionalExpression.Some(AwkExpression.FunctionCall(functionName, [])))]);
        state.InitializeFunctions(new Dictionary<AwkFunctionName, AwkUserFunction>
        {
            [function.FunctionName] = function
        });
        var fileSystem = new MockFileSystem();
        var io = new AwkIo(
            fileSystem,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            cancellation.Token,
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            state,
            static () => []);
        using var context = state.PushExecutionContext(AwkTestHarness.CreateOutputSink(), io);

        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => function
            .InvokeAsync([], new AwkEvaluationContext(state, AwkTestHarness.CreateOutputSink(), io))
            .AsTask());
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
            AwkRuntimeLimits.CreateDefault());
        using var memory = new CooperativeTestMemory(1 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            memory.Configure(options))).State;

        state.SetArrayString(arrayName, new AwkArrayKey("k"), "v");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.SetArrayString(arrayName, new AwkArrayKey("long"), "value"));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.False(state.ContainsArrayValue(arrayName, new AwkArrayKey("long")));
        Assert.Equal(
            before,
            state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
            AwkRuntimeLimits.CreateDefault());
        var state = AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            options);

        state.SetArrayString(arrayName, key, "defg");
        state.SetArrayString(arrayName, key, "x");
        state.DeleteArrayValue(arrayName, key);

        Assert.Equal(0, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task Awk_ArraySortMemoryLimitFailsBeforeReplacingDestination()
    {
        var sourceArrayName = AwkArrayName.FromText("source");
        var destinationArrayName = AwkArrayName.FromText("destination");
        var ordinalOne = new AwkArrayKey("1");
        using var memoryForState = CreateGovernedState(1 << 20);
        var state = memoryForState.State;
        state.SetArrayString(sourceArrayName, new AwkArrayKey("k"), "v");
        state.SetArrayString(destinationArrayName, ordinalOne, "keep");

        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = await Assert.ThrowsAsync<AwkRuntimeException>(
            () => state.SortArrayValuesAsync(
                sourceArrayName,
                destinationArrayName,
                AwkArraySortOrder.Default,
                new AwkEvaluationContext(state)).AsTask());

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", FormatArrayValue(state, destinationArrayName, ordinalOne));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_SplitMemoryLimitFailsBeforeReplacingArray()
    {
        var arrayName = AwkArrayName.FromText("a");
        var ordinalOne = new AwkArrayKey("1");
        using var memoryForState = CreateGovernedState(800);
        var state = memoryForState.State;
        state.SetArrayString(arrayName, ordinalOne, "keep");

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.SplitIntoArray(
                AwkText.FromString(new string('x', 80)),
                arrayName,
                AwkFieldSeparatorSpec.FromText(" "),
                AwkOptionalIdentifier.None));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", FormatArrayValue(state, arrayName, ordinalOne));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task Awk_BuiltinArraySortDoesNotReserveUserComparatorBuffers()
    {
        var sourceArrayName = AwkArrayName.FromText("source");
        var destinationArrayName = AwkArrayName.FromText("destination");
        var ordinalOne = new AwkArrayKey("1");
        using var memoryForState = CreateGovernedState(8_192);
        var state = memoryForState.State;
        state.SetArrayString(sourceArrayName, new AwkArrayKey("k"), "v");
        state.SetArrayString(destinationArrayName, ordinalOne, "keep");

        var count = await state.SortArrayValuesAsync(
            sourceArrayName,
            destinationArrayName,
            AwkArraySortOrder.Default,
            new AwkEvaluationContext(state));

        Assert.Equal(1, count);
        Assert.Equal("v", FormatArrayValue(state, destinationArrayName, ordinalOne));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task Awk_BuiltinArrayKeySortWritesDirectlyToDestination()
    {
        var sourceArrayName = AwkArrayName.FromText("source");
        var destinationArrayName = AwkArrayName.FromText("destination");
        var ordinalOne = new AwkArrayKey("1");
        using var memoryForState = CreateGovernedState(8_192);
        var state = memoryForState.State;
        state.SetArrayString(sourceArrayName, new AwkArrayKey("k"), "v");
        state.SetArrayString(destinationArrayName, ordinalOne, "keep");

        var count = await state.SortArrayKeysAsync(
            sourceArrayName,
            destinationArrayName,
            AwkArraySortOrder.Default,
            new AwkEvaluationContext(state));

        Assert.Equal(1, count);
        Assert.Equal("k", FormatArrayValue(state, destinationArrayName, ordinalOne));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_FieldSplitMemoryLimitFailsBeforeMaterializingFields()
    {
        using var memoryForState = CreateGovernedState(600);
        var state = memoryForState.State;
        var record = new string('x', 100);
        state.SetCurrentRecord(AwkText.FromString(record, state.Counters, state.MemoryAccounting));

        var exception = Assert.Throws<AwkRuntimeException>(() => state.FieldCount);

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(record, state.CurrentRecordByteText.AsString(state.Counters));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_RegexEnumerationHoldsOnlyOneMatchBuffer()
    {
        using var memoryForState = CreateGovernedState(8192);
        var state = memoryForState.State;
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var input = AwkText.FromString(new string('a', 1000));

        var matchCount = 0;
        using (var matches = state.EnumerateRegexMatches(regex, input, AwkRegexCapturePlan.All))
        {
            while (matches.MoveNext(out _))
            {
                matchCount++;
            }

            Assert.Equal(0, state.MemoryAccounting.CurrentCommittedBytes);
        }

        Assert.Equal(1000, matchCount);
        Assert.Equal(0, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_RegexFirstMatchMemoryLimitDoesNotReserveEnumerationList()
    {
        using var memoryForState = CreateGovernedState(8192);
        var state = memoryForState.State;
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var input = AwkText.FromString(new string('a', 10000));

        using (var leasedMatch = state.MatchRegexValue(regex, input, AwkRegexCapturePlan.All))
        {
            var match = leasedMatch.Match;

            Assert.True(match.Success);
            Assert.Equal("a", match.Value.AsString());
        }
        Assert.Equal(0, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, "a")]
    [InlineData(true, "a")]
    [InlineData(false, "😀")]
    [InlineData(true, "😀")]
    public void Awk_RegexFirstMatchMemoryLimitIncludesGroupPayload(bool useUtf8Adapter, string text)
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
                AwkRuntimeLimits.CreateDefault(), regexEngine: (useUtf8Adapter
                    ? true
                    : false) ? AwkUtf8RegexEngine.Instance : null)))).State;
        // Prepare outside the capture-only budget. Runtime scalar preparation has
        // its own admission tests and can require more memory than one match payload.
        var preparation = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, static () => { }, AwkRuntimeCounters.Disabled, regexEngine: useUtf8Adapter ? AwkUtf8RegexEngine.Instance : null);
        var regex = preparation.Create(new AwkRegexPattern("."));
        var input = AwkText.FromString(text);

        var before = state.MemoryAccounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(() =>
            state.MatchRegexValue(regex, input, AwkRegexCapturePlan.All));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_SubFirstReplacementMemoryLimitDoesNotReserveEnumerationList()
    {
        using var memoryForState = CreateGovernedState(8192);
        var state = memoryForState.State;
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var input = AwkText.FromString("aaaa");
        Assert.True(memoryForState.Budget.TryReserve(
            memoryForState.Budget.Limit - memoryForState.Budget.Reserved - 300, out var pressure));
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
        Assert.Equal(0, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task Awk_StandardOutputFlushAvoidsCopyingBuffer()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(300, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;
        using var output = new AwkOutputSink(governor, AwkRuntimeCounters.Disabled);
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
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            state,
            static () => []);

        await output.FlushAsync(io);

        Assert.Equal(new string('x', 80), fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, output.ToString());
        Assert.Equal(0, governor.CurrentReservedBytes);
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
            AwkRuntimeLimits.CreateDefault()) with
        { MemoryBudget = new CooperativeTestBudget(1 << 20) };
        var content = new string('x', 80);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", $"BEGIN{{print \"{content}\" > \"/out.txt\"}}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal(content + "\n", fileSystem.ReadFile("/out.txt"));
        Assert.True(counters.MemoryPeakLiveBytes > 0);
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
            AwkRuntimeLimits.CreateDefault());
        using var memory = new CooperativeTestMemory(1 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            memory.Configure(options))).State;
        state.AssignVariable(variableName, "keep");

        var before = state.MemoryAccounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.AssignVariable(variableName, new string('v', 400)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", FormatVariableValue(state, variableName));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_FunctionLocalMemoryLimitFailsBeforeReplacingValue()
    {
        var variableName = AwkVariableName.FromText("x");
        using var memoryForState = CreateGovernedState(500);
        var state = memoryForState.State;
        state.PushFunctionFrame(
            new AwkFunctionLocalLayout([variableName]),
            [AwkFunctionArgument.Scalar(AwkValue.FromText("keep"))]);

        try
        {
            var exception = Assert.Throws<AwkRuntimeException>(
                () => state.AssignVariable(variableName, new string('v', 400)));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
            Assert.Equal("keep", FormatVariableValue(state, variableName));
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes > 0);
        }
        finally
        {
            state.PopFunctionFrame();
        }

        Assert.Equal(0, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_ConcatenationTreeMemoryLimitFailsBeforeReplacingVariable()
    {
        var variableName = AwkVariableName.FromText("value");
        using var memoryForState = CreateGovernedState(50_000);
        var state = memoryForState.State;
        state.AssignVariable(variableName, "keep");
        var segment = AwkText.FromString("x", state.Counters, state.MemoryAccounting);
        var concatenated = segment;
        for (var index = 0; index < 1_024; index++)
        {
            concatenated = concatenated.Concat(
                segment,
                state.Counters,
                state.MemoryAccounting);
        }

        var exception = Assert.Throws<AwkRuntimeException>(() =>
            state.AssignVariableValue(
                variableName,
                AwkValue.FromDeferredText(concatenated, AwkNumberParsingMode.Decimal)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", FormatVariableValue(state, variableName));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_OmittedFunctionLocalArrayReleasesStorageWithFrame()
    {
        var localName = AwkVariableName.FromText("a");
        var arrayName = new AwkArrayName(localName.Identifier);
        using var memoryForState = CreateGovernedState(1_024);
        var state = memoryForState.State;
        state.PushFunctionFrame(new AwkFunctionLocalLayout([localName]), []);

        state.SetArrayAwkValue(arrayName, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));

        Assert.True(state.IsArray(arrayName));
        Assert.True(state.MemoryAccounting.CurrentCommittedBytes > 0);
        state.PopFunctionFrame();

        Assert.False(state.IsArray(arrayName));
        Assert.Equal(0, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_CurrentRecordMemoryLimitFailsBeforeReplacingValue()
    {
        using var memoryForState = CreateGovernedState(200);
        var state = memoryForState.State;
        state.SetCurrentRecord(AwkText.FromString("keep", state.Counters, state.MemoryAccounting));

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.AssignField(0, new string('v', 400)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", state.CurrentRecordByteText.AsString(state.Counters));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_FieldAssignmentMemoryLimitFailsBeforeRecomposingRecord()
    {
        using var memoryForState = CreateGovernedState(650);
        var state = memoryForState.State;
        state.SetCurrentRecord(AwkText.FromString("a b", state.Counters, state.MemoryAccounting));
        Assert.Equal(2, state.FieldCount);

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.AssignField(2, new string('v', 400)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("a b", state.CurrentRecordByteText.AsString(state.Counters));
        Assert.Equal(2, state.FieldCount);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_FieldCountMemoryLimitFailsBeforeExpandingFields()
    {
        // Admit the original field's value-type buffer while rejecting 30 fields.
        using var memoryForState = CreateGovernedState(500);
        // Admit the original field's value-type buffer while rejecting 30 fields.
        var state = memoryForState.State;
        state.SetCurrentRecord(AwkText.FromString("a", state.Counters, state.MemoryAccounting));

        var exception = Assert.Throws<AwkRuntimeException>(() => state.AssignFieldCount(30));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("a", state.CurrentRecordByteText.AsString(state.Counters));
        Assert.Equal(1, state.FieldCount);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Awk_RecordPlanMemoryFailurePrecedesCounterAndTerminatorMutation(bool paragraph)
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
        var committedBefore = state.MemoryAccounting.CurrentCommittedBytes;
        var processor = new AwkRecordProcessor(state);
        var actionEntered = false;

        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
        {
            var record = AwkText.FromSourceLiteralString("new");
            var terminator = AwkRecordTerminator.NewLine;
            await (paragraph
                ? processor.ProcessParagraphAsync(record, terminator, ApplyRecordAsync)
                : processor.ProcessAsync(record, terminator, ApplyRecordAsync));
        });

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.False(actionEntered);
        Assert.Equal("keep", state.CurrentRecordByteText.AsString());
        Assert.Equal(4, state.RecordNumber);
        Assert.Equal(3, state.FileRecordNumber);
        Assert.Equal(",", state.ResolveSpecialVariableText(AwkSpecialVariableId.RecordTerminator).AsString());
        Assert.Equal(committedBefore, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(1, state.FieldCount);

        ValueTask<AwkStatementFlow> ApplyRecordAsync(AwkText record)
        {
            actionEntered = true;
            state.SetCurrentRecord(record);
            return ValueTask.FromResult(AwkStatementFlow.Continue);
        }
    }

    [Fact]
    public void Awk_SubstitutionFieldReferenceMemoryLimitFailsBeforeExtendingFields()
    {
        // Admit the original field's value-type buffer while rejecting 30 fields.
        using var memoryForState = CreateGovernedState(500);
        // Admit the original field's value-type buffer while rejecting 30 fields.
        var state = memoryForState.State;
        state.SetCurrentRecord(AwkText.FromString("a", state.Counters, state.MemoryAccounting));
        Assert.Equal(1, state.FieldCount);

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.ExtendFieldCountForReference(new AwkFieldIndex(30)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("a", state.CurrentRecordByteText.AsString(state.Counters));
        Assert.Equal(1, state.FieldCount);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_RuntimeLimitsExposeCompatibleDefaults()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        Assert.Equal(AwkRuntimeLimits.DefaultMaxOutputFiles, limits.MaxOutputFiles);
        Assert.Equal(AwkRuntimeLimits.DefaultMaxInputFiles, limits.MaxInputFiles);
        Assert.Equal(AwkRuntimeLimits.DefaultMaxPipes, limits.MaxPipes);
        Assert.Equal(AwkRuntimeLimits.DefaultMaxIncludeDepth, limits.MaxIncludeDepth);
        Assert.Equal(AwkRuntimeLimits.DefaultInputReadBatchLineCount, AwkIo.InputReadBatchLineCount);
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

        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: program nesting limit exceeded.\n",
            fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_DefaultSyntaxLimitRejectsDeepFlatExpressionTreeBeforeExecution()
    {
        var program = new StringBuilder("BEGIN{x=0");
        for (var index = 0; index <= AwkRuntimeLimits.DefaultMaxSyntaxNestingDepth; index++)
            program.Append("+0");

        program.Append('}');
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", program.ToString()));
        var fileSystem = new MockFileSystem();

        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: program nesting limit exceeded.\n",
            fileSystem.GetOutput(AwkFileDescriptor.StdErr));
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
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxInputFiles(1);

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
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxIncludeDepth(1);

        var exitCode = await tool.ExecuteAsync(fileSystem, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: include depth limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, fileSystem.OpenFileCount);
    }

    [Fact]
    public void Awk_MemoryAccountingFailsBeforeMutatingState()
    {
        using var memoryForGovernor = new CooperativeTestMemory(10, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;

        governor.Reserve(6);
        governor.CommitReserved(4);
        var exception = Assert.Throws<AwkRuntimeException>(() => governor.Reserve(5));

        Assert.True(governor.IsEnabled);
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(2, governor.CurrentReservedBytes);
        Assert.Equal(4, governor.CurrentCommittedBytes);
    }

    [Fact]
    public void Awk_MemoryAccountingTracksReleaseCounters()
    {
        using var memoryForGovernor = new CooperativeTestMemory(16, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;

        governor.Reserve(10);
        governor.CommitReserved(6);
        governor.ReleaseReserved(4);
        governor.ReleaseCommitted(6);

        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Fact]
    public void Awk_DisabledMemoryAccountingKeepsDiagnosticsAtZero()
    {
        var governor = AwkMemoryAccounting.Disabled;

        governor.Reserve(long.MaxValue);
        governor.CommitReserved(long.MaxValue);
        governor.ReleaseReserved(long.MaxValue);
        governor.ReleaseCommitted(long.MaxValue);

        Assert.False(governor.IsEnabled);
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
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxOutputFiles(2),
            state,
            static () => []);

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
            AwkRuntimeLimits.CreateDefault().WithMaxOutputFiles(1));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation(
                "awk",
                "BEGIN{print \"one\" > \"/one.txt\"; print \"two\" > \"/two.txt\"}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

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
        state.GetArrayByteText(arrayName, key).AsString(state.Counters);

    private static string FormatVariableValue(AwkRuntimeState state, AwkVariableName name) =>
        state.FormatStringByteText(state.ResolveVariableValue(name)).AsString(state.Counters);

    private static AwkExpression CreateCaptureMatchExpression(AwkArrayName arrayName) =>
        AwkExpression.Match(
            AwkExpression.Literal("abcd"),
            AwkExpression.Literal("(ab)(cd)"),
            AwkOptionalIdentifier.Some(arrayName.Identifier));

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
            limits);

    private static CooperativeTestMemory CreateGovernedState(int memoryBudgetBytes)
    {
        var testMemory = new CooperativeTestMemory(memoryBudgetBytes);
        return testMemory.Initialize(AwkTestHarness.CreateRuntimeState(
                    AwkFieldSeparatorSpec.Default,
                    CancellationToken.None,
                    testMemory.Configure(new AwkRuntimeOptions(
                        () => new AwkUnixTimestampSeconds(0),
                        TimeZoneInfo.Utc,
                        new AwkRandomGenerator(),
                        AwkRuntimeCounters.Disabled,
                        AwkRuntimeLimits.CreateDefault()))));
    }
}
