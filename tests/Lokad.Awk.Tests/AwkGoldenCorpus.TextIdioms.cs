namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> EverydayTextIdiomCases()
    {
        yield return Passing("idiom-nonempty-second-field",
            ["$2!=\"\"{print $1,$2}"], [], "a value\nc next\n", "a value\nb\nc next\n");
        yield return Passing("idiom-reject-short-records",
            ["NF<2{bad++;next}{print $1,$2}END{print \"bad\",bad+0}"], [], "a 1\nc 2\nbad 1\n", "a 1\nb\nc 2\n");
        yield return Passing("idiom-record-length-filter",
            ["length($0)>3{print NR,$0}"], [], "2 longer\n4 text\n", "ab\nlonger\nxyz\ntext\n");
        yield return Passing("idiom-count-empty-records",
            ["NF==0{n++}END{print n+0}"], [], "2\n", "a\n\n \nb\n");
        yield return Passing("idiom-count-digit-only-values",
            ["$1~/^[0-9]+$/{n++}END{print n+0}"], [], "2\n", "12\n0007\n12x\n-3\n");
        yield return Passing("idiom-validate-decimal-field",
            ["$2!~/^-?[0-9]+([.][0-9]+)?$/{print NR,$2}"], [], "3 x\n4 3.\n", "a 1.5\nb -2\nc x\nd 3.\n");
        yield return Passing("idiom-validate-date-spelling",
            ["/^[0-9]{4}-[0-9]{2}-[0-9]{2}$/{print}"], [], "2026-11-17\n", "2026-11-17\n2026/11/17\n26-1-2\n");
        yield return Passing("idiom-validate-identifier",
            ["/^[A-Za-z_][A-Za-z0-9_]*$/{print}"], [], "good_name\nX7\n", "good_name\n2bad\nX7\nhas-dash\n");
        yield return Passing("idiom-validate-ipv4-octets",
            ["{n=split($0,a,\".\");ok=n==4;for(i=1;i<=n;i++)if(a[i]!~/^[0-9]+$/||a[i]+0>255)ok=0;if(ok)print}"], [], "127.0.0.1\n10.20.30.40\n", "127.0.0.1\n256.1.2.3\n1.2.3\n10.20.30.40\n");
        yield return Passing("idiom-extract-leading-integer",
            ["{match($0,/^[0-9]+/);if(RSTART)print substr($0,RSTART,RLENGTH)}"], [], "123\n7\n", "123ms\n7days\nword\n");
        yield return Passing("idiom-extract-trailing-integer",
            ["{if(match($0,/[0-9]+$/))print substr($0,RSTART,RLENGTH)}"], [], "123\n7\n", "item123\nrow7\nnone\n");
        yield return Passing("idiom-count-word-occurrences",
            ["{s=$0;while(match(s,/cat/)){n++;s=substr(s,RSTART+RLENGTH)}}END{print n+0}"], [], "3\n", "cat cat\ndog\ncatdog\n");
        yield return Passing("idiom-case-normalized-search",
            ["index(tolower($0),\"error\"){print NR}"], [], "1\n3\n", "ERROR\ninfo\nError again\n");
        yield return Passing("idiom-extract-http-host",
            ["{s=$0;sub(/^https?:\\/\\//,\"\",s);sub(/\\/.*/,\"\",s);print s}"], [], "example.com\nhost\n", "https://example.com/path\nhttp://host/item\n");
        yield return Passing("idiom-remove-query-string",
            ["{s=$0;sub(/\\?.*/,\"\",s);print s}"], [], "/item\n/plain\n", "/item?id=7\n/plain\n");
        yield return Passing("idiom-extract-query-value",
            ["{n=split($0,a,\"&\");for(i=1;i<=n;i++){split(a[i],p,\"=\");if(p[1]==\"id\")print p[2]}}"], [], "007\n12\n", "q=text&id=007&x=1\nid=12\n");
        yield return Passing("idiom-strip-log-timestamp",
            ["{sub(/^\\[[^]]+\\] /,\"\");print}"], [], "ready\ndone\n", "[2026-11-17] ready\n[2026-11-18] done\n");
        yield return Passing("idiom-remove-carriage-return",
            ["{sub(/\\r$/,\"\");print}"], [], "one\ntwo\n", "one\r\ntwo\r\n");
        yield return Passing("idiom-tabs-to-single-spaces",
            ["{gsub(/\\t/,\" \");print}"], [], "a b c\nx y\n", "a\tb\tc\nx\ty\n");
        yield return Passing("idiom-collapse-horizontal-whitespace",
            ["{gsub(/[ \\t]+/,\" \");print}"], [], "a b c\nx y\n", "a  b\tc\nx   y\n");
        yield return Passing("idiom-trim-fields-individually",
            ["BEGIN{FS=\",\";OFS=\"|\"}{for(i=1;i<=NF;i++){gsub(/^[ \\t]+|[ \\t]+$/,\"\",$i)}print}"], [], "a|b|c\nx|y|z\n", " a , b ,c \n x,y , z\n");
        yield return Passing("idiom-normalize-dashes",
            ["{gsub(/-/,\"_\");print}"], [], "some_name\na_b_c\n", "some-name\na-b-c\n");
        yield return Passing("idiom-normalize-log-level",
            ["{$1=toupper($1);print}"], [], "INFO ready\nWARN retry\n", "info ready\nwarn retry\n");
        yield return Passing("idiom-remove-inline-comment",
            ["{sub(/#.*/,\"\");sub(/[ \\t]+$/,\"\");if(length($0))print}"], [], "a=1\nb=2\n", "a=1 # first\n# only\nb=2\n");
        yield return Passing("idiom-prefix-all-lines",
            ["{print \"prefix:\" $0}"], [], "prefix:a\nprefix:b\n", "a\nb\n");
        yield return Passing("idiom-suffix-all-lines",
            ["{print $0 \":end\"}"], [], "a:end\nb:end\n", "a\nb\n");
        yield return Passing("idiom-surround-selected-field",
            ["{print $1,\"[\"$2\"]\"}"], [], "a [word]\nb [next]\n", "a word\nb next\n");
        yield return Passing("idiom-first-three-characters",
            ["{print substr($0,1,3)}"], [], "abc\nxy\n", "abcdef\nxy\n");
        yield return Passing("idiom-last-three-characters",
            ["{n=length($0);print substr($0,n>3?n-2:1)}"], [], "def\nxy\n", "abcdef\nxy\n");
        yield return Passing("idiom-remove-first-character",
            ["{print substr($0,2)}"], [], "bc\n\n", "abc\nx\n");
        yield return Passing("idiom-remove-last-character",
            ["{print substr($0,1,length($0)-1)}"], [], "ab\n\n", "abc\nx\n");
        yield return Passing("idiom-split-on-first-colon",
            ["{p=index($0,\":\");if(p)print substr($0,1,p-1),substr($0,p+1)}"], [], "name a:b\nother value\n", "name:a:b\nother:value\n");
        yield return Passing("idiom-count-delimiters",
            ["{s=$0;print gsub(/,/,\"\",s)}"], [], "2\n0\n1\n", "a,b,c\nplain\nx,\n");
        yield return Passing("idiom-swap-two-fields",
            ["{t=$1;$1=$2;$2=t;print}"], [], "b a c\ny x z\n", "a b c\nx y z\n");
        yield return Passing("idiom-delete-first-field",
            ["{$1=\"\";sub(/^ /,\"\");print}"], [], "b c\ny\n", "a b c\nx y\n");
        yield return Passing("idiom-drop-last-field",
            ["{NF--;print}"], [], "a b\nx\n", "a b c\nx y\n");
        yield return Passing("idiom-reset-output-separator",
            ["BEGIN{OFS=\":\"}{print $1,$2,$3}"], [], "a:b:c\nx:y:z\n", "a b c\nx y z\n");
        yield return Passing("idiom-record-header-and-summary",
            ["BEGIN{print \"BEGIN\"}{print NR,$0}END{print \"END\",NR}"], [], "BEGIN\n1 a\n2 b\nEND 2\n", "a\nb\n");
        yield return Passing("idiom-empty-input-summary",
            ["{n++;s+=$1}END{print n+0,s+0}"], [], "0 0\n", "");
        yield return Passing("idiom-first-and-last-record",
            ["NR==1{first=$0}{last=$0}END{if(NR)print first,last}"], [], "a c\n", "a\nb\nc\n");
        yield return Passing("idiom-count-per-record-type",
            ["$1==\"A\"{a++}$1==\"B\"{b++}END{print a+0,b+0}"], [], "2 1\n", "A x\nB y\nA z\nC other\n");
        yield return Passing("idiom-sum-selected-record-type",
            ["$1==\"A\"{s+=$2}END{print s+0}"], [], "5\n", "A 2\nB 100\nA 3\n");
        yield return Passing("idiom-conditional-column",
            ["{print $1,($2>10?\"large\":\"small\")}"], [], "a small\nb large\n", "a 7\nb 12\n");
        yield return Passing("idiom-clamp-negative-values",
            ["{x=$1+0;if(x<0)x=0;print x}"], [], "3\n0\n0\n", "3\n-2\n0\n");
        yield return Passing("idiom-absolute-values",
            ["{x=$1+0;print x<0?-x:x}"], [], "3\n2\n0\n", "3\n-2\n0\n");
        yield return Passing("idiom-ratio-report",
            ["$3!=0{printf \"%s %.2f\\n\",$1,$2/$3}"], [], "a 1.50\nc 2.00\n", "a 3 2\nb 1 0\nc 4 2\n");
        yield return Passing("idiom-percentage-report",
            ["{printf \"%s %.1f%%\\n\",$1,100*$2/$3}"], [], "a 25.0%\nb 60.0%\n", "a 1 4\nb 3 5\n");
        yield return Passing("idiom-fixed-decimal-string-join",
            ["BEGIN{CONVFMT=\"%.2f\"}{print $1 \":\" ($2/3)}"], [], "a:0.33\nb:0.67\n", "a 1\nb 2\n");
        yield return Passing("idiom-zero-padded-row-number",
            ["{printf \"%03d %s\\n\",NR,$0}"], [], "001 a\n002 b\n", "a\nb\n");
        yield return Passing("idiom-hexadecimal-report",
            ["{printf \"%02x\\n\",$1}"], [], "0a\n0f\nff\n", "10\n15\n255\n");
        yield return Passing("idiom-octal-report",
            ["{printf \"%o\\n\",$1}"], [], "10\n100\n", "8\n64\n");
        yield return Passing("idiom-character-code-report",
            ["BEGIN{printf \"%c%c%c\\n\",65,66,67}"], [], "ABC\n", "");
        yield return Passing("idiom-truncate-formatted-text",
            ["{printf \"[%.3s]\\n\",$0}"], [], "[abc]\n[xy]\n", "abcdef\nxy\n");
        yield return Passing("idiom-escaped-percent-output",
            ["{printf \"%s: 100%%\\n\",$1}"], [], "a: 100%\nb: 100%\n", "a\nb\n");
        yield return Passing("idiom-case-conversion-pair",
            ["{print tolower($1),toupper($1)}"], [], "abc ABC\nxyz XYZ\n", "AbC\nxYz\n");
        yield return Passing("idiom-string-equality-filter",
            ["$1==\"007\"{print $1,$2}"], [], "007 keep\n", "007 keep\n7 other\nword next\n");
        yield return Passing("idiom-numeric-equality-filter",
            ["$1==7{print \"[\"$1\"]\"}"], [], "[007]\n[7]\n", "007\n7\n7x\n");
        yield return Passing("idiom-explicit-numeric-prefix-total",
            ["{s+=($1+0)}END{print s}"], [], "15\n", "12ms\n3kg\nword\n");
        yield return Passing("idiom-split-call-count-and-fields",
            ["{n=split($0,a,\";\");print n,a[1],a[n]}"], [], "3 a c\n2 x y\n", "a;b;c\nx;y\n");
        yield return Passing("idiom-nonoverlapping-substitution-result",
            ["{s=$0;n=gsub(/ab/,\"X\",s);print n,s}"], [], "2 XX\n2 XXa\n", "abab\nababa\n");
    }
}
