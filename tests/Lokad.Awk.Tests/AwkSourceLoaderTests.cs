using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkSourceLoaderTests
{
    [Theory]
    [InlineData("inline")]
    [InlineData("qualified")]
    [InlineData("bare")]
    [InlineData("include-qualified")]
    [InlineData("include-bare")]
    public async Task UnusedSearchComponentsDoNotConsumeSourceAdmission(string sourceKind)
    {
        const string program = "BEGIN{print length(ENVIRON[\"AWKPATH\"]);exit}";
        var search = ":" + string.Concat(Enumerable.Repeat("x:", 1 << 18))[..^1];
        string[] arguments = sourceKind switch
        {
            "inline" => [program],
            "qualified" => ["-f", "/script.awk"],
            "bare" => ["-f", "script.awk"],
            "include-qualified" => ["@include \"/script.awk\""],
            "include-bare" => ["@include \"script.awk\""],
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind))
        };
        var host = new MockFileSystem();
        host.AddFile("/script.awk", program);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors(
            "awk", arguments, [new("AWKPATH", search)]);
        Assert.True(Awk.TryParse(command, out var awk));
        var budget = new CooperativeTestBudget(4 << 20);

        var exit = await awk.ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("524288\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(sourceKind == "inline" ? 0 : 1, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SearchCandidateAdmissionStopsBeforeHostAccess(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(32 << 20);
        if (cancel)
            budget.OnGrowth = growth => { if (growth >= 4 << 20) cancellation.Cancel(); };
        else
            budget.RefuseNextAdmissionMinimumGrowth = 4 << 20;
        var host = new MockFileSystem();
        var command = AwkCommandInvocation.CreateWithStandardDescriptors(
            "awk", ["-f", "script.awk"], [new("AWKPATH", new string('x', 1 << 20))]);
        Assert.True(Awk.TryParse(command, out var awk));
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget);

        if (cancel)
        {
            var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                awk.ExecuteAsync(host, policy, cancellation.Token));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
        }
        else
        {
            Assert.Equal(AwkExitCode.FatalError, await awk.ExecuteAsync(host, policy, cancellation.Token));
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        }
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(cancel ? "" : "awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> SourceCoordinateCases()
    {
        foreach (var source in new[]
        {
            "@include \"/empty.awk\"; BEGIN{break}",
            "BEGIN{}; @include \"/empty.awk\"; BEGIN{break}",
            "@include \"/empty.awk\"\nBEGIN{break}",
            "@include \\\n\"/empty.awk\"; BEGIN{break}",
            "# first\r\n@include \"/empty.awk\"; BEGIN{break}",
            "@include \"/empty.awk\"; \\\n BEGIN{break}",
            "# first\n@include \"/empty.awk\"; BEGIN{print \"\\u\";exit}",
            "BEGIN{x=1}; \\\n@include \"/empty.awk\"; BEGIN{break}"
        })
        foreach (var depth in new[] { 0, 1, 2 })
        foreach (var governed in new[] { false, true })
            yield return [source, depth, governed];
    }

    [Theory]
    [MemberData(nameof(SourceCoordinateCases))]
    public async Task EmptyIncludesKeepOriginalTailCoordinates(string source, int depth, bool governed)
    {
        var host = new MockFileSystem();
        host.AddFile("/empty.awk", "");
        host.AddFile("/main.awk", source);
        var path = "/main.awk";
        for (var index = 0; index < depth; index++)
        {
            var wrapper = $"/wrapper{index}.awk";
            host.AddFile(wrapper, $"@include \"{path}\"\n");
            path = wrapper;
        }
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "-f", path));
        var warning = source.Contains("\\u", StringComparison.Ordinal);
        var position = source.IndexOf(warning ? "\"\\u\"" : "break", StringComparison.Ordinal);
        var line = source.AsSpan(0, position).Count('\n') + 1;
        var column = position - source.LastIndexOf('\n', position);

        var budget = new CooperativeTestBudget(16 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var exit = await awk.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(warning ? AwkExitCode.Success : AwkExitCode.FatalError, exit);
        Assert.Equal(warning ? "u\n" : "", host.GetOutput(AwkFileDescriptor.StdOut));
        var message = warning ? "warning: no hex digits in `\\u' escape sequence" : "break used outside loop or switch";
        Assert.Equal($"awk: /main.awk:{line}:{column}: {message}\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task CanceledSourceLoadingAwaitsTheActualCloseBeforeRefund()
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(1 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingClose = new TaskCompletionSource<AwkDescriptorCloseResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var closeCount = 0;
        var host = new TestOperationVisitor([])
        {
            CloseDescriptorOverride = (_, token) =>
            {
                Assert.False(token.CanBeCanceled);
                closeCount++;
                entered.TrySetResult();
                return pendingClose.Task;
            }
        };
        host.EnqueueOpenAwkFileDescriptor(100);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("BEGIN{print 1}\n"u8.ToArray(), [], AwkReadCompletion.EndOfFile));
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "-f", "/script.awk"));
        var execution = tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved > 0);
            Assert.Equal(0, budget.ScopesDisposed);
        }
        finally { pendingClose.TrySetResult(AwkDescriptorCloseResult.Closed()); }
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(1, closeCount);
        Assert.Equal(0, budget.Reserved);
        Assert.Empty(host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetAppendedOutput(AwkFileDescriptor.StdErr));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SourceRoleConflictUsesResolvedSearchIdentityBeforeEffects(bool includeFirst)
    {
        var host = new MockFileSystem();
        host.AddFile("/lib/same.awk", "BEGIN{print \"LEAK\"; print 1 > \"/effect\"; \"cmd\" | getline x}");
        string[] arguments = includeFirst
            ? ["-i", "same.awk", "-f", "/lib/./same.awk"]
            : ["-f", "same.awk", "-i", "/lib/./same.awk"];
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", arguments,
            [new AwkEnvironmentVariable("AWKPATH", "/lib")]));
        var budget = new CooperativeTestBudget(16 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget)
            .WithAccessiblePathRoot(new AwkPath("/lib"));

        var exit = await awk.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: /lib/./same.awk: cannot use a file as both program source and include\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.False(host.FileExists("/effect"));
        Assert.Empty(host.AwkCommandInvocations);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task AwkSourceLoader_ReusesOrdinaryNormalizedSourceText()
    {
        var fileSystem = new MockFileSystem();
        var program = "BEGIN{print 1}";
        var invocation = Parse(program);

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Same(program, source.Text);
        var segment = Assert.Single(source.Segments);
        Assert.Equal(new AwkTextSpan(0, program.Length), segment.Span);
    }

    [Fact]
    public async Task AwkSourceLoader_PreservesIncludeSourceSegments()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/common.awk", "function helper(){ return 1 }\n");
        var invocation = Parse("@include \"/lib/common.awk\"\nBEGIN{print helper()}");

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal(new AwkSourceName("<inline:1>"), source.SourceName);
        Assert.Equal("function helper(){ return 1 }\nBEGIN{print helper()}", source.Text);
        Assert.Equal(
            [AwkSourceSegmentKind.Include, AwkSourceSegmentKind.Inline],
            source.Segments.Select(segment => segment.Kind).ToArray());
        Assert.Equal(new AwkSourceName("/lib/common.awk"), source.Segments[0].SourceName);
        Assert.Equal(new AwkSourceName("<inline:1>"), source.Segments[1].SourceName);
        Assert.Equal(new AwkLineOffset(1), source.Segments[1].SourceLineOffset);
        Assert.Equal(new AwkTextSpan(0, "function helper(){ return 1 }\n".Length), source.Segments[0].Span);
        Assert.Equal(
            new AwkTextSpan("function helper(){ return 1 }\n".Length, "BEGIN{print helper()}".Length),
            source.Segments[1].Span);
    }

    [Fact]
    public async Task AwkSourceLoader_RecordsAwkPathSuffixLookupAndDuplicateIncludes()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/common.awk", "BEGIN{marker=1}");
        var invocation = Parse(
            "@include \"common\"\n@include \"common\"\nBEGIN{print marker}",
            [new AwkEnvironmentVariable("AWKPATH", "/lib")]);

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("BEGIN{marker=1}\nBEGIN{print marker}", source.Text);
        Assert.Equal(
            [AwkSourceSegmentKind.Include, AwkSourceSegmentKind.Include, AwkSourceSegmentKind.Inline],
            source.Segments.Select(segment => segment.Kind).ToArray());
        Assert.Equal(new AwkSourceName("/lib/common.awk"), source.Segments[0].SourceName);
        Assert.Equal(new AwkSourceName("<inline:1>"), source.Segments[^1].SourceName);
        Assert.Equal(2, fileSystem.ReadOpenCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AwkSourceLoader_PreservesContinuationCoordinatesAcrossRepeatedEmptyIncludes(bool nested)
    {
        const int includeCount = 256;
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/empty.awk", string.Empty);
        var program = new System.Text.StringBuilder();
        for (var index = 0; index < includeCount; index++)
        {
            program.Append("value += 1 \\\n+ 0\n");
            program.Append("@include \"/empty.awk\"\n");
        }

        fileSystem.AddFile("/many.awk", program.ToString());
        var invocation = Parse(nested ? "@include \"/many.awk\"" : program.ToString());

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal(includeCount, source.Segments.Sum(segment => segment.LineContinuations.Count));
        var position = source.Text.LastIndexOf("value", StringComparison.Ordinal);
        var mapped = Assert.Single(AwkSourceDiagnosticMap.MapDiagnostics(source,
            [new AwkDiagnostic(new AwkTextSpan(position, 1), "test")]));
        Assert.Equal((includeCount - 1) * 3 + 1, mapped.SourceLocation?.LineNumber.Value);
        Assert.Equal(1, mapped.SourceLocation?.ColumnNumber.Value);
        Assert.Equal(nested ? 2 : 1, fileSystem.ReadOpenCount);
        Assert.DoesNotContain("@include", source.Text);
    }

    [Fact]
    public async Task AwkSourceLoader_InvalidUtf8AwkPathIncludeReportsResolvedPath()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/bad.awk", [0x42, 0x45, 0x47, 0x49, 0x4e, 0x80]);
        var invocation = Parse(
            "@include \"bad.awk\"\nBEGIN{print marker}",
            [new AwkEnvironmentVariable("AWKPATH", "/lib")]);

        var result = await LoadAsync(fileSystem, invocation);

        var failure = Assert.IsType<AwkSourceLoadResult.Failed>(result);
        Assert.Equal(AwkDiagnosticId.SourceLoadFailure, failure.Id);
        Assert.Equal(
            "awk: /lib/bad.awk: invalid UTF-8 source\n",
            AwkUtf8Codec.Decode(failure.ErrorMessage));
    }

    [Fact]
    public async Task AwkSourceLoader_ExpandsIncludeDirectiveWithTrailingComment()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/common.awk", "BEGIN{marker=\"included\"}");
        var invocation = Parse("@include \"/lib/common.awk\" # shared directive parser\nBEGIN{print marker}");

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("BEGIN{marker=\"included\"}\nBEGIN{print marker}", source.Text);
    }

    [Theory]
    [InlineData("@ include \"/lib/common.awk\"\nBEGIN{print 1}")]
    [InlineData("@include # \"/lib/common.awk\"\nBEGIN{print 1}")]
    [InlineData("BEGIN{print \"@include \\\"/lib/common.awk\\\"\"}")]
    public async Task AwkSourceLoader_DoesNotExpandNonDirectiveShapes(string program)
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/common.awk", "BEGIN{marker=\"included\"}");
        var invocation = Parse(program);

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal(program, source.Text);
        Assert.DoesNotContain("marker=\"included\"", source.Text);
    }

    [Fact]
    public async Task AwkSourceLoader_CommandLineIncludesPrecedeFilesAndDeduplicateNestedIncludes()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/common.awk", "BEGIN{marker=\"include\"}\n");
        fileSystem.AddFile("/main.awk", "@include \"/lib/common.awk\"\nBEGIN{print marker}\n");
        var invocation = AwkCommandParser.Parse(
            AwkTestHarness.BuildInvocation(
                "awk",
                ["--include", "/lib/common.awk", "--file", "/main.awk"]));

        var result = await LoadAsync(fileSystem, invocation);

        var sources = LoadedSources(result);
        Assert.Equal(2, sources.Count);
        Assert.Equal(new AwkSourceName("/lib/common.awk"), sources[0].SourceName);
        Assert.Equal(new AwkSourceName("/main.awk"), sources[1].SourceName);
        Assert.Contains("marker=\"include\"", sources[0].Text);
        Assert.DoesNotContain("marker=\"include\"", sources[1].Text);
        Assert.Contains("BEGIN{print marker}", sources[1].Text);
    }

    [Fact]
    public async Task AwkSourceLoader_ResolvesRelativeIncludesAgainstCurrentDirectory()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/work/lib/common.awk", "BEGIN{marker=\"relative\"}");
        var invocation = Parse(
            "@include \"lib/common.awk\"\nBEGIN{print marker}",
            [new AwkEnvironmentVariable("PWD", "/work")]);

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("BEGIN{marker=\"relative\"}\nBEGIN{print marker}", source.Text);
        Assert.Equal(new AwkSourceName("lib/common.awk"), source.Segments[0].SourceName);
        Assert.Equal(new AwkSourceName("<inline:1>"), source.Segments[^1].SourceName);
    }

    [Fact]
    public async Task AwkSourceLoader_NormalizesLineContinuationsInsideIncludedFiles()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/common.awk", "BEGIN{marker=\"in\" \\\n\"clude\"}");
        var invocation = Parse("@include \"/lib/common.awk\"\nBEGIN{print marker}");

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("BEGIN{marker=\"in\" \"clude\"}\nBEGIN{print marker}", source.Text);
        Assert.Equal(
            [AwkSourceSegmentKind.Include, AwkSourceSegmentKind.Include, AwkSourceSegmentKind.Inline],
            source.Segments.Select(segment => segment.Kind).ToArray());
    }

    [Fact]
    public async Task AwkSourceLoader_NormalizesLineContinuationsBeforeRecordingSpans()
    {
        var fileSystem = new MockFileSystem();
        var invocation = Parse("BEGIN{print 1 \\\n+ 2}");

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("BEGIN{print 1 + 2}", source.Text);
        var segment = Assert.Single(source.Segments);
        Assert.Equal(AwkSourceSegmentKind.Inline, segment.Kind);
        Assert.Equal(new AwkTextSpan(0, source.Text.Length), segment.Span);
        Assert.Equal(1, segment.LineContinuations.Count);
    }

    [Fact]
    public async Task AwkSourceLoader_BatchesNonDirectiveLinesAroundMarkerText()
    {
        var fileSystem = new MockFileSystem();
        var invocation = Parse("BEGIN{value=1 \\\n+ 2}\n{print \"@marker\", value \\\n+ 3}");

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("BEGIN{value=1 + 2}\n{print \"@marker\", value + 3}", source.Text);
        var segment = Assert.Single(source.Segments);
        Assert.Equal(2, segment.LineContinuations.Count);
    }

    [Theory]
    [InlineData("BEGIN{print \"@load \\\"plugin\\\"\"}")]
    [InlineData("BEGIN{print \"@namespace ns\"}")]
    [InlineData("BEGIN{print \"x::y\", \"@/regex/\", \"_\\\"text\\\"\", \"@fn(1)\", \"a][b\"}")]
    [InlineData("BEGIN{if ($0 ~ /x::y|@\\/regex|_\"text\"|@fn\\(1\\)|a][b/) print}")]
    [InlineData("BEGIN{if ($0 ~ /[@]load|[@]namespace|[:][:]|[@][/]|[_][\"]|[@]fn[(]|[]][[]/) print}")]
    [InlineData("BEGIN{print a[\"@load\"], a[\"@namespace\"], a[\"x::y\"], a[\"@/regex/\"], a[\"_\\\"text\\\"\"], a[\"@fn(1)\"], a[\"][]\"]}")]
    [InlineData("# @load \"plugin\"\n# @namespace ns\nBEGIN{print \"ok\"}")]
    public async Task AwkSourceLoader_IgnoresUnsupportedMarkersInsideNonCode(string program)
    {
        var fileSystem = new MockFileSystem();
        var invocation = Parse(program);

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal(program, source.Text);
        var binding = AwkBinder.Bind(AwkParser.Parse(source.Text));
        Assert.DoesNotContain(
            binding.Diagnostics,
            diagnostic => diagnostic.Id == AwkDiagnosticId.UnsupportedPolicy);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Theory]
    [InlineData("@load \"plugin\"", "awk: dynamic extension loading is unsupported in Lokad awk\n")]
    [InlineData("@namespace ns", "awk: namespaces are unsupported in Lokad awk\n")]
    [InlineData("BEGIN{print ns::x}", "awk: namespaces are unsupported in Lokad awk\n")]
    [InlineData("BEGIN{print @/x/}", "awk: strongly typed regexp constants are unsupported in Lokad awk\n")]
    [InlineData("BEGIN{print _\"text\"}", "awk: internationalization functions are unsupported in Lokad awk\n")]
    [InlineData("BEGIN{print @fn(1)}", "awk: indirect function calls are unsupported in Lokad awk\n")]
    [InlineData("BEGIN{print a[1][2]}", "awk: arrays of arrays are unsupported in Lokad awk\n")]
    [InlineData("BEGIN{print a[1] [2]}", "awk: arrays of arrays are unsupported in Lokad awk\n")]
    [InlineData("BEGIN{print ns::\\\nvalue}", "awk: namespaces are unsupported in Lokad awk\n")]
    public async Task AwkParser_RejectsUnsupportedSyntaxFromTokens(
        string program,
        string expectedError)
    {
        var fileSystem = new MockFileSystem();
        var invocation = Parse(program);

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        var binding = AwkBinder.Bind(AwkParser.Parse(source.Text));
        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.Equal(AwkDiagnosticId.UnsupportedPolicy, diagnostic.Id);
        Assert.Equal(expectedError, AwkUtf8Codec.Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkSourceLoader_ReturnsStructuredFailureWithoutWritingStderr()
    {
        var fileSystem = new MockFileSystem();
        var invocation = Parse("@include \"/missing.awk\"");

        var result = await LoadAsync(fileSystem, invocation);

        var failure = Assert.IsType<AwkSourceLoadResult.Failed>(result);
        Assert.Equal(AwkDiagnosticId.SourceLoadFailure, failure.Id);
        Assert.Equal(
            "awk: /missing.awk: No such file or directory\n",
            AwkUtf8Codec.Decode(failure.ErrorMessage));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkSourceLoader_PreservesHostOpenFailure()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddReadOpenError("/script.awk", "Permission denied");
        var invocation = AwkCommandParser.Parse(
            AwkTestHarness.BuildInvocation("awk", ["--file", "/script.awk"]));

        var result = await LoadAsync(fileSystem, invocation);

        var failure = Assert.IsType<AwkSourceLoadResult.Failed>(result);
        Assert.Equal(AwkDiagnosticId.SourceLoadFailure, failure.Id);
        Assert.Equal(
            "awk: /script.awk: Permission denied\n",
            AwkUtf8Codec.Decode(failure.ErrorMessage));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkSourceLoader_DoesNotCreateInternationalizedStringsAcrossLineContinuations()
    {
        var fileSystem = new MockFileSystem();
        var invocation = Parse("BEGIN{print _\\\n\"text\"}");

        var result = await LoadAsync(fileSystem, invocation);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("BEGIN{print _ \"text\"}", source.Text);
        var binding = AwkBinder.Bind(AwkParser.Parse(source.Text));
        Assert.Empty(binding.Diagnostics);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutor_ReportsScriptFileDiagnosticSourceLocation()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/script.awk", "BEGIN{break}");

        var result = await ExecuteAsync(fileSystem, ["-f", "/script.awk"]);

        Assert.Equal(AwkDiagnostics.FatalExitCode.Value, result.ExitCode);
        Assert.Equal(string.Empty, result.StdOut);
        Assert.Equal("awk: /script.awk:1:7: break used outside loop or switch\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkDiagnostic(result);
    }

    [Fact]
    public async Task AwkExecutor_InvalidUtf8ScriptFileFailsDuringSourceLoad()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/script.awk", [0x42, 0x45, 0x47, 0x49, 0x4e, 0x80]);

        var result = await ExecuteAsync(fileSystem, ["-f", "/script.awk"]);

        Assert.Equal(AwkDiagnostics.FatalExitCode.Value, result.ExitCode);
        Assert.Equal(string.Empty, result.StdOut);
        Assert.Equal("awk: /script.awk: invalid UTF-8 source\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkDiagnostic(result);
    }

    [Fact]
    public async Task AwkExecutor_InvalidUtf8IncludedFileFailsBeforeInlineSourceExecutes()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/bad.awk", [0x42, 0x45, 0x47, 0x49, 0x4e, 0x80]);

        var result = await ExecuteAsync(
            fileSystem,
            ["@include \"/lib/bad.awk\"\nBEGIN{print \"unreachable\"}"]);

        Assert.Equal(AwkDiagnostics.FatalExitCode.Value, result.ExitCode);
        Assert.Equal(string.Empty, result.StdOut);
        Assert.Equal("awk: /lib/bad.awk: invalid UTF-8 source\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkDiagnostic(result);
    }

    [Fact]
    public async Task AwkExecutor_InvalidUtf8AwkPathIncludeFailsDuringSourceLoad()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/bad.awk", [0x42, 0x45, 0x47, 0x49, 0x4e, 0x80]);
        var command = AwkTestHarness.BuildInvocation(
            "awk",
            ["--include", "bad.awk", "BEGIN{print \"unreachable\"}"],
            [new AwkEnvironmentVariable("AWKPATH", "/lib")]);
        var invocation = AwkCommandParser.Parse(command);
        Assert.Contains(
            invocation.RuntimeArgumentsPlan.Environment,
            variable => variable.Name == "AWKPATH" && variable.Value == "/lib");
        var sourceLoad = await LoadAsync(fileSystem, invocation);
        var failure = Assert.IsType<AwkSourceLoadResult.Failed>(sourceLoad);
        Assert.Equal(
            "awk: /lib/bad.awk: invalid UTF-8 source\n",
            AwkUtf8Codec.Decode(failure.ErrorMessage));

        fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/bad.awk", [0x42, 0x45, 0x47, 0x49, 0x4e, 0x80]);
        var parsedTool = AwkTestHarness.ParseAwk(command);
        var exitCode = await parsedTool.ExecuteAsync(fileSystem, CancellationToken.None);
        var result = new AwkTestRunResult(
            exitCode.Value,
            fileSystem.GetOutput(AwkFileDescriptor.StdOut),
            fileSystem.GetOutput(AwkFileDescriptor.StdErr),
            new Dictionary<string, string?>());

        Assert.Equal(AwkDiagnostics.FatalExitCode.Value, result.ExitCode);
        Assert.Equal(string.Empty, result.StdOut);
        Assert.Equal("awk: /lib/bad.awk: invalid UTF-8 source\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkDiagnostic(result);
    }

    [Fact]
    public async Task AwkExecutor_ReportsScriptFileDiagnosticSourceLocationAfterLineContinuation()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/script.awk", "BEGIN{print 1 \\\n+ 2}\nBEGIN{break}");

        var result = await ExecuteAsync(fileSystem, ["-f", "/script.awk"]);

        Assert.Equal(AwkDiagnostics.FatalExitCode.Value, result.ExitCode);
        Assert.Equal(string.Empty, result.StdOut);
        Assert.Equal("awk: /script.awk:3:7: break used outside loop or switch\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkDiagnostic(result);
    }

    [Fact]
    public async Task AwkExecutor_ReportsComparisonLocationAfterTokenSeparatingContinuation()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/script.awk", "BEGIN{print\\\n2;print (1<2<3)}");

        var result = await ExecuteAsync(fileSystem, ["-f", "/script.awk"]);

        Assert.Equal(AwkDiagnostics.FatalExitCode.Value, result.ExitCode);
        Assert.Equal(string.Empty, result.StdOut);
        Assert.Equal("awk: /script.awk:2:13: comparison operators are non-associative.\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkDiagnostic(result);
    }

    [Fact]
    public async Task AwkExecutor_ReportsLocationAfterBackslashTerminatedComment()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/script.awk", "BEGIN{#comment\\\nprint (1<2<3)}");

        var result = await ExecuteAsync(fileSystem, ["-f", "/script.awk"]);

        Assert.Equal(AwkDiagnostics.FatalExitCode.Value, result.ExitCode);
        Assert.Equal(string.Empty, result.StdOut);
        Assert.Equal("awk: /script.awk:2:11: comparison operators are non-associative.\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkDiagnostic(result);
    }

    [Fact]
    public async Task AwkExecutor_ReportsIncludedFileDiagnosticSourceLocation()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/bad.awk", "BEGIN{break}");

        var result = await ExecuteAsync(
            fileSystem,
            ["@include \"/lib/bad.awk\"\nBEGIN{print \"unreachable\"}"]);

        Assert.Equal(AwkDiagnostics.FatalExitCode.Value, result.ExitCode);
        Assert.Equal(string.Empty, result.StdOut);
        Assert.Equal("awk: /lib/bad.awk:1:7: break used outside loop or switch\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkDiagnostic(result);
    }

    [Fact]
    public async Task AwkExecutor_ReportsInlineSegmentDiagnosticAfterInclude()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/good.awk", "function helper(){ return 1 }");

        var result = await ExecuteAsync(
            fileSystem,
            ["@include \"/lib/good.awk\"\nBEGIN{break}"]);

        Assert.Equal(AwkDiagnostics.FatalExitCode.Value, result.ExitCode);
        Assert.Equal(string.Empty, result.StdOut);
        Assert.Equal("awk: <inline:1>:2:7: break used outside loop or switch\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkDiagnostic(result);
    }

    [Fact]
    public async Task AwkExecutor_ReportsExplicitInlineSourceUnitDiagnosticLocation()
    {
        var fileSystem = new MockFileSystem();

        var result = await ExecuteAsync(
            fileSystem,
            ["--source", "BEGIN{ok=1}", "--source", "BEGIN{break}"]);

        Assert.Equal(AwkDiagnostics.FatalExitCode.Value, result.ExitCode);
        Assert.Equal(string.Empty, result.StdOut);
        Assert.Equal("awk: <inline:2>:1:7: break used outside loop or switch\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkDiagnostic(result);
    }

    private static AwkInvocation Parse(string program) =>
        Parse(program, []);

    private static AwkInvocation Parse(
        string program,
        IReadOnlyList<AwkEnvironmentVariable> environment)
    {
        return AwkCommandParser.Parse(
            AwkTestHarness.BuildInvocation(
                "awk",
                [program],
                environment));
    }

    private static IReadOnlyList<AwkSourceUnit> LoadedSources(AwkSourceLoadResult result) =>
        result is AwkSourceLoadResult.Loaded loaded
            ? loaded.Sources
            : throw new InvalidOperationException("Expected loaded awk sources.");

    private static Task<AwkSourceLoadResult> LoadAsync(
        MockFileSystem fileSystem,
        AwkInvocation invocation)
    {
        var runtimeOptions = AwkRuntimeOptions.CreateSystemDefault();
        return AwkSourceLoader.LoadAsync(
            fileSystem,
            invocation,
            new AwkRuntimeResourceMonitor(runtimeOptions.Limits, runtimeOptions.Counters),
            CancellationToken.None);
    }

    private static async Task<AwkTestRunResult> ExecuteAsync(
        MockFileSystem fileSystem,
        IReadOnlyList<string> arguments)
    {
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", arguments, []));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None).ConfigureAwait(false);
        return new AwkTestRunResult(
            exitCode.Value,
            fileSystem.GetOutput(AwkFileDescriptor.StdOut),
            fileSystem.GetOutput(AwkFileDescriptor.StdErr),
            new Dictionary<string, string?>());
    }
}
