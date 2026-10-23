namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FlushCompatibilityCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-0",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"unopened\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `unopened' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-1",
                ["BEGIN{ERRNO=\"keep\";r=fflush(\"unopened\");print r,\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `unopened' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-2",
                ["BEGIN{ERRNO=\"keep\";fflush(\"unopened\");print \"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("[keep]\n")
            .WithStdErr("awk: warning: fflush: `unopened' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-3",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"/missing.txt\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `/missing.txt' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-4",
                ["BEGIN{ERRNO=\"keep\";r=fflush(\"/missing.txt\");print r,\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `/missing.txt' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-5",
                ["BEGIN{ERRNO=\"keep\";fflush(\"/missing.txt\");print \"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("[keep]\n")
            .WithStdErr("awk: warning: fflush: `/missing.txt' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-6",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"space name\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `space name' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-7",
                ["BEGIN{ERRNO=\"keep\";r=fflush(\"space name\");print r,\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `space name' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-8",
                ["BEGIN{ERRNO=\"keep\";fflush(\"space name\");print \"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("[keep]\n")
            .WithStdErr("awk: warning: fflush: `space name' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-9",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"quote'name\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `quote'name' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-10",
                ["BEGIN{ERRNO=\"keep\";r=fflush(\"quote'name\");print r,\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `quote'name' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-11",
                ["BEGIN{ERRNO=\"keep\";fflush(\"quote'name\");print \"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("[keep]\n")
            .WithStdErr("awk: warning: fflush: `quote'name' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-12",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"é😀\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `é😀' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-13",
                ["BEGIN{ERRNO=\"keep\";r=fflush(\"é😀\");print r,\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `é😀' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-14",
                ["BEGIN{ERRNO=\"keep\";fflush(\"é😀\");print \"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("[keep]\n")
            .WithStdErr("awk: warning: fflush: `é😀' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-15",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"/dev/stdin\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `/dev/stdin' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-16",
                ["BEGIN{ERRNO=\"keep\";r=fflush(\"/dev/stdin\");print r,\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `/dev/stdin' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-17",
                ["BEGIN{ERRNO=\"keep\";fflush(\"/dev/stdin\");print \"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("[keep]\n")
            .WithStdErr("awk: warning: fflush: `/dev/stdin' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-18",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"/dev/stdout\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-19",
                ["BEGIN{ERRNO=\"keep\";r=fflush(\"/dev/stdout\");print r,\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-20",
                ["BEGIN{ERRNO=\"keep\";fflush(\"/dev/stdout\");print \"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("[keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-21",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"/dev/stderr\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-22",
                ["BEGIN{ERRNO=\"keep\";r=fflush(\"/dev/stderr\");print r,\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-23",
                ["BEGIN{ERRNO=\"keep\";fflush(\"/dev/stderr\");print \"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("[keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-24",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"/dev/fd/0\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `/dev/fd/0' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-25",
                ["BEGIN{ERRNO=\"keep\";r=fflush(\"/dev/fd/0\");print r,\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `/dev/fd/0' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-26",
                ["BEGIN{ERRNO=\"keep\";fflush(\"/dev/fd/0\");print \"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("[keep]\n")
            .WithStdErr("awk: warning: fflush: `/dev/fd/0' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-27",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"/dev/fd/1\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-28",
                ["BEGIN{ERRNO=\"keep\";r=fflush(\"/dev/fd/1\");print r,\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-29",
                ["BEGIN{ERRNO=\"keep\";fflush(\"/dev/fd/1\");print \"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("[keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-30",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"/dev/fd/2\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-31",
                ["BEGIN{ERRNO=\"keep\";r=fflush(\"/dev/fd/2\");print r,\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-unopened-32",
                ["BEGIN{ERRNO=\"keep\";fflush(\"/dev/fd/2\");print \"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("[keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-original-empty-errno",
                ["BEGIN{print fflush(\"unopened\"),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 \n")
            .WithStdErr("awk: warning: fflush: `unopened' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-output-state-34",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;print fflush(p),fflush(p),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 0 [keep]\n")
            .WithStdErr("")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-output-state-35",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;fflush(p);print close(p),close(p),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 -1 [close of redirection that was never opened]\n")
            .WithStdErr("")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-output-state-36",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;fflush(p);print \"y\">p;print fflush(p),fflush(p);print close(p);print \"z\">p}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 0\n0\n")
            .WithStdErr("")
            .WithExpectedFiles([File("/out.txt", "z\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-output-state-37",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;print fflush(),fflush(p),fflush(\"\");print close(p)}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 0 0\n0\n")
            .WithStdErr("")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-output-state-38",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;print fflush(p);getline s<p;print s;print fflush(p),close(p)}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0\nx\n-1 0\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `out.txt' opened for reading, not writing\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-output-state-39",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;close(p);print fflush(p),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `out.txt' is not an open file, pipe or co-process\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-output-state-40",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;fflush(p);close(p);print fflush(p),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `out.txt' is not an open file, pipe or co-process\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-output-state-41",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;print \"y\">\"out2.txt\";fflush();print fflush(p),fflush(\"out2.txt\");close(p);close(\"out2.txt\")}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 0\n")
            .WithStdErr("")
            .WithExpectedFiles([File("/out.txt", "x\n"), File("/out2.txt", "y\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-output-state-42",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;print \"y\">\"out2.txt\";fflush(\"\");print fflush(p),fflush(\"out2.txt\");close(p);close(\"out2.txt\")}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 0\n")
            .WithStdErr("")
            .WithExpectedFiles([File("/out.txt", "x\n"), File("/out2.txt", "y\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-read-state-43",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"input.txt\"),\"[\"ERRNO\"]\"}"], [File("/input.txt", "line\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("line\n")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `input.txt' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-read-state-44",
                ["BEGIN{ERRNO=\"keep\";fflush(\"input.txt\");print \"[\"ERRNO\"]\"}"], [File("/input.txt", "line\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("line\n")
            .WithStdOut("[keep]\n")
            .WithStdErr("awk: warning: fflush: `input.txt' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-read-state-45",
                ["BEGIN{ERRNO=\"keep\";getline s<\"input.txt\";print fflush(\"input.txt\"),\"[\"ERRNO\"]\"}"], [File("/input.txt", "line\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("line\n")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `input.txt' opened for reading, not writing\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-read-state-46",
                ["BEGIN{ERRNO=\"keep\";getline s<\"input.txt\";fflush(\"input.txt\");print \"[\"ERRNO\"]\"}"], [File("/input.txt", "line\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("line\n")
            .WithStdOut("[keep]\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `input.txt' opened for reading, not writing\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-read-state-47",
                ["BEGIN{ERRNO=\"keep\";getline s<\"/dev/stdin\";print fflush(\"/dev/stdin\"),\"[\"ERRNO\"]\"}"], [File("/input.txt", "line\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("line\n")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `/dev/stdin' opened for reading, not writing\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-read-state-48",
                ["BEGIN{ERRNO=\"keep\";getline s<\"/dev/stdin\";fflush(\"/dev/stdin\");print \"[\"ERRNO\"]\"}"], [File("/input.txt", "line\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("line\n")
            .WithStdOut("[keep]\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `/dev/stdin' opened for reading, not writing\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-read-state-49",
                ["BEGIN{ERRNO=\"keep\";getline s<\"/dev/fd/0\";print fflush(\"/dev/fd/0\"),\"[\"ERRNO\"]\"}"], [File("/input.txt", "line\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("line\n")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `/dev/fd/0' opened for reading, not writing\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-read-state-50",
                ["BEGIN{ERRNO=\"keep\";getline s<\"/dev/fd/0\";fflush(\"/dev/fd/0\");print \"[\"ERRNO\"]\"}"], [File("/input.txt", "line\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("line\n")
            .WithStdOut("[keep]\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `/dev/fd/0' opened for reading, not writing\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-59",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"missing\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-60",
                ["BEGIN{ERRNO=\"keep\";print fflush(123),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-61",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"123\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-62",
                ["BEGIN{ERRNO=\"keep\";print fflush(x),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-63",
                ["BEGIN{ERRNO=\"keep\";print fflush(a[1]),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-64",
                ["BEGIN{ERRNO=\"keep\";print fflush(\"\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-65",
                ["--posix", "BEGIN{ERRNO=\"keep\";print fflush(\"missing\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-66",
                ["--posix", "BEGIN{ERRNO=\"keep\";print fflush(123),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-67",
                ["--posix", "BEGIN{ERRNO=\"keep\";print fflush(\"123\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-68",
                ["--posix", "BEGIN{ERRNO=\"keep\";print fflush(x),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-69",
                ["--posix", "BEGIN{ERRNO=\"keep\";print fflush(a[1]),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-70",
                ["--posix", "BEGIN{ERRNO=\"keep\";print fflush(\"\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-71",
                ["--traditional", "BEGIN{ERRNO=\"keep\";print fflush(\"missing\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-72",
                ["--traditional", "BEGIN{ERRNO=\"keep\";print fflush(123),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-73",
                ["--traditional", "BEGIN{ERRNO=\"keep\";print fflush(\"123\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-74",
                ["--traditional", "BEGIN{ERRNO=\"keep\";print fflush(x),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-75",
                ["--traditional", "BEGIN{ERRNO=\"keep\";print fflush(a[1]),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-76",
                ["--traditional", "BEGIN{ERRNO=\"keep\";print fflush(\"\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-77",
                ["--lint", "BEGIN{ERRNO=\"keep\";print fflush(\"missing\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-78",
                ["--lint", "BEGIN{ERRNO=\"keep\";print fflush(123),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: received non-string argument\nawk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-79",
                ["--lint", "BEGIN{ERRNO=\"keep\";print fflush(\"123\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-82",
                ["--lint", "BEGIN{ERRNO=\"keep\";print fflush(\"\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-83",
                ["--lint=fatal", "BEGIN{ERRNO=\"keep\";print fflush(\"missing\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-84",
                ["--lint=fatal", "BEGIN{ERRNO=\"keep\";print fflush(123),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("")
            .WithStdErr("awk: fatal: fflush: received non-string argument\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-85",
                ["--lint=fatal", "BEGIN{ERRNO=\"keep\";print fflush(\"123\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 [keep]\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-profile-88",
                ["--lint=fatal", "BEGIN{ERRNO=\"keep\";print fflush(\"\"),\"[\"ERRNO\"]\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 [keep]\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-effects-89",
                ["BEGIN{print fflush(a[1]);print typeof(a[1])}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0\nuntyped\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-effects-90",
                ["BEGIN{print close(a[1]);print typeof(a[1])}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1\nuntyped\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-effects-91",
                ["function f(){i++;return \"missing\"}BEGIN{ERRNO=\"keep\";r=fflush(f());print r,i,ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 1 keep\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-effects-92",
                ["function f(p){fflush(p)}BEGIN{ERRNO=\"keep\";f(\"missing\");print ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("keep\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-effects-93",
                ["BEGIN{ERRNO=\"keep\";close(\"missing\");print ERRNO;fflush(\"missing\");print ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("close of redirection that was never opened\nclose of redirection that was never opened\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-effects-94",
                ["BEGIN{ERRNO=\"keep\";fflush(\"out.txt\");print \"x\">\"out.txt\";print fflush(\"out.txt\"),close(\"out.txt\"),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0 0 keep\n")
            .WithStdErr("awk: warning: fflush: `out.txt' is not an open file, pipe or co-process\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-effects-95",
                ["BEGIN{ERRNO=\"keep\";fflush(\"missing\");fflush(\"missing\");print ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("keep\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\nawk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-alias-96",
                ["BEGIN{ERRNO=\"keep\";print \"x\">\"out.txt\";print fflush(\"./out.txt\"),ERRNO;close(\"out.txt\")}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 keep\n")
            .WithStdErr("awk: warning: fflush: `./out.txt' is not an open file, pipe or co-process\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-alias-97",
                ["BEGIN{ERRNO=\"keep\";print \"x\">\"out.txt\";print fflush(\"/out.txt\"),ERRNO;close(\"out.txt\")}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 keep\n")
            .WithStdErr("awk: warning: fflush: `/out.txt' is not an open file, pipe or co-process\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-alias-98",
                ["BEGIN{ERRNO=\"keep\";print \"x\">\"out.txt\";print fflush(\"d/../out.txt\"),ERRNO;close(\"out.txt\")}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1 keep\n")
            .WithStdErr("awk: warning: fflush: `d/../out.txt' is not an open file, pipe or co-process\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-mixed-file-99",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";getline s<p;print \"x\">p;print fflush(p);print close(p),fflush(p),close(p)}"], [File("/out.txt", "before\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0\n0 -1 0\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `out.txt' opened for reading, not writing\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-mixed-file-100",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;fflush(p);getline s<p;print \"y\">p;print fflush(p);print close(p),fflush(p),close(p)}"], [File("/out.txt", "before\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1\n0 0 0\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `out.txt' opened for reading, not writing\n")
            .WithExpectedFiles([File("/out.txt", "x\ny\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-mixed-file-101",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;fflush(p);getline s<p;print fflush(p);print close(p),fflush(p),close(p)}"], [File("/out.txt", "before\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1\n0 0 0\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `out.txt' opened for reading, not writing\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-mixed-file-102",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;getline s<p;print fflush(p);print close(p),fflush(p),close(p)}"], [File("/out.txt", "before\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1\n0 0 0\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `out.txt' opened for reading, not writing\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-mixed-file-103",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";getline s<p;print \"x\">p;getline s<p;print fflush(p);close(p);print fflush(p);close(p)}"], [File("/out.txt", "before\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0\n-1\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `out.txt' opened for reading, not writing\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-mixed-file-104",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;fflush(p);getline s<p;fflush();print fflush(p);close(p);print fflush(p);close(p)}"], [File("/out.txt", "before\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1\n0\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `out.txt' opened for reading, not writing\n")
            .WithExpectedFiles([File("/out.txt", "x\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-mixed-file-105",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";getline s<p;print fflush(p);close(p);print fflush(p)}"], [File("/out.txt", "before\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("-1\n-1\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `out.txt' opened for reading, not writing\nawk: warning: fflush: `out.txt' is not an open file, pipe or co-process\n")
            .WithExpectedFiles([File("/out.txt", "before\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-mixed-file-106",
                ["BEGIN{p=\"out.txt\";ERRNO=\"keep\";print \"x\">p;fflush(p);close(p);print \"y\">p;print fflush(p);close(p)}"], [File("/out.txt", "before\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("")
            .WithStdOut("0\n")
            .WithStdErr("")
            .WithExpectedFiles([File("/out.txt", "y\n")])
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-110",
                ["BEGIN{ERRNO=\"keep\";n=123;fflush(n);print ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("keep\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-111",
                ["BEGIN{ERRNO=\"keep\";n=123;print n;print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("123\n-1 keep\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-112",
                ["BEGIN{ERRNO=\"keep\";n=123;t=n \"\";print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("-1 keep\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-113",
                ["BEGIN{ERRNO=\"keep\";n=\"123\";n+=0;print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("-1 keep\n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-114",
                ["BEGIN{ERRNO=\"keep\";getline n;print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("-1 \n")
            .WithStdErr("awk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-115",
                ["BEGIN{ERRNO=\"keep\";print \"before\";fflush(\"missing\");print \"after\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("before\nafter\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-116",
                ["BEGIN{ERRNO=\"keep\";fflush(\"/dev/stdout\");fflush(\"/dev/stderr\");print ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("keep\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-117",
                ["--lint", "BEGIN{ERRNO=\"keep\";n=123;fflush(n);print ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("keep\n")
            .WithStdErr("awk: warning: fflush: received non-string argument\nawk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-118",
                ["--lint", "BEGIN{ERRNO=\"keep\";n=123;print n;print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("123\n-1 keep\n")
            .WithStdErr("awk: warning: fflush: received non-string argument\nawk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-119",
                ["--lint", "BEGIN{ERRNO=\"keep\";n=123;t=n \"\";print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("-1 keep\n")
            .WithStdErr("awk: warning: fflush: received non-string argument\nawk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-120",
                ["--lint", "BEGIN{ERRNO=\"keep\";n=\"123\";n+=0;print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("-1 keep\n")
            .WithStdErr("awk: warning: fflush: received non-string argument\nawk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-121",
                ["--lint", "BEGIN{ERRNO=\"keep\";getline n;print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("-1 \n")
            .WithStdErr("awk: warning: fflush: received non-string argument\nawk: warning: fflush: `123' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-122",
                ["--lint", "BEGIN{ERRNO=\"keep\";print \"before\";fflush(\"missing\");print \"after\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("before\nafter\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-123",
                ["--lint", "BEGIN{ERRNO=\"keep\";fflush(\"/dev/stdout\");fflush(\"/dev/stderr\");print ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("keep\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-124",
                ["--lint=fatal", "BEGIN{ERRNO=\"keep\";n=123;fflush(n);print ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("")
            .WithStdErr("awk: fatal: fflush: received non-string argument\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-125",
                ["--lint=fatal", "BEGIN{ERRNO=\"keep\";n=123;print n;print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("123\n")
            .WithStdErr("awk: fatal: fflush: received non-string argument\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-126",
                ["--lint=fatal", "BEGIN{ERRNO=\"keep\";n=123;t=n \"\";print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("")
            .WithStdErr("awk: fatal: fflush: received non-string argument\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-127",
                ["--lint=fatal", "BEGIN{ERRNO=\"keep\";n=\"123\";n+=0;print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("")
            .WithStdErr("awk: fatal: fflush: received non-string argument\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-128",
                ["--lint=fatal", "BEGIN{ERRNO=\"keep\";getline n;print fflush(n),ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("")
            .WithStdErr("awk: fatal: fflush: received non-string argument\n")
            .ExitsWith(2)
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-129",
                ["--lint=fatal", "BEGIN{ERRNO=\"keep\";print \"before\";fflush(\"missing\");print \"after\"}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("before\nafter\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-profile-130",
                ["--lint=fatal", "BEGIN{ERRNO=\"keep\";fflush(\"/dev/stdout\");fflush(\"/dev/stderr\");print ERRNO}"], [])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("123\n")
            .WithStdOut("keep\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-read-131",
                ["BEGIN{ERRNO=\"keep\";print (getline s<\"missing\");print fflush(\"missing\"),ERRNO}"], [File("/input.txt", "line\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("line\n")
            .WithStdOut("-1\n-1 No such file or directory\n")
            .WithStdErr("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-read-132",
                ["BEGIN{ERRNO=\"keep\";getline s<\"/dev/stdin\";print fflush(\"/dev/stdin\"),close(\"/dev/stdin\"),fflush(\"/dev/stdin\"),ERRNO}"], [File("/input.txt", "line\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("line\n")
            .WithStdOut("-1 0 -1 keep\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `/dev/stdin' opened for reading, not writing\nawk: warning: fflush: `/dev/stdin' is not an open file, pipe or co-process\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("flush-extra-read-133",
                ["BEGIN{ERRNO=\"keep\";getline s<\"input.txt\";getline s<\"input.txt\";print fflush(\"input.txt\"),ERRNO}"], [File("/input.txt", "line\n")])
            .WithInputDirectories([new AwkGoldenPath("/d")])
            .WithStandardInput("line\n")
            .WithStdOut("-1 keep\n")
            .WithStdErr("awk: warning: fflush: cannot flush: file `input.txt' opened for reading, not writing\n")
            .Build();

    }
}
