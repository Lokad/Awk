namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> PrintfByteTextCases()
    {
        foreach (var option in new[] { "default", "--posix", "--traditional", "-b", "--characters-as-bytes" })
        {
            string[] flags = option == "default" ? [] : [option];
            yield return Passing("printf-general-large-precision-no-padding-" + option,
                [.. flags, """
                    BEGIN {
                        printf "[%.1000000g]|[%.1000000G]\n", 1.25, 1.25
                        print sprintf("[%+.1000000g]|[%#.6g]", 1.25, 1.25)
                    }
                    """], [], "[1.25]|[1.25]\n[+1.25]|[1.25000]\n");
        }

        foreach (var option in new[] { "-b", "--characters-as-bytes" })
        {
            yield return Passing("printf-byte-units-width-precision-" + option,
                [option, """
                    BEGIN {
                        print length("é")
                        printf "[%5s]|[%.2s]\n", "é", "éx"
                        print sprintf("[%.4s]", "ééx")
                        printf "[%-6s]|[%4.0s]\n", "éé", "é"
                        printf "[%5s]|[%.2s]\n", "abc", "abc"
                        printf "[%6.4s]|", "😀x"
                        printf "[%2$*1$.*3$s]\n", 6, "ééx", 4
                    }
                    """], [], "2\n[   é]|[é]\n[éé]\n[éé  ]|[    ]\n[  abc]|[ab]\n[  😀]|[  éé]\n");
            yield return Failing("printf-byte-precision-rejects-partial-scalar-" + option,
                [option, "BEGIN{print \"before\";printf \"prefix %.1s\",\"éx\"}"], [],
                2, "before\n", "awk: invalid UTF-8 input\n");
            yield return Failing("sprintf-byte-precision-rejects-partial-scalar-" + option,
                [option, "BEGIN{print \"before\";print sprintf(\"%.3s\",\"ééx\")}"], [],
                2, "before\n", "awk: invalid UTF-8 input\n");
        }

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
