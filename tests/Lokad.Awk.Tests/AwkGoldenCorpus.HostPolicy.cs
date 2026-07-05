namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] HostPolicyCases() =>
    [
        Failing(
            "system-function-unsupported",
            ["BEGIN{print system(\"echo hi\")}"],
            [],
            2,
            string.Empty,
            "awk: system() is unsupported in Lokad awk\n"),

        Failing(
            "coprocess-output-unsupported",
            ["BEGIN{print \"hi\" |& \"cat\"}"],
            [],
            2,
            string.Empty,
            "awk: coprocesses are unsupported in Lokad awk\n"),

        Failing(
            "coprocess-getline-unsupported",
            ["BEGIN{\"cat\" |& getline seen}"],
            [],
            2,
            string.Empty,
            "awk: coprocesses are unsupported in Lokad awk\n"),

        Failing(
            "inet-output-special-file-unsupported",
            ["BEGIN{print \"hi\" > \"/inet/tcp/0/localhost/80\"}"],
            [],
            2,
            string.Empty,
            "awk: /inet special files are unsupported in Lokad awk\n"),

        Failing(
            "inet-input-special-file-unsupported",
            ["BEGIN{getline seen < \"/inet/tcp/0/localhost/80\"}"],
            [],
            2,
            string.Empty,
            "awk: /inet special files are unsupported in Lokad awk\n"),

        Failing(
            "dev-fd-3-output-unsupported",
            ["BEGIN{print \"hi\" > \"/dev/fd/3\"}"],
            [],
            2,
            string.Empty,
            "awk: unsupported /dev/fd descriptor in Lokad awk\n"),

        Failing(
            "dev-fd-3-input-unsupported",
            ["BEGIN{getline seen < \"/dev/fd/3\"}"],
            [],
            2,
            string.Empty,
            "awk: unsupported /dev/fd descriptor in Lokad awk\n")
    ];
}
