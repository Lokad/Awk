using System.Text.RegularExpressions;

namespace Lokad.Awk.Tests;

public sealed class AwkRegexProviderOwnershipTests
{
    [Fact]
    public void DefaultProviderOwnsAdmissionAndRefundsInvalidConstruction()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        using (var pattern = AwkDotNetRegexPattern.Compile("a+", RegexOptions.CultureInvariant, budget))
        {
            Assert.True(budget.Reserved > 0);
            Assert.Matches(pattern, "aaa");
            Assert.Equal(TimeSpan.FromSeconds(2), pattern.MatchTimeout);
        }
        Assert.Equal(0, budget.Reserved);
        Assert.ThrowsAny<ArgumentException>(() => AwkDotNetRegexPattern.Compile("(", RegexOptions.None, budget));
        Assert.Equal(0, budget.Reserved);
        Assert.Throws<AwkRuntimeException>(() => AwkDotNetRegexPattern.Compile("a", RegexOptions.None,
            new CooperativeTestBudget(1)));
    }

    [Fact]
    public void EvictedDefaultHandleRefundsAndAnOutstandingRegexCanRecompile()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        using (var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, static () => { }, AwkRuntimeCounters.Disabled,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled),
            memoryBudget: budget))
        {
            var first = runtime.Create(new AwkRegexPattern("a"));
            var backend = Assert.IsType<AwkDotNetRegexBackend>(first.Backend);
            Assert.NotNull(backend.Value);
            for (var index = 0; index <= AwkRuntimeLimits.DefaultRegexCacheSize; index++)
                runtime.Create(new AwkRegexPattern("key" + index));
            Assert.Null(backend.Value);
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
        var exit = await awk.ExecuteAsync(host, AwkExecutionPolicy.CreateRestricted(), CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
    }
}
