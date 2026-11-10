namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FieldValueComparisonCases()
    {
        yield return Passing("field-text-snapshots-survive-origin-observation-and-completion",
            ["function part(s){return substr(s,1,4)}{a=part($1);b=part($1);n=NF;v=$1;$3=$1;print \"[\"a\"]\",\"[\"b\"]\",n,typeof(v),v,($1==7),$0}"],
            [], "[0007] [0007] 3 strnum 0007 1 0007 tail 0007\n[7tai] [7tai] 3 string 7tail 0 7tail tail 7tail\n",
            "0007 tail old\n7tail tail old\n");
        yield return Passing("field-text-snapshots-preserve-strict-and-prefix-facts",
            ["function part(s){return substr(s,1,4)}{a=($1==7);n=$1+0;t=part($1);u=part($1);b=($1==7);print a,n,\"[\"t\"]\",\"[\"u\"]\",b,typeof($1),NF}"],
            [], "1 7 [0007] [0007] 1 strnum 3\n0 7 [7tai] [7tai] 0 string 3\n",
            "0007 tail old\n7tail tail old\n");
        yield return Passing("field-text-snapshots-preserve-arithmetic-prefix-before-classification",
            ["function part(s){return substr(s,1,4)}{n=$1+0;t=part($1);u=$1+1;a=($1==7);print n,\"[\"t\"]\",u,a,typeof($1),$1}"],
            [], "7 [0007] 8 1 strnum 0007\n7 [7tai] 8 0 string 7tail\n", "0007 tail old\n7tail tail old\n");
        yield return Passing("field-text-snapshots-preserve-csv-spelling-through-reconstruction",
            ["--csv", "function part(s){return substr(s,1,4)}{a=part($1);b=part($1);n=NF;$3=$1;print \"[\"a\"]\",\"[\"b\"]\",n,typeof($1),$0}"],
            [], "[00\"0] [00\"0] 3 string 00\"07 tail 00\"07\n[0007] [0007] 3 strnum 0007 tail 0007\n",
            "\"00\"\"07\",tail,old\n0007,tail,old\n");
        yield return Passing("field-text-snapshots-own-earlier-arguments-across-mutation",
            ["function change(){$0=\"changed next\";return 0}function part(s,u){return substr(s,1,4)}{a=part($1);b=part($1,change());print \"[\"a\"]\",\"[\"b\"]\",$0}"],
            [], "[0007] [0007] changed next\n", "0007 tail old\n");
        yield return AwkGoldenCaseBuilder.Create("field-text-snapshots-defer-invalid-conversion-until-numeric-use",
                ["--non-decimal-data", "function part(s){return substr(s,1,4)}{print part($1),part($1);print $1+0;print \"later\"}"], [])
            .WithStandardInput("0x1.8\n").WithStdOut("0x1. 0x1.\n").ExitsWith(2)
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .WithNondeterminismNote("Lokad preserves its deferred missing-exponent diagnostic; GNU awk 5.3.2 accepts this input as 1.")
            .Build();
        yield return Passing("text-parameters-preserve-caller-and-result-origins",
            ["function lower(s){return tolower(s)}function observed(s){return typeof(s)\":\"tolower(s)}{v=lower($1);print \"[\"v\"]\",typeof(v),(v==7),typeof($1),observed($1)}"],
            [], "[0007] string 0 strnum strnum:0007\n[7tail] string 0 string string:7tail\n", "0007\n7tail\n");
        yield return Passing("text-parameters-format-numbers-at-use-and-retain-input-spelling",
            ["function lower(s,u){return tolower(s)}function relay(s,u){return lower(s,u)}function change(){CONVFMT=\"%.3f\";return 0}BEGIN{CONVFMT=\"%.1f\";x=1.23456;print relay(x,change());FS=\":\";$0=\"001.23456:end\";CONVFMT=\"%.1f\";print relay($1,change()),$1}"],
            [], "1.235\n001.23456 001.23456\n", "");
        yield return Passing("text-parameters-capture-before-later-record-mutation",
            ["function change(){$0=\"replaced next\";return 0}function part(s,u){return substr(s,1,4)}BEGIN{$0=\"0007 tail\";print part($1,change()),$0}"],
            [], "0007 replaced next\n", "");
        yield return Passing("text-parameters-defer-invalid-input-number-conversion",
            ["--non-decimal-data", "function lower(s){return tolower(s)}function part(s){return substr(s,1,4)}{print \"[\"lower($1)\"]\",\"[\"part($1)\"]\"}"],
            [], "[0x1p] [0x1p]\n", "0x1p\n");
        yield return Passing("numeric-range-filter-boundaries",
            ["$2>=16 && $2<48 {count++;total+=$2} END{print count+0,total+0}"],
            [], "3 95\n", "key\t000\nkey\t016\nkey\t032\nkey\t047\nkey\t048\nkey\t063\n");
        yield return Passing("field-classification-call-keeps-numeric-prefix-comparison-origin",
            ["function clip(s,n,t){t=tolower(s);if(length(t)>n)return substr(t,1,n);return t}{print length(clip($1,$2))}"],
            [], "4\n12\n12\n", "ID:beta:tail 4\nID:beta:tail 4x\nID:beta:tail 32\n");
        yield return Passing("field-classification-survives-completion-and-owned-escape",
            ["{a=($1==7);b=($1==\"0007\");n=$1+0;c=($1==7);count=NF;copy=$1;$3=$1;print a,b,n,c,count,\"[\"copy\"]\",typeof(copy),$0}"],
            [], "1 1 7 1 3 [0007] strnum 0007 tail 0007\n0 0 7 0 3 [7tail] string 7tail tail 7tail\n0 0 0 0 3 [word] string word tail word\n",
            "0007 tail old\n7tail tail old\nword tail old\n");
        yield return Passing("field-classification-survives-raw-copy-and-reconstruction",
            ["{a=($1==7);n=$1+0;$3=$1;b=($1==$3);print a,n,b,NF,$0,$1,typeof($1)}"],
            [], "1 7 1 3 0007 tail 0007 0007 strnum\n0 7 1 3 7tail tail 7tail 7tail string\n0 0 1 3 word tail word word string\n",
            "0007 tail old\n7tail tail old\nword tail old\n");
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
