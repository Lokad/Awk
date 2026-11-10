namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> StringResultCases()
    {
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
    }
}
