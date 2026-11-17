namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> UnassignedFieldCases()
    {
        yield return Passing("unassigned-field-initial",
            ["BEGIN{print typeof(v),(v==0),typeof($0),($0==0),typeof($1),($1==0),NF}"], [],
            "untyped 1 unassigned 0 unassigned 0 0\n", "");
        yield return Passing("unassigned-field-explicit-empty-record",
            ["BEGIN{$0=\"\";print typeof($0),($0==0),typeof($1),($1==0),NF}"], [],
            "string 0 unassigned 0 0\n", "");
        yield return Passing("unassigned-field-grown-nf",
            ["BEGIN{NF=3;print typeof($0),typeof($1),typeof($2),typeof($3),($1==0),NF}"], [],
            "string unassigned unassigned unassigned 0 3\n", "");
        yield return Passing("unassigned-field-grown-field",
            ["BEGIN{$3=\"x\";print typeof($0),typeof($1),typeof($2),typeof($3),($1==0),NF}"], [],
            "string unassigned unassigned string 0 3\n", "");
        yield return Passing("unassigned-field-empty-input-record",
            ["{print typeof($0),typeof($1),typeof($2),($1==0),NF}"], [],
            "string unassigned unassigned 0 0\n", "\n");
        yield return Passing("unassigned-field-empty-admitted-field",
            ["BEGIN{FS=\",\"}{print typeof($1),typeof($2),typeof($3),typeof($4),($2==0),($4==0),NF}"], [],
            "string string string unassigned 0 0 3\n", "a,,b\n");
        yield return Passing("unassigned-field-csv-empty-field",
            ["--csv", "{print typeof($1),typeof($2),typeof($3),typeof($4),typeof($5),($2==0),($3==0),NF}"], [],
            "string string string string unassigned 0 0 4\n", "a,\"\",,b\n");
        yield return Passing("unassigned-field-assigned-empty-field",
            ["BEGIN{$0=\"a b\";$1=\"\";print typeof($1),typeof($2),($1==0),NF}"], [],
            "string string 0 2\n", "");
        yield return Passing("unassigned-field-copied-field",
            ["BEGIN{x=$9;y=x;a[1]=x;print typeof(x),(x==0),typeof(y),(y==0),typeof(a[1]),(a[1]==0)}"], [],
            "unassigned 0 unassigned 0 unassigned 0\n", "");
        yield return Passing("unassigned-field-argument-return",
            ["function f(p){print typeof(p),(p==0);return p}BEGIN{x=f($9);print typeof(x),(x==0)}"], [],
            "unassigned 0\nunassigned 0\n", "");
        yield return Passing("unassigned-field-mutating-record",
            ["BEGIN{x=$9;$0=\"a b\";print typeof(x),(x==0),typeof($9),($9==0)}"], [],
            "unassigned 0 unassigned 0\n", "");
        yield return Passing("unassigned-field-arithmetic-does-not-retype",
            ["BEGIN{x=$9;print (x+0),typeof(x),(x==0);x+=0;print typeof(x),(x==0)}"], [],
            "0 unassigned 0\nnumber 1\n", "");
        yield return Passing("unassigned-field-separators-only",
            ["BEGIN{FS=\",\"}{print NF,typeof($1),typeof($2),typeof($3),($1==0),($2==0)}"], [],
            "2 string string unassigned 0 0\n", ",\n");
        yield return Passing("unassigned-field-reconstructed-empty-field",
            ["BEGIN{FS=\",\";$0=\"a,,b\";print typeof($2),($2==0);$1=\"c\";print typeof($2),($2==0),$0}"], [],
            "string 0\nstring 0 c  b\n", "");
        yield return Passing("unassigned-field-field-assignment-copy",
            ["BEGIN{$0=\"a b\";$1=$9;print typeof($1),($1==0);x=$1;print typeof(x),(x==0)}"], [],
            "unassigned 0\nunassigned 0\n", "");
        yield return Passing("unassigned-field-text-consumers",
            ["function len(x){return length(x)}BEGIN{NF=3;print length($1),len($2),index($3,\"x\"),\"[\" substr($1,1) \"]\",typeof($1),typeof($2);print \"{\"$0\"}\";print typeof($1)}"], [],
            "0 0 0 [] unassigned unassigned\n{  }\nunassigned\n", "");
        yield return Passing("unassigned-field-transformed-results",
            ["BEGIN{NF=4;print substr($2,1)==\"\",tolower($3)==\"\",toupper($4)==\"\",($2==0),typeof($2),typeof($3),typeof($4)}"], [],
            "1 1 1 0 unassigned unassigned unassigned\n", "");
        yield return Passing("unassigned-field-type-sort",
            ["BEGIN{a[\"missing\"]=$9;a[\"empty\"]=\"\";a[\"zero\"]=0;n=asorti(a,k,\"@val_type_asc\");for(i=1;i<=n;i++)print k[i],typeof(a[k[i]]),(a[k[i]]==0)}"], [],
            "zero number 1\nempty string 0\nmissing unassigned 0\n", "");
        yield return Passing("unassigned-field-user-sort",
            ["function cmp(k1,v1,k2,v2){if(typeof(v1)!=typeof(a[k1])||typeof(v2)!=typeof(a[k2]))bad=1;return k1<k2?-1:k1>k2}BEGIN{a[\"b\"]=$9;a[\"a\"]=\"\";n=asorti(a,k,\"cmp\");print n,k[1],k[2],typeof(a[\"b\"]),(bad+0)}"], [],
            "2 a b unassigned 0\n", "");
        yield return Passing("unassigned-field-numeric-cache-rebuild",
            ["BEGIN{NF=3;print $1+0,typeof($1),($1==0);print $0;print typeof($1),typeof($2);$2++;print typeof($1),typeof($2),$2;print $0}"], [],
            "0 unassigned 0\n  \nunassigned unassigned\nunassigned number 1\n 1 \n", "");
        yield return Passing("unassigned-field-shrink-grow",
            ["BEGIN{NF=3;$1=\"\";x=$2;NF=1;NF=3;print typeof($1),typeof($2),typeof($3),typeof(x);$1=\"a\";print $0;print typeof($2),typeof($3)}"], [],
            "string unassigned unassigned unassigned\na  \nunassigned unassigned\n", "");
        yield return Passing("unassigned-field-captured-record",
            ["function keep(x){$0=\"later\";return x}BEGIN{x=keep($0);print typeof(x),(x==0),typeof($0),$0}"], [],
            "unassigned 0 string later\n", "");
        yield return Passing("unassigned-field-empty-return-distinction",
            ["function empty(){}BEGIN{x=$9;y=empty();print typeof(x),(x==0),typeof(y),(y==0);a[1]=x;a[2]=y;print typeof(a[1]),(a[1]==0),typeof(a[2]),(a[2]==0)}"], [],
            "unassigned 0 unassigned 1\nunassigned 0 unassigned 1\n", "");
        yield return Passing("unassigned-field-staged-growth",
            ["BEGIN{NF=1500;print typeof($1),typeof($750),typeof($1500),($750==0),NF;NF=1;NF=1500;print typeof($1500),($1500==0)}"], [],
            "unassigned unassigned unassigned 0 1500\nunassigned 0\n", "");
    }
}

