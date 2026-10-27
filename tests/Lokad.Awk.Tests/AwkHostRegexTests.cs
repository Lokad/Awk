using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

public sealed class AwkHostRegexTests
{
    [Fact]
    public async Task OrdinaryTextProcessingDoesNotRequireARegexProvider()
    {
        var host = new MockFileSystem();
        var exit = await Tool("BEGIN{FS=\",\";$0=\"a,b\";print $2;print 2+3}")
            .ExecuteAsync(host, CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("b\n5\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Theory]
    [InlineData("r=\"a\";print (\"a\"~r)", "before\n")]
    [InlineData("print match(\"a\",/a/)", "")]
    [InlineData("print split(\"a:b\",fields,\"[:]+\")", "before\n")]
    [InlineData("FS=\"[:]+\"", "before\n")]
    [InlineData("RS=\"x.\"", "before\n")]
    public async Task MissingProviderFailsClearlyWithoutLaterEffects(string operation, string output)
    {
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(1 << 20);
        var exit = await Tool("BEGIN{print \"before\";" + operation + ";print \"after\"}END{print \"end\"}")
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal(output, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: a host-supplied regular expression provider is required\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData("BEGIN{print \"before\";if(0)print (\"a\"~/(/)}")]
    [InlineData("BEGIN{print \"before\";if(0)/(/}")]
    [InlineData("BEGIN{print \"before\";if(0)print match(\"a\",/(/)}")]
    [InlineData("BEGIN{print \"before\"}/(/{print}")]
    [InlineData("BEGIN{print \"before\";switch(\"a\"){case /(/:print 1}}")]
    public async Task InvalidLiteralsAreValidatedByTheProviderBeforeEffects(string source)
    {
        var engine = new RecordingEngine();
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(1 << 20);
        var exit = await Tool(source).ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine).WithMemoryBudget(budget),
            CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: invalid regular expression\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Empty(engine.Patterns);
        Assert.Equal(1, engine.Compiles);
        Assert.Equal(0, budget.Reserved);
    }

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

    [Theory]
    [InlineData("BEGIN{r=\"a\";for(i=0;i<3;i++)n+=(\"a\"~r);print n}", "3\n")]
    [InlineData("BEGIN{for(i=0;i<3;i++)n+=(\"a\"~/a/);print n}", "3\n")]
    [InlineData("BEGIN{$0=\"a\";for(i=0;i<3;i++)n+=/a/;print n}", "3\n")]
    [InlineData("BEGIN{for(i=0;i<3;i++)n+=match(\"a\",/a/);print n}", "3\n")]
    [InlineData("/a/{n++}END{print n}", "3\n")]
    [InlineData("/a/,/a/{n++}END{print n}", "3\n")]
    [InlineData("BEGIN{switch(\"a\"){case /a/:print 3;break}}", "3\n")]
    public async Task PublicExecutionCachesMatchesAndDisposesHandlesButBorrowsEngine(string source, string output)
    {
        var engine = new RecordingEngine();
        var host = new MockFileSystem();
        host.SetStandardInput("a\na\na\n");
        var exit = await Tool(source)
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine), CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(output, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        var literalCount = source.Count(character => character == '/') / 2;
        Assert.Equal(1 + literalCount, engine.Compiles);
        Assert.Equal(1 + literalCount, engine.Patterns.Count);
        Assert.All(engine.Patterns, pattern => Assert.True(pattern.Disposed));
        Assert.False(engine.Disposed);
    }

    [Fact]
    public async Task UnsupportedPatternFailsWithoutFallbackOrLaterEffects()
    {
        var engine = new RecordingEngine(Behavior.Unsupported);
        var host = new MockFileSystem();
        var exit = await Tool("BEGIN{r=\"a\";print (\"a\"~r),(\"b\"~r)}")
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine), CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: regular expression is unsupported by the supplied provider\n", host.GetOutput(AwkFileDescriptor.StdErr));
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
    [InlineData(Behavior.NoGroups, false)]
    [InlineData(Behavior.NoGroups, true)]
    [InlineData(Behavior.NegativeGroupCount, false)]
    [InlineData(Behavior.NegativeGroupCount, true)]
    public async Task InvalidGroupCountFailsBeforeLaterEffectsAndDisposesTheHandle(Behavior behavior, bool governed)
    {
        var engine = new RecordingEngine(behavior);
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var exit = await Tool("BEGIN{print \"before\";r=\"a\";print (\"a\"~r);print \"after\"} END{print \"end\"}")
            .ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: invalid regular expression\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(Assert.Single(engine.Patterns).Disposed);
        Assert.False(engine.Disposed);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreparedProgramReuseKeepsEngineCachesSeparate(bool literal)
    {
        var command = AwkTestHarness.BuildInvocation("awk", literal ? "BEGIN{print (\"a\"~/a/)}" : "BEGIN{r=\"a\";print (\"a\"~r)}");
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), command, CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var first = new RecordingEngine();
        var second = new RecordingEngine();
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
        Normal, Unsupported, RefuseCompile, RefuseMatch, BadByteRange, BadUtf16Range, SplitScalar,
        NoGroups, NegativeGroupCount
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
        public int GroupCount => behavior switch
        {
            Behavior.NoGroups => 0,
            Behavior.NegativeGroupCount => -1,
            _ => inner.GroupCount
        };

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
