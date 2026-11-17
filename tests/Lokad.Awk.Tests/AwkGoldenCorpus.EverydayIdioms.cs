namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> EverydayIdiomCases()
    {
        yield return Passing("idiom-sum-a-column",
            ["{total+=$2}END{print total}"], [], "9\n", "a 2\nb 3\na 4\n");
        yield return Passing("idiom-count-error-records",
            ["$1==\"ERROR\"{n++}END{print n+0}"], [], "2\n", "INFO ready\nERROR failed\nERROR retry\n");
        yield return Passing("idiom-project-and-reorder-columns",
            ["{print $3,$1}"], [], "c a\nz x\n", "a b c\nx y z\n");
        yield return Passing("idiom-tab-delimited-output",
            ["BEGIN{OFS=\"\t\"}{print $2,$1}"], [], "right\tleft\nb\ta\n", "left right\na b\n");
        yield return Passing("idiom-skip-empty-and-comment-records",
            ["NF&&$1!~/^#/{print $1}"], [], "alpha\nbeta\n", "\n# comment\nalpha 1\n  \nbeta 2\n");
        yield return Passing("idiom-number-nonempty-lines",
            ["NF{print ++n \":\" $0}"], [], "1:a\n2:b\n", "a\n\nb\n");
        yield return Passing("idiom-print-record-range",
            ["NR>=2&&NR<=3{print}"], [], "two\nthree\n", "one\ntwo\nthree\nfour\n");
        yield return Passing("idiom-first-record-only",
            ["{print;exit}"], [], "first\n", "first\nsecond\n");
        yield return Passing("idiom-count-fields-histogram",
            ["{counts[NF]++}END{for(n=0;n<=3;n++)if(n in counts)print n,counts[n]}"], [], "1 2\n2 1\n3 1\n", "a b\nx\np q r\ny\n");
        yield return Passing("idiom-sum-by-small-key",
            ["{sum[$1]+=$2}END{print sum[\"a\"],sum[\"b\"]}"], [], "7 3\n", "a 2\nb 3\na 5\n");
        yield return Passing("idiom-deduplicate-in-input-order",
            ["!seen[$0]++"], [], "one\ntwo\nthree\n", "one\ntwo\none\nthree\ntwo\n");
        yield return Passing("idiom-adjacent-group-totals",
            ["NR>1&&$1!=key{print key,total;total=0}{key=$1;total+=$2}END{if(NR)print key,total}"], [], "a 5\nb 7\n", "a 2\na 3\nb 7\n");
        yield return Passing("idiom-minimum-and-maximum",
            ["NR==1{lo=hi=$1}$1<lo{lo=$1}$1>hi{hi=$1}END{print lo,hi}"], [], "-2 9\n", "5\n-2\n9\n0\n");
        yield return Passing("idiom-running-total",
            ["{total+=$1;print NR,total}"], [], "1 2\n2 5\n3 4\n", "2\n3\n-1\n");
        yield return Passing("idiom-average-with-empty-input-guard",
            ["{total+=$1;n++}END{if(n)printf \"%.2f\\n\",total/n;else print \"empty\"}"], [], "2.33\n", "1\n2\n4\n");
        yield return Passing("idiom-parse-key-value-records",
            ["{split($0,p,/=/);v[p[1]]=p[2]}END{print v[\"name\"],v[\"count\"]}"], [], "widget 007\n", "name=widget\ncount=007\n");
        yield return Passing("idiom-key-value-before-equals",
            ["{name=$0;sub(/=.*/,\"\",name);print name}"], [], "name\nempty\n", "name=value\nempty=\n");
        yield return Passing("idiom-trim-record-edges",
            ["{gsub(/^[ \t]+|[ \t]+$/,\"\",$0);print \"[\" $0 \"]\"}"], [], "[alpha]\n[beta]\n", "  alpha \t\n beta\n");
        yield return Passing("idiom-replace-path-prefix",
            ["{sub(/^old\\//,\"new/\");print}"], [], "new/item\nkeep/item\n", "old/item\nkeep/item\n");
        yield return Passing("idiom-collapse-repeated-delimiters",
            ["{gsub(/:+/,\":\");print}"], [], "a:b:c\nx:y\n", "a:::b::c\nx:y\n");
        yield return Passing("idiom-count-substitutions",
            ["{n+=gsub(/a/,\"A\");print}END{print n}"], [], "bAnAnA\npeAr\n4\n", "banana\npear\n");
        yield return Passing("idiom-dynamic-record-filter",
            ["BEGIN{wanted=\"^ab\"}$0~wanted{print}"], [], "abc\nabd\n", "abc\nxyz\nabd\n");
        yield return Passing("idiom-colon-field-splitting",
            ["-F", ":", "{print $1,$3}"], [], "a c\nx z\n", "a:b:c\nx:y:z\n");
        yield return Passing("idiom-empty-delimited-fields",
            ["-F", ",", "{print NF,\"[\"$2\"]\",\"[\"$3\"]\"}"], [], "3 [] []\n3 [y] [z]\n", "a,,\nx,y,z\n");
        yield return Passing("idiom-rebuild-after-field-assignment",
            ["BEGIN{OFS=\"|\"}{$2=toupper($2);print $0}"], [], "a|WORD|c\nx|NEXT|z\n", "a word c\nx next z\n");
        yield return Passing("idiom-append-a-derived-field",
            ["{$(NF+1)=length($1);print}"], [], "alpha 7 5\nx 8 1\n", "alpha 7\nx 8\n");
        yield return Passing("idiom-truncate-record-fields",
            ["{NF=2;print}"], [], "a b\nx y\n", "a b c d\nx y z\n");
        yield return Passing("idiom-last-field-projection",
            ["{print $NF}"], [], "c\ny\n", "a b c\nx y\n");
        yield return Passing("idiom-split-numeric-text",
            ["{n=split($0,a,\":\");total=0;for(i=1;i<=n;i++)total+=a[i];print total}"], [], "6\n9\n", "1:2:3\n4:5\n");
        yield return Passing("idiom-join-split-fields",
            ["{n=split($0,a,\",\");for(i=1;i<=n;i++)printf \"%s%s\",i==1?\"\":\"|\",a[i];print \"\"}"], [], "a|b|c\nx|y\n", "a,b,c\nx,y\n");
        yield return Passing("idiom-frequency-report-in-sorted-key-order",
            ["{n[$1]++}END{c=asorti(n,k);for(i=1;i<=c;i++)print k[i],n[k[i]]}"], [], "a 1\nb 2\nc 1\n", "b\na\nb\nc\n");
        yield return Passing("idiom-function-transforms-a-field",
            ["function label(s){return toupper(substr(s,1,3))}{print label($1),$2}"], [], "ALP 2\nBET 3\n", "alpha 2\nbeta 3\n");
        yield return Passing("idiom-function-updates-an-array",
            ["function add(a,k,n){a[k]+=n}{add(s,$1,$2)}END{print s[\"a\"],s[\"b\"]}"], [], "7 4\n", "a 2\nb 4\na 5\n");
        yield return Passing("idiom-function-skips-current-record",
            ["function reject(){next}$1==\"skip\"{reject()}{print}END{print NR}"], [], "keep 1\nkeep 3\n3\n", "keep 1\nskip 2\nkeep 3\n");
        yield return Passing("idiom-read-until-eof-with-getline",
            ["BEGIN{while((getline line)>0){n++;total+=length(line)}print n,total}"], [], "2 4\n", "abc\nx\n");
        yield return Passing("idiom-csv-project-quoted-fields",
            ["--csv", "NR>1{print $1,\"[\"$2\"]\"}"], [], "1 [a,b]\n2 [two]\n", "id,value\n1,\"a,b\"\n2,\"two\"\n");
        yield return Passing("idiom-csv-multiline-field-length",
            ["--csv", "NR>1{print $1,length($2),NF}"], [], "1 3 2\n", "id,value\n1,\"a\nb\"\n");
        yield return Passing("idiom-csv-split-and-procinfo",
            ["--csv", "BEGIN{n=split(\"a,\\\"b,c\\\",d\",a);print PROCINFO[\"CSV\"],n,a[2]}"], [], "1 3 b,c\n", "");
        yield return Passing("idiom-write-quoted-csv",
            ["{s=$2;gsub(/\"/,\"\\\"\\\"\",s);printf \"%s,\\\"%s\\\"\\n\",$1,s}"], [], "a,\"text\"\nb,\"two\"\"quotes\"\n", "a text\nb two\"quotes\n");
        yield return Passing("idiom-utf8-length-and-slice",
            ["{print length($0),substr($0,2,2)}"], [], "4 af\n3 xy\n", "caf\u00e9\n\ud83d\ude00xy\n");
    }
}
