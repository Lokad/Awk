using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkHostReadFailureTests : AwkCurrentSurfaceTestBase
{
    [Theory]
    [InlineData("complete", "argument")] [InlineData("cancel", "argument")] [InlineData("refuse", "argument")]
    [InlineData("complete", "condition")] [InlineData("cancel", "condition")] [InlineData("refuse", "condition")]
    [InlineData("complete", "return")] [InlineData("cancel", "return")] [InlineData("refuse", "return")]
    public async Task OrdinaryFunctionResumesPendingArgumentConditionOrReturnOnce(string outcome, string position)
    {
        var host = new MockFileSystem();
        host.SetStandardInput("4 row\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = async (descriptor, maximum, token) =>
        {
            using var registration = token.Register(() => canceled.TrySetResult());
            entered.TrySetResult();
            await resume.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, token).ConfigureAwait(false);
        };
        var source = "function more(){getline;return $1} " + (position switch
        {
            "condition" => "function score(a,b,c,d,e,t,i){t=a;for(i=0;i<2;i++){if(i==0)continue;if(more())t+=b+c+d+e};return t} " +
                "BEGIN{$0=\"7 row\";print \"ready\";result=score($1,1,2,3,4);print result;exit}",
            "return" => "function score(a,b,c,d,e,t,i){t=a;for(i=0;i<2;i++){if(i==0)continue;return more()+t+b+c+d+e};return 0} " +
                "BEGIN{$0=\"7 row\";print \"ready\";result=score($1,1,2,3,0);print result;exit}",
            _ => "function score(a,b,c,d,e){return a+b+c+d+e} " +
                "BEGIN{$0=\"7 row\";print \"ready\";for(i=0;i<1;i++)result=score($1,more(),1,2,3);print result;exit}"
        }) + " END{print \"completed\"}";
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var budget = new CooperativeTestBudget(4 << 20);
        using var cancellation = new CancellationTokenSource();
        var execution = tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.True(budget.Reserved > 0);
            if (outcome == "cancel")
            {
                cancellation.Cancel();
                await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(execution.IsCompleted);
                Assert.True(budget.Reserved > 0);
            }
            if (outcome == "refuse") budget.RefuseNextAdmissionMinimumGrowth = 1;
            resume.TrySetResult();
            if (outcome == "cancel") await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            else Assert.Equal(outcome == "complete" ? AwkExitCode.Success : AwkExitCode.FatalError, await execution);
            Assert.Equal(outcome == "complete" ? "ready\n17\ncompleted\n" : "ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(outcome == "refuse" ? "awk: memory limit exceeded\n" : "", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            resume.TrySetResult();
            try { await execution; } catch (OperationCanceledException) { }
        }
    }

    [Theory]
    [InlineData("complete", false)] [InlineData("complete", true)]
    [InlineData("cancel", false)] [InlineData("cancel", true)]
    [InlineData("refuse", false)] [InlineData("refuse", true)]
    public async Task CompositeKeyAndCapturedTargetSurvivePendingGetline(string outcome, bool existingTarget)
    {
        var host = new MockFileSystem();
        host.SetStandardInput(existingTarget ? "3\n" : "after row\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = async (descriptor, maximum, token) =>
        {
            entered.TrySetResult();
            await resume.Task.WaitAsync(token).ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, token).ConfigureAwait(false);
        };
        var script = existingTarget
            ? "function rhs(){delete a;a[\"keep\"]=9;getline;return $1} " +
                "BEGIN{a[\"left\",\"right\"]=5;print \"ready\";a[\"left\",\"right\"]+=rhs();" +
                "print a[\"left\",\"right\"],a[\"keep\"],length(a);exit} END{print \"completed\"}"
            : "function readnext(){getline;return $1} " +
            "BEGIN{$0=\"before row\";a[\"keep\"]=9;print \"ready\";" +
            "a[$1,readnext()]=7;print a[\"before\",\"after\"],a[\"keep\"],length(a);exit} END{print \"completed\"}";
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", script));
        var budget = new CooperativeTestBudget(4 << 20);
        using var cancellation = new CancellationTokenSource();
        var execution = tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget),
            cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(execution.IsCompleted);
            Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.True(budget.Reserved > 0);
            if (outcome == "cancel")
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            }
            else
            {
                if (outcome == "refuse") budget.RefuseNextAdmissionMinimumGrowth = 1;
                resume.TrySetResult();
                Assert.Equal(outcome == "complete" ? AwkExitCode.Success : AwkExitCode.FatalError,
                    await execution);
            }
            Assert.Equal(outcome == "complete" ? (existingTarget ? "ready\n8 9 2\ncompleted\n" : "ready\n7 9 2\ncompleted\n") : "ready\n",
                host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(outcome == "refuse" ? "awk: memory limit exceeded\n" : "",
                host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            resume.TrySetResult();
            try { await execution; } catch (OperationCanceledException) { }
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InputCommandHandoffReleasesTheParentStdinBorrow(bool governed)
    {
        var released = false;
        var host = new TestOperationVisitor([]);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent("first\nsecond\n"u8.ToArray(),
            AwkReadCompletion.EndOfFile, _ => released = true));
        host.EnqueueAwkPipeDescriptors(new(new(80), new(81)));
        host.RunCommandOverride = (_, _) =>
        {
            Assert.True(released);
            return Task.FromResult(new AwkExitCode(17));
        };
        host.EnqueueReadBuffer(AwkReadBuffer.EndOfFile);
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk",
            "{\"producer\"|getline x;print $0;close(\"producer\");exit}"));
        var budget = new CooperativeTestBudget(2 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("first\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task WritingAStdinAliasEndsItsBorrowBeforeAppending(bool redirected, bool governed)
    {
        var borrowed = false;
        var completions = 0;
        var host = new MockFileSystem
        {
            ReadOverride = (_, _) =>
            {
                Assert.False(borrowed);
                borrowed = true;
                return AwkReadBuffer.FromContent("first\nsecond\n"u8.ToArray(), AwkReadCompletion.EndOfFile,
                    _ => { borrowed = false; completions++; });
            },
            AfterAppend = () => Assert.False(borrowed)
        };
        var invocation = new AwkCommandInvocation("awk",
            [redirected ? "{print > \"/dev/stdout\";exit}" : "{print;exit}"], [], AwkPath.Root,
            AwkFileDescriptor.StdIn, AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdErr);
        var tool = AwkTestHarness.ParseAwk(invocation);
        var budget = new CooperativeTestBudget(2 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("first\n", host.GetOutput(AwkFileDescriptor.StdIn));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, completions);
        Assert.False(borrowed);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SourcePrefixFollowedByReadFailureDoesNotExecute(bool governed)
    {
        var completed = false;
        var host = new MockFileSystem
        {
            ReadOverride = (_, _) => AwkReadBuffer.Failure(new(AwkOperationErrorKind.IoFailure, "source failed"),
                "BEGIN{print \"body\"}\n"u8.ToArray(), _ => completed = true)
        };
        host.AddFile("/source", "unused");
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "-f", "/source"));
        var budget = new CooperativeTestBudget(2 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: /source: source failed\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(completed);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(new byte[] { 0xff })]
    [InlineData(new byte[] { 0x61, 0x80 })]
    [InlineData(new byte[] { 0xf0, 0x9f })]
    public async Task RegexRecordInputRejectsInvalidOrIncompleteUtf8(byte[] bytes)
    {
        var host = new MockFileSystem();
        host.SetStandardInputBytes(bytes);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "BEGIN{RS=\"x|Q+\"}{print \"after\"}"));
        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: invalid UTF-8 input\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task OversizedHostReadFailsAndDisposesItsBorrow()
    {
        var consumed = -1;
        var host = new MockFileSystem
        {
            ReadOverride = (_, maxBytes) => AwkReadBuffer.FromContent(new byte[maxBytes + 1],
                AwkReadCompletion.EndOfFile, count => consumed = count)
        };
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "{print \"after\"}"));
        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: file descriptor 0: read failed: Read exceeded the requested byte count.\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, consumed);
    }

    [Theory]
    [InlineData("source", 1)] [InlineData("source", 2)] [InlineData("source", 3)] [InlineData("source", 7)]
    [InlineData("newline", 1)] [InlineData("newline", 2)] [InlineData("newline", 3)] [InlineData("newline", 7)]
    [InlineData("regex", 1)] [InlineData("regex", 2)] [InlineData("regex", 3)] [InlineData("regex", 7)]
    public async Task ByteChunksPreserveScalarBoundariesAndBorrowOrder(string kind, int chunkSize)
    {
        foreach (var governed in new[] { false, true })
        {
            var text = kind == "source" ? "BEGIN{print \"α😀\"}" : kind == "regex" ? "αx😀QQlast" : "α\n😀\nlast";
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            var position = 0;
            var borrowed = false;
            var completions = 0;
            var reads = 0;
            var host = new MockFileSystem
            {
                ReadOverride = (_, maxBytes) =>
                {
                    Assert.False(borrowed);
                    if (position == bytes.Length) return AwkReadBuffer.EndOfFile;
                    var length = Math.Min(Math.Min(chunkSize, maxBytes), bytes.Length - position);
                    borrowed = true;
                    reads++;
                    return AwkReadBuffer.FromContent(bytes.AsMemory(position, length),
                        position + length == bytes.Length ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData,
                        consumed => { position += consumed; borrowed = false; completions++; });
                },
                AfterCloseDescriptor = () => Assert.False(borrowed)
            };
            host.AddFile("/source", "unused");
            string[] arguments = kind == "source" ? ["-f", "/source"] :
                [kind == "regex" ? "BEGIN{RS=\"x|Q+\"}{print}" : "{print}"];
            var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", arguments));
            var budget = new CooperativeTestBudget(2 << 20);
            var policy = AwkTestHarness.CreatePolicy();
            if (governed) policy = policy.WithMemoryBudget(budget);
            Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, policy, CancellationToken.None));
            Assert.Equal(kind == "source" ? "α😀\n" : "α\n😀\nlast\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(bytes.Length, position);
            Assert.Equal(reads, completions);
            Assert.False(borrowed);
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Fact]
    public async Task Awk_MainInputReadFailureIsFatalAfterCompletedRecords()
    {
        var readCount = 0;
        var host = new MockFileSystem
        {
            ReadOverride = (_, _) => readCount++ == 0
                ? AwkReadBuffer.FromContent("one\n"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { })
                : AwkReadBuffer.Failure(new AwkOperationError(AwkOperationErrorKind.IoFailure, "Device read failed"))
        };
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "{print}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("one\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: file descriptor 0: read failed: Device read failed\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_GetlineReadFailureReturnsMinusOneAndSetsErrno()
    {
        var host = new MockFileSystem
        {
            ReadOverride = (_, _) =>
                AwkReadBuffer.Failure(new AwkOperationError(AwkOperationErrorKind.IoFailure, "Device read failed"))
        };
        host.AddFile("/data.txt", "unread");
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{result=(getline value < \"/data.txt\"); print result, ERRNO; exit}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("-1 Device read failed\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            string.Empty,
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_ScriptReadFailureUsesTheSourcePathDiagnostic()
    {
        var host = new MockFileSystem
        {
            ReadOverride = (_, _) =>
                AwkReadBuffer.Failure(new AwkOperationError(AwkOperationErrorKind.IoFailure, "Device read failed"))
        };
        host.AddFile("/script.awk", "BEGIN{print \"unread\"}");
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "-f", "/script.awk"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: /script.awk: Device read failed\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_ScriptReadCancellationStillClosesTheSourceDescriptor()
    {
        using var cancellation = new CancellationTokenSource();
        var host = new MockFileSystem
        {
            AfterRead = cancellation.Cancel,
            ReadOverride = (_, _) =>
                AwkReadBuffer.FromContent("BEGIN{print 1}"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { })
        };
        host.AddFile("/script.awk", "unread");
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "-f", "/script.awk"));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), cancellation.Token));

        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public void AwkReadBuffer_SeparatesEndOfFileFromFailure()
    {
        var endOfFile = AwkReadBuffer.EndOfFile;
        var failure = AwkReadBuffer.Failure(new AwkOperationError(AwkOperationErrorKind.IoFailure, "Device read failed"));

        Assert.True(endOfFile.Success);
        Assert.True(endOfFile.ReachedEof);
        Assert.False(endOfFile.Failed);
        Assert.True(failure.Failed);
        Assert.False(failure.ReachedEof);
        Assert.Equal("Device read failed", failure.Error.Message);
    }

    [Fact]
    public void AwkReadBuffer_CommitsOneAbsoluteMonotonicPrefixOnDisposal()
    {
        var committed = -1;
        var completions = 0;
        var borrow = AwkReadBuffer.FromContent("abc"u8.ToArray(), AwkReadCompletion.MoreData,
            count => { committed = count; completions++; });
        Assert.Throws<ArgumentOutOfRangeException>(() => borrow.SetConsumedBytes(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => borrow.SetConsumedBytes(4));
        borrow.SetConsumedBytes(1);
        borrow.SetConsumedBytes(1);
        borrow.SetConsumedBytes(2);
        Assert.Throws<ArgumentOutOfRangeException>(() => borrow.SetConsumedBytes(1));
        Assert.Equal(-1, committed);
        Assert.Equal("abc", System.Text.Encoding.UTF8.GetString(borrow.Content.Span));
        borrow.Dispose();
        borrow.Dispose();
        Assert.Equal(2, committed);
        Assert.Equal(1, completions);
        Assert.Throws<ObjectDisposedException>(() => borrow.Content);
        Assert.Throws<ObjectDisposedException>(() => borrow.SetConsumedBytes(2));
    }

    [Fact]
    public void AwkReadBuffer_RejectsSuccessfulReadsThatCannotMakeProgress()
    {
        Assert.Throws<ArgumentException>(() =>
            AwkReadBuffer.FromContent(ReadOnlyMemory<byte>.Empty, AwkReadCompletion.MoreData, static _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AwkReadBuffer.FromContent("one\n"u8.ToArray(), (AwkReadCompletion)42, static _ => { }));
    }

    [Fact]
    public void AwkReadBuffer_TerminalFailureCanCarryAUsablePrefix()
    {
        var committed = -1;
        using (var result = AwkReadBuffer.Failure(new(AwkOperationErrorKind.PermissionDenied, "read denied"),
            "one\ntail"u8.ToArray(), count => committed = count))
        {
            Assert.True(result.Failed);
            Assert.False(result.ReachedEof);
            Assert.Equal(AwkOperationErrorKind.PermissionDenied, result.Error.Kind);
            Assert.Equal("one\ntail", System.Text.Encoding.UTF8.GetString(result.Content.Span));
            result.SetConsumedBytes(4);
        }
        Assert.Equal(4, committed);
    }

    [Theory]
    [InlineData("newline", false)] [InlineData("newline", true)]
    [InlineData("literal", false)] [InlineData("literal", true)]
    [InlineData("paragraph", false)] [InlineData("paragraph", true)]
    [InlineData("csv", false)] [InlineData("csv", true)]
    public async Task EarlyExitLeavesUnconsumedHostSuffix(string separator, bool governed)
    {
        var host = new MockFileSystem();
        var input = separator switch
        {
            "literal" => "first|second|",
            "paragraph" => "first\n\nsecond\n\n",
            "csv" => "\"a\nb\",c\nlast,d\n",
            _ => "first\nsecond\n"
        };
        host.SetStandardInput(input);
        var script = separator switch
        {
            "literal" => "BEGIN{RS=\"|\"}{print;exit}",
            "paragraph" => "BEGIN{RS=\"\"}{print;exit}",
            "csv" => "{print $1;exit}",
            _ => "{print;exit}"
        };
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk",
            separator == "csv" ? ["--csv", script] : [script]));
        var budget = new CooperativeTestBudget(2 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal(separator == "csv" ? "a\nb\n" : "first\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        using var remaining = await host.ReadAsync(AwkFileDescriptor.StdIn, 100, CancellationToken.None);
        var expected = separator switch
        {
            "literal" => "second|",
            "paragraph" => "second\n\n",
            "csv" => "last,d\n",
            _ => "second\n"
        };
        Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(remaining.Content.Span));
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TerminalDataDeliversCompleteRecordsBeforeFailure(bool getline)
    {
        var reads = 0;
        var host = new MockFileSystem
        {
            ReadOverride = (_, _) =>
            {
                reads++;
                return AwkReadBuffer.Failure(new(AwkOperationErrorKind.IoFailure, "device failed"),
                    "one\ntwo\npartial"u8.ToArray(), static _ => { });
            }
        };
        var script = getline ? "BEGIN{for(i=0;i<3;i++){s=(getline x);print s,x,ERRNO}}" : "{print}END{print \"end\"}";
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", script));
        Assert.Equal(getline ? AwkExitCode.Success : AwkExitCode.FatalError,
            await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None));
        Assert.Equal(getline ? "1 one \n1 two \n-1 two device failed\n" : "one\ntwo\n",
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(getline ? "" : "awk: file descriptor 0: read failed: device failed\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, reads);
    }
}
