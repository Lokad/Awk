namespace Lokad.Awk.Tests;

public sealed class AwkUtf8RegexAdapterTests
{
    [Theory]
    [InlineData("a", 1)]
    [InlineData("(a)(b)?", 3)]
    [InlineData("(?<word>a)(b)", 3)]
    [InlineData("(?<42>a)", 2)]
    public void GroupCountIncludesWholeMatchAndCountsDefinedGroups(string source, int expected)
    {
        using var pattern = Compile(source);
        Assert.Equal(expected, pattern.GroupCount);
    }

    [Fact]
    public void AdapterReturnsByteAndUtf16CaptureRangesWithoutTextCopies()
    {
        using var pattern = Compile("(é)(😀)?(z)?");
        Span<AwkRegexCaptureRange> groups = stackalloc AwkRegexCaptureRange[4];
        var whole = pattern.Match("xé😀z"u8, 0, groups, CancellationToken.None);
        Assert.Equal(4, pattern.GroupCount);
        Assert.Equal(new(true, 1, 7, 1, 4, true), whole);
        Assert.Equal(whole, groups[0]);
        Assert.Equal(new(true, 1, 2, 1, 1, true), groups[1]);
        Assert.Equal(new(true, 3, 4, 2, 2, true), groups[2]);
        Assert.Equal(new(true, 7, 1, 4, 1, true), groups[3]);
    }

    [Fact]
    public void AdapterKeepsMissingGroupsAndClearsSuppliedSlotsOnNoMatch()
    {
        using var pattern = Compile("(é)(😀)?(z)?");
        Span<AwkRegexCaptureRange> groups = stackalloc AwkRegexCaptureRange[4];
        Assert.True(pattern.Match("é"u8, 0, groups, CancellationToken.None).Success);
        Assert.False(groups[2].Success);
        Assert.False(groups[3].Success);
        Assert.False(pattern.Match("nothing"u8, 0, groups, CancellationToken.None).Success);
        foreach (var group in groups) Assert.Equal(default, group);
    }

    [Fact]
    public void SearchOffsetPreservesFullInputLookbehindAndAbsoluteCoordinates()
    {
        using var pattern = Compile("(?<=é)😀");
        var match = pattern.Match("xé😀"u8, 2, [], CancellationToken.None);
        Assert.Equal(new(true, 3, 4, 2, 2, true), match);
    }

    [Fact]
    public void EmptyEndMatchKeepsBothCoordinateSystems()
    {
        using var pattern = Compile("$");
        Assert.Equal(new(true, 6, 0, 3, 0, true),
            pattern.Match("é😀"u8, 3, [], CancellationToken.None));
    }

    [Fact]
    public void CallerCanRequestOnlyWholeMatchOrNoCaptureStorage()
    {
        using var pattern = Compile("(a)(b)");
        Span<AwkRegexCaptureRange> first = stackalloc AwkRegexCaptureRange[1];
        var whole = pattern.Match("zab"u8, 0, first, CancellationToken.None);
        Assert.Equal(whole, first[0]);
        Assert.Equal(whole, pattern.Match("zab"u8, 0, [], CancellationToken.None));
        Assert.True(pattern.IsMatch("zab"u8, CancellationToken.None));
    }

    [Theory]
    [InlineData("(?<42>é)(?<100>😀)?", "xé😀", 0)]
    [InlineData("(?<=é)(😀)?$", "é😀", 1)]
    [InlineData("(a|ab)(b)?", "zab", 0)]
    [InlineData("(x)?$", "é😀", 3)]
    public void RangeOnlyAndDetailedSearchKeepSelectionAndCoordinates(string source, string input, int offset)
    {
        using var pattern = Compile(source);
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var captures = new AwkRegexCaptureRange[pattern.GroupCount];
        var detailed = pattern.Match(bytes, offset, captures, CancellationToken.None);
        Assert.Equal(detailed, pattern.Match(bytes, offset, [], CancellationToken.None));
        Assert.Equal(detailed, captures[0]);
    }

    [Fact]
    public void SparseProviderGroupsFillDenseAwkSlots()
    {
        using var pattern = Compile("(?<42>é)(?<100>😀)?");
        Span<AwkRegexCaptureRange> captures = stackalloc AwkRegexCaptureRange[3];
        var whole = pattern.Match("xé😀"u8, 0, captures, CancellationToken.None);
        Assert.Equal(new(true, 1, 6, 1, 3, true), whole);
        Assert.Equal(new(true, 1, 2, 1, 1, true), captures[1]);
        Assert.Equal(new(true, 3, 4, 2, 2, true), captures[2]);
    }

    [Fact]
    public void RangeOnlyMissReturnsTheDefaultAwkRange()
    {
        using var pattern = Compile("missing");
        Assert.Equal(default, pattern.Match("é😀"u8, 0, [], CancellationToken.None));
    }

    [Fact]
    public void AdapterDoesNotJoinSurrogateAtomsAcrossGroups()
    {
        using var pattern = Compile(@"(\uD83D)(\uDE00)");
        Span<AwkRegexCaptureRange> groups = stackalloc AwkRegexCaptureRange[3];
        Assert.False(pattern.Match("😀"u8, 0, groups, CancellationToken.None).Success);
        foreach (var group in groups) Assert.Equal(default, group);
    }

    [Fact]
    public void CachedHandleUsesTheCurrentOperationToken()
    {
        using var compilation = new CancellationTokenSource();
        Assert.True(AwkUtf8RegexEngine.Instance.TryCompile("a", true, null, compilation.Token, out var pattern));
        using (pattern)
        {
            compilation.Cancel();
            Assert.True(pattern.IsMatch("A"u8, CancellationToken.None));
            using var operation = new CancellationTokenSource();
            operation.Cancel();
            var boolean = Assert.Throws<OperationCanceledException>(() => pattern.IsMatch("a"u8, operation.Token));
            Assert.Equal(operation.Token, boolean.CancellationToken);
            var capture = Assert.Throws<OperationCanceledException>(() => pattern.Match("a"u8, 0, [], operation.Token));
            Assert.Equal(operation.Token, capture.CancellationToken);
            Assert.True(pattern.Match("a"u8, 0, [], CancellationToken.None).Success);
        }
        var compileError = Assert.Throws<OperationCanceledException>(() => AwkUtf8RegexEngine.Instance.TryCompile(
            "a", false, null, compilation.Token, out _));
        Assert.Equal(compilation.Token, compileError.CancellationToken);
    }

    [Fact]
    public void DisposedHandleReleasesItsProducerAndRejectsFurtherUse()
    {
        var pattern = Compile("a");
        pattern.Dispose();
        pattern.Dispose();
        Assert.Throws<ObjectDisposedException>(() => pattern.IsMatch("a"u8, CancellationToken.None));
        Assert.Throws<ObjectDisposedException>(() => pattern.Match("a"u8, 0, [], CancellationToken.None));
    }

    private static IAwkCompiledRegex Compile(string source)
    {
        Assert.True(AwkUtf8RegexEngine.Instance.TryCompile(source, false, null, CancellationToken.None, out var pattern));
        return pattern;
    }
}
