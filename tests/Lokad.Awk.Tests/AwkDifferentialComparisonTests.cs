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
}
