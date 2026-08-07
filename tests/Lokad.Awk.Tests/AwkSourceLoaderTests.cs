using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkSourceLoaderTests
{
    [Fact]
    public async Task AwkSourceLoader_PreservesIncludeSourceSegments()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/common.awk", "function helper(){ return 1 }\n");
        var invocation = Parse("@include \"/lib/common.awk\"\nBEGIN{print helper()}");

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("<inline:1>", source.Name);
        Assert.Equal("function helper(){ return 1 }\nBEGIN{print helper()}", source.Text);
        Assert.Equal(
            [AwkSourceSegmentKind.Include, AwkSourceSegmentKind.Inline],
            source.Segments.Select(segment => segment.Kind).ToArray());
        Assert.Equal("/lib/common.awk", source.Segments[0].Name);
        Assert.Equal("<inline:1>", source.Segments[1].Name);
        Assert.Equal(1, source.Segments[1].LineOffset);
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

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("BEGIN{marker=1}\nBEGIN{print marker}", source.Text);
        Assert.Equal(
            [AwkSourceSegmentKind.Include, AwkSourceSegmentKind.Inline],
            source.Segments.Select(segment => segment.Kind).ToArray());
        Assert.Equal("/lib/common.awk", source.Segments[0].Name);
        Assert.Equal("<inline:1>", source.Segments[1].Name);
    }

    [Fact]
    public async Task AwkSourceLoader_InvalidUtf8AwkPathIncludeReportsResolvedPath()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/bad.awk", [0x42, 0x45, 0x47, 0x49, 0x4e, 0x80]);
        var invocation = Parse(
            "@include \"bad.awk\"\nBEGIN{print marker}",
            [new AwkEnvironmentVariable("AWKPATH", "/lib")]);

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

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

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

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

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

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

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

        var sources = LoadedSources(result);
        Assert.Equal(2, sources.Count);
        Assert.Equal("/lib/common.awk", sources[0].Name);
        Assert.Equal("/main.awk", sources[1].Name);
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

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("BEGIN{marker=\"relative\"}\nBEGIN{print marker}", source.Text);
        Assert.Equal("lib/common.awk", source.Segments[0].Name);
        Assert.Equal("<inline:1>", source.Segments[1].Name);
    }

    [Fact]
    public async Task AwkSourceLoader_NormalizesLineContinuationsInsideIncludedFiles()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/common.awk", "BEGIN{marker=\"in\" \\\n\"clude\"}");
        var invocation = Parse("@include \"/lib/common.awk\"\nBEGIN{print marker}");

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("BEGIN{marker=\"in\" \"clude\"}\nBEGIN{print marker}", source.Text);
        Assert.Equal(
            [AwkSourceSegmentKind.Include, AwkSourceSegmentKind.Inline],
            source.Segments.Select(segment => segment.Kind).ToArray());
    }

    [Fact]
    public async Task AwkSourceLoader_NormalizesLineContinuationsBeforeRecordingSpans()
    {
        var fileSystem = new MockFileSystem();
        var invocation = Parse("BEGIN{print 1 \\\n+ 2}");

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

        var source = Assert.Single(LoadedSources(result));
        Assert.Equal("BEGIN{print 1 + 2}", source.Text);
        var segment = Assert.Single(source.Segments);
        Assert.Equal(AwkSourceSegmentKind.Inline, segment.Kind);
        Assert.Equal(new AwkTextSpan(0, source.Text.Length), segment.Span);
        Assert.Equal(1, segment.LineContinuations.Count);
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

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

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

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

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

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

        var failure = Assert.IsType<AwkSourceLoadResult.Failed>(result);
        Assert.Equal(AwkDiagnosticId.SourceLoadFailure, failure.Id);
        Assert.Equal(
            "awk: /missing.awk: No such file or directory\n",
            AwkUtf8Codec.Decode(failure.ErrorMessage));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkSourceLoader_NormalizesLineContinuationsBeforeParserUnsupportedInspection()
    {
        var fileSystem = new MockFileSystem();
        var invocation = Parse("BEGIN{print _\\\n\"text\"}");

        var result = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);

        var source = Assert.Single(LoadedSources(result));
        var binding = AwkBinder.Bind(AwkParser.Parse(source.Text));
        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.Equal(AwkDiagnosticId.UnsupportedPolicy, diagnostic.Id);
        Assert.Equal(
            "awk: internationalization functions are unsupported in Lokad awk\n",
            AwkUtf8Codec.Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
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
        var sourceLoad = await AwkSourceLoader.LoadAsync(fileSystem, invocation, CancellationToken.None);
        var failure = Assert.IsType<AwkSourceLoadResult.Failed>(sourceLoad);
        Assert.Equal(
            "awk: /lib/bad.awk: invalid UTF-8 source\n",
            AwkUtf8Codec.Decode(failure.ErrorMessage));

        fileSystem = new MockFileSystem();
        fileSystem.AddFile("/lib/bad.awk", [0x42, 0x45, 0x47, 0x49, 0x4e, 0x80]);
        var parsedTool = Assert.IsType<Awk>(Awk.TryParse(command));
        var exitCode = await parsedTool.ExecuteAsync(fileSystem, CancellationToken.None);
        var result = new AwkTestRunResult(
            exitCode,
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

    private static async Task<AwkTestRunResult> ExecuteAsync(
        MockFileSystem fileSystem,
        IReadOnlyList<string> arguments)
    {
        var tool = Assert.IsType<Awk>(Awk.TryParse(AwkTestHarness.BuildInvocation("awk", arguments, [])));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None).ConfigureAwait(false);
        return new AwkTestRunResult(
            exitCode,
            fileSystem.GetOutput(AwkFileDescriptor.StdOut),
            fileSystem.GetOutput(AwkFileDescriptor.StdErr),
            new Dictionary<string, string?>());
    }
}
