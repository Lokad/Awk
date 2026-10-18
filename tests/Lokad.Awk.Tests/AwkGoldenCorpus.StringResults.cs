namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> StringResultCases()
    {
        yield return Passing("short-concatenation-formats-each-operand-before-later-effects",
            ["function fmt(){CONVFMT=\"%.1f\";return \"β\"} BEGIN{CONVFMT=\"%.3f\";s=1.2344 fmt() 1.2344;print s}"],
            [], "1.234β1.2\n");
        yield return Passing("short-concatenation-keeps-fields-across-getline",
            ["{print \"[\" $1 \":\" ((getline)>0 ? $1 : \"EOF\") \"]\"}"],
            [], "[café:δ]\n", "café\nδ\n");
        yield return Passing("string-results-retain-string-typing-and-keys",
            ["BEGIN{s=substr(\"_008_\",2,3);a[s]=s;t=tolower(\"0\");u=toupper(\"0\");print typeof(s),s,s+0,(s==8),(s?1:0),(\"008\" in a),(8 in a),typeof(t),(t?1:0),typeof(u),(u?1:0)}"],
            [], "string 008 8 0 1 1 0 string 1 string 1\n");
        foreach (var (option, number) in new[] { ("", 0), ("--non-decimal-data", 16), ("--posix", 16), ("--traditional", 0) })
        {
            const string program = "BEGIN{s=substr(\"_0x10_\",2,4);t=tolower(\"0X10\");u=toupper(\"0x10\");print s,s+0,t,t+0,u,u+0,length(s)}";
            yield return Passing("string-results-numeric-mode-" + (option.Length == 0 ? "default" : option[2..]),
                option.Length == 0 ? [program] : [option, program], [],
                $"0x10 {number} 0x10 {number} 0X10 {number} 4\n");
        }
        yield return Passing("string-results-snapshots-through-functions-and-arrays",
            ["function save(x,a){a[x]=x;return x} BEGIN{s=substr(\"_007_\",2);t=save(toupper(s),a);s=0;print t,a[t],t+0,(t==7),length(t)}"],
            [], "007_ 007_ 7 0 4\n");
    }
}
