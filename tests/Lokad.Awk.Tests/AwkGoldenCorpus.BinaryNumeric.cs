namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> BinaryNumericCases()
    {
        yield return Passing("binary-numeric-field-operators-and-classification",
            ["{print $1+$2,$1-$2,$1*$2,$1/$2,$1%$2,$1^$2;print typeof($1),$1}"],
            [], "10 6 16 4 0 64\nstrnum 008\n", "008 2\n");
        yield return Passing("binary-numeric-nesting-prefix-and-string-context",
            ["{n=($1*2+$2/2)%17;print n,\"[\"$1\"]\",typeof($2);print (n+1) \" units\"}"],
            [], "16 [007] string\n17 units\n", "007 4tail\n");
        yield return Passing("binary-numeric-left-snapshot-before-right-index-effects",
            ["function change(){$0=\"key 9\";return 2} BEGIN{$0=\"key 3\";print $2*2+$(change()),$0}"],
            [], "15 key 9\n");
        yield return Passing("binary-numeric-cached-modified-and-record-zero",
            ["BEGIN{$0=\"007 tail\";v=$1;print ($1+1)*2;$1=2.5;CONVFMT=\"%.4f\";print ($1+1)*2,$1;$0=3.5;print ($0+1)*2}"],
            [], "16\n7 2.5\n9\n");
        yield return Passing("binary-numeric-unicode-and-empty-fields",
            ["BEGIN{FS=\":\";$0=\"café::3\";print $1+($2+$3)*2,typeof($1),NF}"],
            [], "6 string 3\n");
        const string script = "{print ($1+1)*2,$1}";
        yield return Passing("binary-numeric-decimal", [script], [], "2 0x10\n16 007\n", "0x10\n007\n");
        yield return Passing("binary-numeric-posix", ["--posix", script], [], "34 0x10\n16 007\n", "0x10\n007\n");
        yield return Passing("binary-numeric-nondecimal", ["--non-decimal-data", script], [], "34 0x10\n16 007\n", "0x10\n007\n");
    }
}
