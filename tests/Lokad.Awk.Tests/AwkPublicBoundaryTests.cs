using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkPublicBoundaryTests
{
    [Fact]
    public void AwkExecutionPolicy_ProvidesImmutableCompatibleAndRestrictedPresets()
    {
        var compatible = AwkExecutionPolicy.CreateDefault();
        var restricted = AwkExecutionPolicy.CreateRestricted();
        var customized = restricted.WithCommandPipes(true).WithMaxOutputBytes(42);
        var rooted = compatible.WithAccessiblePathRoot(new AwkPath("/jail"));

        Assert.True(compatible.AllowCommandPipes);
        Assert.True(compatible.AllowFileRedirections);
        Assert.False(restricted.AllowCommandPipes);
        Assert.False(restricted.AllowFileRedirections);
        Assert.False(restricted.AllowSourceIncludes);
        Assert.False(restricted.AllowDynamicMainInputPaths);
        Assert.Equal(42, customized.MaxOutputBytes);
        Assert.True(customized.AllowCommandPipes);
        Assert.False(restricted.AllowCommandPipes);
        Assert.NotEqual(AwkExecutionPolicy.Unlimited, restricted.MaxExecutionMemoryBytes);
        Assert.Equal("/jail", Assert.IsType<AwkPath>(rooted.AccessiblePathRoot).Path);
    }

    [Fact]
    public async Task AwkExecutionPolicy_PublicExecutionOverloadEnforcesSourceBudget()
    {
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{print \"unreachable\"}"));
        var host = new MockFileSystem();
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxSourceBytes(8);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: source byte limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_SourceUnitLimitPreflightsBeforeSourceAssembly()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "-e",
            "BEGIN{print 1}",
            "-e",
            "BEGIN{print 2}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxSourceUnits(1);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: source unit limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_ExpandedSourceLimitChargesRepeatedCachedFiles()
    {
        var host = new MockFileSystem();
        host.AddFile("/program.awk", "BEGIN{print 1}\n");
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "-f",
            "/program.awk",
            "-f",
            "/program.awk"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxExpandedSourceBytes(20);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: expanded source byte limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_FrontendCharacterLimitPreflightsBeforeLexing()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{value=\"abcdefghijklmnopqrstuvwxyz\"}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxFrontendCharacters(16);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: frontend character limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_FrontendTokenLimitStopsBroadPrograms()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{x=1;y=2;z=3}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxFrontendTokens(8);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: frontend token limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_FrontendDiagnosticLimitStopsEscapeWarningAmplification()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{x=\"\\q\\q\\q\"}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxFrontendDiagnostics(2);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: frontend diagnostic limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_RegexPatternLimitAppliesBeforeDynamicCompilation()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{pattern=\"abcde\"; print \"x\" ~ pattern}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxRegexPatternBytes(4);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: regular expression pattern limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_RegexGroupLimitAppliesBeforeSourceCompilation()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{print \"a\" ~ /((a))/}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxRegexGroups(1);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: regular expression group limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_RegexCompilationLimitIncludesSourceLiterals()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{print \"a\" ~ /a/}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxRegexCompilations(1);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: regular expression compilation limit exceeded\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_DynamicSymtabNamesRespectSymbolLimit()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{for(i=0;i<2000;i++) SYMTAB[\"hostile\" i]=0}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxSymbols(1_000);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: symbol limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_DynamicSymtabNamesRespectIdentifierLimit()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{name=\"abcdefghijklmnopq\"; SYMTAB[name]=0}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxIdentifierBytes(16);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: identifier byte limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_CaptureArrayLimitPreflightsBeforeDictionaryAllocation()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{match(\"ab\", /(a)(b)/, captures)}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxArrayElements(4);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: array element limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_EnvironmentLimitPreflightsBeforeMaterialization()
    {
        var invocation = AwkCommandInvocation.CreateWithStandardDescriptors(
            "awk",
            ["BEGIN{print \"unreachable\"}"],
            [
                new AwkEnvironmentVariable("ONE", "1"),
                new AwkEnvironmentVariable("TWO", "2")
            ]);
        Assert.True(Awk.TryParse(invocation, out var tool));
        var host = new MockFileSystem();
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxArrayElements(1);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: array element limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
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
            AwkExecutionPolicy.CreateRestricted(),
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
            AwkExecutionPolicy.CreateRestricted(),
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
            AwkExecutionPolicy.CreateRestricted(),
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
        var policy = AwkExecutionPolicy.CreateDefault()
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
        var policy = AwkExecutionPolicy.CreateDefault()
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
            AwkExecutionPolicy.CreateRestricted(),
            CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("0\n1970-01-01 00:00:00\n", host.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Theory]
    [InlineData("%2147483647Y")]
    [InlineData("%999999999999999999999Y")]
    public async Task AwkExecutionPolicy_StrftimeWidthPreflightsBeforeAllocation(string format)
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            $"BEGIN{{print strftime(\"{format}\", 0, 1)}}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxValueBytes(64);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
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

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: execution failed without exposing host exception details\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.DoesNotContain("sensitive", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutionPolicy_ExecutionTimeoutReturnsAwkDiagnostic()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation(
            "awk",
            "BEGIN{while(1){x++}}"));
        var policy = AwkExecutionPolicy.CreateDefault()
            .WithExecutionTimeout(TimeSpan.FromMilliseconds(10));

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: execution timed out\n", host.GetOutput(AwkFileDescriptor.StdErr));
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
