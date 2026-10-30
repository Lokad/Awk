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
    public void AdapterReturnsByteCaptureRangesWithoutTextCopies()
    {
        using var pattern = Compile("(é)(😀)?(z)?");
        using var search = pattern.CreateSearch(System.Text.Encoding.UTF8.GetBytes("xé😀z"), CancellationToken.None);
        Span<AwkRegexCaptureRange> groups = stackalloc AwkRegexCaptureRange[4];
        var whole = search.Match(0, groups, CancellationToken.None);
        Assert.Equal(4, pattern.GroupCount);
        Assert.Equal(new(true, 1, 7), whole);
        Assert.Equal(whole, groups[0]);
        Assert.Equal(new(true, 1, 2), groups[1]);
        Assert.Equal(new(true, 3, 4), groups[2]);
        Assert.Equal(new(true, 7, 1), groups[3]);
    }

    [Fact]
    public void AdapterKeepsMissingGroupsAndClearsSuppliedSlotsOnNoMatch()
    {
        using var pattern = Compile("(é)(😀)?(z)?");
        using var search = pattern.CreateSearch(System.Text.Encoding.UTF8.GetBytes("é"), CancellationToken.None);
        using var nothing = pattern.CreateSearch("nothing"u8.ToArray(), CancellationToken.None);
        Span<AwkRegexCaptureRange> groups = stackalloc AwkRegexCaptureRange[4];
        Assert.True(search.Match(0, groups, CancellationToken.None).Success);
        Assert.False(groups[2].Success);
        Assert.False(groups[3].Success);
        Assert.False(nothing.Match(0, groups, CancellationToken.None).Success);
        foreach (var group in groups) Assert.Equal(default, group);
    }

    [Fact]
    public void SearchOffsetPreservesFullInputLookbehindAndAbsoluteCoordinates()
    {
        using var pattern = Compile("(?<=é)😀");
        using var search = pattern.CreateSearch(System.Text.Encoding.UTF8.GetBytes("xé😀"), CancellationToken.None);
        var match = search.Match(3, [], CancellationToken.None);
        Assert.Equal(new(true, 3, 4), match);
    }

    [Fact]
    public void EmptyEndMatchUsesAbsoluteByteCoordinates()
    {
        using var pattern = Compile("$");
        using var search = pattern.CreateSearch(System.Text.Encoding.UTF8.GetBytes("é😀"), CancellationToken.None);
        Assert.Equal(new(true, 6, 0),
            search.Match(6, [], CancellationToken.None));
    }

    [Fact]
    public void CallerCanRequestOnlyWholeMatchOrNoCaptureStorage()
    {
        using var pattern = Compile("(a)(b)");
        using var search = pattern.CreateSearch(System.Text.Encoding.UTF8.GetBytes("zab"), CancellationToken.None);
        Span<AwkRegexCaptureRange> first = stackalloc AwkRegexCaptureRange[1];
        var whole = search.Match(0, first, CancellationToken.None);
        Assert.Equal(whole, first[0]);
        Assert.Equal(whole, search.Match(0, [], CancellationToken.None));
        Assert.True(pattern.IsMatch("zab"u8, CancellationToken.None));
    }

    [Theory]
    [InlineData("(?<42>é)(?<100>😀)?", "xé😀", 0)]
    [InlineData("(?<=é)(😀)?$", "é😀", 2)]
    [InlineData("(a|ab)(b)?", "zab", 0)]
    [InlineData("(x)?$", "é😀", 6)]
    public void RangeOnlyAndDetailedSearchKeepSelectionAndCoordinates(string source, string input, int offset)
    {
        using var pattern = Compile(source);
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        using var search = pattern.CreateSearch(bytes, CancellationToken.None);
        var captures = new AwkRegexCaptureRange[pattern.GroupCount];
        var detailed = search.Match(offset, captures, CancellationToken.None);
        Assert.Equal(detailed, search.Match(offset, [], CancellationToken.None));
        Assert.Equal(detailed, captures[0]);
    }

    [Fact]
    public void SparseProviderGroupsFillDenseAwkSlots()
    {
        using var pattern = Compile("(?<42>é)(?<100>😀)?");
        using var search = pattern.CreateSearch(System.Text.Encoding.UTF8.GetBytes("xé😀"), CancellationToken.None);
        Span<AwkRegexCaptureRange> captures = stackalloc AwkRegexCaptureRange[3];
        var whole = search.Match(0, captures, CancellationToken.None);
        Assert.Equal(new(true, 1, 6), whole);
        Assert.Equal(new(true, 1, 2), captures[1]);
        Assert.Equal(new(true, 3, 4), captures[2]);
    }

    [Fact]
    public void RangeOnlyMissReturnsTheDefaultAwkRange()
    {
        using var pattern = Compile("missing");
        using var search = pattern.CreateSearch(System.Text.Encoding.UTF8.GetBytes("é😀"), CancellationToken.None);
        Assert.Equal(default, search.Match(0, [], CancellationToken.None));
    }

    [Fact]
    public void AdapterDoesNotJoinSurrogateAtomsAcrossGroups()
    {
        using var pattern = Compile(@"(\uD83D)(\uDE00)");
        using var search = pattern.CreateSearch(System.Text.Encoding.UTF8.GetBytes("😀"), CancellationToken.None);
        Span<AwkRegexCaptureRange> groups = stackalloc AwkRegexCaptureRange[3];
        Assert.False(search.Match(0, groups, CancellationToken.None).Success);
        foreach (var group in groups) Assert.Equal(default, group);
    }

    [Fact]
    public void CachedHandleUsesTheCurrentOperationToken()
    {
        using var compilation = new CancellationTokenSource();
        Assert.True(AwkUtf8RegexEngine.Instance.TryCompile("a"u8, true, null, compilation.Token, out var pattern));
        using (pattern)
        {
            using var search = pattern.CreateSearch("a"u8.ToArray(), CancellationToken.None);
            compilation.Cancel();
            Assert.True(pattern.IsMatch("A"u8, CancellationToken.None));
            using var operation = new CancellationTokenSource();
            operation.Cancel();
            var boolean = Assert.Throws<OperationCanceledException>(() => pattern.IsMatch("a"u8, operation.Token));
            Assert.Equal(operation.Token, boolean.CancellationToken);
            var capture = Assert.Throws<OperationCanceledException>(() => search.Match(0, [], operation.Token));
            Assert.Equal(operation.Token, capture.CancellationToken);
            Assert.True(search.Match(0, [], CancellationToken.None).Success);
        }
        var compileError = Assert.Throws<OperationCanceledException>(() => AwkUtf8RegexEngine.Instance.TryCompile(
            "a"u8, false, null, compilation.Token, out _));
        Assert.Equal(compilation.Token, compileError.CancellationToken);
    }

    [Fact]
    public void DisposedHandleReleasesItsProducerAndRejectsFurtherUse()
    {
        var pattern = Compile("a");
        pattern.Dispose();
        pattern.Dispose();
        Assert.Throws<ObjectDisposedException>(() => pattern.IsMatch("a"u8, CancellationToken.None));
        Assert.Throws<ObjectDisposedException>(() => pattern.CreateSearch("a"u8.ToArray(), CancellationToken.None));
    }

    private static IAwkCompiledRegex Compile(string source)
    {
        Assert.True(AwkUtf8RegexEngine.Instance.TryCompile(System.Text.Encoding.UTF8.GetBytes(source), false, null, CancellationToken.None, out var pattern));
        return pattern;
    }

    [Fact]
    public void ScopedByteRestartsAreIndependentAndMayMoveInEitherDirection()
    {
        using var pattern = Compile("😀|é");
        using var construction = new CancellationTokenSource();
        using var first = pattern.CreateSearch(System.Text.Encoding.UTF8.GetBytes("😀é😀"), construction.Token);
        using var second = pattern.CreateSearch(System.Text.Encoding.UTF8.GetBytes("é😀"), CancellationToken.None);
        construction.Cancel();
        Assert.Equal(new(true, 6, 4), first.Match(6, [], CancellationToken.None));
        Assert.Equal(new(true, 4, 2), first.Match(4, [], CancellationToken.None));
        Assert.Equal(new(true, 2, 4), second.Match(2, [], CancellationToken.None));
        Assert.Equal(new(true, 0, 4), first.Match(0, [], CancellationToken.None));
        Assert.Throws<ArgumentException>(() => first.Match(1, [], CancellationToken.None));
        first.Dispose();
        first.Dispose();
        Assert.Throws<ObjectDisposedException>(() => first.Match(0, [], CancellationToken.None));
    }
}
