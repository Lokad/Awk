namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] StatementRuntimeCases() =>
    [
        Exiting(
            "exit-main-runs-end-and-status",
            ["{print $1; if(NR==2) exit 7; print \"after\"} END{print \"end\", NR}", "/file.txt"],
            [File("/file.txt", "a\nb\nc\n")],
            7,
            "a\nafter\nb\nend 2\n"),

        Passing(
            "exit-begin-skips-input-and-runs-end",
            ["BEGIN{print \"begin\"; exit} {print $0} END{print \"end\", NR}", "/file.txt"],
            [File("/file.txt", "a\nb\n")],
            "begin\nend 0\n"),

        Exiting(
            "exit-end-overrides-status-and-stops-end",
            ["BEGIN{exit 3} END{print \"end\"; exit 4; print \"late\"}"],
            [],
            4,
            "end\n"),

        CompilerDiagnostic(Failing(
            "return-outside-function-fails",
            ["BEGIN{return}"],
            [],
            2,
            string.Empty,
            "awk: return used outside function\n")),

        CompilerDiagnostic(Failing(
            "return-expression-outside-function-fails",
            ["BEGIN{return 1 + 2}"],
            [],
            2,
            string.Empty,
            "awk: return used outside function\n")),

        Failing(
            "division-by-zero-fails",
            ["BEGIN{print 1 / 0}"],
            [],
            2,
            string.Empty,
            "awk: division by zero attempted\n"),

        Failing(
            "modulo-by-zero-fails",
            ["BEGIN{print 1 % 0}"],
            [],
            2,
            string.Empty,
            "awk: division by zero attempted in `%'\n"),

        Failing(
            "divide-assignment-by-zero-fails",
            ["BEGIN{x=1; x/=0; print x}"],
            [],
            2,
            string.Empty,
            "awk: division by zero attempted in `/='\n"),

        Failing(
            "modulo-assignment-by-zero-fails",
            ["BEGIN{x=1; x%=0; print x}"],
            [],
            2,
            string.Empty,
            "awk: division by zero attempted in `%='\n"),

        Passing(
            "nextfile-skips-current-file",
            ["{print FILENAME, FNR, NR, $1; nextfile} END{print \"end\", NR}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\nb2\n")],
            "/a.txt 1 1 a1\n/b.txt 1 2 b1\nend 2\n"),

        Passing(
            "nextfile-from-main-runs-endfile",
            ["BEGINFILE{print \"begin\", FILENAME} {print \"main\", FILENAME, FNR, NR; nextfile} ENDFILE{print \"endfile\", FILENAME, FNR} END{print \"end\", NR}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\n")],
            "begin /a.txt\nmain /a.txt 1 1\nendfile /a.txt 1\nbegin /b.txt\nmain /b.txt 1 2\nendfile /b.txt 1\nend 2\n"),

        Passing(
            "beginfile-nextfile-skips-current-file",
            ["BEGINFILE{print \"skip\", FILENAME; nextfile} {print \"bad\"} ENDFILE{print \"end\", FILENAME, FNR}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\n")],
            "skip /a.txt\nskip /b.txt\n"),

        CompilerDiagnostic(Failing(
            "next-in-begin-fails",
            ["BEGIN{next}"],
            [],
            2,
            string.Empty,
            "awk: next used outside input action\n")),

        CompilerDiagnostic(Failing(
            "next-in-end-fails",
            ["END{next}"],
            [],
            2,
            string.Empty,
            "awk: next used outside input action\n")),

        CompilerDiagnostic(Failing(
            "nextfile-in-begin-fails",
            ["BEGIN{nextfile}"],
            [],
            2,
            string.Empty,
            "awk: nextfile used outside input action\n")),

        CompilerDiagnostic(Failing(
            "nextfile-in-endfile-fails",
            ["ENDFILE{nextfile}", "/a.txt"],
            [File("/a.txt", "a1\n")],
            2,
            string.Empty,
            "awk: nextfile used outside input action\n"))
    ];
}
