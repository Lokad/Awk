namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> SpecialArrayReadCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-0",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in SYMTAB);print SYMTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-1",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in SYMTAB);print typeof(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-2",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in SYMTAB);print isarray(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-3",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in SYMTAB);print length(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-4",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in SYMTAB);print SYMTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-5",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in SYMTAB);print \"[\" SYMTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-6",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in SYMTAB);print f(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-7",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in SYMTAB);print (0?7:SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-8",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in SYMTAB);print SYMTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-9",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in SYMTAB);print typeof(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-10",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in SYMTAB);print isarray(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-11",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in SYMTAB);print length(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-12",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in SYMTAB);print SYMTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-13",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in SYMTAB);print \"[\" SYMTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-14",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in SYMTAB);print f(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-15",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in SYMTAB);print (0?7:SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-16",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in SYMTAB);print SYMTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-17",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in SYMTAB);print typeof(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-18",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in SYMTAB);print isarray(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-19",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in SYMTAB);print length(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-20",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in SYMTAB);print SYMTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-21",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in SYMTAB);print \"[\" SYMTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-22",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in SYMTAB);print f(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-23",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in SYMTAB);print (0?7:SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-24",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in SYMTAB);print SYMTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-25",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in SYMTAB);print typeof(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-26",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in SYMTAB);print isarray(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-27",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in SYMTAB);print length(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-28",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in SYMTAB);print SYMTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-29",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in SYMTAB);print \"[\" SYMTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-30",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in SYMTAB);print f(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-31",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in SYMTAB);print (0?7:SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-32",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in SYMTAB);print SYMTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-33",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in SYMTAB);print typeof(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-34",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in SYMTAB);print isarray(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-35",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in SYMTAB);print length(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-36",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in SYMTAB);print SYMTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-37",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in SYMTAB);print \"[\" SYMTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-38",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in SYMTAB);print f(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-39",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in SYMTAB);print (0?7:SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-40",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in SYMTAB);print SYMTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-41",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in SYMTAB);print typeof(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-42",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in SYMTAB);print isarray(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-43",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in SYMTAB);print length(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-44",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in SYMTAB);print SYMTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-45",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in SYMTAB);print \"[\" SYMTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-46",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in SYMTAB);print f(SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-SYMTAB-47",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in SYMTAB);print (0?7:SYMTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-48",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in FUNCTAB);print FUNCTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-49",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in FUNCTAB);print typeof(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-50",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in FUNCTAB);print isarray(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-51",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in FUNCTAB);print length(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-52",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in FUNCTAB);print FUNCTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-53",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in FUNCTAB);print \"[\" FUNCTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-54",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in FUNCTAB);print f(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-55",
                ["function f(x){return x}BEGIN{k=\"absent\";print \"before\",(k in FUNCTAB);print (0?7:FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-56",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in FUNCTAB);print FUNCTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-57",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in FUNCTAB);print typeof(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-58",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in FUNCTAB);print isarray(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-59",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in FUNCTAB);print length(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-60",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in FUNCTAB);print FUNCTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-61",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in FUNCTAB);print \"[\" FUNCTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-62",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in FUNCTAB);print f(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-63",
                ["function f(x){return x}BEGIN{k=\"missing1\";print \"before\",(k in FUNCTAB);print (0?7:FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"missing1\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-64",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in FUNCTAB);print FUNCTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-65",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in FUNCTAB);print typeof(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-66",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in FUNCTAB);print isarray(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-67",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in FUNCTAB);print length(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-68",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in FUNCTAB);print FUNCTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-69",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in FUNCTAB);print \"[\" FUNCTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-70",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in FUNCTAB);print f(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-71",
                ["function f(x){return x}BEGIN{k=\"\";print \"before\",(k in FUNCTAB);print (0?7:FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-72",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in FUNCTAB);print FUNCTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-73",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in FUNCTAB);print typeof(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-74",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in FUNCTAB);print isarray(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-75",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in FUNCTAB);print length(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-76",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in FUNCTAB);print FUNCTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-77",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in FUNCTAB);print \"[\" FUNCTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-78",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in FUNCTAB);print f(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-79",
                ["function f(x){return x}BEGIN{k=\"1bad\";print \"before\",(k in FUNCTAB);print (0?7:FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"1bad\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-80",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in FUNCTAB);print FUNCTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-81",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in FUNCTAB);print typeof(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-82",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in FUNCTAB);print isarray(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-83",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in FUNCTAB);print length(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-84",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in FUNCTAB);print FUNCTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-85",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in FUNCTAB);print \"[\" FUNCTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-86",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in FUNCTAB);print f(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-87",
                ["function f(x){return x}BEGIN{k=\"é😀\";print \"before\",(k in FUNCTAB);print (0?7:FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"é😀\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-88",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in FUNCTAB);print FUNCTAB[k];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-89",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in FUNCTAB);print typeof(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-90",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in FUNCTAB);print isarray(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-91",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in FUNCTAB);print length(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-92",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in FUNCTAB);print FUNCTAB[k]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-93",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in FUNCTAB);print \"[\" FUNCTAB[k] \"]\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-94",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in FUNCTAB);print f(FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-missing-FUNCTAB-95",
                ["function f(x){return x}BEGIN{k=\"a,b\";print \"before\",(k in FUNCTAB);print (0?7:FUNCTAB[k]);print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"a,b\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-membership-96",
                ["BEGIN{k=\"absent\";print (k in SYMTAB);for(j=0;j<3;j++)print (k in SYMTAB);print \"after\"}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n0\n0\n0\nafter\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-membership-97",
                ["BEGIN{k=\"\";print (k in SYMTAB);for(j=0;j<3;j++)print (k in SYMTAB);print \"after\"}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n0\n0\n0\nafter\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-membership-98",
                ["BEGIN{k=\"1bad\";print (k in SYMTAB);for(j=0;j<3;j++)print (k in SYMTAB);print \"after\"}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n0\n0\n0\nafter\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-membership-99",
                ["BEGIN{k=\"é😀\";print (k in SYMTAB);for(j=0;j<3;j++)print (k in SYMTAB);print \"after\"}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n0\n0\n0\nafter\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-membership-100",
                ["BEGIN{k=\"SYMTAB\";print (k in SYMTAB);for(j=0;j<3;j++)print (k in SYMTAB);print \"after\"}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n0\n0\n0\nafter\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-membership-101",
                ["BEGIN{k=\"absent\";print (k in FUNCTAB);for(j=0;j<3;j++)print (k in FUNCTAB);print \"after\"}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n0\n0\n0\nafter\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-membership-102",
                ["BEGIN{k=\"\";print (k in FUNCTAB);for(j=0;j<3;j++)print (k in FUNCTAB);print \"after\"}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n0\n0\n0\nafter\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-membership-103",
                ["BEGIN{k=\"1bad\";print (k in FUNCTAB);for(j=0;j<3;j++)print (k in FUNCTAB);print \"after\"}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n0\n0\n0\nafter\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-membership-104",
                ["BEGIN{k=\"é😀\";print (k in FUNCTAB);for(j=0;j<3;j++)print (k in FUNCTAB);print \"after\"}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n0\n0\n0\nafter\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-membership-105",
                ["BEGIN{k=\"SYMTAB\";print (k in FUNCTAB);for(j=0;j<3;j++)print (k in FUNCTAB);print \"after\"}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n0\n0\n0\nafter\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-effect-106",
                ["function f(){print \"index\";i++;return \"absent\"}BEGIN{print \"before\";print typeof(SYMTAB[f()]);print i}"], [])
            .ExitsWith(2)
            .WithStdOut("before\nindex\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-effect-107",
                ["function f(){print \"index\";i++;return \"absent\"}BEGIN{print \"before\";print isarray(SYMTAB[f()]);print i}"], [])
            .ExitsWith(2)
            .WithStdOut("before\nindex\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-effect-108",
                ["function f(){print \"index\";i++;return \"absent\"}BEGIN{print \"before\";print SYMTAB[f()];print i}"], [])
            .ExitsWith(2)
            .WithStdOut("before\nindex\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-effect-109",
                ["function f(){print \"index\";i++;return \"absent\"}BEGIN{print \"before\";print typeof(FUNCTAB[f()]);print i}"], [])
            .ExitsWith(2)
            .WithStdOut("before\nindex\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-effect-110",
                ["function f(){print \"index\";i++;return \"absent\"}BEGIN{print \"before\";print isarray(FUNCTAB[f()]);print i}"], [])
            .ExitsWith(2)
            .WithStdOut("before\nindex\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-effect-111",
                ["function f(){print \"index\";i++;return \"absent\"}BEGIN{print \"before\";print FUNCTAB[f()];print i}"], [])
            .ExitsWith(2)
            .WithStdOut("before\nindex\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-112",
                ["BEGIN{print (\"x\" in SYMTAB);print SYMTAB[\"x\"];print typeof(x)}"], [])
            .ExitsWith(0)
            .WithStdOut("1\n\nuntyped\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-113",
                ["BEGIN{print (\"x\" in SYMTAB);print typeof(SYMTAB[\"x\"]);print typeof(x)}"], [])
            .ExitsWith(0)
            .WithStdOut("1\nunassigned\nuntyped\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-114",
                ["BEGIN{print (\"x\" in SYMTAB);print isarray(SYMTAB[\"x\"]);print typeof(x)}"], [])
            .ExitsWith(0)
            .WithStdOut("1\n0\nuntyped\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-115",
                ["BEGIN{print (\"x\" in SYMTAB);print SYMTAB[\"x\"];x=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\n\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-116",
                ["BEGIN{print (\"x\" in SYMTAB);print typeof(SYMTAB[\"x\"]);x=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\nunassigned\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-117",
                ["BEGIN{print (\"x\" in SYMTAB);print isarray(SYMTAB[\"x\"]);x=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\n0\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-118",
                ["BEGIN{print (\"x\" in SYMTAB);print SYMTAB[\"x\"];if(0)x=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\n\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-119",
                ["BEGIN{print (\"x\" in SYMTAB);print typeof(SYMTAB[\"x\"]);if(0)x=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\nunassigned\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-120",
                ["BEGIN{print (\"x\" in SYMTAB);print isarray(SYMTAB[\"x\"]);if(0)x=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\n0\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-121",
                ["BEGIN{print (\"a\" in SYMTAB);print SYMTAB[\"a\"];ENDMARK=1; a[1]=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\n\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-122",
                ["BEGIN{print (\"a\" in SYMTAB);print typeof(SYMTAB[\"a\"]);ENDMARK=1; a[1]=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\nunassigned\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-123",
                ["BEGIN{print (\"a\" in SYMTAB);print isarray(SYMTAB[\"a\"]);ENDMARK=1; a[1]=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\n0\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-124",
                ["BEGIN{print (\"a\" in SYMTAB);print SYMTAB[\"a\"];if(0)a[1]=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\n\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-125",
                ["BEGIN{print (\"a\" in SYMTAB);print typeof(SYMTAB[\"a\"]);if(0)a[1]=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\nunassigned\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-known-126",
                ["BEGIN{print (\"a\" in SYMTAB);print isarray(SYMTAB[\"a\"]);if(0)a[1]=1}"], [])
            .ExitsWith(0)
            .WithStdOut("1\n0\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-function-global-127",
                ["function f(local){global=1;local=2}BEGIN{print (\"global\" in SYMTAB),(\"local\" in SYMTAB);print SYMTAB[\"global\"]}"], [])
            .ExitsWith(0)
            .WithStdOut("1 0\n\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-function-global-128",
                ["function f(local){global=1;local=2}BEGIN{print (\"global\" in SYMTAB),(\"local\" in SYMTAB);print typeof(SYMTAB[\"global\"])}"], [])
            .ExitsWith(0)
            .WithStdOut("1 0\nunassigned\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-function-global-129",
                ["function f(local){global=1;local=2}BEGIN{print (\"global\" in SYMTAB),(\"local\" in SYMTAB);print isarray(SYMTAB[\"global\"])}"], [])
            .ExitsWith(0)
            .WithStdOut("1 0\n0\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-function-local-SYMTAB",
                ["function f(local){local=1}BEGIN{print \"before\",(\"local\" in SYMTAB);print SYMTAB[\"local\"]}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"local\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-function-local-FUNCTAB",
                ["function f(local){local=1}BEGIN{print \"before\",(\"local\" in FUNCTAB);print FUNCTAB[\"local\"]}"], [])
            .ExitsWith(2)
            .WithStdOut("before 0\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"local\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-control-132",
                ["function f(local){local=1}BEGIN{local=7;print SYMTAB[\"local\"],(\"local\" in SYMTAB)}"], [])
            .ExitsWith(0)
            .WithStdOut("7 1\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-control-133",
                ["function f(){return 1}BEGIN{print FUNCTAB[\"f\"],typeof(FUNCTAB[\"f\"]),isarray(FUNCTAB[\"f\"])}"], [])
            .ExitsWith(0)
            .WithStdOut("f string 0\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-control-134",
                ["BEGIN{print FUNCTAB[\"length\"],typeof(FUNCTAB[\"length\"]),isarray(FUNCTAB[\"length\"])}"], [])
            .ExitsWith(0)
            .WithStdOut("length string 0\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-control-135",
                ["BEGIN{x=2;print SYMTAB[\"x\"];SYMTAB[\"x\"]+=3;print x;SYMTAB[\"x\"]++;print x}"], [])
            .ExitsWith(0)
            .WithStdOut("2\n5\n6\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-control-136",
                ["BEGIN{print SYMTAB[\"FS\"]==\" \";SYMTAB[\"FS\"]=\":\";$0=\"a:b\";print $2}"], [])
            .ExitsWith(0)
            .WithStdOut("1\nb\n")
            .WithStdErr("")
            .WithNondeterminismNote("Lokad preserves its shared FS setter for SYMTAB writes; GNU bypasses the splitter update.")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-control-137",
                ["BEGIN{print (\"SYMTAB\" in SYMTAB);seen=0;for(k in SYMTAB)if(k==\"SYMTAB\")seen++;print seen}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n0\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-control-138",
                ["function f(local){local=1;global=2}BEGIN{seen=0;for(k in SYMTAB)if(k==\"global\")seen++;print seen;seen=0;for(k in SYMTAB)if(k==\"local\")seen++;print seen}"], [])
            .ExitsWith(0)
            .WithStdOut("1\n0\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-control-139",
                ["BEGIN{print (\"x\" in SYMTAB);print typeof(SYMTAB[\"x\"]),typeof(x);print (\"x\" in SYMTAB)}"], [])
            .ExitsWith(0)
            .WithStdOut("1\nunassigned untyped\n1\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-control-140",
                ["BEGIN{SYMTAB[\"created\"]=7;print SYMTAB[\"created\"],(\"created\" in SYMTAB)}"], [])
            .ExitsWith(0)
            .WithStdOut("7 1\n")
            .WithStdErr("")
            .WithNondeterminismNote("Lokad preserves its governed dynamic SYMTAB name creation policy; GNU rejects arbitrary writes.")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-alias-SYMTAB",
                ["function f(t){print \"inside\";print t[\"absent\"]}BEGIN{print \"before\";f(SYMTAB)}"], [])
            .ExitsWith(2)
            .WithStdOut("before\ninside\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-alias-member-SYMTAB",
                ["function f(t){print (\"absent\" in t)}BEGIN{f(SYMTAB)}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-alias-FUNCTAB",
                ["function f(t){print \"inside\";print t[\"absent\"]}BEGIN{print \"before\";f(FUNCTAB)}"], [])
            .ExitsWith(2)
            .WithStdOut("before\ninside\n")
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"absent\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-alias-member-FUNCTAB",
                ["function f(t){print (\"absent\" in t)}BEGIN{f(FUNCTAB)}"], [])
            .ExitsWith(0)
            .WithStdOut("0\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-self-145",
                ["BEGIN{print \"before\";print SYMTAB[\"SYMTAB\"]}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"SYMTAB\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-self-146",
                ["BEGIN{print \"before\";print typeof(SYMTAB[\"SYMTAB\"])}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"SYMTAB\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-self-147",
                ["BEGIN{print \"before\";print isarray(SYMTAB[\"SYMTAB\"])}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"SYMTAB\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-isarray-effects-148",
                ["function f(){i++;return 1}BEGIN{i=0;print isarray(i++),i}"], [])
            .ExitsWith(0)
            .WithStdOut("0 1\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-isarray-effects-149",
                ["function f(){i++;return 1}BEGIN{i=0;print isarray(i=5),i}"], [])
            .ExitsWith(0)
            .WithStdOut("0 5\n")
            .WithStdErr("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("special-array-reads-isarray-effects-150",
                ["function f(){i++;return 1}BEGIN{i=0;print isarray(f()),i}"], [])
            .ExitsWith(0)
            .WithStdOut("0 1\n")
            .WithStdErr("")
            .Build();

    }
}
