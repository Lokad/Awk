namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> EverydayCsvControlIdiomCases()
    {
        yield return Passing("idiom-csv-record-count",
            ["--csv", "END{print NR}"], [], "3\n", "id,value\n1,\"a,b\"\n2,x\n");
        yield return Passing("idiom-csv-field-count",
            ["--csv", "{print NR,NF}"], [], "1 3\n2 3\n", "a,b,c\n1,\"x,y\",3\n");
        yield return Passing("idiom-csv-numeric-total",
            ["--csv", "NR>1{total+=$2}END{printf \"%.2f\\n\",total}"], [], "3.75\n", "id,value\na,1.25\nb,\"2.50\"\n");
        yield return Passing("idiom-csv-header-name-lookup",
            ["--csv", "NR==1{for(i=1;i<=NF;i++)column[$i]=i;next}{print $(column[\"name\"]),$(column[\"amount\"])}"], [], "alpha 7\nbeta 8\n", "amount,name\n7,alpha\n8,beta\n");
        yield return Passing("idiom-csv-reorder-as-tsv",
            ["--csv", "BEGIN{OFS=\"\\t\"}NR>1{print $2,$1}"], [], "alpha,beta\t1\ngamma\t2\n", "id,name\n1,\"alpha,beta\"\n2,gamma\n");
        yield return Passing("idiom-csv-empty-field-report",
            ["--csv", "{print NF,\"[\"$2\"]\",\"[\"$3\"]\"}"], [], "3 [] []\n3 [] [c]\n", "a,,\nb,\"\",c\n");
        yield return Passing("idiom-csv-escaped-quote-report",
            ["--csv", "{print \"[\"$2\"]\",length($2)}"], [], "[two\"quotes] 10\n[plain] 5\n", "a,\"two\"\"quotes\"\nb,\"plain\"\n");
        yield return Passing("idiom-csv-crlf-records",
            ["--csv", "{print NR,$1,length($2)}"], [], "1 a 4\n2 b 4\n", "a,word\r\nb,next\r\n");
        yield return Passing("idiom-csv-multiline-record-number",
            ["--csv", "{print NR,NF,length($2)}"], [], "1 2 7\n2 2 3\n", "a,\"one\ntwo\"\nb,end\n");
        yield return Passing("idiom-csv-retain-numeric-spelling",
            ["--csv", "{print \"[\"$2\"]\",$2+0}"], [], "[0007] 7\n[0012] 12\n", "a,0007\nb,\"0012\"\n");
        yield return Passing("idiom-csv-grouped-total",
            ["--csv", "NR>1{s[$1]+=$2}END{print s[\"a\"],s[\"b\"]}"], [], "6 3\n", "key,value\na,2\nb,3\na,4\n");
        yield return Passing("idiom-csv-filter-quoted-comma",
            ["--csv", "$2==\"a,b\"{print $1}"], [], "1\n", "1,\"a,b\"\n2,other\n");
        yield return Passing("idiom-csv-field-mutation-rebuild",
            ["--csv", "BEGIN{OFS=\"|\"}{$2=toupper($2);print $0}"], [], "a|WORD|c\nb|NEXT|d\n", "a,word,c\nb,next,d\n");
        yield return Passing("idiom-csv-split-trailing-empty",
            ["--csv", "BEGIN{n=split(\"a,b,\",a);print n,\"[\"a[3]\"]\"}"], [], "3 []\n", "");
        yield return Passing("idiom-csv-write-normalized-quoted",
            ["--csv", "function csv(s){gsub(/\"/,\"\\\"\\\"\",s);return \"\\\"\" s \"\\\"\"}{print csv($1) \",\" csv($2)}"], [], "\"id\",\"value\"\n\"1\",\"a,b\"\n\"2\",\"x\"\"y\"\n", "id,value\n1,\"a,b\"\n2,\"x\"\"y\"\n");
        yield return Passing("idiom-nested-matrix-total",
            ["{m[$1][$2]=$3}END{total=0;for(r in m)for(c in m[r])total+=m[r][c];print total}"], [], "9\n", "a x 2\na y 3\nb x 4\n");
        yield return Passing("idiom-nested-per-group-distinct",
            ["{seen[$1][$2]=1}END{print length(seen[\"a\"]),length(seen[\"b\"])}"], [], "2 1\n", "a x\na x\na y\nb z\n");
        yield return Passing("idiom-nested-delete-cell",
            ["BEGIN{a[\"r\"][\"x\"]=2;a[\"r\"][\"y\"]=3;delete a[\"r\"][\"x\"];print length(a[\"r\"]),(\"x\" in a[\"r\"]),a[\"r\"][\"y\"]}"], [], "1 0 3\n", "");
        yield return Passing("idiom-nested-delete-row",
            ["BEGIN{a[\"r\"][\"x\"]=2;a[\"s\"][\"x\"]=3;delete a[\"r\"];print length(a),(\"r\" in a),a[\"s\"][\"x\"]}"], [], "1 0 3\n", "");
        yield return Passing("idiom-nested-array-parameter",
            ["function add(row,k,v){row[k]+=v}BEGIN{add(a[\"r\"],\"x\",2);add(a[\"r\"],\"x\",3);print a[\"r\"][\"x\"]}"], [], "5\n", "");
        yield return Passing("idiom-nested-array-kind",
            ["BEGIN{a[\"r\"][\"x\"]=2;print isarray(a),isarray(a[\"r\"]),isarray(a[\"r\"][\"x\"])}"], [], "1 1 0\n", "");
        yield return Passing("idiom-nested-sorted-row-keys",
            ["BEGIN{a[\"b\"][\"x\"]=2;a[\"a\"][\"x\"]=1;n=asorti(a,k);for(i=1;i<=n;i++)print k[i],a[k[i]][\"x\"]}"], [], "a 1\nb 2\n", "");
        yield return Passing("idiom-composite-versus-nested-keys",
            ["BEGIN{a[\"r\",\"x\"]=2;b[\"r\"][\"x\"]=3;print length(a),length(b),a[\"r\",\"x\"],b[\"r\"][\"x\"]}"], [], "1 1 2 3\n", "");
        yield return Passing("idiom-function-array-clear",
            ["function clear(a){delete a}BEGIN{a[1]=2;a[2]=3;clear(a);print length(a)}"], [], "0\n", "");
        yield return Passing("idiom-function-swap-array-values",
            ["function swap(a,x,y,t){t=a[x];a[x]=a[y];a[y]=t}BEGIN{a[1]=\"left\";a[2]=\"right\";swap(a,1,2);print a[1],a[2]}"], [], "right left\n", "");
        yield return Passing("idiom-function-local-shadow",
            ["function f(x,tmp){tmp=x+1;return tmp}BEGIN{tmp=99;print f(4),tmp}"], [], "5 99\n", "");
        yield return Passing("idiom-function-return-string",
            ["function decorate(s){return \"[\" s \"]\"}{print decorate($1)}"], [], "[alpha]\n[beta]\n", "alpha\nbeta\n");
        yield return Passing("idiom-function-boolean-filter",
            ["function valid(s){return length(s)>=3&&s!~/^#/}valid($1){print $1}"], [], "alpha\nbeta\n", "ab\nalpha\n#comment\nbeta\n");
        yield return Passing("idiom-function-recursive-gcd",
            ["function gcd(a,b){return b==0?a:gcd(b,a%b)}BEGIN{print gcd(48,18)}"], [], "6\n", "");
        yield return Passing("idiom-function-mutual-recursion",
            ["function even(n){return n==0?1:odd(n-1)}function odd(n){return n==0?0:even(n-1)}BEGIN{print even(8),odd(8)}"], [], "1 0\n", "");
        yield return Passing("idiom-function-nextfile-on-first-record",
            ["function done(){nextfile}{print;done()}END{print NR}"], [], "first\n1\n", "first\nsecond\n");
        yield return Passing("idiom-loop-break-at-sentinel",
            ["{for(i=1;i<=NF;i++){if($i==\"STOP\")break;print $i}}"], [], "a\nb\nx\n", "a b STOP c\nx STOP y\n");
        yield return Passing("idiom-loop-continue-negative-fields",
            ["{s=0;for(i=1;i<=NF;i++){if($i<0)continue;s+=$i}print s}"], [], "4\n4\n", "1 -2 3\n-1 4\n");
        yield return Passing("idiom-do-while-decimal-digits",
            ["{n=int($1);digits=0;do{digits++;n=int(n/10)}while(n>0);print digits}"], [], "1\n1\n3\n", "0\n7\n123\n");
        yield return Passing("idiom-while-power-of-two",
            ["{n=1;while(n<$1)n*=2;print n}"], [], "1\n4\n16\n", "1\n3\n9\n");
        yield return Passing("idiom-switch-record-classification",
            ["{switch($1){case \"A\":print \"alpha\";break;case \"B\":print \"beta\";break;default:print \"other\"}}"], [], "alpha\nbeta\nother\n", "A\nB\nC\n");
        yield return Passing("idiom-getline-count-without-main-actions",
            ["BEGIN{for(n=0;(getline)>0;n++);print n,NR}"], [], "2 2\n", "one\ntwo\n");
        yield return Passing("idiom-get-record-and-field-count",
            ["{print NR,NF,length($0)}"], [], "1 2 3\n2 1 1\n", "a b\nx\n");
        yield return Passing("idiom-print-blank-line-between-groups",
            ["NR>1&&$1!=previous{print \"\"}{print;previous=$1}"], [], "a 1\na 2\n\nb 3\n", "a 1\na 2\nb 3\n");
        yield return Passing("idiom-collect-key-list",
            ["{if(!($1 in seen)){seen[$1]=1;keys=keys (keys==\"\"?\"\":\",\") $1}}END{print keys}"], [], "a,b,c\n", "a\nb\na\nc\n");
        yield return Passing("idiom-sum-count-average-per-key",
            ["{sum[$1]+=$2;count[$1]++}END{printf \"a %d %.1f\\n\",count[\"a\"],sum[\"a\"]/count[\"a\"]}"], [], "a 2 3.0\n", "a 2\nb 9\na 4\n");
        yield return Passing("idiom-label-minimum-record",
            ["NR==1||$2<minimum{minimum=$2;label=$1}END{print label,minimum}"], [], "b 3\n", "a 8\nb 3\nc 5\n");
        yield return Passing("idiom-concatenate-records-with-separator",
            ["{text=text (NR==1?\"\":\";\") $0}END{print text}"], [], "alpha;beta;gamma\n", "alpha\nbeta\ngamma\n");
        yield return Passing("idiom-count-words-by-fixed-vocabulary",
            ["{for(i=1;i<=NF;i++)n[tolower($i)]++}END{print n[\"cat\"],n[\"dog\"]}"], [], "3 1\n", "Cat dog\ncat CAT\n");
        yield return Passing("idiom-field-list-to-key-value",
            ["{for(i=1;i<=NF;i++)print i \"=\" $i}"], [], "1=a\n2=b\n1=x\n2=y\n3=z\n", "a b\nx y z\n");
        yield return Passing("idiom-utc-date-format",
            ["BEGIN{print strftime(\"%Y-%m-%dT%H:%M:%SZ\",0,1)}"], [], "1970-01-01T00:00:00Z\n", "");
        yield return Passing("idiom-utc-date-parse",
            ["BEGIN{print mktime(\"1970 01 02 00 00 00\",1)}"], [], "86400\n", "");
        yield return Passing("idiom-bitwise-flags",
            ["BEGIN{flags=or(1,4);print and(flags,4),and(flags,2),xor(flags,1)}"], [], "4 0 4\n", "");
        yield return Passing("idiom-numeric-base-conversion",
            ["BEGIN{print strtonum(\"0xff\"),strtonum(\"010\"),strtonum(\"12\")}"], [], "255 8 12\n", "");
        yield return Passing("idiom-argument-variable-filter",
            ["-v", "minimum=5", "$2>=minimum{print $1}"], [], "b\nc\n", "a 2\nb 5\nc 7\n");
    }
}
