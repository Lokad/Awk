using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkDifferentialComparisonTests
{
    private static AwkGoldenCase Golden => AwkGoldenCorpus.DirectCases.Single(test => test.Name == "idiom-sum-a-column");
    private static AwkDifferentialObservation Observe(int status, string output, string error = "") =>
        new(status, Encoding.UTF8.GetBytes(output), Encoding.UTF8.GetBytes(error));

    [Fact]
    public void StoredGoldenAgreementCannotHideDifferentLiveNativeOutput()
    {
        var test = new AwkDifferentialCase(Golden);
        var differences = AwkDifferentialTests.Compare(test, Observe(0, Golden.ExpectedStdOut), Observe(0, "wrong\n"));
        Assert.Contains("stdout differs", differences);
    }

    [Fact]
    public void MatchingFailuresCannotPassAnExpectedSuccessfulProgram()
    {
        var test = new AwkDifferentialCase(Golden);
        var failure = Observe(2, "", "failed\n");
        var differences = AwkDifferentialTests.Compare(test, failure, failure);
        Assert.Contains(differences, difference => difference.StartsWith("Awk status", StringComparison.Ordinal));
        Assert.Contains(differences, difference => difference.StartsWith("GNU status", StringComparison.Ordinal));
    }

    [Fact]
    public void NativeStatusExceptionsAreExplicitAndLimitedToTheirDeclaredValue()
    {
        Assert.Throws<ArgumentException>(() => new AwkDifferentialCase(Golden, 1));
        var test = new AwkDifferentialCase(Golden, 1, "Synthetic comparison contract.");
        Assert.Empty(AwkDifferentialTests.Compare(test, Observe(0, "ok"), Observe(1, "ok")));
        Assert.NotEmpty(AwkDifferentialTests.Compare(test, Observe(0, "ok"), Observe(2, "ok")));
    }

    [Fact]
    public void DiagnosticsCompareClassesWhileGoldenTestsKeepExactMessages()
    {
        var diagnostic = AwkGoldenCorpus.DirectCases.Single(test =>
            test.Name == "computed-field-validates-builtin-arity-before-effects");
        var test = new AwkDifferentialCase(diagnostic, 1, "GNU compile diagnostic status.");
        Assert.Empty(AwkDifferentialTests.Compare(test, Observe(2, "", "awk: invalid call\n"),
            Observe(1, "", "gawk: source location: invalid call\n")));
        Assert.NotEmpty(AwkDifferentialTests.Compare(test, Observe(2, "", "awk: invalid call\n"), Observe(1, "")));
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void ExpectedFilesMustExistEvenWhenBothImplementationsOmitThem(bool actualPresent, bool nativePresent)
    {
        var golden = AwkGoldenCorpus.DirectCases.Single(test => test.Name == "idiom-file-output-truncate-append");
        var file = Assert.Single(golden.ExpectedFiles);
        var actual = Observe(0, golden.ExpectedStdOut) with
        { Files = new Dictionary<string, byte[]?> { [file.Path] = actualPresent ? Encoding.UTF8.GetBytes(file.Content) : null } };
        var native = Observe(0, golden.ExpectedStdOut) with
        { Files = new Dictionary<string, byte[]?> { [file.Path] = nativePresent ? Encoding.UTF8.GetBytes(file.Content) : null } };
        var differences = AwkDifferentialTests.Compare(new AwkDifferentialCase(golden), actual, native);
        Assert.Equal((actualPresent ? 0 : 1) + (nativePresent ? 0 : 1), differences.Count);
        if (!actualPresent) Assert.Contains("Awk missing expected file: " + file.Path, differences);
        if (!nativePresent) Assert.Contains("GNU missing expected file: " + file.Path, differences);
    }

    [Fact]
    public void MatchingStreamsCannotHideDifferentOutputFileBytes()
    {
        var golden = AwkGoldenCorpus.DirectCases.Single(test => test.Name == "idiom-file-output-truncate-append");
        var path = Assert.Single(golden.ExpectedFiles).Path;
        var actual = Observe(0, golden.ExpectedStdOut) with
        { Files = new Dictionary<string, byte[]?> { [path] = Encoding.UTF8.GetBytes("a\0\u00e8\n") } };
        var native = Observe(0, golden.ExpectedStdOut) with
        { Files = new Dictionary<string, byte[]?> { [path] = Encoding.UTF8.GetBytes("a\0\u00e9\n") } };
        Assert.Contains("file bytes differ: " + path,
            AwkDifferentialTests.Compare(new AwkDifferentialCase(golden), actual, native));
    }
}
