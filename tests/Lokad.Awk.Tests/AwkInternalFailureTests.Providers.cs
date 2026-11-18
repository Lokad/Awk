using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk.Tests;

public sealed partial class AwkInternalFailureTests
{
    [Theory]
    [InlineData("compile")] [InlineData("groups")] [InlineData("boolean")]
    [InlineData("search")] [InlineData("match")] [InlineData("pattern-dispose")]
    [InlineData("search-dispose")]
    public async Task ProviderFaultIsNotALibraryBug(string stage)
    {
        var host = new MockFileSystem();
        var notifications = new List<Exception>();
        var budget = new CooperativeTestBudget(4 << 20);
        var policy = AwkTestHarness.CreatePolicy().WithRegexEngine(new FaultRegex(stage))
            .WithMemoryBudget(budget).WithInternalFailureObserver(notifications.Add);
        var program = stage == "boolean" ? "BEGIN{s=\"x\";print s~s}" :
            "BEGIN{s=\"x\";print match(s,s,a)}";
        var result = await AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", program))
            .ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, result);
        Assert.Empty(notifications);
        Assert.Equal("awk: execution failed without exposing host exception details\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("scope")] [InlineData("reserve")] [InlineData("resize")]
    [InlineData("reservation-dispose")] [InlineData("scope-dispose")]
    public async Task BudgetCallbackFaultIsNotALibraryBugAndRetiresFunding(string stage)
    {
        var host = new MockFileSystem();
        var notifications = new List<Exception>();
        var budget = new CooperativeTestBudget(4 << 20);
        var policy = AwkTestHarness.CreatePolicy().WithMemoryBudget(new FaultBudget(budget, stage))
            .WithInternalFailureObserver(notifications.Add);
        var result = await AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{print 1}"))
            .ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, result);
        Assert.Empty(notifications);
        Assert.Equal("awk: execution failed without exposing host exception details\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    private sealed class FaultRegex(string stage) : IAwkRegexEngine
    {
        private static void Fail() => throw new InvalidOperationException("private provider detail");

        public AwkRegexCompilationStatus TryCompile(ReadOnlySpan<byte> bytes, bool ignoreCase, IMemoryBudget? budget,
            CancellationToken token, out IAwkCompiledRegex? compiledPattern)
        {
            if (stage == "compile") Fail();
            var status = AwkUtf8RegexEngine.Instance.TryCompile(bytes, ignoreCase, budget, token, out var pattern);
            if (status != AwkRegexCompilationStatus.Compiled || pattern is null)
            { compiledPattern = pattern; return status; }
            compiledPattern = new Pattern(pattern, stage);
            return AwkRegexCompilationStatus.Compiled;
        }

        private sealed class Pattern(IAwkCompiledRegex pattern, string stage) : IAwkCompiledRegex
        {
            public int GroupCount
            {
                get { if (stage == "groups") Fail(); return pattern.GroupCount; }
            }
            public bool IsMatch(ReadOnlySpan<byte> bytes, CancellationToken token)
            { if (stage == "boolean") Fail(); return pattern.IsMatch(bytes, token); }
            public IAwkRegexSearch CreateSearch(ReadOnlyMemory<byte> bytes, CancellationToken token)
            { if (stage == "search") Fail(); return new Search(pattern.CreateSearch(bytes, token), stage); }
            public void Dispose()
            { pattern.Dispose(); if (stage == "pattern-dispose") Fail(); }
        }

        private sealed class Search(IAwkRegexSearch search, string stage) : IAwkRegexSearch
        {
            public AwkRegexCaptureRange Match(int start, Span<AwkRegexCaptureRange> captures, CancellationToken token)
            { if (stage == "match") Fail(); return search.Match(start, captures, token); }
            public void Dispose()
            { search.Dispose(); if (stage == "search-dispose") Fail(); }
        }
    }

    private sealed class FaultBudget(IMemoryBudget budget, string stage) : IMemoryBudget
    {
        private static void Fail() => throw new InvalidOperationException("private budget detail");
        public IMemoryScope CreateScope()
        { if (stage == "scope") Fail(); return new Scope(budget.CreateScope(), stage); }
        public bool TryReserve(long bytes, [NotNullWhen(true)] out IMemoryReservation? reservation) =>
            budget.TryReserve(bytes, out reservation);

        private sealed class Scope(IMemoryScope scope, string stage) : IMemoryScope
        {
            public IMemoryScope CreateScope() => scope.CreateScope();
            public bool TryReserve(long bytes, [NotNullWhen(true)] out IMemoryReservation? reservation)
            {
                if (stage == "reserve") Fail();
                if (!scope.TryReserve(bytes, out var retained)) { reservation = null; return false; }
                reservation = new Reservation(retained, stage);
                return true;
            }
            public void Dispose()
            { scope.Dispose(); if (stage == "scope-dispose") Fail(); }
        }

        private sealed class Reservation(IMemoryReservation reservation, string stage) : IMemoryReservation
        {
            public long Bytes => reservation.Bytes;
            public bool TryResize(long bytes)
            { if (stage == "resize") Fail(); return reservation.TryResize(bytes); }
            public void Dispose()
            { reservation.Dispose(); if (stage == "reservation-dispose") Fail(); }
        }
    }
}
