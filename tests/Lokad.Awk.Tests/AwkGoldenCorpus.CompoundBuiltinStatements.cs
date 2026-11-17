namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> CompoundBuiltinStatementCases()
    {
        yield return Passing("compound-builtin-close-short-circuit",
            ["BEGIN{close(\"f\")||x++;print x}"], [], "\n");
        yield return Passing("compound-builtin-close-concatenation",
            ["BEGIN{close(\"f\") y;print \"ok\"}"], [], "ok\n");
        yield return PassingWithExpectedFiles("compound-builtin-close-output-status",
            ["BEGIN{print \"ready\" > \"/out\";close(\"/out\") || ++x;print x;close(\"/out\") && ++x;print x}"], [], "1\n2\n",
            [File("/out", "ready\n")]);
        yield return PassingWithExpectedFiles("compound-builtin-fflush-concatenation",
            ["BEGIN{print \"ready\" > \"/out\";fflush(\"/out\") ++x;print x;close(\"/out\")}"], [], "1\n",
            [File("/out", "ready\n")]);
        yield return Passing("compound-builtin-split-concatenation",
            ["BEGIN{split(\"a b\",a) ++x;print x,length(a),a[1],a[2]}"], [], "1 2 a b\n");
        yield return Passing("compound-builtin-patsplit-short-circuit",
            ["BEGIN{patsplit(\"a b\",a,/[a-z]+/) && ++x;print x,a[1],a[2]}"], [], "1 a b\n");
        yield return Passing("compound-builtin-sub-short-circuit",
            ["BEGIN{x=\"aba\";sub(/a/,\"x\",x) && ++n;gsub(/a/,\"y\",x) || ++n;print x,n}"], [], "xby 1\n");
        yield return Passing("compound-builtin-sort-concatenation",
            ["BEGIN{a[1]=2;a[2]=1;asort(a) ++x;asorti(a,b) ++x;print x,a[1],a[2],b[1],b[2]}"], [], "2 1 2 1 2\n");
        yield return PassingWithExpectedFiles("compound-builtin-close-argument-order",
            ["function target(){print \"target\";return \"/out\"}function suffix(){print \"suffix\";return 7}BEGIN{print \"ready\">\"/out\";close(target()) suffix();print \"done\"}"], [], "target\nsuffix\ndone\n",
            [File("/out", "ready\n")]);
    }
}
