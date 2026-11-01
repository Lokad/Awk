namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FieldValueComparisonCases()
    {
        yield return Passing("scoped-field-matrix",
            ["BEGIN{FS=\":\"}{n=7;s=\"7\";a=($1==n);b=(n==$1);c=($1==$2);d=($1>s);e=($1+0);print a,b,c,d,e,\"[\"$1\"]\",typeof($1)}"], [], "1 1 1 0 7 [0007] strnum\n0 0 0 1 7 [7tail] string\n0 0 0 1 0 [text] string\n0 0 0 0 0 [0x10] string\n1 1 1 0 7 [ 7 ] strnum\n0 0 0 0 0 [ ] string\n0 0 0 0 0 [] string\n", "0007:7\n7tail:7\ntext:0\n0x10:16\n 7 :7\n :0\n:0\n");
        yield return Passing("scoped-field-warm-cache",
            ["{n=$1+0;a=($1==7);b=(7==$1);c=($1==$2);print a,b,c,n,$1,typeof($1)}"], [], "1 1 1 7 0007 strnum\n0 0 0 7 7tail string\n0 0 0 0 text string\n", "0007 7\n7tail 7\ntext 0\n");
        yield return Passing("scoped-field-ignorecase",
            ["BEGIN{IGNORECASE=1}{v=\"alpha\";a=($1==v);b=(v==$1);c=($1==$2);print a,b,c,$1}"], [], "1 1 1 ALPHA\n1 1 1 Alpha\n", "ALPHA alpha\nAlpha ALPHA\n");
        yield return Passing("scoped-field-unicode",
            ["{v=\"café\";a=($1==v);b=(v==$1);c=($1<$2);print a,b,c,$1,$2}"], [], "1 1 1 café 😀\n0 0 1 cafe café\n", "café 😀\ncafe café\n");
        yield return Passing("scoped-field-dirty-reconstruction",
            ["BEGIN{$0=\"0007 tail\";$2=\"new\";a=($1<$0);print a,$0,$1,typeof($1)}"], [], "1 0007 new 0007 strnum\n", "");
        yield return Passing("scoped-field-dirty-named-reconstruction",
            ["function size(){return length($0)} BEGIN{$0=\"0007 tail\";$2=\"new\";a=($1<size());print a,$0,$1,typeof($1)}"], [], "1 0007 new 0007 strnum\n", "");
        yield return Passing("scoped-field-mutating-rhs",
            ["BEGIN{$0=\"0007 tail\";a=($1==($1=7));print a,$1,typeof($1)}"], [], "1 7 number\n", "");
        yield return Passing("scoped-field-record-changing-rhs",
            ["function change(){$0=\"next 3\";return 7} BEGIN{$0=\"0007 tail\";a=($1==change());print a,$0}"], [], "1 next 3\n", "");
        yield return Passing("scoped-field-array-growth-rhs",
            ["function touch(a,i){for(i=1;i<=40;i++)a[i]=i;return 7}{t=($1==touch(x));print t,$1,length(x)}"], [], "1 0007 40\n1 0007 40\n", "0007\n0007\n");
        yield return Passing("scoped-field-prepared-condition",
            ["function value(x){return x+0}{if(value($1)>0){if(int($1)>0)sum+=value($1)}}END{print sum}"], [], "10\n", "0007\n-2\n3\n");
        yield return Passing("scoped-field-do-condition",
            ["function value(x){return x+0}{i=0;do{sum+=value($1)}while(++i<2)}END{print sum}"], [], "20\n", "0007\n3\n");
        yield return Passing("scoped-field-owned-fields",
            ["BEGIN{$0=\"0007 old\";$1=7;n=($1==7);s=($1==\"7\");$1=\"0007\";t=($1==7);print n,s,t,$1,typeof($1);NF=4;print ($3==0),($3==\"\"),($5==0),($5==\"\")}"], [], "1 1 0 0007 string\n0 1 0 1\n", "");
        yield return Passing("scoped-field-nondecimal",
            ["--non-decimal-data", "{a=($1==16);b=(16==$1);c=($1==$2);n=$1+0;print a,b,c,n,$1,typeof($1)}"], [], "1 1 1 16 0x10 strnum\n1 1 1 16 020 strnum\n", "0x10 16\n020 16\n");
        yield return Passing("scoped-field-posix",
            ["--posix", "{a=($1==16);b=(16==$1);n=$1+0;print a,b,n,$1}"], [], "1 1 16 0x10\n0 0 20 020\n", "0x10\n020\n");
        yield return Passing("scoped-field-formatting",
            ["BEGIN{CONVFMT=\"%.4f\";$0=\"1.2500 1.25\";n=1.25;a=($1==n);b=($1==$2);$1=n;c=($1==\"1.2500\");print a,b,c,$1,$0}"], [], "1 1 1 1.25 1.2500 1.25\n", "");
        yield return AwkGoldenCaseBuilder.Create("scoped-field-symbolic-nf-write",
            ["BEGIN{$0=sprintf(\"%s\",\"word tail\");NF;v=($1>((SYMTAB[\"NF\"]=1)+length($0)));print v,$0,$1}"], [])
            .WithStdOut("1 word word\n")
            .WithNondeterminismNote("Lokad shares NF state between direct and SYMTAB access. GNU 5.3.2's classified stale indirect write produces 1 word tail word; this preserves the documented shared NF contract.")
            .Build();
        yield return Passing("field-value-comparison-all-operators",
            ["BEGIN{$0=\"alpha tail\";s=\"alpha\";print (s==$1),(s!=$1),(\"a\"<$1),(s<=$1),(\"z\">$1),(s>=$1)}"],
            [], "1 0 1 1 1 1\n");
        yield return Passing("field-value-comparison-input-classification",
            ["{s=\"007\";print s==$1,\"7\"==$1,7==$1,(8>$1);print typeof($1),$1}"],
            [], "1 0 1 1\nstrnum 007\n", "007 tail\n");
        yield return Passing("field-value-comparison-effect-order",
            ["function left(){a++;return $1} function idx(){b++;$0=\"after before\";return 2} BEGIN{$0=\"before tail\";print left()==$(idx()),a,b,$0}"],
            [], "1 1 1 after before\n");
        yield return Passing("field-value-comparison-array-alias-and-retention",
            ["function same(a){return a[\"k\"]==$2} BEGIN{a[\"k\"]=\"old\";$0=\"k old\";print same(a);$0=\"k new\";print same(a),a[\"k\"];a[\"k\"]=7;$0=\"k 007\";print same(a)}"],
            [], "1\n0 old\n1\n");
        yield return Passing("field-value-comparison-mutation-and-empty-fields",
            ["BEGIN{s=\"0007\";$0=\"0007 tail\";v=$1;print s==$1;$1=7;print s==$1,7==$1;$1=s;print s==$1;NF=4;print \"\"==$3,\"\"==$5;FS=\":\";$0=\"a:0007\";print s==$2}"],
            [], "1\n0 1\n1\n1 1\n1\n");
        yield return Passing("field-value-comparison-record-zero-and-formatting",
            ["BEGIN{$0=\"alpha tail\";s=$0;print s==$0;$1=1.25;CONVFMT=\"%.4f\";print \"1.2500\"==$1;print \"1.2500 tail\"==$0}"],
            [], "1\n1\n1\n");
        yield return Passing("field-value-comparison-unicode",
            ["BEGIN{$0=\"café 😀\";s=\"café\";print s==$1,\"😀\"==$2,\"é\"<$2}"],
            [], "1 1 1\n");
        yield return Passing("field-value-comparison-byte-mode",
            ["-b", "BEGIN{$0=\"café 😀\";s=\"café\";print s==$1,\"😀\"==$2,\"é\"<$2}"],
            [], "1 1 1\n");
        yield return Passing("field-value-comparison-ignorecase-and-short-circuit",
            ["BEGIN{$0=\"Alpha tail\";s=\"alpha\";IGNORECASE=1;print s==$1;IGNORECASE=0;print s==$1;print (s==$1&&++n),n+0;$0=\"alpha tail\";print (s==$1&&++n),n}"],
            [], "1\n0\n0 0\n1 1\n");
        const string classification = "{s=\"0x10\";print s==$1,16==$1}";
        yield return Passing("field-value-comparison-decimal", [classification], [], "1 0\n", "0x10 tail\n");
        yield return Passing("field-value-comparison-posix", ["--posix", classification], [], "1 1\n", "0x10 tail\n");
        yield return Passing("field-value-comparison-traditional", ["--traditional", classification], [], "1 0\n", "0x10 tail\n");
        yield return Passing("field-value-comparison-nondecimal", ["--non-decimal-data", classification], [], "1 1\n", "0x10 tail\n");
    }
}
