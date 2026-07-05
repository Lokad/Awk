using System.Collections.Generic;

namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> RegexCases()
    {
        yield return Failing(
            "dynamic-invalid-regex-fails-at-runtime",
            ["BEGIN{print \"before\"; r=\"[\"; print \"x\" ~ r; print \"after\"}"],
            [],
            2,
            "before\n",
            "awk: invalid regular expression\n");

        yield return CompilerDiagnostic(Failing(
            "literal-invalid-regex-fails-before-output",
            ["BEGIN{print \"before\"; print \"x\" ~ /[z-a]/; print \"after\"}"],
            [],
            2,
            string.Empty,
            "awk: invalid regular expression\n"));

        yield return Posix(Passing(
            "regex-posix-ere-basics",
            ["BEGIN{print (\"abc\" ~ /^a.c$/), (\"bbb\" ~ /^ab*$/), (\"ac\" ~ /^ab?c$/), (\"abbbc\" ~ /^ab+c$/), (\"abc\" ~ /^(ab|xy)c$/), (\"m\" ~ /^[a-z]$/), (\"M\" ~ /^[^a-z]$/), (\"aaa\" ~ /^a{2,3}$/)}"],
            [],
            "1 0 1 1 1 1 1 1\n"));

        yield return Passing(
            "regex-posix-equivalence-and-collating-c-locale",
            ["BEGIN{print (\"a\" ~ /[[=a=]]/), (\"b\" ~ /[[=a=]]/), (\"a\" ~ /[[.a.]]/), (\"b\" ~ /[[.a.]]/)}"],
            [],
            "1 0 1 0\n");

        yield return Posix(Passing(
            "regex-quantifier-braces",
            ["match($0,/a{2}/) {print $0}", "/file.txt"],
            [File("/file.txt", "a\naa\naaa\n")],
            "aa\naaa\n"));

        yield return Passing(
            "regex-escaped-slash",
            ["/a\\/b/ {print $0}", "/file.txt"],
            [File("/file.txt", "a/b\nab\n")],
            "a/b\n");

        yield return Passing(
            "regex-awk-escape-translation",
            ["BEGIN{print (\"A\" ~ /\\101/), (\"A\" ~ /\\x41/), (\"a\\bb\" ~ /\\b/), (\"a+b\" ~ /a\\+b/)}"],
            [],
            "1 1 1 1\n");

        yield return Passing(
            "regex-valid-utf8-match-and-captures",
            ["BEGIN{print match(\"café\", /fé/), RSTART, RLENGTH; if(match(\"ab12\", /([a-z]+)([0-9]+)/, m)) print m[0], m[1], m[2], m[1,\"start\"], m[2,\"length\"]}"],
            [],
            "3 3 2\nab12 ab 12 1 2\n");

        yield return Passing(
            "regex-empty-match-reports-zero-length",
            ["BEGIN{print match(\"abc\", //), RSTART, RLENGTH}"],
            [],
            "1 1 0\n");

        yield return Passing(
            "regex-c-style-control-escapes",
            ["BEGIN{print (\"\\n\" ~ /\\n/), (\"\\t\" ~ /\\t/), (\"\\r\" ~ /\\r/), (\"\\f\" ~ /\\f/), (\"\\v\" ~ /\\v/), (\"\\a\" ~ /\\a/)}"],
            [],
            "1 1 1 1 1 1\n");

        yield return Posix(Passing(
            "regex-posix-character-classes",
            ["BEGIN{print (\"A9\" ~ /^[[:alnum:]]+$/), (\"_\" ~ /^[[:alpha:]]+$/), (\" \" ~ /^[[:space:]]$/), (\"f\" ~ /^[[:xdigit:]]$/)}"],
            [],
            "1 0 1 1\n"));

        yield return Passing(
            "regex-gnu-word-boundaries",
            ["BEGIN{print (\"ab cd\" ~ /\\<cd/), (\"abcd\" ~ /\\<cd/), (\"ab\" ~ /ab\\>/), (\"abc\" ~ /ab\\>/), (\"ab cd\" ~ /\\ycd/)}"],
            [],
            "1 0 1 0 1\n");

        yield return Passing(
            "regex-gnu-word-operators",
            ["BEGIN{print (\"a_b\" ~ /^\\w+$/), (\"-\" ~ /^\\W$/), (\"ab\" ~ /a\\B/), (\"a \" ~ /a\\B/)}"],
            [],
            "1 1 1 0\n");

        yield return Passing(
            "regex-gnu-buffer-operators-dynamic",
            ["BEGIN{start=\"\\\\\" sprintf(\"%c\",96) \"abc\"; end=\"abc\\\\\" sprintf(\"%c\",39); print (\"abc\" ~ start), (\"xabc\" ~ start), (\"abc\" ~ end), (\"abcx\" ~ end)}"],
            [],
            "1 0 1 0\n");

        yield return Passing(
            "regex-tagged-field-separator",
            ["BEGIN{FS=\"[,;]\"} {print NF, $1, $2, $3}", "/file.txt"],
            [File("/file.txt", "a,b;c\n")],
            "3 a b c\n");

        yield return Passing(
            "regex-split-separator-array",
            ["BEGIN{n=split(\"a,b;c\", a, \"[,;]\", s); print n, a[1], a[2], a[3], \"[\" s[0] \"]\", \"[\" s[1] \"]\", \"[\" s[2] \"]\", \"[\" s[3] \"]\"}"],
            [],
            "3 a b c [] [,] [;] []\n");

        yield return Passing(
            "regex-tagged-record-separator",
            ["BEGIN{RS=\"[,;]\"} {print NR \":\" $0}", "/file.txt"],
            [File("/file.txt", "a,b;c")],
            "1:a\n2:b\n3:c\n");
    }
}
