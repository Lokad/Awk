namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> TriviaProgramCases()
    {
        (string Name, string[] Arguments, string Error)[] cases =
        [
            ("inline", ["# comment"], ""),
            ("spaces", [" \t# first\n\t# second\n"], ""),
            ("unicode", ["# café 😀"], ""),
            ("file", ["-f", "comments.awk"], ""),
            ("pieces", ["-e", "# first", "-e", "# second"], ""),
            ("repeated-files", ["-f", "comments.awk", "-f", "empty.awk"], ""),
            ("empty-control", [""], ""),
            ("invalid-control", ["BEGIN{"], "awk: unterminated action block.\n"),
            ("semicolon-control", [";"], "awk: expected awk pattern or action.\n"),
            ("comment-semicolon-control", ["# comment\n;"], "awk: expected awk pattern or action.\n"),
            ("nbsp-control", ["\u00a0"], "awk: unrecognized awk token.\n")
        ];
        AwkGoldenFile[] files = [File("/comments.awk", "# file comment\n"), File("/empty.awk", "")];
        foreach (var profile in new[] { "", "--posix", "--traditional" })
        foreach (var test in cases)
        {
            string[] arguments = profile.Length == 0 ? test.Arguments : [profile, .. test.Arguments];
            var builder = AwkGoldenCaseBuilder.Create("trivia-program-" + test.Name + "-" + (profile.Length == 0 ? "default" : profile[2..]), arguments, files)
                .WithStandardInput("DATA\n").ExitsWith(test.Error.Length == 0 ? 0 : 2).WithStdErr(test.Error);
            if (test.Error.Length > 0)
                builder.WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]);
            yield return builder.Build();
        }
        yield return AwkGoldenCaseBuilder.Create("trivia-program-include", ["# before\n@include \"comments.awk\"\n# after"], files)
            .WithStandardInput("DATA\n").Build();
        yield return AwkGoldenCaseBuilder.Create("trivia-program-include-option", ["-i", "comments.awk", "# main"], files)
            .WithStandardInput("DATA\n").Build();
    }
}
