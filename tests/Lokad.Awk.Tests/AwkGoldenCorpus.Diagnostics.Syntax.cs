namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] SyntaxDiagnosticCases() =>
    [
        Failing(
            "invalid-inline-script",
            ["{print"],
            [],
            2,
            string.Empty,
            "awk: unterminated action block.\n"),

        Failing(
            "invalid-script-file",
            ["-f", "/script.awk"],
            [File("/script.awk", "{print")],
            2,
            string.Empty,
            "awk: /script.awk:1:1: unterminated action block.\n"),

        Failing(
            "unterminated-string-fails",
            ["BEGIN{print \"unterminated\n}"],
            [],
            2,
            string.Empty,
            "awk: unrecognized awk token.\n"),

        Failing(
            "unterminated-regex-fails",
            ["BEGIN{print $0 ~ /unterminated}"],
            [],
            2,
            string.Empty,
            "awk: invalid regular expression\n"),

        Failing(
            "unrecognized-lexical-token-fails",
            ["BEGIN{print `bad`}"],
            [],
            2,
            string.Empty,
            "awk: unrecognized awk token.\n"),

        Failing(
            "missing-closing-parenthesis-fails",
            ["BEGIN{print (1 + 2}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "missing-closing-bracket-fails",
            ["BEGIN{a[1 = 2}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "missing-statement-separator-fails",
            ["BEGIN{print 1 print 2}"],
            [],
            2,
            string.Empty,
            "awk: invalid `print' statement.\n"),

        Failing(
            "stray-closing-parenthesis-fails",
            ["BEGIN{print 1)}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "extra-closing-brace-fails",
            ["BEGIN{print 1}}"],
            [],
            2,
            string.Empty,
            "awk: invalid pattern.\n"),

        Failing(
            "malformed-if-condition-fails",
            ["BEGIN{if (1 print 1}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "stray-else-fails",
            ["BEGIN{else print 1}"],
            [],
            2,
            string.Empty,
            "awk: invalid statement.\n"),

        Failing(
            "malformed-while-condition-fails",
            ["BEGIN{while (1 print 1}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "malformed-do-while-condition-fails",
            ["BEGIN{do print 1 while 1}"],
            [],
            2,
            string.Empty,
            "awk: expected do-while condition.\n"),

        Failing(
            "malformed-for-header-fails",
            ["BEGIN{for (i = 0; i < 3 i++) print i}"],
            [],
            2,
            string.Empty,
            "awk: invalid statement.\n"),

        Failing(
            "malformed-for-in-target-fails",
            ["BEGIN{for (1 in a) print 1}"],
            [],
            2,
            string.Empty,
            "awk: invalid statement.\n"),

        Failing(
            "malformed-switch-label-fails",
            ["BEGIN{switch (1) { case 1 print 1 }}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "malformed-switch-default-label-fails",
            ["BEGIN{switch (1) { default print 1 }}"],
            [],
            2,
            string.Empty,
            "awk: invalid action block.\n"),

        Failing(
            "malformed-delete-missing-target-fails",
            ["BEGIN{delete}"],
            [],
            2,
            string.Empty,
            "awk: expected delete target.\n"),

        Failing(
            "malformed-print-redirection-fails",
            ["BEGIN{print \"x\" >}"],
            [],
            2,
            string.Empty,
            "awk: invalid expression.\n"),

        Failing(
            "trailing-print-redirection-token-fails",
            ["BEGIN{print x > y z}"],
            [],
            2,
            string.Empty,
            "awk: : Invalid path\n"),

        Failing(
            "malformed-printf-redirection-fails",
            ["BEGIN{printf \"%s\", \"x\" >}"],
            [],
            2,
            string.Empty,
            "awk: invalid expression.\n"),

        Failing(
            "malformed-getline-redirection-fails",
            ["BEGIN{getline x <}"],
            [],
            2,
            string.Empty,
            "awk: input redirection getline requires a source expression.\n"),

        Failing(
            "trailing-close-call-token-fails",
            ["BEGIN{close(\"/tmp\")y}"],
            [],
            2,
            string.Empty,
            "awk: unexpected token after close statement.\n"),

        Failing(
            "trailing-fflush-call-token-fails",
            ["BEGIN{fflush(\"/tmp\")y}"],
            [],
            2,
            string.Empty,
            "awk: unexpected token after fflush statement.\n"),

        Failing(
            "trailing-delete-token-fails",
            ["BEGIN{a[1]=1; delete a[1] y}"],
            [],
            2,
            string.Empty,
            "awk: unexpected token after delete target.\n"),

        Failing(
            "trailing-getline-target-token-fails",
            ["BEGIN{getline x y}"],
            [],
            2,
            string.Empty,
            "awk: unexpected token after getline target.\n"),

        Failing(
            "trailing-split-call-token-fails",
            ["BEGIN{split(\"a\", a) y}"],
            [],
            2,
            string.Empty,
            "awk: unexpected token after split statement.\n"),

        Failing(
            "trailing-break-argument-fails",
            ["BEGIN{while (1) {break 1}}"],
            [],
            2,
            string.Empty,
            "awk: `break' does not take an argument.\n"),

        Failing(
            "trailing-continue-argument-fails",
            ["BEGIN{while (1) {continue 1}}"],
            [],
            2,
            string.Empty,
            "awk: `continue' does not take an argument.\n"),

        Failing(
            "trailing-next-argument-fails",
            ["{next 1}"],
            [],
            2,
            string.Empty,
            "awk: `next' does not take an argument.\n"),

        Failing(
            "trailing-nextfile-argument-fails",
            ["{nextfile 1}"],
            [],
            2,
            string.Empty,
            "awk: `nextfile' does not take an argument.\n"),

        Failing(
            "trailing-return-comma-expression-fails",
            ["function f(){return 1, 2} BEGIN{print f()}"],
            [],
            2,
            string.Empty,
            "awk: invalid `return' statement.\n"),

        Failing(
            "malformed-isarray-missing-argument-fails",
            ["BEGIN{print isarray()}"],
            [],
            2,
            string.Empty,
            "awk: invalid number of arguments for isarray\n"),

        Failing(
            "malformed-typeof-missing-argument-fails",
            ["BEGIN{print typeof()}"],
            [],
            2,
            string.Empty,
            "awk: invalid number of arguments for typeof\n"),

        Failing(
            "malformed-typeof-too-many-arguments-fails",
            ["BEGIN{print typeof(a, b)}"],
            [],
            2,
            string.Empty,
            "awk: invalid number of arguments for typeof\n"),

        Failing(
            "post-increment-adjacent-token-fails",
            ["BEGIN{i++j}"],
            [],
            2,
            string.Empty,
            "awk: unexpected token after postfix update expression.\n"),

        Failing(
            "unterminated-ternary-fails",
            ["BEGIN{print 1 ? 2}"],
            [],
            2,
            string.Empty,
            "awk: invalid `print' statement.\n"),

        Failing(
            "malformed-function-definition-fails",
            ["function 1bad(){return 1} BEGIN{print 1}"],
            [],
            2,
            string.Empty,
            "awk: expected function name.\n")
    ];
}
