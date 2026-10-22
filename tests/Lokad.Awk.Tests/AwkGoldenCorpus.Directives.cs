namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> DirectiveCases()
    {
        (string Name, string Source, string Output, string Error, int ErrorLine)[] cases =
        [
            ("tail", "@include \"/lib.awk\"; BEGIN{print \"TAIL\"}\nBEGIN{print \"MAIN\"}", "LIB\nTAIL\nMAIN\n", "", 0),
            ("two", "@include \"/lib.awk\"; @include \"/other.awk\"\nBEGIN{print \"MAIN\"}", "LIB\nOTHER\nMAIN\n", "", 0),
            ("after-action", "BEGIN{print \"FIRST\"}; @include \"/lib.awk\"\nBEGIN{print \"MAIN\"}", "FIRST\nLIB\nMAIN\n", "", 0),
            ("after-brace", "BEGIN{print \"FIRST\"} @include \"/lib.awk\"\nBEGIN{print \"MAIN\"}", "FIRST\nLIB\nMAIN\n", "", 0),
            ("deduplicated", "@include \"/lib.awk\"; @include \"/lib.awk\"; BEGIN{print \"MAIN\"}", "LIB\nMAIN\n", "", 0),
            ("comment", "@include \"/lib.awk\" # trailing comment\nBEGIN{print \"MAIN\"}", "LIB\nMAIN\n", "", 0),
            ("semicolon", "@include \"/lib.awk\";\nBEGIN{print \"MAIN\"}", "LIB\nMAIN\n", "", 0),
            ("continuation", "@include \\\n\"/lib.awk\"; BEGIN{print \"MAIN\"}", "LIB\nMAIN\n", "", 0),
            ("comment-control", "# @include \"/missing.awk\"\nBEGIN{print \"MAIN\"}", "MAIN\n", "", 0),
            ("literal-control", "BEGIN{print (\"@include\" ~ /@include/)}", "1\n", "", 0),
            ("missing", "@include\nBEGIN{print \"unreachable\"}", "", "invalid @include directive.", 1),
            ("numeric", "@include 42\nBEGIN{print \"unreachable\"}", "", "invalid @include directive.", 1),
            ("extra", "@include \"/lib.awk\" garbage\nBEGIN{print \"unreachable\"}", "", "invalid @include directive.", 1),
            ("two-arguments", "@include \"/lib.awk\" \"/other.awk\"\nBEGIN{print \"unreachable\"}", "", "invalid @include directive.", 1),
            ("unknown", "@mystery \"/lib.awk\"\nBEGIN{print \"unreachable\"}", "", "unknown awk directive.", 1),
            ("block", "BEGIN{\n@include \"/body.awk\"\nprint \"unreachable\"}", "", "invalid statement.", 2),
            ("function", "function f(){\n@include \"/body.awk\"\n}\nBEGIN{f()}", "", "invalid statement.", 2)
        ];
        foreach (var file in new[] { false, true })
        foreach (var test in cases)
        {
            AwkGoldenFile[] files =
            [
                File("/lib.awk", "BEGIN{print \"LIB\"}\n"),
                File("/other.awk", "BEGIN{print \"OTHER\"}\n"),
                File("/body.awk", "print \"unreachable\"\n"),
                File("/main.awk", test.Source)
            ];
            string[] arguments = file ? ["-f", "/main.awk"] : [test.Source];
            var error = test.Error.Length == 0 ? "" : "awk: " +
                (file ? "/main.awk:" + test.ErrorLine + ":1: " : "") + test.Error + "\n";
            var builder = AwkGoldenCaseBuilder.Create("directive-" + test.Name + (file ? "-file" : "-inline"), arguments, files)
                .WithStdOut(test.Output).WithStdErr(error).ExitsWith(test.Error.Length == 0 ? 0 : 2);
            if (test.Error.Length > 0) builder.WithTags([AwkGoldenTag.CompilerDiagnostic]);
            yield return builder.Build();
        }
    }
}
