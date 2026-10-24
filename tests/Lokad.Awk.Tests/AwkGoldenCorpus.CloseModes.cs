namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> CloseModeCases()
    {
        foreach (var mode in new[] { "to", "from" })
        {
            foreach (var input in new[] { false, true })
            {
                var open = input ? "getline x<p;" : "print \"new\">p;";
                var builder = AwkGoldenCaseBuilder.Create($"close-mode-file-{input}-{mode}",
                    ["BEGIN{p=\"data\";" + open + "ERRNO=\"keep\";print close(p,\"" + mode +
                        "\"),ERRNO;print close(p)}"], [File("/data", "old\n")])
                    .WithStdOut("0 keep\n-1\n");
                if (!input) builder.WithExpectedFiles([File("/data", "new\n")]);
                yield return builder.Build();
            }

            foreach (var inputLast in new[] { false, true })
            {
                const string read = "getline x<p;";
                const string write = "print \"new\">p;";
                var builder = AwkGoldenCaseBuilder.Create($"close-mode-latest-{inputLast}-{mode}",
                    ["BEGIN{p=\"data\";" + (inputLast ? write + read : read + write) +
                        "print close(p,\"" + mode + "\"),fflush(p),close(p),close(p)}"],
                    [File("/data", "old\n")])
                    .WithStdOut(inputLast ? "0 0 0 -1\n" : "0 -1 0 -1\n")
                    .WithExpectedFiles([File("/data", "new\n")]);
                if (!inputLast) builder.WithStdErr(
                    "awk: warning: fflush: cannot flush: file `data' opened for reading, not writing\n");
                yield return builder.Build();
            }
        }

        yield return AwkGoldenCaseBuilder.Create("close-mode-captures-name-before-mode",
            ["BEGIN{p=\"data\";getline x<p;print \"new\">\"out\";print close(p,((p=\"out\")?\"to\":\"from\"));print close(\"data\"),close(p)}"],
            [File("/data", "old\n")]).WithStdOut("0\n-1 0\n")
            .WithExpectedFiles([File("/out", "new\n")]).Build();

        foreach (var mode in new[] { "\"bad\"", "\"\"", "17", "unknown" })
            yield return AwkGoldenCaseBuilder.Create("close-mode-invalid-" + mode,
                ["BEGIN{getline x<\"data\";print \"before\";print close(\"data\"," + mode + ");print \"unreachable\"}"],
                [File("/data", "old\n")]).WithStdOut("before\n")
                .WithStdErr("awk: close second argument must be `to' or `from'\n")
                .ExitsWith(2).Build();

        foreach (var append in new[] { false, true })
        foreach (var existing in new[] { false, true })
            yield return AwkGoldenCaseBuilder.Create($"file-open-visible-{append}-{existing}",
                ["BEGIN{print \"new\"" + (append ? ">>" : ">") +
                    "\"data\";s=(getline x<\"data\");print s,\"[\"x\"]\";close(\"data\");close(\"data\")}"],
                existing ? [File("/data", "old\n")] : [])
                .WithStdOut(append && existing ? "1 [old]\n" : "0 []\n")
                .WithExpectedFiles([File("/data", append && existing ? "old\nnew\n" : "new\n")]).Build();
    }

    // A supporting command host runs these canonical cases in AwkIoCloseDirectionTests.
    internal static IEnumerable<AwkGoldenCase> CloseModePipeCases()
    {
        foreach (var input in new[] { false, true })
        foreach (var mode in new[] { "to", "from" })
        {
            var open = input ? "cmd|getline x;" : "print \"new\"|cmd;";
            yield return AwkGoldenCaseBuilder.Create($"close-mode-pipe-{input}-{mode}",
                ["BEGIN{cmd=\"worker a:b\";" + open + "ERRNO=\"keep\";print close(cmd,\"" + mode +
                    "\"),ERRNO;print close(cmd)}"], [])
                .WithStdOut((input ? "7" : "17") + " keep\n-1\n").Build();
        }

        foreach (var input in new[] { false, true })
        foreach (var mode in new[] { "\"bad\"", "\"\"", "17", "unknown" })
        {
            var open = input ? "cmd|getline x;" : "print \"new\"|cmd;";
            yield return AwkGoldenCaseBuilder.Create($"close-mode-pipe-{input}-invalid-{mode}",
                ["BEGIN{cmd=\"worker a:b\";" + open + "print \"before\";print close(cmd," + mode +
                    ");print \"unreachable\"}"], []).WithStdOut("before\n")
                .WithStdErr("awk: close second argument must be `to' or `from'\n").ExitsWith(2).Build();
        }
    }
}
