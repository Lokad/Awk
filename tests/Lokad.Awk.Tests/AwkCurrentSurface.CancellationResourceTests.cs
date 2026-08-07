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

        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token);
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
            AwkStatement.Assignment(AwkReference.Scalar("i"), AwkExpression.Number("0", 0)),
            new AwkCondition([[AwkConditionClause.Truthy(AwkExpression.Number("1", 1))]]),
            AwkStatement.PostIncrement(AwkReference.Scalar("i")),
            [AwkStatement.PostIncrement(AwkReference.Scalar("x"))]);

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await statement.ExecuteAsync(state, new AwkOutputSink(), io));
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

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "{print $0}", "/file.txt")));

        await Assert.ThrowsAsync<OperationCanceledException>(() => tool.ExecuteAsync(fileSystem, cancellation.Token));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, fileSystem.OpenFileCount);
    }

    [Fact]
    public async Task Awk_BeginExitClosesThePreparedInputDescriptor()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "unread\n");
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{exit}",
            "/file.txt")));

        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(0, exitCode);
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
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "{print}",
            "/file.txt")));

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
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{print \"one\" > \"/out.txt\"; print \"unreached\"}")));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tool.ExecuteAsync(fileSystem, cancellation.Token));

        Assert.Equal(0, fileSystem.OpenFileCount);
        Assert.Equal("one\n", fileSystem.ReadFile("/out.txt"));
    }

    [Fact]
    public async Task Awk_RuntimeFailureClosesRedirectedOutputBeforeReportingTheDiagnostic()
    {
        var fileSystem = new MockFileSystem();
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{print \"one\" > \"/out.txt\"; values[1]=1; print values}")));

        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(2, exitCode);
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

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", program)));

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

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "BEGIN{print sprintf(\"%10000s\", \"x\")}")));

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

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{print \"before\"; print \"after\"}")));

        await Assert.ThrowsAsync<OperationCanceledException>(() => tool.ExecuteAsync(fileSystem, cancellation.Token));

        Assert.Equal("before\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_StandardOutputAppendFailureFailsExecution()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendExitCode(7);
        host.EnqueueAppendExitCode(0);

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "BEGIN{print \"x\"}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
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
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
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
            () => io.WriteAsync(new AwkRedirectedOutput("/out.txt", AwkOutputRedirectionKind.Write, "x\n")));

        Assert.Equal(
            "awk: file descriptor 3: append failed with status 9\n",
            AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(AwkDiagnosticId.HostIoFailure, exception.Id);
    }

    [Fact]
    public void Awk_Utf8RegexAdapterEnumerationObservesCancellationChecks()
    {
        var checks = 0;
        var runtime = new AwkRegexRuntime(
            static () => false,
            static () => false,
            () =>
            {
                checks++;
                if (checks > 5)
                    throw new OperationCanceledException();
            },
            AwkRuntimeCounters.Disabled,
            AwkRegexEngineKind.Utf8Adapter);
        var regex = runtime.Create(new AwkRegexPattern("a"));
        var input = AwkText.FromString(new string('a', 32));

        using var matches = runtime.EnumerateMatchValues(regex, input, AwkMemoryGovernor.Disabled);
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
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);
        var input = new string('a', 64);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{s=\"" + input + "\"; gsub(/a/,\"b\",s); print length(s)}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("64\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RegexMatches >= 64);
        Assert.True(counters.CancellationChecks >= 64);
    }

    [Theory]
    [InlineData("BEGIN{while(1){i++}}", 4)]
    [InlineData("BEGIN{for(i=0;1;i++){x++}}", 4)]
    [InlineData("BEGIN{do{x++}while(1)}", 4)]
    [InlineData("BEGIN{a[1]=1; a[2]=2; a[3]=3; for(k in a){x++}}", 7)]
    [InlineData("BEGIN{a=1; b=2; c=3}", 2)]
    [InlineData("BEGIN{x=1+1+1+1+1+1+1+1+1+1+1+1+1+1+1+1; print x}", 10)]
    [InlineData("function f(n){return f(n+1)} BEGIN{print f(0)}", 20)]
    public async Task Awk_ExecutionStepLimitFailsWithDiagnostic(string program, int maxExecutionSteps)
    {
        var options = CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(maxExecutionSteps));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", program),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: execution step limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_DefaultExecutionStepBudgetAllowsMeasuredStreamingWorkload()
    {
        const string program =
            "{sum[$1,$2]+=$4; count[$3]++} END{print length(sum), length(count)}";
        var input = BuildExecutionStepCrossingInput();
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", program),
            CancellationToken.None);

        var limitedFileSystem = new MockFileSystem();
        limitedFileSystem.SetStandardInput(input);
        var limitedExitCode = await compiled.ExecuteAsync(
            limitedFileSystem,
            CancellationToken.None,
            CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(1_000_000)));

        Assert.Equal(AwkExitCode.FatalError.Value, limitedExitCode);
        Assert.Equal(string.Empty, limitedFileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: execution step limit exceeded\n",
            limitedFileSystem.GetOutput(AwkFileDescriptor.StdErr));

        var counters = AwkRuntimeCounters.CreateEnabled();
        var measuredOptions = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var measuredFileSystem = new MockFileSystem();
        measuredFileSystem.SetStandardInput(input);

        var measuredExitCode = await compiled.ExecuteAsync(
            measuredFileSystem,
            CancellationToken.None,
            measuredOptions);

        Assert.Equal(0, measuredExitCode);
        Assert.Equal("1111 2\n", measuredFileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, measuredFileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.ExecutionSteps > 1_000_000);
        Assert.True(counters.ExecutionSteps < AwkRuntimeLimits.DefaultMaxExecutionSteps);

        var systemDefaultFileSystem = new MockFileSystem();
        systemDefaultFileSystem.SetStandardInput(input);

        var systemDefaultExitCode = await compiled.ExecuteAsync(
            systemDefaultFileSystem,
            CancellationToken.None,
            AwkRuntimeOptions.CreateSystemDefault());

        Assert.Equal(0, systemDefaultExitCode);
        Assert.Equal("1111 2\n", systemDefaultFileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, systemDefaultFileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_OutputBuilderLimitFailsBeforeWritingStandardOutput()
    {
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxBuilderBytes(4),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{print \"abcde\"}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_OutputByteLimitFailsBeforeWritingStandardOutput()
    {
        var options = CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxOutputBytes(4));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{print \"abcde\"}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_ValueByteLimitFailsBeforeStringAssignmentTakesEffect()
    {
        var options = CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(4));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{x=\"ok\"; x=x \"abcd\"; print x}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
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
    public async Task Awk_MemoryGovernorCoversRuntimeAwkTextAllocations(
        string program,
        string standardInput,
        int maxExecutionMemoryBytes)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(maxExecutionMemoryBytes),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", program),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput(standardInput);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.MemoryReserveCalls > 0);
    }

    [Fact]
    public async Task Awk_RecordByteLimitFailsBeforeProcessingHugeRecord()
    {
        var options = CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxRecordBytes(4));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "{print NR; print $0}", "/file.txt"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "abcde\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_FieldExplosionLimitFailsBeforeMaterializingFieldValues()
    {
        var options = CreateOptions(
            AwkRuntimeLimits.CreateDefault()
                .WithMaxArrayElements(16)
                .WithMaxFields(2));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "{print $3}", "/file.txt"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "a b c\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: array element limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_FieldLimitDoesNotConstrainAssociativeArrays()
    {
        var options = CreateOptions(
            AwkRuntimeLimits.CreateDefault()
                .WithMaxArrayElements(4)
                .WithMaxFields(1));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{a[1]=1; a[2]=2; print length(a)}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("2\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_SplitExplosionLimitFailsBeforeReplacingArray()
    {
        var options = CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxArrayElements(2));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{a[1]=\"keep\"; split(\"a,b,c\", a, \",\"); print a[1]}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: array element limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_GsubExpansionLimitFailsBeforeAssigningReplacement()
    {
        var options = CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(5));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{x=\"aa\"; gsub(\"a\", \"bbb\", x); print x}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
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

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal("awk: function recursion limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_HostCallLimitFailsBeforeHostRead()
    {
        var options = CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxHostCalls(1));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "{print $0}", "/file.txt"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "one\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: host call limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_RegexCaptureLimitFailsBeforeClearingDestinationArray()
    {
        var options = CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxRegexCaptureBytes(2));
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{a[1]=\"keep\"; match(\"abcd\", /(ab)(cd)/, a); print a[1]}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public void Awk_VmStackLimitUsesAwkDiagnostic()
    {
        var options = CreateOptions(AwkRuntimeLimits.CreateDefault().WithMaxVmStackBytes(8));
        var state = new AwkRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            options);

        var exception = Assert.Throws<AwkRuntimeException>(() => state.ThrowIfVmStackBytesExceeded(9));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
    }

    [Fact]
    public async Task Awk_RegexEvaluationObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token);
        var expression = AwkExpression.RegexMatch(
            AwkExpression.Literal("abc"),
            AwkExpression.Literal("a"),
            negate: false);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => expression.EvaluateAsync(new AwkEvaluationContext(0, state)).AsTask());
    }

    [Fact]
    public void Awk_RegexFactoryAppliesMatchTimeout()
    {
        var regex = AwkRegex.Create(new AwkRegexPattern("a+"), ignoreCase: false);

        Assert.True(regex.TryGetDotNetRegex(ignoreCase: false, out var dotNetRegex));
        Assert.Equal(AwkRegex.MatchTimeout, dotNetRegex.MatchTimeout);
    }

    [Fact]
    public async Task Awk_RecursiveFunctionEntryObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token);
        var functionName = AwkFunctionName.FromText("f");
        var function = new AwkUserFunction(
            functionName,
            [],
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
        using var context = state.PushExecutionContext(new AwkOutputSink(), io);

        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => function
            .InvokeAsync([], new AwkEvaluationContext(0, state, new AwkOutputSink(), io))
            .AsTask());
    }

    [Fact]
    public void Awk_ArrayGrowthLimitFailsBeforeAddingElement()
    {
        var arrayName = AwkArrayName.FromText("a");
        var state = new AwkRuntimeState(AwkRuntimeStatePlan.Create(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            new AwkRuntimeOptions(
                () => new AwkUnixTimestampSeconds(0),
                TimeZoneInfo.Local,
                new AwkRandomGenerator(),
                AwkRuntimeCounters.Disabled,
                AwkRuntimeLimits.CreateDefault(),
                AwkRegexEngineKind.DotNetCompatibility,
                AwkExecutionEngineKind.Interpreter),
            AwkCompatibilityMode.GnuDefault,
            AwkLintMode.Off,
            charactersAsBytes: false,
            nonDecimalData: false,
            maxArrayElements: 2));

        state.SetArrayString(arrayName, new AwkArrayKey("1"), "one");
        state.SetArrayString(arrayName, new AwkArrayKey("2"), "two");
        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.SetArrayString(arrayName, new AwkArrayKey("3"), "three"));

        Assert.Equal("awk: array element limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.False(state.ContainsArrayValue(arrayName, new AwkArrayKey("3")));
    }

    [Fact]
    public void Awk_RuntimeOptionsArrayLimitFlowsToState()
    {
        var arrayName = AwkArrayName.FromText("a");
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxArrayElements(1),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var state = new AwkRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            options);

        state.SetArrayString(arrayName, new AwkArrayKey("1"), "one");
        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.SetArrayString(arrayName, new AwkArrayKey("2"), "two"));

        Assert.Equal("awk: array element limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.False(state.ContainsArrayValue(arrayName, new AwkArrayKey("2")));
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
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(300),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var state = new AwkRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            options);

        state.SetArrayString(arrayName, new AwkArrayKey("k"), "v");
        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.SetArrayString(arrayName, new AwkArrayKey("long"), "value"));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.False(state.ContainsArrayValue(arrayName, new AwkArrayKey("long")));
        Assert.Equal(
            AwkRuntimeMemory.EstimateArrayElementBytes(new AwkArrayKey("k"), AwkVariableSlot.FromText("v")),
            state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_ArrayMemoryGovernorReleasesDeletedAndShrunkElements()
    {
        var arrayName = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("abc");
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1024),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var state = new AwkRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            options);

        state.SetArrayString(arrayName, key, "defg");
        state.SetArrayString(arrayName, key, "x");
        state.DeleteArrayValue(arrayName, key);

        Assert.Equal(0, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
        Assert.Equal(2, state.MemoryGovernor.ReleaseCommittedCount);
    }

    [Fact]
    public void Awk_ArraySnapshotMemoryLimitFailsBeforeReturningKeys()
    {
        var arrayName = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("k");
        var state = CreateStateWithMemoryLimit(220);
        state.SetArrayString(arrayName, key, "v");

        var exception = Assert.Throws<AwkRuntimeException>(() => state.SnapshotArrayKeys(arrayName));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.True(state.ContainsArrayValue(arrayName, key));
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_ArraySnapshotLeaseReleasesReservationAndRejectsFurtherUse()
    {
        var arrayName = AwkArrayName.FromText("a");
        var state = CreateStateWithMemoryLimit(2_000);
        state.SetArrayString(arrayName, new AwkArrayKey("k"), "v");
        var baselineCommittedBytes = state.MemoryGovernor.CurrentCommittedBytes;
        var keys = state.SnapshotArrayKeys(arrayName);

        Assert.Single(keys);
        Assert.True(state.MemoryGovernor.CurrentCommittedBytes > baselineCommittedBytes);

        keys.Dispose();

        Assert.Equal(baselineCommittedBytes, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Throws<ObjectDisposedException>(() => _ = keys.Count);
    }

    [Fact]
    public void Awk_ArraySortMemoryLimitFailsBeforeReplacingDestination()
    {
        var sourceArrayName = AwkArrayName.FromText("source");
        var destinationArrayName = AwkArrayName.FromText("destination");
        var ordinalOne = new AwkArrayKey("1");
        var state = CreateStateWithMemoryLimit(600);
        state.SetArrayString(sourceArrayName, new AwkArrayKey("k"), "v");
        state.SetArrayString(destinationArrayName, ordinalOne, "keep");

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.SortArrayValues(sourceArrayName, destinationArrayName, AwkArraySortOrder.Default));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", state.GetArrayString(destinationArrayName, ordinalOne));
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_SplitMemoryLimitFailsBeforeReplacingArray()
    {
        var arrayName = AwkArrayName.FromText("a");
        var ordinalOne = new AwkArrayKey("1");
        var state = CreateStateWithMemoryLimit(400);
        state.SetArrayString(arrayName, ordinalOne, "keep");

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.SplitIntoArray(
                AwkText.FromString(new string('x', 80)),
                arrayName,
                " ",
                AwkOptionalIdentifier.None));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", state.GetArrayString(arrayName, ordinalOne));
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_SplitMemoryLimitFailsBeforeReplacingEitherCaptureArray()
    {
        var fieldArrayName = AwkArrayName.FromText("fields");
        var separatorArrayName = AwkArrayName.FromText("separators");
        var ordinalOne = new AwkArrayKey("1");
        var state = CreateStateWithMemoryLimit(650);
        state.SetArrayString(fieldArrayName, ordinalOne, "keep-field");
        state.SetArrayString(separatorArrayName, ordinalOne, "keep-separator");

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.SplitIntoArray(
                AwkText.FromString("a b"),
                fieldArrayName,
                " ",
                AwkOptionalIdentifier.FromText(separatorArrayName.Text)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep-field", state.GetArrayString(fieldArrayName, ordinalOne));
        Assert.Equal("keep-separator", state.GetArrayString(separatorArrayName, ordinalOne));
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_PatsplitMemoryLimitFailsBeforeReplacingEitherCaptureArray()
    {
        var fieldArrayName = AwkArrayName.FromText("fields");
        var separatorArrayName = AwkArrayName.FromText("separators");
        var ordinalOne = new AwkArrayKey("1");
        var state = CreateStateWithMemoryLimit(650);
        state.SetArrayString(fieldArrayName, ordinalOne, "keep-field");
        state.SetArrayString(separatorArrayName, ordinalOne, "keep-separator");

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.PatsplitIntoArray(
                AwkText.FromString("a b"),
                fieldArrayName,
                "[^ ]+",
                AwkOptionalIdentifier.FromText(separatorArrayName.Text)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep-field", state.GetArrayString(fieldArrayName, ordinalOne));
        Assert.Equal("keep-separator", state.GetArrayString(separatorArrayName, ordinalOne));
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_FieldSplitMemoryLimitFailsBeforeMaterializingFields()
    {
        var state = CreateStateWithMemoryLimit(300);
        var record = new string('x', 100);
        state.SetCurrentRecord(record);

        var exception = Assert.Throws<AwkRuntimeException>(() => state.FieldCount);

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(record, state.CurrentRecordString);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_RegexEnumerationHoldsOnlyOneMatchBuffer()
    {
        var state = CreateStateWithMemoryLimit(200);
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var input = AwkText.FromString("aaaa");

        var matchCount = 0;
        using (var matches = state.EnumerateRegexMatches(regex, input))
        {
            while (matches.MoveNext(out _))
                matchCount++;

            Assert.InRange(state.MemoryGovernor.CurrentCommittedBytes, 1, 200);
        }

        Assert.Equal(4, matchCount);
        Assert.Equal(0, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_RegexFirstMatchMemoryLimitDoesNotReserveEnumerationList()
    {
        var state = CreateStateWithMemoryLimit(200);
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var input = AwkText.FromString("aaaa");

        var match = state.MatchRegexValue(regex, input);

        Assert.True(match.Success);
        Assert.Equal("a", match.Value.AsString());
        Assert.Equal(0, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_SubFirstReplacementMemoryLimitDoesNotReserveEnumerationList()
    {
        var state = CreateStateWithMemoryLimit(300);
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var input = AwkText.FromString("aaaa");

        var replaced = AwkRegexSubstitution.ReplaceAll(
            state,
            regex,
            input,
            "X",
            AwkReplacementMode.Substitution,
            maxReplacements: 1,
            out var replacementCount);

        Assert.Equal(1, replacementCount);
        Assert.Equal("Xaaa", replaced.AsString());
        Assert.Equal(0, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public async Task Awk_StandardOutputFlushAvoidsCopyingBuffer()
    {
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(300);
        var governor = AwkMemoryGovernor.Create(limits);
        using var output = new AwkOutputSink(limits, governor, AwkRuntimeCounters.Disabled);
        output.Append(new string('x', 80));
        var fileSystem = new MockFileSystem();
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
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(950),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var content = new string('x', 80);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", $"BEGIN{{print \"{content}\" > \"/out.txt\"}}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal(content + "\n", fileSystem.ReadFile("/out.txt"));
        Assert.InRange(counters.MemoryPeakLiveBytes, 1, 950);
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
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(200),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var state = new AwkRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            options);
        state.AssignVariable(variableName, "keep");

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.AssignVariable(variableName, new string('v', 400)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", state.ResolveVariable(variableName));
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_FunctionLocalMemoryLimitFailsBeforeReplacingValue()
    {
        var variableName = AwkVariableName.FromText("x");
        var state = CreateStateWithMemoryLimit(300);
        state.PushFunctionFrame(
            new AwkFunctionLocalLayout([variableName]),
            [AwkFunctionArgument.Scalar(AwkValue.FromText("keep"))]);

        try
        {
            var exception = Assert.Throws<AwkRuntimeException>(
                () => state.AssignVariable(variableName, new string('v', 400)));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
            Assert.Equal("keep", state.ResolveVariable(variableName));
            Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
            Assert.True(state.MemoryGovernor.CurrentCommittedBytes > 0);
        }
        finally
        {
            state.PopFunctionFrame();
        }

        Assert.Equal(0, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_CurrentRecordMemoryLimitFailsBeforeReplacingValue()
    {
        var state = CreateStateWithMemoryLimit(200);
        state.SetCurrentRecord("keep");

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.AssignField(0, new string('v', 400)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", state.CurrentRecordString);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_FieldAssignmentMemoryLimitFailsBeforeRecomposingRecord()
    {
        var state = CreateStateWithMemoryLimit(650);
        state.SetCurrentRecord("a b");
        Assert.Equal(2, state.FieldCount);

        var exception = Assert.Throws<AwkRuntimeException>(
            () => state.AssignField(2, new string('v', 400)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("a b", state.CurrentRecordString);
        Assert.Equal(2, state.FieldCount);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_FieldCountMemoryLimitFailsBeforeExpandingFields()
    {
        var state = CreateStateWithMemoryLimit(300);
        state.SetCurrentRecord("a");

        var exception = Assert.Throws<AwkRuntimeException>(() => state.AssignFieldCount(30));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("a", state.CurrentRecordString);
        Assert.Equal(1, state.FieldCount);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void Awk_RuntimeLimitsExposeCompatibleDefaults()
    {
        var limits = AwkRuntimeLimits.CreateDefault();

        Assert.Equal(10_000_000, AwkRuntimeLimits.DefaultMaxExecutionSteps);
        Assert.Equal(10_000_000, AwkRuntimeOptions.DefaultMaxExecutionSteps);
        Assert.Equal(10_000_000, limits.MaxExecutionSteps);
        Assert.Equal(10_000_000, AwkRuntimeOptions.CreateSystemDefault().Limits.MaxExecutionSteps);
        Assert.Equal(AwkRuntimeOptions.DefaultMaxExecutionSteps, limits.MaxExecutionSteps);
        Assert.Equal(AwkRuntimeState.DefaultMaxArrayElements, limits.MaxArrayElements);
        Assert.Equal(AwkRuntimeLimits.DefaultMaxFields, limits.MaxFields);
        Assert.Equal(AwkIo.DefaultMaxOutputFiles, limits.MaxOutputFiles);
        Assert.Equal(AwkRuntimeLimits.DefaultMaxBuilderBytes, limits.MaxBuilderBytes);
        Assert.Equal(AwkRuntimeLimits.DefaultInputReadBatchLineCount, AwkIo.InputReadBatchLineCount);
        Assert.Equal(
            TimeSpan.FromSeconds(AwkRuntimeLimits.DefaultRegexMatchTimeoutSeconds),
            AwkRegex.MatchTimeout);
        Assert.Equal(AwkRuntimeLimits.DefaultMaxFunctionCallDepth, limits.MaxFunctionCallDepth);
        Assert.Equal(AwkRuntimeLimits.Unlimited, limits.MaxHostCalls);
        Assert.Equal(AwkRuntimeLimits.Unlimited, limits.MaxRecordBytes);
        Assert.Equal(AwkRuntimeLimits.Unlimited, limits.MaxValueBytes);
        Assert.Equal(AwkRuntimeLimits.Unlimited, limits.MaxOutputBytes);
        Assert.Equal(AwkRuntimeLimits.Unlimited, limits.MaxRegexCaptureBytes);
        Assert.Equal(AwkRuntimeLimits.Unlimited, limits.MaxVmStackBytes);
        Assert.Equal(AwkRuntimeLimits.Unlimited, limits.MaxExecutionMemoryBytes);
    }

    [Fact]
    public void Awk_MemoryGovernorFailsBeforeMutatingState()
    {
        var governor = AwkMemoryGovernor.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(10));

        governor.Reserve(6);
        governor.CommitReserved(4);
        var exception = Assert.Throws<AwkRuntimeException>(() => governor.Reserve(5));

        Assert.True(governor.IsEnabled);
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(2, governor.CurrentReservedBytes);
        Assert.Equal(4, governor.CurrentCommittedBytes);
        Assert.Equal(6, governor.PeakReservedBytes);
        Assert.Equal(4, governor.PeakCommittedBytes);
        Assert.Equal(6, governor.PeakLiveBytes);
        Assert.Equal(1, governor.ReserveCount);
        Assert.Equal(1, governor.CommitCount);
        Assert.Equal(6, governor.TotalReservedBytes);
        Assert.Equal(4, governor.TotalCommittedBytes);
    }

    [Fact]
    public void Awk_MemoryGovernorTracksReleaseCounters()
    {
        var governor = AwkMemoryGovernor.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(16));

        governor.Reserve(10);
        governor.CommitReserved(6);
        governor.ReleaseReserved(4);
        governor.ReleaseCommitted(6);

        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(1, governor.ReserveCount);
        Assert.Equal(1, governor.CommitCount);
        Assert.Equal(1, governor.ReleaseReservedCount);
        Assert.Equal(1, governor.ReleaseCommittedCount);
        Assert.Equal(10, governor.TotalReservedBytes);
        Assert.Equal(6, governor.TotalCommittedBytes);
        Assert.Equal(4, governor.TotalReleasedReservedBytes);
        Assert.Equal(6, governor.TotalReleasedCommittedBytes);
        Assert.Equal(10, governor.PeakLiveBytes);
    }

    [Fact]
    public void Awk_DisabledMemoryGovernorKeepsDiagnosticsAtZero()
    {
        var governor = AwkMemoryGovernor.Disabled;

        governor.Reserve(long.MaxValue);
        governor.CommitReserved(long.MaxValue);
        governor.ReleaseReserved(long.MaxValue);
        governor.ReleaseCommitted(long.MaxValue);

        Assert.False(governor.IsEnabled);
        Assert.Equal(0, governor.ReserveCount);
        Assert.Equal(0, governor.CommitCount);
        Assert.Equal(0, governor.ReleaseReservedCount);
        Assert.Equal(0, governor.ReleaseCommittedCount);
        Assert.Equal(0, governor.TotalReservedBytes);
        Assert.Equal(0, governor.TotalCommittedBytes);
        Assert.Equal(0, governor.TotalReleasedReservedBytes);
        Assert.Equal(0, governor.TotalReleasedCommittedBytes);
    }

    [Fact]
    public async Task Awk_OutputFileLimitFailsBeforeOpeningAnotherFile()
    {
        var fileSystem = new MockFileSystem();
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
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

        await io.WriteAsync(new AwkRedirectedOutput("/one.txt", AwkOutputRedirectionKind.Write, "one\n"));
        await io.WriteAsync(new AwkRedirectedOutput("/two.txt", AwkOutputRedirectionKind.Write, "two\n"));
        var exception = await Assert.ThrowsAsync<AwkRuntimeException>(
            () => io.WriteAsync(new AwkRedirectedOutput("/three.txt", AwkOutputRedirectionKind.Write, "three\n")));

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
            AwkRuntimeLimits.CreateDefault().WithMaxOutputFiles(1),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation(
                "awk",
                "BEGIN{print \"one\" > \"/one.txt\"; print \"two\" > \"/two.txt\"}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal("awk: output file limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(fileSystem.FileExists("/one.txt"));
        Assert.False(fileSystem.FileExists("/two.txt"));
    }

    private static AwkRuntimeOptions CreateOptions(AwkRuntimeLimits limits) =>
        new(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            limits,
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);

    private static string BuildExecutionStepCrossingInput()
    {
        const int recordCount = 100_000;
        var builder = new StringBuilder(recordCount * 26);
        for (var index = 0; index < recordCount; index++)
        {
            builder.Append("sku");
            builder.Append(index % 101);
            builder.Append(" region");
            builder.Append(index % 11);
            builder.Append(index % 2 == 0 ? " open " : " closed ");
            builder.Append(index % 100);
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static AwkRuntimeState CreateStateWithMemoryLimit(int maxExecutionMemoryBytes) =>
        new(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            new AwkRuntimeOptions(
                () => new AwkUnixTimestampSeconds(0),
                TimeZoneInfo.Utc,
                new AwkRandomGenerator(),
                AwkRuntimeCounters.Disabled,
                AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(maxExecutionMemoryBytes),
                AwkRegexEngineKind.DotNetCompatibility,
                AwkExecutionEngineKind.Interpreter));
}
