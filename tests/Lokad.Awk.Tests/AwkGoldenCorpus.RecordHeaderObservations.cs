namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> RecordHeaderObservationCases()
    {
        yield return Passing("record-header-computed-symbol-table-reads",
            ["{n=\"N\" \"R\";f=\"F\" \"NR\";print SYMTAB[n],SYMTAB[f]}END{print SYMTAB[n],SYMTAB[f]}"],
            [], "1 1\n2 2\n2 2\n", "one\ntwo\n");
        yield return Passing("record-header-symbol-table-counter-write",
            ["{n=\"N\" \"R\";f=\"F\" \"NR\";if(SYMTAB[f]==1)SYMTAB[n]=\"007\";print SYMTAB[n]}END{print SYMTAB[n],SYMTAB[f]}"],
            [], "007\n8\n8 2\n", "one\ntwo\n");
        yield return Passing("record-header-counter-read-only-in-function-at-end",
            ["function count(){return NR} {sum+=$1}END{print sum,count()}"],
            [], "5 2\n", "2\n3\n");
        yield return Passing("record-header-computed-terminator-retained-at-eof",
            ["BEGIN{RS=\",\";k=\"R\" \"T\"}{print \"[\" SYMTAB[k] \"]\"}END{print \"[\" SYMTAB[k] \"]\"}"],
            [], "[,]\n[,]\n[,]\n", "a,b,");
        yield return Passing("record-header-computed-terminator-paragraph-eof-clear",
            ["BEGIN{RS=\"\";k=\"R\" \"T\";SYMTAB[k]=\"marker\"}END{print \"[\"SYMTAB[k]\"]\"}"],
            [], "[]\n", "\n\n");
        yield return Passing("record-header-terminator-read-only-in-function-after-exit",
            ["function term(){return RT}BEGIN{RS=\",\"}{n++;if(n==2)exit}END{print n,\"[\"term()\"]\"}"],
            [], "2 [,]\n", "a,b,c");
        yield return Passing("record-header-computed-terminator-redirected-getline",
            ["BEGIN{k=\"R\" \"T\";RS=\",\";getline x <\"/extra\";print x,\"[\"SYMTAB[k]\"]\";SYMTAB[k]=\"marker\";getline x <\"/extra\";print x,\"[\"SYMTAB[k]\"]\";close(\"/extra\")}"],
            [File("/extra", "a,b")], "a [,]\nb []\n");
        yield return Passing("record-header-unobserved-option-conversion-is-deferred",
            ["--non-decimal-data", "-v", "NR=0x1.8", "BEGIN{print \"before\"}"],
            [], "before\n");

        const string error = "awk: hexadecimal floating point constants require an exponent\n";
        yield return AwkGoldenCaseBuilder.Create("record-header-unobserved-option-counter-error",
                ["--non-decimal-data", "-v", "NR=0x1.8", "{print \"later\"}"], [])
            .WithStandardInput("one\n").ExitsWith(2).WithStdErr(error).Build();
        yield return AwkGoldenCaseBuilder.Create("record-header-unobserved-operand-counter-error",
                ["--non-decimal-data", "{print \"later\"}", "NR=0x1.8", "/rows"], [File("/rows", "one\n")])
            .ExitsWith(2).WithStdErr(error).Build();
        yield return AwkGoldenCaseBuilder.Create("record-header-mutable-argv-counter-error",
                ["--non-decimal-data", "BEGIN{print \"before\";ARGV[1]=\"NR=0x1.8\";ARGV[2]=\"/rows\";ARGC=3}{print \"later\"}"],
                [File("/rows", "one\n")])
            .ExitsWith(2).WithStdOut("before\n").WithStdErr(error).Build();
    }
}
