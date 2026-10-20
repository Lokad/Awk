using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

public sealed class AwkHostRegexTests
{
    [Fact]
    public void PolicyIsImmutableAndCoreAssemblyHasNoProducerReference()
    {
        var original = AwkExecutionPolicy.CreateDefault();
        var engine = new RecordingEngine();
        var custom = original.WithRegexEngine(engine);
        Assert.Null(original.RegexEngine);
        Assert.Same(engine, custom.RegexEngine);
        Assert.DoesNotContain(typeof(Awk).Assembly.GetReferencedAssemblies(),
            assembly => assembly.Name == "Lokad.Utf8Regex");
        Assert.Equal(24, Unsafe.SizeOf<AwkRegexCaptureRange>());
    }

    [Fact]
    public async Task PublicExecutionCompilesOnceAndDisposesHandlesButBorrowsEngine()
    {
        var engine = new RecordingEngine();
        var host = new MockFileSystem();
        var exit = await Tool("BEGIN{r=\"a\";for(i=0;i<3;i++)n+=(\"a\"~r);print n}")
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine), CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("3\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, engine.Compiles);
        Assert.Single(engine.Patterns);
        Assert.All(engine.Patterns, pattern => Assert.True(pattern.Disposed));
        Assert.False(engine.Disposed);
    }

