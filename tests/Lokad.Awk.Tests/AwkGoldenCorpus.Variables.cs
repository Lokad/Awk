
namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] VariableCases() =>
    [
        Posix(PassingWithEnvironment(
            "environ-array",
            ["BEGIN{print ENVIRON[\"SKU\"] \":\" ENVIRON[\"EMPTY\"]}"],
            [],
            [new AwkEnvironmentVariable("SKU", "A1"), new AwkEnvironmentVariable("EMPTY", string.Empty)],
            "A1:\n")),

        Passing(
            "environ-does-not-inherit-ambient-host-values",
            ["BEGIN{present=(\"PATH\" in ENVIRON); print present, \"[\" ENVIRON[\"PATH\"] \"]\"}"],
            [],
            "0 []\n"),

        PassingWithEnvironment(
            "environ-array-mutation",
            ["BEGIN{ENVIRON[\"SKU\"]=\"B2\"; ENVIRON[\"NEW\"]=\"created\"; print ENVIRON[\"SKU\"], ENVIRON[\"NEW\"]}"],
            [],
            [new AwkEnvironmentVariable("SKU", "A1")],
            "B2 created\n"),

        Passing(
            "procinfo-version",
            ["BEGIN{print PROCINFO[\"version\"]}"],
            [],
            "Lokad awk\n"),

        Passing(
            "builtin-variable-defaults",
            ["BEGIN{print ARGC, ARGIND, BINMODE, CONVFMT, \"[\" ERRNO \"]\", \"[\" FIELDWIDTHS \"]\", \"[\" FILENAME \"]\", FNR, (length(FPAT)>0), \"[\" FS \"]\", IGNORECASE, LINT, NF, NR, OFMT, length(OFS), length(ORS), PREC, ROUNDMODE, length(RS), \"[\" RT \"]\", RSTART, RLENGTH, length(SUBSEP), TEXTDOMAIN}"],
            [],
            "1 0 0 %.6g [] [] [] 0 1 [ ] 0 0 0 0 %.6g 1 1 53 N 1 [] 0 0 1 messages\n")
    ];
}
