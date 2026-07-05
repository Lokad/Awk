namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] OutputCases() =>
    [
        Posix(Passing(
            "print-empty-first-value-keeps-separator",
            ["{print \"\", $0}", "/file.txt"],
            [File("/file.txt", "x\n")],
            " x\n")),

        Posix(Passing(
            "print-uses-ofs-and-ors-assignments",
            ["BEGIN{OFS=\"|\"; ORS=\";\"} {print $1, $2}", "/file.txt"],
            [File("/file.txt", "a b\nc d\n")],
            "a|b;c|d;")),

        Passing(
            "print-uses-ofs-from-variable-option",
            ["-v", "OFS=:", "{print $1, $2}", "/file.txt"],
            [File("/file.txt", "a b\nc d\n")],
            "a:b\nc:d\n"),

        Posix(Passing(
            "begin-main-end-stdout-order",
            ["BEGIN{print \"begin\"} {print \"main\", $0} END{print \"end\", NR}", "/file.txt"],
            [File("/file.txt", "one\ntwo\n")],
            "begin\nmain one\nmain two\nend 2\n")),

        Posix(Exiting(
            "exit-in-main-flushes-output-before-end",
            ["{print \"main\", $0; exit 7} END{print \"end\", NR}", "/file.txt"],
            [File("/file.txt", "one\ntwo\n")],
            7,
            "main one\nend 1\n")),

        PassingWithExpectedFiles(
            "stdout-and-file-output-flush-independently",
            ["BEGIN{print \"file-1\" > \"/out.txt\"; print \"stdout-1\"; print \"file-2\" > \"/out.txt\"; print \"stdout-2\"}"],
            [],
            "stdout-1\nstdout-2\n",
            [File("/out.txt", "file-1\nfile-2\n")]),

        PassingWithStdErr(
            "stdout-and-stderr-special-paths-flush-independently",
            ["BEGIN{printf \"out-1\" > \"/dev/stdout\"; printf \"err-1\" > \"/dev/stderr\"; print \"-out-2\"; print \"-err-2\" > \"/dev/stderr\"}"],
            [],
            "out-1-out-2\n",
            "err-1-err-2\n")
    ];
}
