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
            "begin-getline-honors-argv-replacement-before-first-read",
            ["BEGIN{ARGV[1]=\"/two.txt\"; getline x; print \"got\", x, FILENAME} BEGINFILE{print \"beginfile\", FILENAME} {print \"main\", FILENAME, $0} ENDFILE{print \"endfile\", FILENAME}", "/one.txt"],
            [File("/one.txt", "one-a\none-b\n"), File("/two.txt", "two-a\ntwo-b\n")],
            "beginfile /two.txt\ngot two-a /two.txt\nmain /two.txt two-b\nendfile /two.txt\n"),

        Passing(
            "begin-getline-applies-leading-command-line-assignment",
            ["BEGIN{print \"before\", \"[\" x \"]\"; getline; print \"after\", x, $0}", "x=1", "/file.txt"],
            [File("/file.txt", "line\n")],
            "before []\nafter 1 line\n"),

        Passing(
            "begin-getline-continues-across-input-files",
            ["BEGIN{while((getline x)>0) print \"got\", FILENAME, NR, FNR, x; print \"done\", NR, FNR} BEGINFILE{print \"beginfile\", FILENAME} ENDFILE{print \"endfile\", FILENAME, FNR}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a\nb\n"), File("/b.txt", "c\n")],
            "beginfile /a.txt\ngot /a.txt 1 1 a\ngot /a.txt 2 2 b\nendfile /a.txt 2\nbeginfile /b.txt\ngot /b.txt 3 1 c\nendfile /b.txt 1\ndone 3 1\n"),

        Passing(
            "begin-getline-deleted-input-argument-falls-back-to-stdin",
            ["BEGIN{delete ARGV[1]; getline x; print x, FILENAME}", "/unused.txt"],
            [File("/unused.txt", "unused\n")],
            "stdin -\n",
            "stdin\n"),

        Passing(
            "begin-getline-keeps-open-input-after-current-argv-replacement",
            ["BEGIN{getline x; print \"got\", x, FILENAME; ARGV[1]=\"/two.txt\"} {print \"main\", FILENAME, $0}", "/one.txt"],
            [File("/one.txt", "one-a\none-b\n"), File("/two.txt", "two-a\n")],
            "got one-a /one.txt\nmain /one.txt one-b\n"),

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
