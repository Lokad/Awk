namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] FunctionSyntaxCases() =>
    [
        Passing(
            "builtin-function-call-whitespace",
            ["BEGIN{print length (\"abc\")}"],
            [],
            "3\n")
    ];
}
