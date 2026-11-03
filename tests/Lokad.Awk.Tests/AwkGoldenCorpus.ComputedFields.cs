namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ComputedFieldCases()
    {
        yield return Passing("regex-numeric-input-formats-after-pattern-effects",
            ["function pattern(){CONVFMT=\"%.1f\";$0=\"changed tail\";return \"^0[.]125$\"}BEGIN{CONVFMT=\"%.3f\";print(0.125~pattern()),$0,CONVFMT}"],
            [], "0 changed tail %.1f\n");
        yield return Passing("regex-field-input-captured-before-pattern-effects",
            ["function pattern(){$0=\"changed tail\";return \"^old$\"}BEGIN{$0=\"old tail\";print($1~pattern()),$0}"],
            [], "1 changed tail\n");
        yield return Passing("regex-numeric-input-formats-after-pattern-effects-with-split",
            ["function pattern(){CONVFMT=\"%.1f\";return \"^0[.]125$\"}BEGIN{split(\"\",tmp);CONVFMT=\"%.3f\";print(0.125~pattern()),CONVFMT}"],
            [], "0 %.1f\n");
        yield return Passing("computed-field-simple-assignment-captures-rhs-before-index",
            ["function choose(){i++;return i}function rhs(){i=2;return \"0007\"}{i=1;$(choose())=rhs();print i,NF,$0,typeof($3)}"],
            [], "3 3 a b 0007 string\n", "a b\n");
        yield return Passing("computed-field-compound-captures-prior-and-destination",
            ["function rhs(){i=2;$1=100;return 5}{i=1;r=($i+=rhs());print i,r,$0;before=$i++;print before,$0}"],
            [], "2 15 15 20\n20 15 21\n", "10 20\n");
        yield return Passing("computed-field-record-assignment-and-split-snapshot",
            ["function replace(){$0=\"0007:old\";FS=\" \";i=1;return $i}{FS=\":\";saved=replace();$2=saved;print saved,$0;print typeof(saved),typeof($2)}"],
            [], "0007 0007 0007\nstrnum strnum\n", "ignored\n");
        yield return Passing("computed-field-concat-formats-before-following-effects",
            ["function change(){CONVFMT=\"%.2f\";$0=\"new record\";return \":\"}{CONVFMT=\"%.1f\";x=1.25;text=(x change() x);print text,$0;next}"],
            [], "1.2:1.25 new record\n", "ignored\n");
        yield return Passing("computed-field-nested-concat-keeps-separate-prefixes",
            ["function decorate(s){return \"[\" s \"]\"}{text=($1 \"/\" decorate($2 \"-\" $1) \"/\" $1);print text}"],
            [], "0007/[é-0007]/0007\n", "0007 é\n");
        (string Name, string Statement)[] operations =
        [
            ("print", "print $n"), ("numeric", "print $n+1"),
            ("read", "print \"[\"$n\"]\""), ("length", "print length($n)"),
            ("compare", "print ($n==\"kept record\")"),
            ("compare-right", "print (\"kept record\"==$n)"),
            ("array-write", "a[$n]=1"), ("array-read", "print a[$n]"),
            ("array-update", "a[$n]+=1"), ("membership", "print ($n in a)"),
            ("write", "$n=\"changed\""), ("update", "$n+=1"),
            ("increment", "$n++"), ("sub", "sub(/kept/,\"changed\",$n)"),
            ("getline", "getline $n"), ("printf", "printf \"%s\",$n")
        ];
        foreach (var profile in new[] { "", "--posix", "--traditional" })
        {
            foreach (var (name, statement) in operations)
            {
                var source = "BEGIN{$0=\"kept record\";n=\"+nan\"+0;print \"before\";" +
                    statement + ";print \"after\"}";
                yield return AwkGoldenCaseBuilder.Create("computed-field-nan-" + name + profile,
                        profile.Length == 0 ? [source] : [profile, source], [])
                    .WithStandardInput("unread\n").WithStdOut("before\n")
                    .WithStdErr("awk: non-finite field index\n").ExitsWith(2).Build();
            }
            foreach (var value in new[] { "-nan", "+inf", "-inf" })
            {
                var source = "BEGIN{$0=\"kept record\";n=\"" + value +
                    "\"+0;print \"before\";$n=\"changed\";print $0}";
                yield return AwkGoldenCaseBuilder.Create("computed-field-" + value + profile,
                        profile.Length == 0 ? [source] : [profile, source], [])
                    .WithStdOut("before\n").WithStdErr("awk: non-finite field index\n").ExitsWith(2).Build();
            }
            (string Name, string Body)[] functions =
            [
                ("read", "return $n"), ("length", "return length($n)"),
                ("compare", "return $n==\"kept record\""), ("array", "return a[$n]")
            ];
            foreach (var (name, body) in functions)
            {
                var source = "function f(n){" + body + "}BEGIN{$0=\"kept record\";print \"before\";f(\"+nan\"+0);print \"after\"}";
                yield return AwkGoldenCaseBuilder.Create("computed-field-function-" + name + profile,
                        profile.Length == 0 ? [source] : [profile, source], [])
                    .WithStdOut("before\n").WithStdErr("awk: non-finite field index\n").ExitsWith(2).Build();
            }
            var finite = "BEGIN{$0=\"alpha beta\";print $(-0.1),$(2.9),\"[\"$(2147483648)\"]\";exit}";
            yield return AwkGoldenCaseBuilder.Create("computed-field-finite" + profile,
                    profile.Length == 0 ? [finite] : [profile, finite], [])
                .WithStdOut("alpha beta beta []\n").Build();
        }
    }
}
