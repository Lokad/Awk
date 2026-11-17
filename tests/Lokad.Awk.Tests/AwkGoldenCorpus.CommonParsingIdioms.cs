namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> CommonParsingIdiomCases()
    {
        yield return Passing("key-value-splitting-with-equals-regex",
            ["BEGIN{n=split(\"answer=42\",kv,/=/);print n,kv[1],kv[2]}"], [], "2 answer 42\n");
        yield return Passing("equals-regex-matching-and-substitution",
            ["BEGIN{print(\"=\"~/=/);x=\"a=b=c\";print gsub(/=/,\":\",x),x;sub(/:.*/,\"\",x);print x}"], [], "1\n2 a:b:c\na\n");
        yield return Passing("equals-regex-keeps-division-assignment",
            ["BEGIN{x=8;x/=2;print x,(\"=\"~/=/)}"], [], "4 1\n");
        yield return Passing("next-with-trailing-comment",
            ["{if($1)next # skip\nprint \"kept\"}"], [], "kept\n", "0\n1\n");
        yield return Passing("continue-with-trailing-comment",
            ["BEGIN{for(i=1;i<=3;i++){if(i==2)continue # skip\nprint i}}"], [], "1\n3\n");
        yield return Passing("break-with-trailing-comment",
            ["BEGIN{for(i=1;i<=3;i++){if(i==2)break # done\nprint i}}"], [], "1\n");
        yield return Passing("nextfile-with-trailing-comment",
            ["{if($1)nextfile # skip file\nprint \"kept\"}", "/one", "/two"],
            [File("/one", "1\n0\n"), File("/two", "0\n")], "kept\n");
        yield return Passing("next-with-trailing-comment-and-crlf",
            ["{if($1)next # skip\r\nprint \"kept\"}"], [], "kept\n", "0\n1\n");
        yield return Passing("for-membership-in-condition",
            ["BEGIN{a[1]=3;a[2]=4;for(i=1;i in a;i++)n++;print n}"], [], "2\n");
        yield return Passing("for-membership-with-empty-initializer",
            ["BEGIN{a[1]=3;a[2]=4;i=1;for(;i in a;i++)n++;print n}"], [], "2\n");
        yield return Passing("for-membership-in-initializer",
            ["BEGIN{a[1]=3;for(i=1 in a;i<2;i++)n++;print n}"], [], "1\n");
        yield return Passing("for-membership-in-step",
            ["BEGIN{a[1]=3;for(i=0;i<2;i+=1 in a)n++;print n}"], [], "2\n");
        yield return Passing("for-in-membership-control",
            ["BEGIN{a[1]=3;a[2]=4;for(k in a)n++;print n}"], [], "2\n");
        // The pinned GNU 5.3.2 treats both one-character separators literally.
        yield return Passing("single-character-split-regex-matches-pinned-gnu",
            ["BEGIN{print split(\"a.a.a\",a,/./);print split(\"a.a.a\",b,\".\")}"], [], "3\n3\n");
    }
}
