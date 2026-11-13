namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> StringResultCases()
    {
        yield return Passing("numeric-builtin-arguments-keep-arity-captures-and-format-at-use",
            ["""function change(){CONVFMT="%.2f";return "done"}BEGIN{print substr("abcdef","2",3);print substr("abcdef",2);CONVFMT="%.1f";print sprintf("%s:%s",1.25+0,change());print sprintf("%d",sqrt(16)+1)}"""],
            [], "bcd\nbcdef\n1.25:done\n5\n", "");
        yield return Passing("text-producer-special-assignment-ends-prior-field-demand",
            ["""BEGIN{FS=",";OFS="2"}{seen=length($3);NF=tolower(OFS);printf "[%s] %s <%s>\n",$3,NF,$0;NF=substr("0004",1);printf "[%s] %s <%s>\n",$4,NF,$0}"""],
            [], "[] 2 <a2b>\n[] 0004 <a2b22>\n", "a,b,c\n");
        yield return Passing("immediate-case-length-keeps-formatting-unicode-and-input-origins",
            ["""function size(s){return length(tolower(s))+length(toupper(s))}BEGIN{OFS="|";print length(tolower("CAFÉ")),length(toupper("résumé")),length(tolower("")),length(toupper(1.25)),size("AbC")}{print size($1),length(tolower($1)),typeof($1),$1}"""], [],
            "4|6|0|4|6\n8|4|strnum|0007\n8|4|string|café\n", "0007\ncafé\n");
        yield return Passing("case-length-keeps-observed-assignments-and-nested-producers",
            ["""function sizes(s,t){print length(t=tolower(s)),typeof(t),t;print length((t=toupper(s))),typeof(t),t;print length(tolower(toupper(s))),s}BEGIN{sizes("café")}"""], [],
            "4 string café\n4 string CAFÉ\n4 café\n");
        yield return Passing("substring-printing-function-keeps-its-text-argument-through-host-output",
            ["""function emit(s){print substr(s,1,2);return index(s,"!")}{print emit($1);exit}"""], [],
            "ré\n7\n", "résumé! tail\n");
        yield return Passing("stored-text-consumers-preserve-origins-formatting-and-array-length",
            ["""function show(x,t){t=tolower(x);print length(x),length(t),substr(x,2,2),index(x,"0"),typeof(x),typeof(t),x}function count(a){return length(a)}BEGIN{CONVFMT="%.2f";show("0007");show(12.5);a[1]=1;a[2]=2;print count(a)}{show($1)}"""], [],
            "4 4 00 1 string string 0007\n5 5 2. 5 number string 12.5\n2\n4 4 00 1 strnum string 0007\n", "0007\n");
        yield return Passing("stored-text-captures-precede-later-global-mutation-and-format-at-use",
            ["""function later(){s="changed";CONVFMT="%.3f";return 2}function dot(){CONVFMT="%.3f";return "."}function clip(x,t){t=tolower(x);return substr(t,later(),2)}BEGIN{s="café";print substr(s,later(),2),s;print clip("CAFÉ"),s;CONVFMT="%.1f";s=1.23456;print index(s,dot()),s;print length(s),typeof(s)}"""], [],
            "af changed\naf changed\n2 1.23456\n5 number\n");
        yield return Passing("stored-text-reads-hide-caller-locals-during-file-phase-reentry",
            ["""function consume(s){getline;print length(s),tolower(s);return substr(s,1,2)}BEGIN{s="GLOBAL";print consume("LOCAL")}BEGINFILE{print length(s),tolower(s)}"""], [],
            "6 global\n5 local\nLO\n", "data\n");
        yield return Passing("consumed-return-assignments-preserve-scalar-origins-and-used-snapshots",
            ["""function keep(x){return x}function local(x,t){t=keep(x);print typeof(t),"["t"]",t+0;return t}BEGIN{v=keep("0007");print typeof(v),v;print (v=keep("before")),(v="after");v=local(7);print typeof(v),v}{v=local($1);print typeof(v),v}"""],
            [], "string 0007\nbefore after\nnumber [7] 7\nnumber 7\nstrnum [0007] 7\nstrnum 0007\n", "0007\n");
        yield return Passing("consumed-return-assignments-preserve-recursion-and-special-targets",
            ["""function keep(x){return x}function recurse(x,n,t){if(n==0)return x;t=recurse(x,n-1);return t}BEGIN{v=recurse("é0007",4);OFS=keep("|");ORS=keep("!");$0="a b c";NF=keep("02");print v,NF,$0;v=keep("");print typeof(v),"["v"]"}"""],
            [], "é0007|02|a|b!string|[]!", "");
        yield return Passing("substring-indices-preserve-observed-numeric-string-origins",
            ["""function clip(s,n,t){t=typeof(n);if(length(s)>n)return t ":" substr(s,1,n);return t ":" s}{print clip($1,$2),$2,typeof($2)}"""],
            [], "strnum:ré 0002 strnum\nstring:ré 2tail string\n", "résumé 0002\nrésumé 2tail\n");
        yield return Passing("substring-indices-retain-captures-before-later-effects",
            ["""function later(){start=3;FS=":";$0="new:uvw";return 2}BEGIN{start=2;$0="key abcd";print substr($2,start,later()),start,$0}"""],
            [], "bc 3 new:uvw\n");
        yield return Passing("substring-indices-preserve-computed-and-general-numeric-arguments",
            ["""function clip(s,n){return substr(s,1,n)}BEGIN{s="é😀z";print "["clip(s,1+1)"]","["clip(s,"02tail")"]","["clip(s,0)"]","["clip(s,-1)"]","["clip(s,2.9)"]";print substr(s,1+0),substr(s,1+1,1+0)}"""],
            [], "[é😀] [é😀] [] [] [é😀]\né😀z 😀\n");
        yield return Passing("substring-print-keeps-earlier-captures-and-later-separators",
            ["""function change(){$0="next abcd";OFS="|";ORS="!";return 2}BEGIN{$0="old café";print length($1),substr($2,change(),2)}"""],
            [], "3|af!", "");
        yield return Passing("substring-print-preserves-owned-unicode-ranges-and-empty-results",
            ["""BEGIN{s="é😀z";print substr(s,2,1);print substr(s,-2,1);print substr(s,1.9,2.8);print "["substr(s,99)"]";print "x",substr(s,2,0)}"""],
            [], "😀\né\né😀\n[]\nx \n", "");
        yield return Passing("local-text-assignments-preserve-used-snapshots-and-chained-values",
            ["""function f(s,t,a){t=tolower(s);print typeof(t),t;a=t=substr(s,1);print a,t;print (t=tolower(s)),(t="changed");print t;return a}BEGIN{print f("0007"),f("CAFÉ")}"""],
            [], "string 0007\n0007 0007\n0007 changed\nchanged\nstring café\nCAFÉ CAFÉ\ncafé changed\nchanged\n0007 CAFÉ\n", "");
        yield return Passing("string-views-own-dirty-fields-before-immediate-record-reconstruction",
            ["BEGIN{$0=\"old abcd\";$1=1;print substr($2,$0,2),$0;$0=\"old abcd\";$1=1;print index($2,$0),$0}"],
            [], "ab 1 abcd\n0 1 abcd\n", "");
        yield return Passing("string-views-keep-assigned-number-formatting-at-consumption",
            ["function change(){CONVFMT=\"%.3f\";return \".\"}BEGIN{CONVFMT=\"%.1f\";$0=1.23456;print index($0,change());CONVFMT=\"%.1f\";$1=1.23456;print index($1,change())}"],
            [], "2\n2\n", "");
        yield return Passing("string-views-preserve-full-empty-and-scalar-substrings",
            ["{a=substr($2,1);b=substr($2,2,2);c=substr($2,99);d=substr($2,1,0);print \"[\"a\"]\",\"[\"b\"]\",\"[\"c\"]\",\"[\"d\"]\",typeof(a),$2}"],
            [], "[é😀z] [😀z] [] [] string é😀z\n", "key é😀z\n");
        yield return Passing("direct-text-keeps-input-origins-through-later-observation",
            ["{a=tolower($1);b=substr($1,2,3);n=index($1,\"0\");print \"[\"a\"]\",\"[\"b\"]\",n,typeof($1),$1}"],
            [], "[0007] [007] 1 strnum 0007\n[7tail] [tai] 0 string 7tail\n", "0007\n7tail\n");
        yield return Passing("direct-text-numeric-records-and-fields-format-after-later-arguments",
            ["function change(){CONVFMT=\"%.3f\";return 1}BEGIN{CONVFMT=\"%.1f\";$0=1.23456;print \"[\"substr($0,change())\"]\";CONVFMT=\"%.1f\";$1=1.23456;print \"[\"substr($1,change())\"]\"}"],
            [], "[1.235]\n[1.235]\n", "");
        yield return Passing("direct-text-recomposed-record-keeps-text-origin",
            ["BEGIN{CONVFMT=\"%.1f\";$0=1.23456;$2=\"tail\";CONVFMT=\"%.3f\";print \"[\"substr($0,1)\"]\",typeof($0);print tolower($0),typeof($1)}"],
            [], "[1.2 tail] string\n1.2 tail strnum\n", "");
        yield return Passing("direct-text-captures-dynamic-fields-before-later-effects",
            ["function change(){$0=\"changed next\";return 2}{i=1;print substr($(i++),change(),3),i,$0}"],
            [], "007 2 changed next\n", "0007 tail\n");
        yield return Passing("direct-text-csv-decoding-does-not-change-input-origin",
            ["--csv", "{a=substr($1,1,4);b=tolower($1);v=$1;print \"[\"a\"]\",\"[\"b\"]\",typeof(v),v,NF}"],
            [], "[00\"0] [00\"07] string 00\"07 2\n[0007] [0007] strnum 0007 2\n",
            "\"00\"\"07\",tail\n0007,tail\n");
        yield return AwkGoldenCaseBuilder.Create("direct-substring-defers-numeric-error-until-after-later-argument",
                ["--non-decimal-data", "function later(){print \"later\";return 2}{print substr($1,$2,later());print \"unreached\"}"], [])
            .WithStandardInput("abcd 0x1.8\n").WithStdOut("later\n").ExitsWith(2)
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .WithNondeterminismNote("Lokad preserves its documented missing-exponent diagnostic; GNU accepts a numeric prefix.")
            .Build();
        yield return Passing("prepared-array-keys-keep-spelling-origins-and-current-format",
            ["function use(k,missing){a[k][\"sum\"]+=2;a[\"literal\"][\"n\"]++;a[missing]+=3;print typeof(k),typeof(missing),(k in a),(missing in a)}" +
             "function read(k){print typeof(k),k,b[k]}" +
             "BEGIN{key=\"007\";a[key][\"sum\"]=5;use(key);print a[\"007\"][\"sum\"],a[\"literal\"][\"n\"],a[\"\"];" +
             "n=1.25;CONVFMT=\"%.3f\";b[n]=9;CONVFMT=\"%.2f\";b[n]=11;print b[\"1.250\"],b[\"1.25\"];key=\"café\";b[key]=12;print b[key],key}" +
             "{b[$1]=10;read($1)}"], [],
            "string unassigned 1 1\n7 1 3\n9 11\n12 café\nstrnum 007 10\n", "007\n");
        yield return Passing("captured-scalar-keys-keep-short-long-utf8-and-nul-spelling",
            ["""function add(a,k,v){a[k]["sum"]+=v;a[k]["n"]++}BEGIN{SUBSEP="|";PROCINFO["sorted_in"]="@ind_str_asc";add(a,"abc😀",1);add(a,"abc😀x",2);add(a,"a\0b",3);add(a,"007",4);add(a,7,5);for(k in a)print length(k),k,a[k]["sum"],a[k]["n"];k="a\0b";old=a[k]["sum"]++;delete a[k]["n"];print old,a[k]["sum"],("n" in a[k]);print ("missing" in a),length(a)}"""], [],
            "3 007 4 1\n1 7 5 1\n3 a\0b 3 1\n4 abc😀 1 1\n5 abc😀x 2 1\n3 4 0\n0 5\n");
        yield return Passing("prepared-scalar-arguments-keep-origin-and-earlier-capture",
            ["""function leaf(x){print typeof(x),"["x"]",(x<3);return x}function forward(x){return leaf(x)}function change(){g="after";return 9}function capture(a,b){print "["a"]",b}BEGIN{g="007";print forward(g);capture(g,change());g=7;print forward(g)}{g=$1;print forward(g)}"""], [],
            "string [007] 1\n007\n[007] 9\nnumber [7] 0\n7\nstrnum [007] 0\n007\n", "007\n");
        foreach (var option in new[] { "", "--posix", "--traditional", "-b", "--characters-as-bytes" })
        {
            const string program = "BEGIN{print toupper(\"éaΩ\"),tolower(\"ÉAΩ\");print toupper(\"é09Ω\"),tolower(\"é09ω\");print (toupper(\"0\")?1:0),(tolower(\"0\")?1:0)}";
            var bytes = option is "-b" or "--characters-as-bytes";
            yield return Passing("case-conversion-units-" + (option.Length == 0 ? "default" : option.TrimStart('-')),
                option.Length == 0 ? [program] : [option, program], [],
                (bytes ? "éAΩ ÉaΩ\né09Ω é09ω\n" : "ÉAΩ éaω\nÉ09Ω é09ω\n") + "1 1\n");
        }
        yield return Passing("field-substrings-own-partial-and-whole-results-across-getline",
            ["{a=substr($2,1,2);b=substr($2,1);getline;print a,b,$2;exit}"],
            [], "ré résumé replaced\n", "café résumé\nnext replaced\n");
        yield return Passing("field-substring-keeps-input-before-mutating-argument",
            ["function change(){$0=\"x different\";return 1} {print substr($2,change(),2),$2}"],
            [], "ré different\n", "café résumé\n");
        yield return Passing("short-concatenation-formats-each-operand-before-later-effects",
            ["function fmt(){CONVFMT=\"%.1f\";return \"β\"} BEGIN{CONVFMT=\"%.3f\";s=1.2344 fmt() 1.2344;print s}"],
            [], "1.234β1.2\n");
        yield return Passing("short-concatenation-keeps-fields-across-getline",
            ["{print \"[\" $1 \":\" ((getline)>0 ? $1 : \"EOF\") \"]\"}"],
            [], "[café:δ]\n", "café\nδ\n");
        yield return Passing("concatenation-consumers-preserve-plain-strings-and-used-snapshots",
            ["""function id(x){return x}BEGIN{a="" "";b="" "007";c="00" "7";d="" "0" "" "07" "";print typeof(a),"["a"]",typeof(b),b,typeof(c),c,typeof(d),d;print(b==7),(c==7),(d==7),length("" "007"),length("00" "7");saved=(used="00" "7");print typeof(saved),saved,used;keys[b]=c;print(7 in keys),("007" in keys),keys["007"],id("00" "7")}{s="" $1;print typeof(s),(s==7),s}"""],
            [], "string [] string 007 string 007 string 007\n0 0 0 3 3\nstring 007 007\n0 1 007 007\nstring 0 007\n", "007\n");
        foreach (var (option, number) in new[] { ("", 0), ("--non-decimal-data", 16), ("--posix", 16), ("--traditional", 0) })
        {
            const string program = "function id(x){return x}BEGIN{v=\"0\" \"x10\";print v,v+0,id(\"0\" \"x10\")}";
            yield return Passing("concatenation-consumer-numeric-mode-" + (option.Length == 0 ? "default" : option[2..]),
                option.Length == 0 ? [program] : [option, program], [], $"0x10 {number} 0x10\n");
        }
        yield return Passing("string-results-retain-string-typing-and-keys",
            ["BEGIN{s=substr(\"_008_\",2,3);a[s]=s;t=tolower(\"0\");u=toupper(\"0\");print typeof(s),s,s+0,(s==8),(s?1:0),(\"008\" in a),(8 in a),typeof(t),(t?1:0),typeof(u),(u?1:0)}"],
            [], "string 008 8 0 1 1 0 string 1 string 1\n");
        foreach (var (option, number) in new[] { ("", 0), ("--non-decimal-data", 16), ("--posix", 16), ("--traditional", 0) })
        {
            const string program = "BEGIN{s=substr(\"_0x10_\",2,4);t=tolower(\"0X10\");u=toupper(\"0x10\");print s,s+0,t,t+0,u,u+0,length(s)}";
            yield return Passing("string-results-numeric-mode-" + (option.Length == 0 ? "default" : option[2..]),
                option.Length == 0 ? [program] : [option, program], [],
                $"0x10 {number} 0x10 {number} 0X10 {number} 4\n");
        }
        yield return Passing("string-results-snapshots-through-functions-and-arrays",
            ["function save(x,a){a[x]=x;return x} BEGIN{s=substr(\"_007_\",2);t=save(toupper(s),a);s=0;print t,a[t],t+0,(t==7),length(t)}"],
            [], "007_ 007_ 7 0 4\n");
        foreach (var (option, number) in new[] { ("", 0), ("--non-decimal-data", 16), ("--posix", 16), ("--traditional", 0) })
        {
            const string program = "function clip(s,t,u){t=tolower(s);u=substr(t,1,4);return u}BEGIN{v=clip(\"0X10\");print v,v+0,clip(\"0X10\")+0;v=tolower(\"0X10\");print v,v+0}";
            yield return Passing("computed-text-publication-keeps-local-and-global-number-mode-" + (option.Length == 0 ? "default" : option[2..]),
                option.Length == 0 ? [program] : [option, program], [],
                $"0x10 {number} {number}\n0x10 {number}\n");
        }
    }
}
