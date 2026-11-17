namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> UnicodeRecordSeparatorCases()
    {
        yield return Passing("review-unicode-record-separator-class",
            ["BEGIN{RS=\"[é,]\"}{print NR \": \" $0}"], [], "1: a\n2: b\n3: c\n4: d\n", "aébéc,d");
        yield return Passing("review-unicode-record-separator-alternative",
            ["BEGIN{RS=\",|[é]\"}{print NR \": \" $0}"], [], "1: a\n2: b\n3: c\n4: d\n", "aébéc,d");
        yield return Passing("review-unicode-record-separator-supplementary",
            ["BEGIN{RS=\"[😀,]\"}{print NR \": \" $0}"], [], "1: a\n2: b\n3: c\n4: d\n", "a😀b😀c,d");
    }
}
