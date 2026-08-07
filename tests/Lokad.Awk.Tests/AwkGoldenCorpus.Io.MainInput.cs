namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] MainInputIoCases() =>
    [
        Passing(
            "begin-getline-triggers-beginfile-and-continues-input",
            ["BEGIN{print \"begin\"; getline x; print \"got\", x, FILENAME, NR, FNR} BEGINFILE{print \"beginfile\", FILENAME, FNR} {print \"main\", FILENAME, NR, FNR, $0} ENDFILE{print \"endfile\", FILENAME, FNR}", "/file.txt"],
            [File("/file.txt", "one\ntwo\n")],
            "begin\nbeginfile /file.txt 0\ngot one /file.txt 1 1\nmain /file.txt 2 2 two\nendfile /file.txt 2\n"),

        Passing(
            "begin-getline-honors-custom-record-separator",
            ["BEGIN{RS=\"|\"; getline x; print \"got\", x \"[\" RT \"]\", NR, FNR} {print \"main\", $0 \"[\" RT \"]\", NR, FNR}", "/file.txt"],
            [File("/file.txt", "a|b|c|")],
            "got a[|] 1 1\nmain b[|] 2 2\nmain c[|] 3 3\n"),

        Passing(
            "main-getline-shares-dynamic-record-separator-cursor",
            ["{print \"main\", $0 \"[\" RT \"]\", NR; if(NR==1){RS=\"|\"; getline x; print \"got\", x \"[\" RT \"]\", NR}}", "/file.txt"],
            [File("/file.txt", "a\nb|c|")],
            "main a[\n] 1\ngot b[|] 2\nmain c[|] 3\n"),

        Passing(
            "redirected-getline-honors-custom-record-separator",
            ["BEGIN{RS=\"|\"; while((getline x < \"/records.txt\") > 0) print x \"[\" RT \"]\"; close(\"/records.txt\")}"],
            [File("/records.txt", "a|b|")],
            "a[|]\nb[|]\n"),

        Passing(
            "paragraph-getline-current-record-preserves-paragraph-field-splitting",
            ["BEGIN{FS=\",\"; RS=\"\"; getline; print NF, $1, $2}", "/file.txt"],
            [File("/file.txt", "a\nb\n\n")],
            "2 a b\n")
    ];
}
