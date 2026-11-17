namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> EverydayReportIdiomCases()
    {
        yield return Passing("idiom-unique-first-column",
            ["!seen[$1]++{print $1}"], [], "b\na\nc\n", "b 1\na 2\nb 3\nc 4\n");
        yield return Passing("idiom-distinct-count",
            ["{seen[$1]=1}END{print length(seen)}"], [], "3\n", "a\nb\na\nc\n");
        yield return Passing("idiom-duplicate-records-only",
            ["++seen[$0]==2{print}"], [], "a\nb\n", "a\nb\na\na\nb\n");
        yield return Passing("idiom-keep-last-value-per-key",
            ["{last[$1]=$2}END{print last[\"a\"],last[\"b\"]}"], [], "new keep\n", "a old\nb keep\na new\n");
        yield return Passing("idiom-lookup-filter",
            ["BEGIN{allowed[\"a\"];allowed[\"c\"]}$1 in allowed{print}"], [], "a 1\nc 3\n", "a 1\nb 2\nc 3\n");
        yield return Passing("idiom-lookup-join",
            ["BEGIN{price[\"a\"]=2;price[\"b\"]=3}($1 in price){print $1,$2*price[$1]}"], [], "a 8\nb 6\n", "a 4\nx 5\nb 2\n");
        yield return Passing("idiom-composite-key-sums",
            ["{s[$1,$2]+=$3}END{print s[\"a\",\"x\"],s[\"a\",\"y\"],s[\"b\",\"x\"]}"], [], "7 3 4\n", "a x 2\na y 3\nb x 4\na x 5\n");
        yield return Passing("idiom-nested-array-sums",
            ["{s[$1][$2]+=$3}END{print s[\"a\"][\"x\"],s[\"a\"][\"y\"],s[\"b\"][\"x\"]}"], [], "7 3 4\n", "a x 2\na y 3\nb x 4\na x 5\n");
        yield return Passing("idiom-nested-array-row-counts",
            ["{s[$1][$2]++}END{print length(s),length(s[\"a\"]),s[\"a\"][\"x\"]}"], [], "2 2 2\n", "a x\na y\nb x\na x\n");
        yield return Passing("idiom-remove-seen-key",
            ["{if($1 in seen){print \"again\",$1;delete seen[$1]}else seen[$1]=1}END{print length(seen)}"], [], "again a\n2\n", "a\nb\na\nc\n");
        yield return Passing("idiom-sorted-frequency-descending-values",
            ["{n[$1]++}END{PROCINFO[\"sorted_in\"]=\"@val_num_desc\";for(k in n)print k,n[k]}"], [], "a 3\nb 2\nc 1\n", "a\nb\na\nc\na\nb\n");
        yield return Passing("idiom-sort-numeric-values",
            ["{a[NR]=$1+0}END{n=asort(a);for(i=1;i<=n;i++)print a[i]}"], [], "-1\n2\n10\n", "10\n2\n-1\n");
        yield return Passing("idiom-sort-into-separate-array",
            ["BEGIN{a[\"z\"]=3;a[\"a\"]=1;a[\"x\"]=2;n=asort(a,b);print n,b[1],b[3],a[\"z\"]}"], [], "3 1 3 3\n", "");
        yield return Passing("idiom-clear-array-per-record",
            ["{delete a;n=split($0,a,\",\");print n,length(a),a[1]}"], [], "3 3 a\n2 2 x\n", "a,b,c\nx,y\n");
        yield return Passing("idiom-reverse-field-order",
            ["{for(i=NF;i>=1;i--)printf \"%s%s\",i==NF?\"\":\" \",$i;print \"\"}"], [], "c b a\ny x\n", "a b c\nx y\n");
        yield return Passing("idiom-sum-all-fields",
            ["{s=0;for(i=1;i<=NF;i++)s+=$i;print s}"], [], "6\n9\n", "1 2 3\n4 5\n");
        yield return Passing("idiom-column-count-validation",
            ["NF!=3{print NR,NF}"], [], "2 2\n3 4\n", "a b c\nx y\np q r s\n");
        yield return Passing("idiom-fixed-width-columns",
            ["BEGIN{FIELDWIDTHS=\"2 3 2\"}{print \"[\"$1\"]\",\"[\"$2\"]\",\"[\"$3\"]\"}"], [], "[ab] [123] [XY]\n[cd] [456] [ZZ]\n", "ab123XY\ncd456ZZ\n");
        yield return Passing("idiom-character-field-splitting",
            ["BEGIN{FS=\"\"}{print NF,$1,$NF}"], [], "3 a c\n2 x y\n", "abc\nxy\n");
        yield return Passing("idiom-literal-pipe-field-separator",
            ["BEGIN{FS=\"|\"}{print NF,$2}"], [], "3 b\n2 y\n", "a|b|c\nx|y\n");
        yield return Passing("idiom-regex-field-separator",
            ["BEGIN{FS=\"[,:]\"}{print NF,$1,$NF}"], [], "3 a c\n3 x z\n", "a,b:c\nx:y,z\n");
        yield return Passing("idiom-blank-record-separator",
            ["BEGIN{RS=\"\"}{print NR,NF,$1,$NF}"], [], "1 3 a c\n2 2 x y\n", "a b\nc\n\nx y\n\n");
        yield return Passing("idiom-literal-record-separator",
            ["BEGIN{RS=\";\"}{print NR,\"[\"$0\"]\"}"], [], "1 [a]\n2 [b]\n3 [c]\n", "a;b;c;");
        yield return Passing("idiom-regex-record-separator-terminator",
            ["BEGIN{RS=\"[,;]+\"}{print \"[\"$0\"]\",\"[\"RT\"]\"}"], [], "[a] [,,]\n[b] [;]\n[c] [;]\n", "a,,b;c;");
        yield return Passing("idiom-word-count-report",
            ["{lines++;words+=NF;chars+=length($0)}END{print lines,words,chars}"], [], "2 3 12\n", "one two\nthree\n");
        yield return Passing("idiom-per-record-prefix-and-suffix",
            ["{printf \"<%s>\\n\",$0}"], [], "<alpha>\n<beta>\n", "alpha\nbeta\n");
        yield return Passing("idiom-printf-aligned-columns",
            ["{printf \"%-6s%04d\\n\",$1,$2}"], [], "a     0007\nlong  0012\n", "a 7\nlong 12\n");
        yield return Passing("idiom-printf-star-width",
            ["{printf \"[%*s]\\n\",5,$1}"], [], "[    a]\n[  abc]\n", "a\nabc\n");
        yield return Passing("idiom-printf-rounded-currency",
            ["{printf \"%s %.2f\\n\",$1,$2*$3}"], [], "a 7.50\nb 4.80\n", "a 2.5 3\nb 1.2 4\n");
        yield return Passing("idiom-numeric-string-spelling",
            ["{print \"[\"$1\"]\",$1+0,($1==7)}"], [], "[0007] 7 1\n[7] 7 1\n[7tail] 7 0\n", "0007\n7\n7tail\n");
        yield return Passing("idiom-integer-truncation",
            ["{print int($1)}"], [], "2\n-2\n0\n", "2.9\n-2.9\n0.5\n");
        yield return Passing("idiom-numeric-math-report",
            ["{printf \"%.3f %.3f\\n\",sqrt($1),log($1)}"], [], "1.000 0.000\n2.000 1.386\n", "1\n4\n");
        yield return Passing("idiom-trigonometric-constant",
            ["BEGIN{printf \"%.6f %.6f\\n\",atan2(0,-1),sin(0)+cos(0)}"], [], "3.141593 1.000000\n", "");
        yield return Passing("idiom-string-index-filter",
            ["index($0,\"needle\"){print NR,index($0,\"needle\")}"], [], "1 1\n2 3\n", "needle\nx needle y\nother\n");
        yield return Passing("idiom-strip-filename-extension",
            ["{name=$0;sub(/\\.[^.]*$/,\"\",name);print name}"], [], "report\narchive.tar\nplain\n", "report.csv\narchive.tar.gz\nplain\n");
        yield return Passing("idiom-basename-with-split",
            ["{n=split($0,a,\"/\");print a[n]}"], [], "file.txt\nname\n", "dir/file.txt\nother/name\n");
        yield return Passing("idiom-extract-between-delimiters",
            ["{start=index($0,\"[\");end=index($0,\"]\");if(start&&end>start)print substr($0,start+1,end-start-1)}"], [], "abc\n42\n", "x[abc]y\nnone\n[42]\n");
        yield return Passing("idiom-capture-and-reformat",
            ["{if(match($0,/^([a-z]+):([0-9]+)$/,m))print m[2],m[1]}"], [], "007 abc\n12 def\n", "abc:007\nbad\ndef:12\n");
        yield return Passing("idiom-gensub-reorder-captures",
            ["{print gensub(/^([a-z]+):([0-9]+)$/,\"\\\\2-\\\\1\",1,$0)}"], [], "007-abc\n12-def\n", "abc:007\ndef:12\n");
        yield return Passing("idiom-ignorecase-record-filter",
            ["BEGIN{IGNORECASE=1}/^error/{print NR}"], [], "1\n3\n", "ERROR one\ninfo two\nError three\n");
        yield return Passing("idiom-lowercase-key-count",
            ["{n[tolower($1)]++}END{print n[\"a\"],n[\"b\"]}"], [], "2 1\n", "A\na\nB\n");
        yield return Passing("idiom-getline-pairs",
            ["{first=$0;if((getline second)>0)print first \":\" second;else print first \":missing\"}"], [], "a:b\nc:missing\n", "a\nb\nc\n");
        yield return Passing("idiom-skip-header-and-footer",
            ["NR==1{next}{if(NR>2)print previous;previous=$0}"], [], "one\ntwo\n", "header\none\ntwo\nfooter\n");
        yield return Passing("idiom-range-pattern-markers",
            ["/^START$/,/^END$/{print}"], [], "START\na\nEND\n", "skip\nSTART\na\nEND\ntail\n");
        yield return Passing("idiom-last-two-records",
            ["{previous=last;last=$0}END{if(NR>1)print previous;if(NR)print last}"], [], "b\nc\n", "a\nb\nc\n");
        yield return Passing("idiom-bounded-early-exit",
            ["{print;if(NR==2)exit}"], [], "a\nb\n", "a\nb\nc\n");
        yield return Passing("idiom-stateful-section-totals",
            ["/^GROUP/{if(n)print group,total;group=$2;total=0;n++;next}{total+=$1}END{if(n)print group,total}"], [], "a 5\nb 7\n", "GROUP a\n2\n3\nGROUP b\n7\n");
        yield return Passing("idiom-function-recursive-factorial",
            ["function fact(n){return n<2?1:n*fact(n-1)}BEGIN{print fact(6)}"], [], "720\n", "");
        yield return Passing("idiom-function-local-loop-index",
            ["function sum(a,n, i,s){for(i=1;i<=n;i++)s+=a[i];return s}BEGIN{split(\"2 3 4\",a);i=99;print sum(a,3),i}"], [], "9 99\n", "");
        yield return Passing("idiom-function-global-counter",
            ["function nextid(){return ++id}{print nextid(),$0}"], [], "1 a\n2 b\n", "a\nb\n");
    }
}
