using System.Text.RegularExpressions;

namespace Lokad.Awk.Tests;

public sealed class AwkRegexProviderOwnershipTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LiteralAndDynamicProviderHandlesRequireExecutionAdmission(bool literal)
    {
        var pattern = new string('a', 1024);
        var source = literal
            ? $"BEGIN{{print \"before\";print (\"x\"~/{pattern}/);print \"after\"}}"
            : $"BEGIN{{r=\"{pattern}\";print \"before\";print (\"x\"~r);print \"after\"}}";
        var budget = new CooperativeTestBudget(16 << 20);
        IMemoryReservation? pressure = null;
        var host = new MockFileSystem
        {
            AfterAppend = () =>
            {
                if (pressure is not null) return;
                Assert.True(budget.TryReserve(budget.Limit - budget.Reserved - (16 << 10), out var reserved));
                pressure = reserved;
            }
        };
        try
        {
            var command = AwkTestHarness.BuildInvocation("awk", source);
            Assert.True(Awk.TryParse(command, out var awk));
            var exit = await awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget),
                CancellationToken.None);
            Assert.Equal(AwkExitCode.FatalError, exit);
            Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
        }
        finally { pressure?.Dispose(); }
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void EvictedProviderHandleRefundsAndAnOutstandingRegexCanRecompile()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        using (var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, static () => { }, AwkRuntimeCounters.Disabled,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled),
            memoryBudget: budget, regexEngine: AwkUtf8RegexEngine.Instance))
        {
            var first = runtime.Create(new AwkRegexPattern("a"));
            Assert.True(runtime.IsMatch(first, AwkText.FromString("a")));
            for (var index = 0; index <= 8 * AwkRuntimeLimits.DefaultRegexCacheSize; index++)
                runtime.Create(new AwkRegexPattern("key" + index));
            Assert.True(runtime.IsMatch(first, AwkText.FromString("a")));
            Assert.True(budget.Reserved > 0);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void Utf8ProviderFundsItsHandleWithoutDisposingTheBorrowedBudget()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        Assert.True(budget.TryReserve(17, out var borrowed));
        using (borrowed)
        {
            Assert.True(AwkUtf8RegexEngine.Instance.TryCompile("a", false, budget,
                CancellationToken.None, out var pattern));
            using (pattern)
            {
                Assert.True(budget.Reserved > borrowed.Bytes);
                Assert.True(pattern.IsMatch("a"u8, CancellationToken.None));
            }
            Assert.Equal(borrowed.Bytes, budget.Reserved);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void Utf8ProviderRefusalDoesNotBecomeAnUnsupportedPattern()
    {
        var budget = new CooperativeTestBudget(1);
        Assert.Throws<AwkRegexAdmissionException>(() => AwkUtf8RegexEngine.Instance.TryCompile(
            "a", false, budget, CancellationToken.None, out _));
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void Utf8MatchRefusalPreservesItsLiveHandleAndTemporaryStorageRefunds()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        Assert.True(AwkUtf8RegexEngine.Instance.TryCompile("a", false, budget,
            CancellationToken.None, out var pattern));
        using (pattern)
        {
            var retained = budget.Reserved;
            Assert.True(budget.TryReserve(budget.Limit - retained - 1, out var pressure));
            using (pressure)
                Assert.Throws<AwkRegexAdmissionException>(() => pattern.IsMatch("a"u8, CancellationToken.None));
            Assert.Equal(retained, budget.Reserved);
            Assert.True(pattern.IsMatch("a"u8, CancellationToken.None));
            Assert.Equal(retained, budget.Reserved);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task RestrictedExecutionLetsTheProviderAcceptBroadCapturePatterns()
    {
        var pattern = string.Concat(Enumerable.Repeat("(a)", 300));
        var input = new string('a', 300);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            [$"BEGIN{{r=\"{pattern}\";print (\"{input}\"~r)}}"], []);
        Assert.True(Awk.TryParse(command, out var awk));
        var host = new MockFileSystem();
        var exit = await awk.ExecuteAsync(host, AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance), CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
    }
}
