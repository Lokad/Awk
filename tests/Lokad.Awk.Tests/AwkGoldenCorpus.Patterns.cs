namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] PatternCases() =>
    [
        Passing(
            "regex-filter-and-nr",
            ["/foo/ {print NR \":\" $0}", "/file.txt"],
            [File("/file.txt", "foo\nbar\nfood\n")],
            "1:foo\n3:food\n"),

        Passing(
            "regex-pattern-default-print",
            ["/foo/", "/file.txt"],
            [File("/file.txt", "foo\nbar\nfood\n")],
            "foo\nfood\n"),

        Passing(
            "regex-match-expression-literal-and-dynamic",
            ["BEGIN{pat=\"ba.\"; print (\"bar\" ~ /ba./), (\"foo\" !~ /ba./), (\"bar\" ~ pat), (\"foo\" !~ pat)}"],
            [],
            "1 1 1 1\n"),

        Passing(
            "regex-pattern-ignorecase-default-print",
            ["BEGIN{IGNORECASE=1} /foo/", "/file.txt"],
            [File("/file.txt", "Foo\nbar\nFOOD\n")],
            "Foo\nFOOD\n"),

        Passing(
            "regex-range-pattern",
            ["/start/,/end/ {print $0}", "/file.txt"],
            [File("/file.txt", "a\nstart\nx\nend\ny\nstart end\nz\n")],
            "start\nx\nend\nstart end\n"),

        Passing(
            "expression-range-pattern",
            ["NR==2, NR==4 {print NR \":\" $0}", "/file.txt"],
            [File("/file.txt", "a\nb\nc\nd\ne\n")],
            "2:b\n3:c\n4:d\n"),

        Passing(
            "expression-range-pattern-default-print",
            ["NR==2, NR==3", "/file.txt"],
            [File("/file.txt", "a\nb\nc\nd\n")],
            "b\nc\n"),

        Passing(
            "expression-range-pattern-same-record-reset",
            ["NR==2, NR==2 {print NR \":\" $0}", "/file.txt"],
            [File("/file.txt", "a\nb\nc\n")],
            "2:b\n"),

        Passing(
            "begin-end",
            ["BEGIN{print \"start\"} {print $1} END{print NR}", "/file.txt"],
            [File("/file.txt", "a b\nc d\n")],
            "start\na\nc\n2\n"),

        Passing(
            "missing-pattern-runs-every-record",
            ["{print NR, $1}", "/file.txt"],
            [File("/file.txt", "a\nb\n")],
            "1 a\n2 b\n"),

        Passing(
            "condition-with-printf",
            ["NR>=2 && NR<=3 {printf \"%5d %s\\n\", NR, $0}", "/file.txt"],
            [File("/file.txt", "zero\none\ntwo\nthree\n")],
            "    2 one\n    3 two\n"),

        Passing(
            "variables-and-if",
            ["BEGIN{max=0} {if($1>max){max=$1;file=$2}} END{print max, file}", "/file.txt"],
            [File("/file.txt", "10 a\n5 b\n20 c\n")],
            "20 c\n"),

        Passing(
            "condition-only-default-print",
            ["NF>0", "/file.txt"],
            [File("/file.txt", "alpha\n\n  \nbeta gamma\n")],
            "alpha\nbeta gamma\n"),

        Passing(
            "expression-patterns",
            ["length($0)>3 {print \"len\", NR} NR%2==0 {print \"even\", NR}", "/file.txt"],
            [File("/file.txt", "a\nbbbb\nccc\ndddd\n")],
            "len 2\neven 2\nlen 4\neven 4\n"),

        Passing(
            "function-call-expression-pattern",
            ["function keep(value){return value ~ /^a/} keep($1) {print $2}", "/file.txt"],
            [File("/file.txt", "a1 one\nb2 two\na3 three\n")],
            "one\nthree\n"),

        Passing(
            "composed-parenthesized-pattern",
            ["($1==\"a\" || $2==\"y\") && !($3==\"skip\") {print NR}", "/file.txt"],
            [File("/file.txt", "a x ok\nb y ok\na y skip\nb z ok\n")],
            "1\n2\n"),

        Passing(
            "matching-rules-run-in-source-order",
            ["NR==1{print \"first\"} NR==1{print \"second\"} {print \"all\", NR}", "/file.txt"],
            [File("/file.txt", "one\ntwo\n")],
            "first\nsecond\nall 1\nall 2\n"),

        Passing(
            "or-condition-without-whitespace",
            ["NR==2||NR==4{print}", "/file.txt"],
            [File("/file.txt", "zero\none\ntwo\nthree\n")],
            "one\nthree\n")
    ];
}