    [Fact]
    public async Task UnsupportedPatternFallsBackOnceWithoutChangingOutput()
    {
        var engine = new RecordingEngine(Behavior.Unsupported);
        var host = new MockFileSystem();
        var exit = await Tool("BEGIN{r=\"a\";print (\"a\"~r),(\"b\"~r)}")
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine), CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1 0\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, engine.Compiles);
        Assert.Empty(engine.Patterns);
    }

    [Theory]
    [InlineData(Behavior.RefuseCompile, "awk: memory limit exceeded\n")]
    [InlineData(Behavior.RefuseMatch, "awk: memory limit exceeded\n")]
    public async Task RefusalFailsWithoutFallbackOrLaterEffects(Behavior behavior, string diagnostic)
    {
        var engine = new RecordingEngine(behavior);
        var budget = new CooperativeTestBudget(1 << 20);
        if (behavior == Behavior.RefuseCompile) budget.RefuseNextAdmissionMinimumGrowth = 1L << 40;
        var host = new MockFileSystem();
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine).WithMemoryBudget(budget);
        var exit = await Tool("BEGIN{print \"before\";r=\"a\";print (\"a\"~r);print \"after\"} END{print \"end\"}")
            .ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(diagnostic, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, engine.Compiles);
        Assert.All(engine.Patterns, pattern => Assert.True(pattern.Disposed));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
        if (behavior == Behavior.RefuseCompile)
        {
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
            Assert.True(engine.BudgetSupplied);
        }
    }

    [Theory]
    [InlineData(Behavior.BadByteRange)]
    [InlineData(Behavior.BadUtf16Range)]
    [InlineData(Behavior.SplitScalar)]
    public void InvalidRangesFailBeforeCaptureAllocationOrPublication(Behavior behavior)
    {
        var engine = new RecordingEngine(behavior);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            limits, regexEngine: engine);
        using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var regex = state.CreateRegex(new("(é)"));
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var error = Assert.Throws<AwkRuntimeException>(() => state.MatchRegexCaptureValue(regex, AwkText.FromString("é")));
        Assert.Equal(AwkDiagnosticId.RuntimeInvalidRegex, error.Id);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task CancellationDisposesPatternsAndPreservesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var engine = new RecordingEngine { BeforeMatch = _ => cancellation.Cancel() };
        var host = new MockFileSystem();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Tool("BEGIN{print \"before\";r=\"a\";print (\"a\"~r)}").ExecuteAsync(host,
                AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine), cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.All(engine.Patterns, pattern => Assert.True(pattern.Disposed));
    }

    [Fact]
    public async Task EvictionReleasesHandlesAndCapturedFieldPatternsRemainUsable()
    {
        var engine = new RecordingEngine();
        var host = new MockFileSystem();
        var exit = await Tool("BEGIN{FS=\"a[bc]\";$0=\"xabz\";for(i=0;i<260;i++){r=\"p\" i;n+=(\"x\"~r)};print $2}")
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine), CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("z\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(engine.SawEvictionBeforeCompletion);
        Assert.All(engine.Patterns, pattern => Assert.True(pattern.Disposed));
    }

    [Fact]
    public async Task PreparedProgramReuseKeepsEngineCachesSeparate()
    {
        var command = AwkTestHarness.BuildInvocation("awk", "BEGIN{r=\"a\";print (\"a\"~r)}");
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), command, CancellationToken.None);
        var first = new RecordingEngine();
        var second = new RecordingEngine(Behavior.Unsupported);
        foreach (var engine in new[] { first, second, first })
        {
            var host = new MockFileSystem();
            var options = AwkRuntimeOptions.Create(AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine));
            Assert.Equal(AwkExitCode.Success, await compiled.ExecuteAsync(host, CancellationToken.None, options));
            Assert.Equal("1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        }
        Assert.Equal(2, first.Compiles);
        Assert.Equal(1, second.Compiles);
        Assert.All(first.Patterns, pattern => Assert.True(pattern.Disposed));
    }

    [Theory]
    [InlineData("compile", false)] [InlineData("compile", true)]
    [InlineData("boolean", false)] [InlineData("boolean", true)]
    [InlineData("captures", false)] [InlineData("captures", true)]
    public async Task PublicExecutionForwardsCancellationIntoEveryEngineOperation(string stage, bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var budget = new CooperativeTestBudget(1 << 20);
        void Cooperate(CancellationToken token)
        {
            Assert.True(token.CanBeCanceled);
            entered.TrySetResult(token);
            if (!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("The test did not release its cooperative engine.");
            token.ThrowIfCancellationRequested();
        }
        var engine = new RecordingEngine
        {
            BeforeCompile = stage == "compile" ? Cooperate : null,
            BeforeMatch = stage == "compile" ? null : Cooperate
        };
        var host = new MockFileSystem();
        var script = stage == "captures" ? "BEGIN{print \"before\";r=\"(a)\";print match(\"a\",r,captures);print \"after\"}"
            : "BEGIN{print \"before\";r=\"a\";print (\"a\"~r);print \"after\"}";
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var execution = Task.Run(() => Tool(script).ExecuteAsync(host, policy, cancellation.Token));
        try
        {
            var received = await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(received.IsCancellationRequested);
            Assert.False(execution.IsCompleted);
            cancellation.Cancel();
            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.True(received.IsCancellationRequested);
            Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.All(engine.Patterns, pattern => Assert.True(pattern.Disposed));
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            try { await execution; } catch (OperationCanceledException) { }
        }
    }

    private static Awk Tool(string source) => AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));

    public enum Behavior
    {
        Normal, Unsupported, RefuseCompile, RefuseMatch, BadByteRange, BadUtf16Range, SplitScalar
    }

    private sealed class RecordingEngine(Behavior behavior = Behavior.Normal) : IAwkRegexEngine, IDisposable
    {
        public int Compiles { get; private set; }
        public bool Disposed { get; private set; }
        public bool BudgetSupplied { get; private set; }
        public bool SawEvictionBeforeCompletion { get; private set; }
        public Action<CancellationToken>? BeforeMatch { get; init; }
        public Action<CancellationToken>? BeforeCompile { get; init; }
        public List<Pattern> Patterns { get; } = [];

        public bool TryCompile(string pattern, bool ignoreCase, IMemoryBudget? memoryBudget,
            CancellationToken token, [NotNullWhen(true)] out IAwkCompiledRegex? compiledPattern)
        {
            BeforeCompile?.Invoke(token);
            Compiles++;
            BudgetSupplied |= memoryBudget is not null;
            SawEvictionBeforeCompletion |= Patterns.Any(pattern => pattern.Disposed);
            if (behavior == Behavior.RefuseCompile)
            {
                Assert.NotNull(memoryBudget);
                Assert.False(memoryBudget.TryReserve(1L << 40, out _));
                throw new AwkRegexAdmissionException();
            }
            if (behavior == Behavior.Unsupported) { compiledPattern = null; return false; }
            if (!AwkUtf8RegexEngine.Instance.TryCompile(pattern, ignoreCase, memoryBudget, token, out var inner))
            { compiledPattern = null; return false; }
            var result = new Pattern(inner, behavior, BeforeMatch);
            Patterns.Add(result);
            compiledPattern = result;
            return true;
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class Pattern(IAwkCompiledRegex inner, Behavior behavior, Action<CancellationToken>? beforeMatch) : IAwkCompiledRegex
    {
        public bool Disposed { get; private set; }
        public int GroupCount => inner.GroupCount;
        public IReadOnlyList<string> GroupNames => inner.GroupNames;

        private void Before(CancellationToken token)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            beforeMatch?.Invoke(token);
            token.ThrowIfCancellationRequested();
            if (behavior == Behavior.RefuseMatch) throw new AwkRegexAdmissionException();
        }

        public bool IsMatch(ReadOnlySpan<byte> input, CancellationToken token)
        {
            Before(token);
            return inner.IsMatch(input, token);
        }

        public AwkRegexCaptureRange Match(ReadOnlySpan<byte> input, int startInUtf16,
            Span<AwkRegexCaptureRange> captures, CancellationToken token)
        {
            Before(token);
            var result = inner.Match(input, startInUtf16, captures, token);
            result = behavior switch
            {
                Behavior.BadByteRange => result with { ByteIndex = input.Length + 1 },
                Behavior.BadUtf16Range => result with { Utf16Index = 1 },
                Behavior.SplitScalar => result with { ByteIndex = 1, ByteLength = 0 },
                _ => result
            };
            if (captures.Length != 0) captures[0] = result;
            return result;
        }

        public void Dispose()
        {
            Disposed = true;
            inner.Dispose();
        }
    }
}
