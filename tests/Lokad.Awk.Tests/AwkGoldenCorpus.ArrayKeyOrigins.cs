namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ArrayKeyOriginCases()
    {
        yield return Passing("array-key-origin-raw-field",
            ["BEGIN{$0=\"0007\";a[$1]=1}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7),(k==k+0)}"], [], "<0007> strnum 1 1\n", "");
        yield return Passing("array-key-origin-assigned-field",
            ["BEGIN{$1=\"0007\";a[$1]=1}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7),(k==k+0)}"], [], "<0007> string 0 0\n", "");
        yield return Passing("array-key-origin-assigned-record",
            ["BEGIN{$0=\"0007\";a[$0]=1}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7),(k==k+0)}"], [], "<0007> string 0 0\n", "");
        yield return Passing("array-key-origin-copied-field",
            ["{k=$1;a[k]=1}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7),(k==k+0)}"], [], "<0007> strnum 1 1\n", "0007\n");
        yield return Passing("array-key-origin-key-argument",
            ["function put(a,k){a[k]=1}{put(a,$1)}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7),(k==k+0)}"], [], "<0007> strnum 1 1\n", "0007\n");
        yield return Passing("array-key-origin-forwarded-key-argument",
            ["function put(a,k){a[k]=1}function outer(a,k){put(a,k)}{outer(a,$1)}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7),(k==k+0)}"], [], "<0007> strnum 1 1\n", "0007\n");
        yield return Passing("array-key-origin-concatenated-field",
            ["{a[$1 \"\"]=1}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7),(k==k+0)}"], [], "<0007> string 0 0\n", "0007\n");
        yield return Passing("array-key-origin-sorted-traversal",
            ["{a[$1]=1}END{PROCINFO[\"sorted_in\"]=\"@ind_str_asc\";for(k in a)print \"<\"k\">\",typeof(k),(k==7),(k==k+0)}"], [], "<0007> strnum 1 1\n", "0007\n");
        yield return Passing("array-key-origin-asorti-values",
            ["{a[$1]=1}END{n=asorti(a,b);print n;for(i=1;i<=n;i++)print \"<\"b[i]\">\",typeof(b[i]),(b[i]==7)}"], [], "1\n<0007> strnum 1\n", "0007\n");
        yield return Passing("array-key-origin-comparator-key-types",
            ["function cmp(i1,v1,i2,v2){if(typeof(i1)!=\"strnum\"||typeof(i2)!=\"strnum\")bad=1;return(i1>i2)-(i1<i2)}{a[$1]=1}END{print asorti(a,b,\"cmp\"),bad+0}"], [], "2 0\n", "0007\n0008\n");
        yield return Passing("array-key-origin-nested-owner",
            ["{a[\"p\"][$1]=1}END{for(k in a[\"p\"])print \"<\"k\">\",typeof(k),(k==7)}"], [], "<0007> strnum 1\n", "0007\n");
        yield return Passing("array-key-origin-split-value",
            ["BEGIN{split(\"0007\",p);a[p[1]]=1}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7),(k==k+0)}"], [], "<0007> strnum 1 1\n", "");
        yield return Passing("array-key-origin-hex-default",
            ["{a[$1]=1}END{for(k in a)print \"<\"k\">\",typeof(k),(k==16)}"], [], "<0x10> string 0\n", "0x10\n");
        yield return Passing("array-key-origin-hex-input-mode",
            ["--non-decimal-data", "{a[$1]=1}END{for(k in a)print \"<\"k\">\",typeof(k),(k==16)}"], [], "<0x10> strnum 1\n", "0x10\n");
        yield return Passing("array-key-origin-octal-input-mode",
            ["--non-decimal-data", "{a[$1]=1}END{for(k in a)print \"<\"k\">\",typeof(k),(k==8)}"], [], "<010> strnum 1\n", "010\n");
        yield return Passing("array-key-origin-partial-number",
            ["{a[$1]=1}END{for(k in a)print \"<\"k\">\",typeof(k),(k==12)}"], [], "<12tail> string 0\n", "12tail\n");
        yield return Passing("array-key-origin-membership-origin",
            ["BEGIN{a[\"0007\"]=1}{ok=($1 in a)}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7),(k==k+0);print ok}"], [], "<0007> string 0 0\n1\n", "0007\n");
        yield return Passing("array-key-origin-rekey-traversal",
            ["{a[$1]=1}END{for(k in a)b[k]=1;for(k in b)print \"<\"k\">\",typeof(k),(k==7)}"], [], "<0007> strnum 1\n", "0007\n");
        yield return Passing("array-key-origin-input-capture-delete",
            ["function erase(a){delete a[\"0007\"];return 1}BEGIN{a[\"0007\"]=4}{a[$1]+=erase(a)}END{print length(a);for(k in a)print \"<\"k\">\",typeof(k),(k==7)}"], [], "1\n<0007> strnum 1\n", "0007\n");
        yield return Passing("array-key-origin-input-capture-reinsert-string",
            ["function reset(a){delete a[\"0007\"];a[\"0007\"]=10;return 1}{a[$1]=4;a[$1]+=reset(a)}END{print length(a);for(k in a)print \"<\"k\">\",typeof(k),(k==7)}"], [], "1\n<0007> string 0\n", "0007\n");
        yield return Passing("array-key-origin-builder-capture-delete",
            ["function erase(a){delete a[\"0007\"];return 1}{a[$1]=4;a[$1 \"\"]+=erase(a)}END{print length(a);for(k in a)print \"<\"k\">\",typeof(k),(k==7)}"], [], "1\n<0007> string 0\n", "0007\n");
        yield return Passing("array-key-origin-builder-capture-reinsert-string",
            ["function reset(a){delete a[\"0007\"];a[\"0007\"]=10;return 1}{a[$1]=4;a[$1 \"\"]+=reset(a)}END{print length(a);for(k in a)print \"<\"k\">\",typeof(k),(k==7)}"], [], "1\n<0007> string 0\n", "0007\n");
        yield return Passing("array-key-origin-literal-first-input-delete-reinsert",
            ["function reset(a,v){delete a[\"0007\"];a[v]=10;return 1}BEGIN{a[\"0007\"]=4}{a[$1]+=reset(a,$1)}END{print length(a);for(k in a)print \"<\"k\">\",typeof(k),(k==7)}"], [], "1\n<0007> strnum 1\n", "0007\n");
        yield return Passing("array-key-origin-input-key-through-other-owner",
            ["function put(a,k){a[k]=1}{a[$1]=1;put(b,$1)}END{for(k in b)print \"<\"k\">\",typeof(k),(k==7)}"], [], "<0007> strnum 1\n", "0007\n");
        yield return Passing("array-key-origin-deferred-hex-spelling",
            ["--non-decimal-data", "{a[$1]=1}END{for(k in a)print \"<\"k\">\"}"], [], "<0x1.8>\n", "0x1.8\n");
        yield return Passing("array-key-origin-mixed-key-casing",
            ["function put(a,k){n=length(tolower(k));a[k]=n}{put(a,$1)}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7)}"], [], "<0007> strnum 1\n", "0007\n");
        yield return Passing("array-key-origin-mixed-key-index",
            ["function put(a,k){n=index(k,\"0\");a[k]=n}{put(a,$1)}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7)}"], [], "<0007> strnum 1\n", "0007\n");
        yield return Passing("array-key-origin-mixed-key-forwarding",
            ["function put(a,k){a[k]=1}function outer(a,k){n=length(tolower(k));put(a,k)}{outer(a,$1)}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7)}"], [], "<0007> strnum 1\n", "0007\n");
        yield return Passing("array-key-origin-computed-string-argument",
            ["function put(a,k){a[k]=1}{put(a,tolower($1))}END{for(k in a)print \"<\"k\">\",typeof(k),(k==7)}"], [], "<0007> string 0\n", "0007\n");
        yield return Passing("array-key-origin-conditional-key-argument",
            ["function put(a,k){a[k]=1}{put(a[NR],NR==1?$1:\"0007\")}END{for(i=1;i<=2;i++)for(k in a[i])print typeof(k),(k==7)}"], [], "strnum 1\nstring 0\n", "0007\n0007\n");
        yield return Passing("array-key-origin-long-field",
            ["{a[$1]=1}END{for(k in a)print length(k),typeof(k),(k==7)}"], [], "8193 strnum 1\n", new string('0', 8192) + "7\n");
        yield return Passing("array-key-origin-long-key-argument",
            ["function put(a,k){a[k]=1}{put(a,$1)}END{for(k in a)print length(k),typeof(k),(k==7)}"], [], "8193 strnum 1\n", new string('0', 8192) + "7\n");
        yield return Passing("array-key-origin-long-asorti",
            ["{a[$1]=1}END{n=asorti(a,b);print n,length(b[1]),typeof(b[1]),(b[1]==7)}"], [], "1 8193 strnum 1\n", new string('0', 8192) + "7\n");
        yield return Passing("array-key-origin-cloned-child",
            ["{src[\"child\"][$1]=1}END{asort(src,dst);delete src[\"child\"][\"0007\"];for(k in dst[1])print typeof(k),(k==7);print length(src[\"child\"])}"], [], "strnum 1\n0\n", "0007\n");
    }

    private static IEnumerable<AwkGoldenCase> ArrayKeyOriginPolicyCases()
    {
        // Keep the existing nondecimal input policy separate from GNU's prefix conversion.
        yield return AwkGoldenCaseBuilder.Create("array-key-origin-deferred-hex-number-policy",
            ["--non-decimal-data", "{a[$1]=1}END{for(k in a){print \"before\";print k+0;print \"after\"}}"], [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("before\n")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .ExitsWith(2)
            .Build();
    }
}
