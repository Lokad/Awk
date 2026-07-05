namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] DiagnosticCases() =>
    [
        Posix(Failing(
            "invalid-delete-scalar-fails",
            ["BEGIN{a=1; delete a}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n")),

        Failing(
            "invalid-delete-scalar-element-fails",
            ["BEGIN{a=1; delete a[1]}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n"),

        Failing(
            "invalid-delete-field-fails",
            ["BEGIN{delete $1}"],
            [],
            2,
            string.Empty,
            "awk: invalid delete target.\n"),

        Failing(
            "invalid-delete-parenthesized-target-fails",
            ["BEGIN{a[1]=1; delete (a[1])}"],
            [],
            2,
            string.Empty,
            "awk: invalid delete target.\n"),

        Failing(
            "invalid-delete-multiple-targets-fails",
            ["BEGIN{a[1]=1; delete a[1], a[2]}"],
            [],
            2,
            string.Empty,
            "awk: delete expects a single target.\n"),

        Failing(
            "reserved-keyword-variable-fails",
            ["BEGIN{if=1}"],
            [],
            2,
            string.Empty,
            "awk: expected if condition.\n"),

        Failing(
            "reserved-builtin-variable-fails",
            ["BEGIN{length=1}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n"),

        Failing(
            "reserved-builtin-bare-expression-fails",
            ["BEGIN{print rand}"],
            [],
            2,
            string.Empty,
            "awk: function `rand' requires a call.\n"),

        Failing(
            "reserved-keyword-function-name-fails",
            ["function if(){return 1} BEGIN{print if()}"],
            [],
            2,
            string.Empty,
            "awk: expected function name.\n"),

        Failing(
            "reserved-builtin-function-name-fails",
            ["function length(){return 1} BEGIN{print length()}"],
            [],
            2,
            string.Empty,
            "awk: function name `length' is reserved.\n"),

        Failing(
            "duplicate-function-definition-fails",
            ["function f(){return 1} function f(){return 2} BEGIN{print f()}"],
            [],
            2,
            string.Empty,
            "awk: duplicate function definition `f'.\n"),

        Failing(
            "duplicate-function-parameter-fails",
            ["function f(a,a){return a} BEGIN{print f(1)}"],
            [],
            2,
            string.Empty,
            "awk: duplicate function parameter `a'.\n"),

        Failing(
            "reserved-function-parameter-fails",
            ["function f(length){return 1} BEGIN{print f()}"],
            [],
            2,
            string.Empty,
            "awk: function parameter `length' is reserved.\n"),

        Failing(
            "builtin-scalar-as-array-fails",
            ["BEGIN{IGNORECASE[1]=1}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `IGNORECASE' as an array\n"),

        Failing(
            "undefined-function-fails",
            ["BEGIN{print missing_function(1)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: missing_function\n"),

        Failing(
            "undefined-function-fails-after-prior-output",
            ["BEGIN{print \"before\"; missing_function(); print \"after\"}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: missing_function\n"),

        Failing(
            "undefined-function-lint-warning",
            ["--lint", "BEGIN{print missing_function(1)}"],
            [],
            2,
            string.Empty,
            "awk: warning: function `missing_function' called but never defined\nawk: undefined function: missing_function\n"),

        Failing(
            "undefined-function-lint-fatal",
            ["--lint=fatal", "BEGIN{print missing_function(1)}"],
            [],
            2,
            string.Empty,
            "awk: fatal: function `missing_function' called but never defined\n"),

        Failing(
            "undefined-function-short-lint-separated-fatal",
            ["-L", "fatal", "BEGIN{print missing_function(1)}"],
            [],
            2,
            string.Empty,
            "awk: fatal: function `missing_function' called but never defined\n"),

        Posix(Failing(
            "bad-lvalue-fails-before-prior-output",
            ["BEGIN{print \"before\"; 1 = 2; print \"after\"}"],
            [],
            2,
            string.Empty,
            "awk: invalid assignment target.\n")),

        Failing(
            "unterminated-action-fails-before-prior-output",
            ["BEGIN{print \"before\"} {print \"record\""],
            [],
            2,
            string.Empty,
            "awk: unterminated action block.\n"),

        Failing(
            "bindtextdomain-function-unsupported",
            ["BEGIN{print bindtextdomain(\"messages\", \"/tmp\")}"],
            [],
            2,
            string.Empty,
            "awk: internationalization functions are unsupported in Lokad awk\n"),

        Failing(
            "dcgettext-function-unsupported",
            ["BEGIN{print dcgettext(\"messages\", \"hello\", \"LC_MESSAGES\")}"],
            [],
            2,
            string.Empty,
            "awk: internationalization functions are unsupported in Lokad awk\n"),

        Failing(
            "dcngettext-function-unsupported",
            ["BEGIN{print dcngettext(\"messages\", \"one\", \"many\", 2, \"LC_MESSAGES\")}"],
            [],
            2,
            string.Empty,
            "awk: internationalization functions are unsupported in Lokad awk\n"),

        Failing(
            "internationalized-string-marker-unsupported",
            ["BEGIN{print _\"hello\"}"],
            [],
            2,
            string.Empty,
            "awk: internationalization functions are unsupported in Lokad awk\n"),

        Passing(
            "internationalized-string-marker-in-string",
            ["BEGIN{print \"_\\\"hello\\\"\"}"],
            [],
            "_\"hello\"\n"),

        PassingWithStdErr(
            "ambiguous-string-escape-warnings",
            ["BEGIN{print \"a\\qz\"; print \"a\\xz\"; print \"a\\/z\"}"],
            [],
            "aqz\naxz\na/z\n",
            "awk: warning: escape sequence `\\q' treated as plain `q'\n" +
            "awk: warning: no hex digits in `\\x' escape sequence\n" +
            "awk: warning: escape sequence `\\/' treated as plain `/\'\n")
    ];
}
