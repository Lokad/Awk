using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkRegexCacheAdmissionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetainedPatternKeysRequireAdmissionIndependentOfProviderStorage(bool sufficientMemory)
    {
        const int records = 64;
        var input = new StringBuilder();
        for (var index = 0; index < records; index++)
            input.Append('[').Append('x', 30_000 + index).Append("]\n");
        var host = new MockFileSystem();
        host.SetStandardInput(input.ToString());
        var budget = new CooperativeTestBudget(sufficientMemory ? 64L << 20 : 1L << 20);
        var command = AwkTestHarness.BuildInvocation("awk", "{print (\"x\" ~ $0)} END{print \"completed\"}");
        Assert.True(Awk.TryParse(command, out var awk));
        var exit = await awk.ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget).WithRegexEngine(new CompactClassEngine()),
            CancellationToken.None);
        if (sufficientMemory)
        {
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal(string.Concat(Enumerable.Repeat("1\n", records)) + "completed\n",
                host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        }
        else
        {
            Assert.Equal(AwkExitCode.FatalError, exit);
            Assert.DoesNotContain("completed", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        }
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void EvictedKeysStayFundedUntilTheirActiveSearchFinishes()
    {
        using var memory = new CooperativeTestMemory(16L << 20);
        using var runtime = new AwkRegexRuntime(static () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, static () => { }, AwkRuntimeCounters.Disabled,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled),
            memory.Accounting, regexEngine: new CompactClassEngine(), memoryBudget: memory.Budget);
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes); // Cache storage is lazy.
        var source = AwkText.FromSourceLiteralString("[" + new string('x', 30_000) + "]");
        var regex = runtime.Create(new AwkRegexPattern(source));
        using var search = runtime.OpenSearch(regex, AwkText.FromSourceLiteralString("x"));
        for (var index = 0; index < AwkRuntimeLimits.DefaultRegexCacheSize + 1; index++)
            runtime.Create(new AwkRegexPattern("[" + new string('x', 4_000 + index) + "]"));
        runtime.Dispose();
        Assert.True(memory.Accounting.CurrentCommittedBytes >= AwkRuntimeMemory.EstimateTextBytes(source));
        Assert.True(search.MatchSpan(0).Success);
        search.Dispose();
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    // A conforming compact adapter for a tiny ERE subset. Redundant class entries do
    // not require a large program or retained source; the published provider does all
    // matching. This separates core key ownership from provider storage estimates.
    private sealed class CompactClassEngine : IAwkRegexEngine
    {
        public bool TryCompile(ReadOnlySpan<byte> patternUtf8, bool ignoreCase, IMemoryBudget? memoryBudget,
            CancellationToken cancellationToken, [NotNullWhen(true)] out IAwkCompiledRegex? compiledPattern)
        {
            compiledPattern = null;
            cancellationToken.ThrowIfCancellationRequested();
            if (patternUtf8.Length < 3 || patternUtf8[0] != (byte)'[' || patternUtf8[^1] != (byte)']')
                return false;
            for (var index = 1; index < patternUtf8.Length - 1; index++)
            {
                if ((index & 16383) == 0) cancellationToken.ThrowIfCancellationRequested();
                if (patternUtf8[index] != (byte)'x') return false;
            }
            return AwkUtf8RegexEngine.Instance.TryCompile("x"u8, ignoreCase, memoryBudget,
                cancellationToken, out compiledPattern);
        }
    }
}
