
namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] CommandLineRuntimeCases() =>
    [
        Passing(
            "argc-argv-and-argind-for-files",
            ["BEGIN{print ARGC, ARGV[0], ARGV[1], ARGV[2]} {print ARGIND, FILENAME, $1}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a1\n"), File("/b.txt", "b1\n")],
            "3 awk /a.txt /b.txt\n1 /a.txt a1\n2 /b.txt b1\n"),

        Passing(
            "argc-argv-and-argind-for-stdin",
            ["BEGIN{print ARGC, ARGV[0], ARGIND} {print ARGIND, FILENAME, $1}"],
            [],
            "1 awk 0\n0 - a\n",
            "a b\n"),

        Passing(
            "explicit-stdin-input-argument",
            ["{print ARGC, ARGV[1], ARGIND, FILENAME, FNR, $1}", "-"],
            [],
            "2 - 1 - 1 a\n2 - 1 - 2 c\n",
            "a b\nc d\n"),

        Passing(
            "repeated-stdin-input-argument",
            ["{print FILENAME, FNR, NR, $1}", "-", "-"],
            [],
            "- 1 1 a\n- 2 2 b\n",
            "a\nb\n"),

        Passing(
            "dev-stdin-input-argument",
            ["{print ARGV[1], FILENAME, FNR, $1}", "/dev/stdin"],
            [],
            "/dev/stdin /dev/stdin 1 a\n",
            "a b\n"),

        Failing(
            "missing-input-file-without-beginfile-fails",
            ["{print FILENAME, FNR, NR, $1}", "/missing.txt", "/ok.txt"],
            [File("/ok.txt", "ok\n")],
            2,
            string.Empty,
            "awk: /missing.txt: No such file or directory\n"),

        FailingWithDirectories(
            "input-directory-without-beginfile-fails",
            ["{print FILENAME, FNR, NR, $1}", "/data", "/ok.txt"],
            ["/data"],
            [File("/ok.txt", "ok\n")],
            2,
            string.Empty,
            "awk: /data: Is a directory\n"),

        Passing(
            "argv-excludes-options-and-program-source",
            ["-F", ":", "-v", "prefix=SKU", "BEGIN{print ARGC, ARGV[0], ARGV[1], prefix}", "/file.txt"],
            [File("/file.txt", "a:b\n")],
            "2 awk /file.txt SKU\n"),

        PassingWithEnvironment(
            "numeric-string-command-line-argv-and-environ",
            ["-v", "x=00", "BEGIN{print typeof(x), (x==0), (x==\"00\"), (x?1:0); print typeof(ARGV[1]), (ARGV[1]==0), (ARGV[1]==\"00\"), (ARGV[1]?1:0); print typeof(ENVIRON[\"N\"]), (ENVIRON[\"N\"]==0), (ENVIRON[\"N\"]==\"00\"), (ENVIRON[\"N\"]?1:0); exit}", "00"],
            [],
            [new AwkEnvironmentVariable("N", "00")],
            "strnum 1 1 0\nstrnum 1 1 0\nstrnum 1 1 0\n"),

        Passing(
            "post-program-assignment-after-begin-before-input",
            ["BEGIN{print ARGC, ARGV[1], ARGV[2]; if(x==\"\") print \"begin-empty\"} {print x, $1}", "x=1", "/file.txt"],
            [File("/file.txt", "a\n")],
            "3 x=1 /file.txt\nbegin-empty\n1 a\n"),

        Passing(
            "post-program-assignment-between-input-files",
            ["{print FILENAME, x, $1}", "x=1", "/a.txt", "x=2", "/b.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "/a.txt 1 a\n/b.txt 2 b\n"),

        Passing(
            "delete-argv-skips-input-file",
            ["BEGIN{delete ARGV[1]} {print $0}", "/file.txt"],
            [File("/file.txt", "a\n")],
            string.Empty),

        Passing(
            "empty-argv-skips-input-file",
            ["BEGIN{ARGV[1]=\"\"} {print $0}", "/file.txt"],
            [File("/file.txt", "a\n")],
            string.Empty),

        Passing(
            "delete-argv-skips-command-assignment",
            ["BEGIN{delete ARGV[1]} {print x, $1}", "x=1"],
            [],
            " a\n",
            "a\n"),

        Passing(
            "mutated-argv-replaces-input-file",
            ["BEGIN{ARGV[1]=\"/b.txt\"} {print FILENAME, $1}", "/a.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "/b.txt b\n")
    ];
}
