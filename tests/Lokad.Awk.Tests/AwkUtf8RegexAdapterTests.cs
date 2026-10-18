namespace Lokad.Awk.Tests;

public sealed class AwkUtf8RegexAdapterTests
{
    [Fact]
    public void AdapterReturnsByteAndUtf16CaptureRangesWithoutTextCopies()
    {
        using var pattern = Compile("(é)(😀)?(z)?");
        Span<AwkRegexCaptureRange> groups = stackalloc AwkRegexCaptureRange[4];
        var whole = pattern.Match("xé😀z"u8, 0, groups, static () => { });
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
        Assert.True(pattern.Match("é"u8, 0, groups, static () => { }).Success);
        Assert.False(groups[2].Success);
        Assert.False(groups[3].Success);
        Assert.False(pattern.Match("nothing"u8, 0, groups, static () => { }).Success);
        foreach (var group in groups) Assert.Equal(default, group);
    }

    [Fact]
    public void SearchOffsetPreservesFullInputLookbehindAndAbsoluteCoordinates()
    {
        using var pattern = Compile("(?<=é)😀");
        var match = pattern.Match("xé😀"u8, 2, [], static () => { });
        Assert.Equal(new(true, 3, 4, 2, 2, true), match);
    }

    [Fact]
    public void EmptyEndMatchKeepsBothCoordinateSystems()
    {
        using var pattern = Compile("$");
        Assert.Equal(new(true, 6, 0, 3, 0, true),
            pattern.Match("é😀"u8, 3, [], static () => { }));
    }

    [Fact]
    public void CallerCanRequestOnlyWholeMatchOrNoCaptureStorage()
    {
        using var pattern = Compile("(a)(b)");
        Span<AwkRegexCaptureRange> first = stackalloc AwkRegexCaptureRange[1];
        var whole = pattern.Match("zab"u8, 0, first, static () => { });
        Assert.Equal(whole, first[0]);
        Assert.Equal(whole, pattern.Match("zab"u8, 0, [], static () => { }));
        Assert.True(pattern.IsMatch("zab"u8, static () => { }));
    }

    [Fact]
    public void AdapterReportsSurrogateSplitCapturesBeforeAwkPublication()
    {
        using var pattern = Compile(@"(\uD83D)(\uDE00)");
        Span<AwkRegexCaptureRange> groups = stackalloc AwkRegexCaptureRange[3];
        Assert.True(pattern.Match("😀"u8, 0, groups, static () => { }).IsByteAligned);
        Assert.False(groups[1].IsByteAligned);
        Assert.False(groups[2].IsByteAligned);
    }

    [Fact]
    public void CompileAndMatchObserveCancellationWithoutRetainingCompileCheckpoint()
    {
        var armed = false;
        void Checkpoint()
        {
            if (armed) throw new OperationCanceledException();
        }
        Assert.True(AwkUtf8RegexEngine.Instance.TryCompile("a", true, TimeSpan.FromSeconds(1),
            null, Checkpoint, out var pattern));
        using (pattern)
        {
            armed = true;
            Assert.True(pattern.IsMatch("A"u8, static () => { }));
            Assert.Throws<OperationCanceledException>(() => pattern.IsMatch("a"u8, Checkpoint));
            Assert.Throws<OperationCanceledException>(() => pattern.Match("a"u8, 0, [], Checkpoint));
        }
        Assert.Throws<OperationCanceledException>(() => AwkUtf8RegexEngine.Instance.TryCompile(
            "a", false, TimeSpan.FromSeconds(1), null, Checkpoint, out _));
    }

    [Fact]
    public void DisposedHandleReleasesItsProducerAndRejectsFurtherUse()
    {
        var pattern = Compile("a");
        pattern.Dispose();
        pattern.Dispose();
        Assert.Throws<ObjectDisposedException>(() => pattern.IsMatch("a"u8, static () => { }));
        Assert.Throws<ObjectDisposedException>(() => pattern.Match("a"u8, 0, [], static () => { }));
    }

    private static IAwkCompiledRegex Compile(string source)
    {
        Assert.True(AwkUtf8RegexEngine.Instance.TryCompile(source, false, TimeSpan.FromSeconds(1),
            null, static () => { }, out var pattern));
        return pattern;
    }
}
