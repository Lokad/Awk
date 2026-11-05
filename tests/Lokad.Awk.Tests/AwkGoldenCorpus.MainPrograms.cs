namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> MainProgramCases()
    {
        var rules = string.Concat(Enumerable.Range(1, 37).Select(index =>
            "{a[\"k" + index + "\"]+=$2+" + index + ";s+=" + index + "}"));
        yield return Passing("many-rules-preserve-constants-and-shared-updates",
            [rules + "END{print a[\"k1\"],a[\"k37\"],s,typeof(a[\"k1\"])}"],
            [], "12 84 1406 number\n", "k 0007\nk 3\n");
        yield return Passing("nonlocal-next-skips-later-rules",
            ["function bump(k){a[k]++;if(NR%2)next}{a[$1]+=$2}{bump($1);a[$1]+=100}{tail++}END{print a[\"a\"],a[\"b\"],tail}"],
            [], "18 109 1\n", "a 0007\nb 8\na 9\n");
    }
}
