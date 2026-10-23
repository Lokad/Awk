namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> SourceCompositionCases()
    {
        foreach (var mode in new[] { "--posix", "--traditional" })
        foreach (var name in new[] { "switch", "case", "default", "BEGINFILE", "ENDFILE" })
        foreach (var newline in new[] { "\n", "\r\n" })
        foreach (var file in new[] { false, true })
        {
            var source = "BEGIN{" + name + "=8;print " + name + " /1\\" + newline + "2/3}";
            string[] arguments = file ? [mode, "-f", "/source.awk"] : [mode, source];
            yield return AwkGoldenCaseBuilder.Create(
                    $"source-profile-{mode}-{name}-{newline.Length}-{file}", arguments, [File("/source.awk", source)])
                .WithStdOut("80.666667\n").Build();
        }

        foreach (var mode in new[] { "", "--posix", "--traditional" })
        foreach (var test in new[]
        {
            (Name: "ordinary", Source: "BEGIN{v=8;print v /1\\\n2/3}", Output: "80.666667\n"),
            (Name: "regex", Source: "BEGIN{print (\"12\" ~ /1\\\n2/)}", Output: "1\n"),
            (Name: "parameter", Source: "function f(switch){return switch /1\\\n2/3} BEGIN{print f(8)}", Output: "80.666667\n")
        })
        {
            string[] arguments = mode.Length == 0 ? [test.Source] : [mode, test.Source];
            yield return AwkGoldenCaseBuilder.Create($"source-profile-control-{mode}-{test.Name}", arguments, [])
                .WithStdOut(test.Output).Build();
        }

        (string Name, string[] Arguments, string ErrorPath)[] conflicts =
        [
            ("file-include", ["-f", "/same.awk", "-i", "/same.awk"], "/same.awk"),
            ("include-file", ["-i", "/same.awk", "-f", "/same.awk"], "/same.awk"),
            ("alias", ["-i", "/same.awk", "-f", "/./same.awk"], "/./same.awk"),
            ("inline-after-file", ["-f", "/same.awk", "-e", "@include \"/same.awk\""], "/same.awk"),
            ("file-after-inline", ["-e", "@include \"/same.awk\"", "-f", "/same.awk"], "/same.awk"),
            ("self", ["-f", "/self.awk"], "/self.awk"),
            ("indirect-self", ["-f", "/a.awk"], "/a.awk")
        ];
        foreach (var mode in new[] { "", "--posix", "--traditional" })
        foreach (var test in conflicts)
        {
            if (mode.Length != 0 && test.Name is not ("file-include" or "include-file" or "alias")) continue;
            string[] arguments = mode.Length == 0 ? test.Arguments : [mode, .. test.Arguments];
            yield return AwkGoldenCaseBuilder.Create("source-role-" + test.Name + mode, arguments,
                [File("/same.awk", "BEGIN{print \"DUPLICATED\"}"),
                 File("/self.awk", "@include \"/self.awk\"\nBEGIN{print \"SELF\"}"),
                 File("/a.awk", "@include \"/b.awk\"\nBEGIN{print \"A\"}"),
                 File("/b.awk", "@include \"/a.awk\"\nBEGIN{print \"B\"}")])
                .WithStdErr($"awk: {test.ErrorPath}: cannot use a file as both program source and include\n")
                .ExitsWith(2).Build();
        }

        foreach (var test in new[]
        {
            (Name: "repeated-file", Arguments: new[] { "-f", "/same.awk", "-f", "/same.awk" }, Output: "A\nA\n"),
            (Name: "repeated-alias", Arguments: new[] { "-f", "/same.awk", "-f", "/./same.awk" }, Output: "A\nA\n"),
            (Name: "repeated-include", Arguments: new[] { "-i", "/same.awk", "-i", "/./same.awk", "BEGIN{}" }, Output: "A\n")
        })
            yield return AwkGoldenCaseBuilder.Create("source-role-control-" + test.Name, test.Arguments,
                    [File("/same.awk", "BEGIN{print \"A\"}")])
                .WithStdOut(test.Output).Build();
    }
}
