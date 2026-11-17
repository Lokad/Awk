namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> RegexLiteralBoundaryCases()
    {
        yield return Passing("regex-literal-leading-close",
            ["BEGIN{print (\"/\"~/[]/]/),(\"]\"~/[]/]/),(\"x\"~/[]/]/)}"], [], "1 1 0\n");
        yield return Passing("regex-literal-posix-class",
            ["BEGIN{print (\"/\"~/[[:alpha:]/]/),(\"a\"~/[[:alpha:]/]/),(\"7\"~/[[:alpha:]/]/)}"], [], "1 1 0\n");
        yield return Passing("regex-literal-negated-close",
            ["BEGIN{print (\"/\"~/[^]/]/),(\"x\"~/[^]/]/),(\"]\"~/[^]/]/)}"], [], "0 1 0\n");
        yield return Passing("regex-literal-literal-open",
            ["BEGIN{print (\"[\"~/[[]/),(\"/\"~/[[]/)}"], [], "1 0\n");
        yield return Passing("regex-literal-open-and-slash",
            ["BEGIN{print (\"[\"~/[[/]/),(\"/\"~/[[/]/),(\"a\"~/[[/]/)}"], [], "1 1 0\n");
    }
}
