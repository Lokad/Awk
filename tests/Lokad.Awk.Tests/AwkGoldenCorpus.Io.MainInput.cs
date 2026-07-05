namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] MainInputIoCases() =>
    [
        Passing(
            "begin-getline-triggers-beginfile-and-continues-input",
            ["BEGIN{print \"begin\"; getline x; print \"got\", x, FILENAME, NR, FNR} BEGINFILE{print \"beginfile\", FILENAME, FNR} {print \"main\", FILENAME, NR, FNR, $0} ENDFILE{print \"endfile\", FILENAME, FNR}", "/file.txt"],
            [File("/file.txt", "one\ntwo\n")],
            "begin\nbeginfile /file.txt 0\ngot one /file.txt 1 1\nmain /file.txt 2 2 two\nendfile /file.txt 2\n")
    ];
}
