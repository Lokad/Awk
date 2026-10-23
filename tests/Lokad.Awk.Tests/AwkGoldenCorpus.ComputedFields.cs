namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ComputedFieldCases()
    {
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
