namespace Lokad.Awk.Tests;

public sealed class AwkRegexCompilationTests
{
    public static IEnumerable<object[]> Outcomes()
    {
        foreach (var status in new[] { AwkRegexCompilationStatus.Compiled, AwkRegexCompilationStatus.Unsupported, AwkRegexCompilationStatus.Invalid })
        foreach (var literal in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [status, literal, governed];
    }

    [Theory]
    [MemberData(nameof(Outcomes))]
    public async Task ExplicitOutcomesPreserveDiagnosticsEffectsAndOwnership(
        AwkRegexCompilationStatus status, bool literal, bool governed)
    {
        var engine = new OutcomeEngine(status);
        var host = new MockFileSystem();
        var budget = AwkMemoryBudget.CreateFixed(2 << 20);
        var observed = new List<Exception>();
        var policy = AwkTestHarness.CreatePolicy().WithRegexEngine(engine).WithInternalFailureObserver(observed.Add);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var result = await Tool(literal).ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(status == AwkRegexCompilationStatus.Compiled ? AwkExitCode.Success : AwkExitCode.FatalError, result);
        Assert.Equal(status == AwkRegexCompilationStatus.Compiled ? "before\n1\nafter\nend\n" : literal ? "" : "before\n",
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(status switch
        {
            AwkRegexCompilationStatus.Compiled => "",
            AwkRegexCompilationStatus.Invalid => "awk: invalid regular expression\n",
            _ => "awk: regular expression is unsupported by the supplied provider\n"
        }, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Empty(observed);
        Assert.All(engine.Handles, handle => Assert.True(handle.Disposed));
        Assert.Equal(0, budget.ReservedBytes);
    }

    [Theory]
    [InlineData(AwkRegexCompilationStatus.Invalid, false)]
    [InlineData(AwkRegexCompilationStatus.Invalid, true)]
    [InlineData(AwkRegexCompilationStatus.Unsupported, false)]
    [InlineData(AwkRegexCompilationStatus.Unsupported, true)]
    [InlineData((AwkRegexCompilationStatus)99, false)]
    [InlineData((AwkRegexCompilationStatus)99, true)]
    public async Task FailureAlsoRetiresAnUnexpectedReturnedHandle(AwkRegexCompilationStatus status, bool literal)
    {
        var engine = new OutcomeEngine(status) { ReturnHandleOnFailure = true };
        var host = new MockFileSystem();
        var budget = AwkMemoryBudget.CreateFixed(2 << 20);
        var result = await Tool(literal).ExecuteAsync(host, AwkTestHarness.CreatePolicy()
            .WithRegexEngine(engine).WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, result);
        Assert.Equal(literal ? "" : "before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.NotEmpty(engine.Handles);
        Assert.All(engine.Handles, handle => Assert.True(handle.Disposed));
        Assert.Equal(0, budget.ReservedBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ProviderArgumentFaultIsNotPatternSyntax(bool literal)
    {
        var engine = new OutcomeEngine(AwkRegexCompilationStatus.Compiled) { ThrowArgumentFault = true };
        var host = new MockFileSystem();
        var observed = new List<Exception>();
        var budget = AwkMemoryBudget.CreateFixed(2 << 20);
        var result = await Tool(literal).ExecuteAsync(host, AwkTestHarness.CreatePolicy()
            .WithRegexEngine(engine).WithMemoryBudget(budget).WithInternalFailureObserver(observed.Add), CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, result);
        Assert.Equal("awk: execution failed without exposing host exception details\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(literal ? "" : "before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(observed);
        Assert.All(engine.Handles, handle => Assert.True(handle.Disposed));
        Assert.Equal(0, budget.ReservedBytes);
    }

    [Theory]
    [InlineData(AwkRegexCompilationStatus.Compiled)]
    [InlineData(AwkRegexCompilationStatus.Invalid)]
    [InlineData(AwkRegexCompilationStatus.Unsupported)]
    public async Task CancellationAfterProviderReturnDoesNotBecomeASyntaxDiagnostic(AwkRegexCompilationStatus status)
    {
        using var cancellation = new CancellationTokenSource();
        var engine = new OutcomeEngine(status) { AfterCompile = cancellation.Cancel };
        var host = new MockFileSystem();
        var budget = AwkMemoryBudget.CreateFixed(2 << 20);
        var execution = Tool(true).ExecuteAsync(host, AwkTestHarness.CreatePolicy()
            .WithRegexEngine(engine).WithMemoryBudget(budget), cancellation.Token);
        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.All(engine.Handles, handle => Assert.True(handle.Disposed));
        Assert.Equal(0, budget.ReservedBytes);
    }

    [Fact]
    public async Task InvalidCoreClassDoesNotCallTheProviderOrNotifyTheObserver()
    {
        var engine = new OutcomeEngine(AwkRegexCompilationStatus.Compiled);
        var host = new MockFileSystem();
        var observed = new List<Exception>();
        var result = await AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "BEGIN{print \"before\";r=\"[[:unknown:]]\";print(\"a\"~r);print \"after\"}"))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithRegexEngine(engine)
                .WithInternalFailureObserver(observed.Add), CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, result);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: invalid regular expression\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Empty(engine.Handles);
        Assert.Empty(observed);
    }

    private static Awk Tool(bool literal) => AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
        "BEGIN{print \"before\";" + (literal ? "print(\"a\"~/a/)" : "r=\"a\";print(\"a\"~r)") +
        ";print \"after\"}END{print \"end\"}"));

    private sealed class OutcomeEngine(AwkRegexCompilationStatus status) : IAwkRegexEngine
    {
        public bool ReturnHandleOnFailure { get; init; }
        public bool ThrowArgumentFault { get; init; }
        public Action? AfterCompile { get; init; }
        public List<TrackedPattern> Handles { get; } = [];

        public AwkRegexCompilationStatus TryCompile(ReadOnlySpan<byte> source, bool ignoreCase, IMemoryBudget? budget,
            CancellationToken token, out IAwkCompiledRegex? pattern)
        {
            pattern = null;
            if (status == AwkRegexCompilationStatus.Compiled || ReturnHandleOnFailure)
            {
                Assert.Equal(AwkRegexCompilationStatus.Compiled,
                    AwkUtf8RegexEngine.Instance.TryCompile(source, ignoreCase, budget, token, out var compiled));
                Assert.NotNull(compiled);
                var tracked = new TrackedPattern(compiled);
                Handles.Add(tracked);
                pattern = tracked;
            }
            if (ThrowArgumentFault) throw new ArgumentException("private provider argument fault");
            AfterCompile?.Invoke();
            return status;
        }
    }

    private sealed class TrackedPattern(IAwkCompiledRegex inner) : IAwkCompiledRegex
    {
        public bool Disposed { get; private set; }
        public int GroupCount => inner.GroupCount;
        public bool IsMatch(ReadOnlySpan<byte> input, CancellationToken token) => inner.IsMatch(input, token);
        public IAwkRegexSearch CreateSearch(ReadOnlyMemory<byte> input, CancellationToken token) => inner.CreateSearch(input, token);
        public void Dispose() { Disposed = true; inner.Dispose(); }
    }
}
