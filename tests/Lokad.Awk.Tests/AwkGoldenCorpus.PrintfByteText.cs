namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> PrintfByteTextCases()
    {
        foreach (var profile in new[] { "default", "posix", "traditional" })
        {
            string[] options = profile == "default" ? [] : ["--" + profile];
            yield return Passing("printf-evaluated-format-literal-backslashes-" + profile,
                [.. options, """
                    BEGIN {
                        printf "<%s>\\n", "x"; print "|"
                        print sprintf("<%s>\\t", "x")
                        printf "\\%s", "é"; printf "|\n"
                        printf "<%s>\\\\", "x"; print "|"
                        printf "end\\"
                    }
                    """], [], "<x>\\n|\n<x>\\t\n\\é|\n<x>\\\\|\nend\\");
            yield return Passing("printf-evaluated-format-dynamic-backslashes-" + profile,
                [.. options, """
                    BEGIN {
                        bs = sprintf("%c", 92)
                        f = "a" bs "q"; printf f; print "|"
                        f = "<%s>" bs "n"; print sprintf(f, "é")
                        f = bs "%s"; printf f, "x"; print "|"
                    }
                    """], [], "a\\q|\n<é>\\n\n\\x|\n");
        }

        yield return Passing("printf-signed-integer-digits-flags-fractions-and-boundaries",
            ["BEGIN{printf \"%+06d|% 6i|%#.4d|%.0d|%06d\\n\",7,-7.75,7,0,-7;printf \"%d|%d\\n\",9007199254740991,-9223372036854775808}"],
            [], "+00007|    -7|0007||-00007\n9007199254740991|-9223372036854775808\n");

        yield return Passing("printf-byte-text-fields",
            ["{printf \"<%s>|%s\\n\",$1,$2}"], [], "<café>|😀\n", "café 😀\n");
        yield return Passing("printf-byte-text-numeric-convfmt-and-sprintf",
            ["BEGIN{CONVFMT=\"%.3f\";n=7/4;printf \"%s|%s|%s\\n\",n,7,\"\";print sprintf(\"%s\",n)}"], [], "1.750|7|\n1.750\n", "");
        yield return Passing("printf-byte-text-positional-and-empty",
            ["BEGIN{printf \"%2$s|%1$s|%3$s\\n\",\"é\",\"😀\",\"\"}"], [], "😀|é|\n", "");
        yield return Passing("printf-byte-text-width-precision-character-controls",
            ["BEGIN{printf \"%5s|%.1s|%c|%-4s\\n\",\"界\",\"😀tail\",\"éx\",\"é\"}"], [], "    界|😀|é|é   \n", "");
        yield return Passing("printf-byte-text-byte-mode",
            ["-b", "{printf \"%s|%s\\n\",$1,$2}"], [], "café|😀\n", "café 😀\n");
        yield return Passing("printf-byte-text-snapshot-before-later-argument-effects",
            ["function mutate(){$0=\"new next\";return \"tail\"} {printf \"%s|%s|%s\\n\",$1,mutate(),$0}"], [], "old|tail|new next\n", "old rest\n");
    }
}
