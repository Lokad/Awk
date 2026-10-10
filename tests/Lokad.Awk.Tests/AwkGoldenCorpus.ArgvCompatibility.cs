namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] ArgumentSchedulingCompatibilityRepairs() =>
    [
        Passing(
            "argv-begin-appends-input",
            ["BEGIN{ARGV[ARGC++]=\"b.txt\"}{print ARGIND,FILENAME,$0}", "a.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "1 a.txt a\n2 b.txt b\n"),
        Passing(
            "argc-shrink-selects-stdin",
            ["BEGIN{ARGC=1}{print ARGIND,FILENAME,$0}", "a.txt"],
            [File("/a.txt", "a\n")],
            "0 - stdin\n", "stdin\n"),
        Passing(
            "argc-shrink-skips-later-input",
            ["BEGIN{ARGC=2}{print ARGIND,FILENAME,$0}", "a.txt", "b.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "1 a.txt a\n"),
        Passing(
            "argc-fractional-value-truncates-for-scheduling",
            ["BEGIN{ARGC=2.1}{print ARGIND,FILENAME,$0}", "a.txt", "b.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "1 a.txt a\n"),
        Passing(
            "argv-replaces-assignment-value",
            ["BEGIN{ARGV[1]=\"x=9\";print x+0}{print x,$0}", "x=7", "a.txt"],
            [File("/a.txt", "a\n")],
            "0\n9 a\n"),
        Passing(
            "argv-file-becomes-assignment-and-stdin-fallback",
            ["BEGIN{ARGV[1]=\"x=9\"}{print ARGIND,FILENAME,x,$0}", "a.txt"],
            [File("/a.txt", "a\n")],
            "1 - 9 stdin\n", "stdin\n"),
        Passing(
            "argv-assignment-becomes-file",
            ["BEGIN{ARGV[1]=\"a.txt\";x=11}{print ARGIND,FILENAME,x,$0}", "x=7"],
            [File("/a.txt", "a\n")],
            "1 a.txt 11 a\n"),
        Passing(
            "argv-sparse-entries-stay-absent",
            ["BEGIN{ARGV[5]=\"b.txt\";ARGC=6}{print ARGIND,FILENAME,$0}END{print length(ARGV),(2 in ARGV),(3 in ARGV),(4 in ARGV)}", "a.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "1 a.txt a\n5 b.txt b\n3 0 0 0\n"),
        Passing(
            "argv-assignment-updates-live-argc",
            ["BEGIN{ARGV[2]=\"a.txt\";ARGV[3]=\"x=9\"}{print x+0,$0}END{print x,ARGC}", "ARGC=4"],
            [File("/a.txt", "a\n")],
            "0 a\n9 4\n"),
        Passing(
            "argv-zero-is-not-an-input-operand",
            ["BEGIN{ARGV[0]=\"b.txt\"}{print FILENAME,$0}", "a.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "a.txt a\n"),
        Passing(
            "argv-empty-after-file-does-not-change-argind",
            ["BEGIN{ARGV[2]=\"\"}{print ARGIND,FILENAME,$0}END{print ARGIND}", "a.txt", "ignored.txt"],
            [File("/a.txt", "a\n")],
            "1 a.txt a\n1\n"),
        Passing(
            "argv-assignment-only-stdin-retains-argind",
            ["{print ARGIND,FILENAME,x,$0}END{print ARGIND,FILENAME}", "x=7"],
            [], "1 - 7 stdin\n1 -\n", "stdin\n"),
        Passing(
            "argv-final-assignment-advances-argind",
            ["{print ARGIND,FILENAME,x,$0}END{print ARGIND,FILENAME,x}", "x=7", "a.txt", "x=9"],
            [File("/a.txt", "a\n")],
            "2 a.txt 7 a\n3 a.txt 9\n"),
        Passing(
            "argv-assigned-argind-is-preserved-through-stdin",
            ["BEGIN{ARGIND=9}BEGINFILE{print \"B\",ARGIND}{print ARGIND,FILENAME,$0}"],
            [], "B 9\n9 - stdin\n", "stdin\n"),
        Passing(
            "argv-argind-assignment-overrides-admitted-index",
            ["END{print ARGIND,FILENAME}", "ARGIND=9"],
            [], "9 -\n"),
        Passing(
            "argv-record-action-appends-input",
            ["{print ARGIND,FILENAME,$0;if(ARGIND==1&&FNR==1)ARGV[ARGC++]=\"b.txt\"}", "a.txt"],
            [File("/a.txt", "a\na2\n"), File("/b.txt", "b\n")],
            "1 a.txt a\n1 a.txt a2\n2 b.txt b\n"),
        Passing(
            "argv-endfile-appends-input",
            ["{print ARGIND,FILENAME,$0}ENDFILE{if(ARGIND==1)ARGV[ARGC++]=\"b.txt\";print \"E\",ARGIND,FILENAME}", "a.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "1 a.txt a\nE 1 a.txt\n2 b.txt b\nE 2 b.txt\n"),
        Passing(
            "argv-beginfile-appends-input",
            ["BEGINFILE{if(ARGIND==1)ARGV[ARGC++]=\"b.txt\"}{print ARGIND,FILENAME,$0}", "a.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "1 a.txt a\n2 b.txt b\n"),
        Passing(
            "argv-record-action-reclassifies-next-operand",
            ["{print ARGIND,FILENAME,x+0,$0;if(ARGIND==1)ARGV[2]=\"b.txt\"}END{print x+0}", "a.txt", "x=7"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "1 a.txt 0 a\n2 b.txt 0 b\n0\n"),
        Passing(
            "argv-endfile-reclassifies-file-as-assignment",
            ["{print FILENAME,$0}ENDFILE{ARGV[2]=\"x=9\"}END{print ARGIND,x}", "a.txt", "b.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "a.txt a\n2 9\n"),
        Passing(
            "argv-nextfile-admits-appended-input",
            ["{print FILENAME,$0;if(ARGIND==1){ARGV[ARGC++]=\"b.txt\";nextfile}}", "a.txt"],
            [File("/a.txt", "a\nskipped\n"), File("/b.txt", "b\n")],
            "a.txt a\nb.txt b\n"),
        Passing(
            "argv-dynamic-unicode-and-space-path",
            ["BEGIN{ARGV[ARGC++]=\"données space.txt\"}{print ARGIND,FILENAME,$0}", "a.txt"],
            [File("/a.txt", "a\n"), File("/données space.txt", "b\n")],
            "1 a.txt a\n2 données space.txt b\n"),
        Passing(
            "argv-explicit-stdin-appended-after-file",
            ["BEGIN{ARGV[ARGC++]=\"-\"}{print ARGIND,FILENAME,FNR,NR,$0}", "a.txt"],
            [File("/a.txt", "a\n")],
            "1 a.txt 1 1 a\n2 - 1 2 stdin\n", "stdin\n"),
        Passing(
            "argv-getline-observes-begin-edits",
            ["BEGIN{ARGC=1;getline;print \"B\",ARGIND,FILENAME,$0}{print \"M\",$0}", "a.txt"],
            [File("/a.txt", "a\n")],
            "B 0 - one\nM two\n", "one\ntwo\n"),
        Passing(
            "argv-source-file-is-excluded-from-live-operands",
            ["-f", "/program.awk", "a.txt"],
            [File("/program.awk", "BEGIN{print ARGC,ARGV[1];ARGV[ARGC++]=\"b.txt\"}{print FILENAME,$0}"),
                File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "2 a.txt\na.txt a\nb.txt b\n"),
        Passing(
            "argv-command-assignments-during-getline-update-global-variables",
            ["function read(x){while((getline)>0)print \"local\",x,$0;return x}BEGIN{print \"return\",read(42);print \"global\",x}", "x=7", "a.txt", "x=9", "b.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "local 42 a\nlocal 42 b\nreturn 42\nglobal 9\n"),
        Passing(
            "argv-final-nf-assignment-updates-the-record-special-variable",
            ["{print $0}END{print ARGIND,NF,\"[\"$0\"]\"}", "a.txt", "NF=1"],
            [File("/a.txt", "a b\n")],
            "a b\n2 1 [a]\n"),
        Failing(
            "argv-command-assignment-rejects-global-array-hidden-by-local-parameter",
            ["function read(x){getline;print x}BEGIN{x[1]=1;read(42)}", "x=7"],
            [], 2, "", "awk: attempt to use array `x' in a scalar context\n")
    ];
}
