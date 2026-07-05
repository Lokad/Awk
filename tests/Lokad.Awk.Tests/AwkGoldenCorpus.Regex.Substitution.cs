namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] RegexSubstitutionCases() =>
    [
        Passing(
            "gsub-mutates-current-line",
            ["NR==1{gsub(\"\\t\",\"<TAB>\"); print NR \":\" $0} NR==2{gsub(/\\t/,\"<TAB>\"); print NR \":\" $0}", "/file.txt"],
            [File("/file.txt", "a\tb\nx\ty\n")],
            "1:a<TAB>b\n2:x<TAB>y\n"),

        Passing(
            "sub-mutates-first-match-in-current-line",
            ["{sub(/a/,\"X\"); print $0}", "/file.txt"],
            [File("/file.txt", "banana\n")],
            "bXnana\n"),

        Passing(
            "sub-and-gsub-replacement-ampersand",
            ["NR==1{gsub(/[0-9]+/,\"<&>\"); print $0} NR==2{sub(/[0-9]+/,\"\\\\&\"); print $0}", "/file.txt"],
            [File("/file.txt", "a12b3\nc45d\n")],
            "a<12>b<3>\nc&d\n"),

        Passing(
            "sub-and-gsub-replacement-backslash-literals",
            ["BEGIN{s=\"abc\"; gsub(/b/,\"X\\\\qY\",s); print s; s=\"abc\"; gsub(/b/,\"X\\\\\\\\qY\",s); print s; s=\"abc\"; gsub(/b/,\"X\\\\1Y\",s); print s; s=\"abc\"; gsub(/b/,\"X\\\\\\\\&Y\",s); print s}"],
            [],
            "aX\\qYc\naX\\\\qYc\naX\\1Yc\naX\\bYc\n"),

        Passing(
            "gensub-capture-replacement",
            ["BEGIN{print gensub(/([a-z]+)([0-9]+)/,\"[\\\\2:\\\\1]\",\"g\",\"ab12 cd34\")}"],
            [],
            "[12:ab] [34:cd]\n"),

        Passing(
            "sub-and-gsub-explicit-targets",
            ["BEGIN{s=\"a1b2\"; gsub(/[0-9]/,\"X\",s); print s; sub(/X/,\"Y\",s); print s; a[\"k\"]=\"p1q2\"; gsub(/[0-9]/,\"Z\",a[\"k\"]); print a[\"k\"]} {$2=\"b2\"; gsub(/[0-9]/,\"X\",$2); print $0}", "/file.txt"],
            [File("/file.txt", "a b\n")],
            "aXbX\naYbX\npZqZ\na bX\n"),

        Passing(
            "sub-and-gsub-expression-return-values",
            ["BEGIN{s=\"banana\"; n=gsub(/a/,\"X\",s); print n, s; m=sub(/X/,\"Y\",s); print m, s; $0=\"foo\"; print sub(/z/,\"Q\"), $0}"],
            [],
            "3 bXnXnX\n1 bYnXnX\n0 foo\n"),

        CompilerDiagnostic(Failing(
            "sub-target-expression-fails",
            ["BEGIN{sub(/a/,\"X\",1+2)}"],
            [],
            2,
            string.Empty,
            "awk: argument 3 of `sub' must be an lvalue.\n")),

        Failing(
            "gsub-target-array-fails",
            ["BEGIN{a[1]=1; gsub(/1/,\"2\",a)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `a' in a scalar context\n"),

        Failing(
            "sub-invalid-dynamic-regex-fails-after-prior-output",
            ["BEGIN{print \"before\"; r=\"[+\"; s=\"abc\"; sub(r,\"X\",s); print \"after\"}"],
            [],
            2,
            "before\n",
            "awk: invalid regular expression\n"),

        Failing(
            "gsub-invalid-dynamic-regex-fails-after-prior-output",
            ["BEGIN{print \"before\"; r=\"[+\"; s=\"abc\"; gsub(r,\"X\",s); print \"after\"}"],
            [],
            2,
            "before\n",
            "awk: invalid regular expression\n"),

        Failing(
            "gensub-invalid-dynamic-regex-fails-after-prior-output",
            ["BEGIN{print \"before\"; r=\"[+\"; print gensub(r,\"X\",\"g\",\"abc\"); print \"after\"}"],
            [],
            2,
            "before\n",
            "awk: invalid regular expression\n")
    ];
}
