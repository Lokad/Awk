namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] RecordSchedulingCases() =>
    [
        Posix(Passing(
            "stdin-default-input",
            ["{print $2}"],
            [],
            "b\nd\n",
            "a b\nc d\n")),

        Passing(
            "long-record-field-processing",
            ["{print length($1), $2}", "/long.txt"],
            [File("/long.txt", new string('x', 100_000) + " tail\n")],
            "100000 tail\n"),

        Passing(
            "single-character-record-separator",
            ["BEGIN{RS=\",\"} {print NR \":\" $0}"],
            [],
            "1:a\n2:b\n3:c\n",
            "a,b,c"),

        Passing(
            "record-terminator-variable",
            ["BEGIN{RS=\",\"} {print $0 \"[\" RT \"]\"}"],
            [],
            "a[,]\nb[,]\nc[]\n",
            "a,b,c"),

        Passing(
            "dynamic-record-separator-custom-to-newline",
            ["BEGIN{RS=\",\"} {print NR \":\" $0 \"[\" RT \"]\"; if(NR==1) RS=\"\\n\"}"],
            [],
            "1:a[,]\n2:b[\n]\n3:c[\n]\n",
            "a,b\nc\n"),

        Passing(
            "dynamic-record-separator-redirects-only-unread-tail",
            ["BEGIN{RS=\",\"} {print NR \":\" $0; if(NR==1) RS=\"\\n\"; else if(NR==2) RS=\"|\"}"],
            [],
            "1:a\n2:b\n3:c\n4:d\n",
            "a,b\nc|d|"),

        Passing(
            "regex-record-separator",
            ["BEGIN{RS=\"[,;]\"} {print NR \":\" $0 \"[\" RT \"]\"}"],
            [],
            "1:a[,]\n2:b[;]\n3:c[]\n",
            "a,b;c"),

        Passing(
            "regex-record-separator-repeated-match",
            ["BEGIN{RS=\"-+\"} {print NR \":\" $0 \"[\" RT \"]\"}"],
            [],
            "1:a[--]\n2:b[---]\n3:c[]\n",
            "a--b---c"),

        Failing(
            "regex-record-separator-invalid-regex-fails-after-begin-output",
            ["BEGIN{print \"before\"; RS=\"[+\"} {print \"after\"}", "/file.txt"],
            [File("/file.txt", "abc\n")],
            2,
            "before\n",
            "awk: invalid regular expression\n"),

        Passing(
            "paragraph-record-separator",
            ["BEGIN{RS=\"\"} {print NR \":\" NF \":\" $1 \":\" $2}"],
            [],
            "1:2:a1:a2\n2:2:b1:b2\n3:1:c1:\n",
            "a1\na2\n\nb1 b2\n\n\nc1\n"),

        Passing(
            "paragraph-record-separator-crlf-run-preserves-entire-rt",
            ["BEGIN{RS=\"\"} {t=RT; gsub(/\\r/,\"R\",t); gsub(/\\n/,\"N\",t); print NR \":\" $0 \":\" t}"],
            [],
            "1:a:RNRNRN\n2:b:\n",
            "a\r\n\r\n\r\nb"),

        Passing(
            "paragraph-record-separator-whitespace-only-line-is-record-data",
            ["BEGIN{RS=\"\"} {text=$0; gsub(/\\n/,\"<NL>\",text); print NR \":\" text}"],
            [],
            "1:a<NL> <NL>b\n",
            "a\n \nb\n\n"),

        Passing(
            "paragraph-record-separator-preserves-lone-carriage-returns",
            ["BEGIN{RS=\"\"} {print NR \":\" length($0) \":\" index($0,\"\\r\")}"],
            [],
            "1:2:1\n2:2:2\n",
            "\ra\n\nb\r"),

        Passing(
            "paragraph-record-separator-single-character-fs-also-splits-newlines",
            ["BEGIN{RS=\"\"; FS=\",\"} {print NF \":\" $1 \":\" $2 \":\" $3 \":\" $4}"],
            [],
            "4:a:b:c:d\n",
            "a,b\nc,d\n\n"),

        Passing(
            "default-rs-crlf-trims-cr-and-sets-rt",
            ["{t=RT; if(t==\"\\n\") t=\"<LF>\"; print NR \":\" length($0) \":\" index($0,\"\\r\") \":[\" t \"]\"}", "/file.txt"],
            [File("/file.txt", "a\r\nb\n")],
            "1:1:0:[<LF>]\n2:1:0:[<LF>]\n"),

        Passing(
            "default-rs-final-record-has-empty-rt",
            ["{t=RT; if(t==\"\\n\") t=\"<LF>\"; print NR \":\" $0 \":[\" t \"]\"}", "/file.txt"],
            [File("/file.txt", "a\nb")],
            "1:a:[<LF>]\n2:b:[]\n"),

        Passing(
            "default-rs-lone-cr-remains-record-data",
            ["{print NR, length($0), index($0,\"\\r\"), RT==\"\"}", "/file.txt"],
            [File("/file.txt", "a\rb")],
            "1 3 2 1\n"),

        Passing(
            "default-rs-nul-byte-remains-record-data",
            ["{print NR, length($0), index($0,\"\\0\"), RT==\"\\n\"}", "/file.txt"],
            [File("/file.txt", "a\0b\n")],
            "1 3 2 1\n"),

        Posix(Passing(
            "multiple-input-files",
            ["{print NR \":\" $1}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\n")],
            "1:a1\n2:a2\n3:b1\n")),

        Passing(
            "input-file-without-final-newline",
            ["{print NR, $1, $2}", "/file.txt"],
            [File("/file.txt", "a b")],
            "1 a b\n"),

        Posix(Passing(
            "filename-fnr-and-nr-across-files",
            ["{print FILENAME, FNR, NR, $1}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\n")],
            "/a.txt 1 1 a1\n/a.txt 2 2 a2\n/b.txt 1 3 b1\n")),

        Passing(
            "beginfile-and-endfile-for-files",
            ["BEGINFILE{print \"begin\", FILENAME, FNR} {print FILENAME, FNR, $1} ENDFILE{print \"end\", FILENAME, FNR}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\na2\n"), File("/b.txt", "b1\n")],
            "begin /a.txt 0\n/a.txt 1 a1\n/a.txt 2 a2\nend /a.txt 2\nbegin /b.txt 0\n/b.txt 1 b1\nend /b.txt 1\n"),

        Passing(
            "beginfile-missing-file-nextfile-suppresses-error",
            ["BEGINFILE{print \"begin\", FILENAME, ERRNO; nextfile} END{print \"end\"}", "/missing.txt"],
            [],
            "begin /missing.txt No such file or directory\nend\n"),

        Passing(
            "filename-and-fnr-for-stdin",
            ["{print FILENAME, FNR, NR, $1}"],
            [],
            "- 1 1 a\n- 2 2 c\n",
            "a b\nc d\n"),

        Passing(
            "beginfile-and-endfile-for-stdin",
            ["BEGINFILE{print \"begin\", FILENAME, FNR} {print FILENAME, FNR, $1} ENDFILE{print \"end\", FILENAME, FNR}"],
            [],
            "begin - 0\n- 1 a\nend - 1\n",
            "a b\n"),

        Passing(
            "empty-argv-skip-preserves-argind-and-counters",
            ["BEGIN{ARGV[1]=\"\"} {print ARGIND, FILENAME, FNR, NR, $1}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "2 /b.txt 1 1 b\n"),

        Failing(
            "beginfile-missing-file-without-nextfile-fails",
            ["BEGINFILE{print \"begin\", FILENAME, ERRNO} {print \"bad\"} ENDFILE{print \"end\"} END{print \"done\"}", "/missing.txt"],
            [],
            2,
            "begin /missing.txt No such file or directory\n",
            "awk: /missing.txt: No such file or directory\n"),

        Passing(
            "beginfile-nextfile-recovers-from-missing-file",
            ["BEGINFILE{if(ERRNO){print \"skip\", FILENAME, ERRNO; nextfile}else{print \"open\", FILENAME, \"[\" ERRNO \"]\"}} {print FILENAME, FNR, NR, $1} END{print \"end\", NR, FILENAME}", "/missing.txt", "/ok.txt"],
            [File("/ok.txt", "ok\n")],
            "skip /missing.txt No such file or directory\nopen /ok.txt []\n/ok.txt 1 1 ok\nend 1 /ok.txt\n"),

        Failing(
            "beginfile-exit-on-missing-file-runs-end-with-status",
            ["BEGINFILE{if(ERRNO){print \"begin\", FILENAME, ERRNO; exit 5}} {print \"bad\"} END{print \"end\", NR, FILENAME}", "/missing.txt", "/ok.txt"],
            [File("/ok.txt", "ok\n")],
            5,
            "begin /missing.txt No such file or directory\nend 0 /missing.txt\n",
            string.Empty),

        CompilerDiagnostic(Failing(
            "begin-combined-with-expression-fails",
            ["BEGIN && 1 {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        CompilerDiagnostic(Failing(
            "begin-end-range-pattern-fails",
            ["BEGIN, END {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        CompilerDiagnostic(Failing(
            "end-combined-with-expression-fails",
            ["END || 1 {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        CompilerDiagnostic(Failing(
            "beginfile-combined-with-expression-fails",
            ["BEGINFILE && 1 {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        CompilerDiagnostic(Failing(
            "beginfile-endfile-range-pattern-fails",
            ["BEGINFILE, ENDFILE {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        CompilerDiagnostic(Failing(
            "endfile-combined-with-expression-fails",
            ["ENDFILE || 1 {print}"],
            [],
            2,
            string.Empty,
            "awk: expected awk action block.\n")),

        Exiting(
            "beginfile-exit-skips-file-and-runs-end",
            ["BEGINFILE{print \"beginfile\", FILENAME, FNR; exit 6} {print \"bad\"} ENDFILE{print \"endfile\"} END{print \"end\", NR, FILENAME}", "/ok.txt"],
            [File("/ok.txt", "ok\n")],
            6,
            "beginfile /ok.txt 0\nend 0 /ok.txt\n"),

        Exiting(
            "endfile-exit-runs-end-with-status",
            ["{print \"main\", NR} ENDFILE{print \"endfile\", FNR; exit 7} END{print \"end\", NR, FILENAME}", "/ok.txt"],
            [File("/ok.txt", "ok\n")],
            7,
            "main 1\nendfile 1\nend 1 /ok.txt\n")
    ];
}
