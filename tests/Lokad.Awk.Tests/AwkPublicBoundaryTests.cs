using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkPublicBoundaryTests
{
    public static IEnumerable<object[]> DirectiveBoundaryCases()
    {
        foreach (var test in AwkGoldenCorpus.DirectCases.Where(test => test.Name.StartsWith("directive-", StringComparison.Ordinal)))
        foreach (var restricted in new[] { false, true })
        foreach (var governed in new[] { false, true })
            yield return [test, restricted, governed];
    }

    [Theory]
    [MemberData(nameof(DirectiveBoundaryCases))]
    public async Task DirectivesPreserveSourceEffectsAndCleanup(AwkGoldenCase test, bool restricted, bool governed)
    {
        var host = AwkTestHarness.BuildFileSystem(test);
        var budget = new CooperativeTestBudget(16 << 20);
        var policy = (restricted ? AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance) : AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance))
            .WithSourceIncludes(true);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", test.Arguments, test.Environment));

        var exit = await awk.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(test.ExpectedExitCode, exit.Value);
        Assert.Equal(test.ExpectedStdOut, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(test.ExpectedStdErr, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(governed, budget.ScopesCreated > 0);
        Assert.Empty(host.AwkCommandInvocations);
    }

    public static IEnumerable<object[]> TriviaProgramBoundaryCases()
    {
        foreach (var test in AwkGoldenCorpus.DirectCases.Where(test => test.Name.StartsWith("trivia-program-", StringComparison.Ordinal)))
        foreach (var restricted in new[] { false, true })
        foreach (var governed in new[] { false, true })
            yield return [test, restricted, governed];
    }

    [Theory]
    [MemberData(nameof(TriviaProgramBoundaryCases))]
    public async Task TriviaProgramsKeepDiagnosticsAndHostLifetime(AwkGoldenCase test, bool restricted, bool governed)
    {
        var host = AwkTestHarness.BuildFileSystem(test);
        var budget = new CooperativeTestBudget(16 << 20);
        var policy = (restricted ? AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance) : AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance))
            .WithSourceIncludes(true);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", test.Arguments, test.Environment));

        var exit = await awk.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(test.ExpectedExitCode, exit.Value);
        Assert.Equal(test.ExpectedStdOut, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(test.ExpectedStdErr, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(governed, budget.ScopesCreated > 0);
    }

    [Theory]
    [InlineData("", false)] [InlineData("", true)]
    [InlineData("--posix", false)] [InlineData("--posix", true)]
    [InlineData("--traditional", false)] [InlineData("--traditional", true)]
    public async Task TriviaTextBoundariesPreserveLineEndingsAndInvalidSpace(string profile, bool governed)
    {
        foreach (var source in new[] { "# comment\n", "# comment\r\n", " \t\n", "\u0085" })
        {
            var host = new MockFileSystem();
            host.SetStandardInput("DATA\n");
            var budget = new CooperativeTestBudget(16 << 20);
            var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
            if (governed) policy = policy.WithMemoryBudget(budget);
            string[] arguments = profile.Length == 0 ? [source] : [profile, source];
            var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", arguments));

            var exit = await awk.ExecuteAsync(host, policy, CancellationToken.None);

            Assert.Equal(source == "\u0085" ? AwkExitCode.FatalError : AwkExitCode.Success, exit);
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(source == "\u0085" ? "awk: unrecognized awk token.\n" : "", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CommentOnlySourceCancellationClosesTheSource(bool restricted, bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        var host = new MockFileSystem();
        host.AddFile("/comments.awk", "# " + new string('x', 64 << 10));
        host.AfterReadLines = cancellation.Cancel;
        var budget = new CooperativeTestBudget(16 << 20);
        var policy = restricted ? AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance) : AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "-f", "/comments.awk"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => awk.ExecuteAsync(host, policy, cancellation.Token));

        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommentOnlySourceRefusalStillFailsWithoutOpeningInput(bool restricted)
    {
        var host = new MockFileSystem();
        host.AddFile("/comments.awk", "# " + new string('x', 64 << 10));
        host.AddFile("/data", "data\n");
        var budget = new CooperativeTestBudget(16 << 20);
        host.AfterReadLines = () => budget.RefuseNextAdmissionMinimumGrowth = 1;
        var policy = (restricted ? AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance) : AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance))
            .WithMemoryBudget(budget);
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "-f", "/comments.awk", "/data"));

        var exit = await awk.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
        Assert.True(budget.Refusals > 0);
    }

    [Theory]
    [InlineData("file", false, false)] [InlineData("file", false, true)]
    [InlineData("file", true, false)] [InlineData("file", true, true)]
    [InlineData("include", false, false)] [InlineData("include", false, true)]
    [InlineData("include", true, false)] [InlineData("include", true, true)]
    [InlineData("include-option", false, false)] [InlineData("include-option", false, true)]
    [InlineData("include-option", true, false)] [InlineData("include-option", true, true)]
    public async Task SourceSearchAuthorizesTheActualCandidate(string kind, bool deniedFirst, bool governed)
    {
        string[] arguments = kind switch
        {
            "file" => ["-f", "common.awk"],
            "include-option" => ["-i", "common.awk", "BEGIN{}"],
            _ => ["@include \"common.awk\"\nBEGIN{}"]
        };
        var host = new MockFileSystem();
        host.AddFile("/lib/common.awk", "BEGIN{print \"LIB\"}");
        host.AddFile("/common.awk", "BEGIN{print \"WRONG\"}");
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments,
            [new("AWKPATH", deniedFirst ? "/outside:/lib" : "/lib")]);
        Assert.True(Awk.TryParse(command, out var tool));
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithAccessiblePathRoot(new AwkPath("/lib"));
        if (governed) policy = policy.WithMemoryBudget(budget);

        Assert.Equal(deniedFirst ? AwkExitCode.FatalError : AwkExitCode.Success,
            await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal(deniedFirst ? "" : "LIB\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(deniedFirst ? "awk: path is outside the execution policy root\n" : "", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(deniedFirst ? 0 : 1, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> MalformedHostTextCases()
    {
        foreach (var placement in new[] { "path", "file-source", "include-source", "separator", "assignment", "inline", "environment-name", "environment-value" })
        foreach (var lowSurrogate in new[] { false, true })
        foreach (var governed in new[] { false, true })
            yield return [placement, lowSurrogate, governed];
    }

    [Theory]
    [MemberData(nameof(MalformedHostTextCases))]
    public async Task MalformedHostTextProducesAnAwkFailure(string placement, bool lowSurrogate, bool governed)
    {
        var invalid = new string(lowSurrogate ? (char)0xdc00 : (char)0xd800, 1);
        const string program = "BEGIN{print \"unreachable\"}";
        string[] arguments = placement switch
        {
            "path" => [program, "bad" + invalid],
            "file-source" => ["-f", "bad" + invalid],
            "include-source" => ["-i", "bad" + invalid, program],
            "separator" => ["-F", invalid, program],
            "assignment" => ["-v", "x=" + invalid, program],
            "inline" => ["BEGIN{print \"" + invalid + "\"}"],
            _ => ["BEGIN{print ENVIRON[\"BAD\"]}"]
        };
        AwkEnvironmentVariable[] environment = placement switch
        {
            "environment-name" => [new("BAD" + invalid, "value")],
            "environment-value" => [new("BAD", invalid)],
            _ => []
        };
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments, environment);
        Assert.True(Awk.TryParse(command, out var tool));
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);

        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: invalid UTF-8 input\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Empty(host.AwkCommandInvocations);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ValidSupplementaryHostTextRemainsIntact(bool governed)
    {
        const string text = "\U0001f603";
        var host = new MockFileSystem();
        host.AddFile("/" + text, "left" + text + "right\n");
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["-F", text, "-v", "x=" + text, "{print $1,$2,x,ENVIRON[\"TEXT\"]}", text],
            [new("TEXT", text)]);
        Assert.True(Awk.TryParse(command, out var tool));
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);

        Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("left right " + text + " " + text + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void AwkExecutionPolicy_ProvidesImmutableCompatibleAndRestrictedPresets()
    {
        var compatible = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        var restricted = AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        var customized = restricted.WithCommandPipes(true).WithMaxPipes(42);
        var rooted = compatible.WithAccessiblePathRoot(new AwkPath("/jail"));

        Assert.True(compatible.AllowCommandPipes);
        Assert.True(compatible.AllowFileRedirections);
        Assert.False(restricted.AllowCommandPipes);
        Assert.False(restricted.AllowFileRedirections);
        Assert.False(restricted.AllowSourceIncludes);
        Assert.False(restricted.AllowDynamicMainInputPaths);
        Assert.False(restricted.ExposeEnvironment);
        Assert.False(restricted.ExposeHostErrorDetails);
        Assert.Equal(42, customized.MaxPipes);
        Assert.True(customized.AllowCommandPipes);
        Assert.False(restricted.AllowCommandPipes);
        Assert.Equal(32, restricted.MaxFunctionCallDepth);
        Assert.Equal("/jail", Assert.IsType<AwkPath>(rooted.AccessiblePathRoot).Path);
    }

    [Fact]
    public async Task AwkExecutionPolicy_RestrictedPresetHidesInvocationEnvironment()
    {
        var invocation = AwkCommandInvocation.CreateWithStandardDescriptors(
            "awk",
            ["BEGIN{print (\"SECRET\" in ENVIRON)}"],
            [new AwkEnvironmentVariable("SECRET", "sensitive")]);
        Assert.True(Awk.TryParse(invocation, out var tool));
        var host = new MockFileSystem();

        var exitCode = await tool.ExecuteAsync(
            host,
            AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance),
            CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("0\n", host.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task AwkExecutionPolicy_RestrictedPresetDoesNotSearchAwkPath()
    {
        var invocation = AwkTestHarness.BuildInvocation(
            "awk",
            ["-f", "program.awk"],
            [new AwkEnvironmentVariable("AWKPATH", "/lib")]);
        var tool = AwkTestHarness.ParseAwk(invocation);
        var host = new MockFileSystem();
        host.AddFile("/lib/program.awk", "BEGIN{print \"unreachable\"}");

        var exitCode = await tool.ExecuteAsync(
            host,
            AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance),
            CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(1, host.ReadOpenCount);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Contains("No such file or directory", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_RestrictedPresetSanitizesStructuredHostErrors()
    {
        var host = new MockFileSystem();
        host.AddReadOpenError("/input.txt", "sensitive host path and credential");
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "{print}",
            "/input.txt"));

        var exitCode = await tool.ExecuteAsync(
            host,
            AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance),
            CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: /input.txt: Operation failed.\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.DoesNotContain("sensitive", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_HostErrorByteLimitUsesGenericDiagnostic()
    {
        var host = new MockFileSystem();
        host.AddReadOpenError("/input.txt", "host failure details");
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "{print}",
            "/input.txt"));
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMaxHostErrorBytes(4);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: /input.txt: Operation failed.\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_RestrictedPresetDeniesFileRedirections()
    {
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{print \"secret\" > \"/out.txt\"}"));
        var host = new MockFileSystem();

        var exitCode = await tool.ExecuteAsync(
            host,
            AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance),
            CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.False(host.FileExists("/out.txt"));
        Assert.Equal(
            "awk: file redirections are disabled by execution policy\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_RestrictedPresetDeniesSourceIncludesBeforeHostOpen()
    {
        var host = new MockFileSystem();
        host.AddFile("/secret.awk", "BEGIN{print \"secret\"}\n");
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "@include \"/secret.awk\"\nBEGIN{print \"unreachable\"}"));

        var exitCode = await tool.ExecuteAsync(
            host,
            AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance),
            CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: source includes are disabled by execution policy\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_RestrictedPresetDeniesDynamicMainInputPaths()
    {
        var host = new MockFileSystem();
        host.AddFile("/safe.txt", "safe\n");
        host.AddFile("/secret.txt", "secret\n");
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{ARGV[1]=\"/secret.txt\"} {print}",
            "/safe.txt"));

        var exitCode = await tool.ExecuteAsync(
            host,
            AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance),
            CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: dynamic main-input paths are disabled by execution policy\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_PathRootDeniesMainInputOutsideRoot()
    {
        var host = new MockFileSystem();
        host.AddFile("/outside.txt", "secret\n");
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "{print}",
            "/outside.txt"));
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance)
            .WithAccessiblePathRoot(new AwkPath("/jail"));

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: path is outside the execution policy root\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_PathRootDeniesSourceOutsideRoot()
    {
        var host = new MockFileSystem();
        host.AddFile("/outside.awk", "BEGIN{print \"unreachable\"}\n");
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "-f",
            "/outside.awk"));
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance)
            .WithAccessiblePathRoot(new AwkPath("/jail"));

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: path is outside the execution policy root\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_RestrictedPresetUsesFixedUtcTime()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{print systime(); print strftime(\"%Y-%m-%d %H:%M:%S\")}"));

        var exitCode = await tool.ExecuteAsync(
            host,
            AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance),
            CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("0\n1970-01-01 00:00:00\n", host.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_PublicExecutionDoesNotLeakUnexpectedHostExceptionDetails()
    {
        var host = new MockFileSystem
        {
            OpenFileException = new InvalidOperationException("sensitive host implementation detail")
        };
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "{print}",
            "/input.txt"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: execution failed without exposing host exception details\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.DoesNotContain("sensitive", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public void AwkCommandInvocation_SnapshotsCallerOwnedCollections()
    {
        var arguments = new List<string> { "BEGIN{print 1}" };
        var environment = new List<AwkEnvironmentVariable>
        {
            new("PWD", "/work")
        };
        var invocation = AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments, environment);

        arguments[0] = "BEGIN{print 2}";
        environment[0] = new AwkEnvironmentVariable("PWD", "/changed");

        Assert.Equal("BEGIN{print 1}", Assert.Single(invocation.Arguments));
        Assert.Equal("/work", Assert.Single(invocation.Environment).Value);
        Assert.Equal("/work", invocation.CurrentDirectory.Path);
    }

    [Fact]
    public void AwkCommandInvocation_RejectsNullCollectionElements()
    {
        Assert.Throws<ArgumentException>(() => new AwkEnvironmentVariable(string.Empty, "value"));
        Assert.Throws<ArgumentNullException>(() =>
            AwkCommandInvocation.CreateWithStandardDescriptors("awk", new string[1], []));
        Assert.Throws<ArgumentException>(() =>
            AwkCommandInvocation.CreateWithStandardDescriptors("awk", [], [default]));
    }

    [Fact]
    public void AwkPaths_HandleNullTryInputsAndRejectNullRequiredInputs()
    {
        Assert.False(AwkPath.IsValid(null));
        Assert.False(AwkPathSegment.IsValid(null));
        Assert.False(AwkPathSegment.TryCreate(null, out _));
        Assert.False(AwkPath.Root.TryChild((string?)null, out var nullChild));
        Assert.Null(nullChild);
        Assert.False(AwkPath.Root.TryChild(default(AwkPathSegment), out var defaultChild));
        Assert.Null(defaultChild);

        Assert.True(AwkPath.Root.TryChild("child", out var child));
        Assert.Equal("/child", Assert.IsType<AwkPath>(child).Path);
    }

    [Fact]
    public void Awk_InternalValueObjectsExposeNonNullDefaultViews()
    {
        Assert.Empty(default(AwkAction).Statements);
        Assert.Equal(string.Empty, default(AwkSourceUnit).Text);
        Assert.Empty(default(AwkSourceUnit).Segments);
        Assert.Equal(string.Empty, default(AwkSourceName).Text);
        Assert.Equal(string.Empty, default(AwkSourceIdentity).Text);
        Assert.Equal(string.Empty, default(AwkRegexPattern).Text);
        Assert.Equal(string.Empty, default(AwkRegexLiteralText).Text);
        Assert.Equal(string.Empty, default(AwkErrno).Text);
    }

    [Fact]
    public void Awk_InternalEmptyValueObjectsHaveCanonicalEquality()
    {
        Assert.Equal(default, new AwkRegexPattern(string.Empty));
        Assert.Equal(default, new AwkRegexLiteralText(string.Empty));
        Assert.Equal(default, new AwkSourceName(string.Empty));
        Assert.Equal(default, new AwkSourceIdentity(string.Empty));
        Assert.Equal(default, new AwkRedirectionPath(string.Empty));
        Assert.Equal(default, AwkErrno.None);
    }

    [Fact]
    public void Awk_DefaultValueTypesDoNotSilentlyViolateNonNullableContracts()
    {
        var nullability = new NullabilityInfoContext();
        var violations = new List<string>();

        foreach (var type in typeof(Awk).Assembly.GetTypes())
        {
            if (!type.IsValueType || type.IsByRefLike || type.ContainsGenericParameters)
                continue;

            var instance = Activator.CreateInstance(type);
            foreach (var property in type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly))
            {
                if (property.GetMethod is null ||
                    property.GetIndexParameters().Length != 0 ||
                    property.PropertyType.IsValueType ||
                    nullability.Create(property).ReadState != NullabilityState.NotNull)
                {
                    continue;
                }

                try
                {
                    if (property.GetValue(instance) is null)
                        violations.Add(type.FullName + "." + property.Name + " returned null.");
                }
                catch (TargetInvocationException exception)
                    when (exception.InnerException is InvalidOperationException)
                {
                    // Some carriers have no meaningful default. Their getters reject that state explicitly.
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void AwkRawPaths_CanonicalizeAndEnforceBoundaries()
    {
        var current = new AwkPath("/work/input");

        Assert.Equal("/work/output/data", AwkRawPath.Parse("../output/./data").ToAbsolute(current).Path);
        Assert.Throws<AwkPathException>(() => AwkRawPath.Parse("../output").ToAbsoluteWithin(current));
        Assert.Throws<AwkPathException>(() => AwkRawPath.Parse("../../..").ToAbsolute(current));
    }

    [Fact]
    public void AwkPaths_ValidateWithoutAllocatingSegments()
    {
        const string canonical = "/work/input/data.csv";
        const string raw = "../input/data.csv";
        _ = AwkPath.IsValid(canonical);
        _ = AwkRawPath.IsValid(raw);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            if (!AwkPath.IsValid(canonical) || !AwkRawPath.IsValid(raw))
                throw new InvalidOperationException("A valid path was rejected.");
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.True(allocatedBytes < 1_024, "Allocated bytes: " + allocatedBytes);
    }

    [Fact]
    public void AwkOperationError_RequiresTextAndDefaultsToAHostSafeMessage()
    {
        Assert.Throws<ArgumentException>(() => new AwkOperationError(string.Empty));
        Assert.Equal("Operation failed.", default(AwkOperationError).Message);
    }

    [Fact]
    public void AwkFileDescriptor_RejectsNegativeIdentifiers()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AwkFileDescriptor(-1));
        Assert.Equal(0, default(AwkFileDescriptor).Id);
    }
}
