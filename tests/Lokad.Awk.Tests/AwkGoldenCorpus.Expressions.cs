namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] ExpressionCases() =>
    [
        Posix(Passing(
            "arithmetic-operators",
            ["BEGIN{print 7+2, 7-2, 7*2, 7/2, 7%2}"],
            [],
            "9 5 14 3.5 1\n")),

        Passing(
            "concat-function-field-unary-precedence",
            ["function f(){return \"B\"} BEGIN{$0=\"x y\"; n=2; print \"A\" f(), $n \"!\", -1 \"x\", \"v=\" 1+2}"],
            [],
            "AB y! -1x v=3\n"),

        Passing(
            "explicit-line-continuation-in-expression-and-string",
            ["BEGIN{print 1 + \\\n2; print \"a\\\nb\"}"],
            [],
            "3\nab\n"),

        Posix(Passing(
            "chained-assignment-expression",
            ["BEGIN{print (a = b = 7), a, b; c += d = 2; print c, d}"],
            [],
            "7 7 7\n2 2\n")),

        Passing(
            "assignment-expression-dynamic-target-order",
            ["BEGIN{i=1; a[i]=i=2; print i, \"[\" a[1] \"]\", a[2]; i=1; $0=\"x y\"; $(i)=i=2; print i, $0, $1, $2}"],
            [],
            "2 [] 2\n2 x 2 x 2\n"),

        Passing(
            "ambiguous-concat-comparison-and-regex-match",
            ["BEGIN{a=10; b=2; c=3; x=\"a\"; y=\"b\"; print (a < b c), (x y ~ /ab/), (x y ~ /a b/)}"],
            [],
            "1 1 0\n"),

        Passing(
            "redirection-looking-tokens-in-expression-contexts",
            ["BEGIN{s=\"a > b |& c >> d\"; print s; print (\"a>b\" ~ />/), (\"|\" ~ /[>|&]/); print index(\"x >> y\", \">>\"); print (1 ? \"x > y\" : \"z |& q\"); switch(\">\") {case \">\": print \"switch\"; default: print \"bad\"} if(1){print \"nested >>\"}}"],
            [],
            "a > b |& c >> d\n1 1\n3\nx > y\nswitch\nbad\nnested >>\n"),

        Posix(Passing(
            "nested-ternary-associativity",
            ["BEGIN{print (0 ? 1 : 0 ? 2 : 3), (1 ? 0 ? 2 : 3 : 4)}"],
            [],
            "3 3\n")),

        Passing(
            "unary-and-exponent-precedence",
            ["BEGIN{print -2^2, (-2)^2, 2^-2, 2^3^2}"],
            [],
            "-4 4 0.25 512\n"),

        Passing(
            "membership-precedence-with-comparison-logical-and-ternary",
            ["BEGIN{a[\"x\"]=1; print (\"x\" in a == 1), ((\"x\" in a)==1), (\"y\" in a || 1), (0 ? \"x\" in a : 7)}"],
            [],
            "1 1 1 7\n")
    ];
}
