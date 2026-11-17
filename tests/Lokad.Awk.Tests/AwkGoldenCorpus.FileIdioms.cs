namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // Expected results captured with pinned GNU awk 5.3.2; scripts remain independent fixtures.
    private static IEnumerable<AwkGoldenCase> FileIdiomCases()
    {
        yield return AwkGoldenCaseBuilder.Create("idiom-file-two-file-lookup-join",
            ["NR==FNR{label[$1]=$2;next}{print $1,label[$1],$2}", "names.tsv", "data.tsv"],
            [File("/names.tsv", "a Alpha\nb Beta\n"), File("/data.tsv", "b 3\na 7\n")])
            .WithStandardInput("").WithStdOut("b Beta 3\na Alpha 7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-record-counters-reset",
            ["{print FILENAME,FNR,NR,$1}", "left.txt", "right.txt"],
            [File("/left.txt", "a\nb\n"), File("/right.txt", "c\n")])
            .WithStandardInput("").WithStdOut("left.txt 1 1 a\nleft.txt 2 2 b\nright.txt 1 3 c\n").Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-beginfile-endfile-empty-input",
            ["BEGINFILE{print \"begin\",FILENAME,FNR}{n++}ENDFILE{print \"end\",FILENAME,FNR}END{print \"total\",n}", "data.txt", "empty.txt"],
            [File("/data.txt", "a\nb\n"), File("/empty.txt", "")])
            .WithStandardInput("").WithStdOut("begin data.txt 0\nend data.txt 2\nbegin empty.txt 0\nend empty.txt 0\ntotal 2\n").Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-getline-build-lookup",
            ["BEGIN{while((getline line<\"lookup.txt\")>0){split(line,p);v[p[1]]=p[2]}close(\"lookup.txt\")}{print $1,v[$1]}"],
            [File("/lookup.txt", "a Alpha\nb Beta\n")])
            .WithStandardInput("b\na\n").WithStdOut("b Beta\na Alpha\n").Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-output-truncate-append",
            ["BEGIN{print \"first\">\"out.txt\";print \"second\">>\"out.txt\";print close(\"out.txt\")}"],
            [])
            .WithStandardInput("").WithStdOut("0\n")
            .WithExpectedFiles([File("/out.txt", "first\nsecond\n")]).Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-flush-before-getline",
            ["BEGIN{print \"visible\">\"out.txt\";print fflush(\"out.txt\");print (getline line<\"out.txt\"),line;print close(\"out.txt\")}"],
            [])
            .WithStandardInput("").WithStdOut("0\n1 visible\n0\n")
            .WithExpectedFiles([File("/out.txt", "visible\n")]).Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-source-file-projects-records",
            ["-f", "scripts/main.awk", "records.csv"],
            [File("/scripts/main.awk", "BEGIN{FS=\",\";OFS=\"|\"}{print $2,$1}\n"), File("/records.csv", "a,b\nc,d\n")])
            .WithStandardInput("").WithStdOut("b|a\nd|c\n").Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-included-format-function",
            ["-f", "scripts/main.awk", "records.txt"],
            [File("/scripts/main.awk", "@include \"lib/format.awk\"\n{print decorate($1)}\n"), File("/lib/format.awk", "function decorate(s){return \"[\"s\"]\"}\n"), File("/records.txt", "alpha\nbeta\n")])
            .WithStandardInput("").WithStdOut("[alpha]\n[beta]\n").Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-repeated-include",
            ["-f", "scripts/main.awk"],
            [File("/scripts/main.awk", "@include \"lib/f.awk\"\n@include \"lib/f.awk\"\nBEGIN{print count(3)}\n"), File("/lib/f.awk", "function count(x){return x+1}\n")])
            .WithStandardInput("").WithStdOut("4\n").Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-nextfile-across-inputs",
            ["FNR==1{print FILENAME,$0;nextfile}END{print NR}", "first.txt", "second.txt"],
            [File("/first.txt", "a\nskip\n"), File("/second.txt", "b\nskip\n")])
            .WithStandardInput("").WithStdOut("first.txt a\nsecond.txt b\n2\n").Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-assignment-between-inputs",
            ["{print FILENAME,$1,label}", "label=left", "one.txt", "label=right", "two.txt"],
            [File("/one.txt", "a\n"), File("/two.txt", "b\n")])
            .WithStandardInput("").WithStdOut("one.txt a left\ntwo.txt b right\n").Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-missing-getline-is-recoverable",
            ["BEGIN{print (getline x<\"absent.txt\"),(ERRNO!=\"\");print \"kept\"}"],
            [File("/present.txt", "unused\n")])
            .WithStandardInput("").WithStdOut("-1 1\nkept\n").Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-csv-per-file-header-skip",
            ["--csv", "FNR>1{s[$1]+=$2}END{print \"a\",s[\"a\"];print \"b\",s[\"b\"]}", "left.csv", "right.csv"],
            [File("/left.csv", "key,value\r\na,2\r\nb,\"3\"\r\n"), File("/right.csv", "key,value\r\na,\"4\"\r\n")])
            .WithStandardInput("").WithStdOut("a 6\nb 3\n").Build();
        yield return AwkGoldenCaseBuilder.Create("idiom-file-sorted-report-output",
            ["BEGIN{OFS=\"\\t\"}{sum[$1]+=$2}END{n=asorti(sum,k);for(i=1;i<=n;i++)print k[i],sum[k[i]]>\"report.tsv\";close(\"report.tsv\")}"],
            [])
            .WithStandardInput("beta 4\nalpha 1\nalpha 2\n").WithStdOut("")
            .WithExpectedFiles([File("/report.tsv", "alpha\t3\nbeta\t4\n")]).Build();
    }
}
