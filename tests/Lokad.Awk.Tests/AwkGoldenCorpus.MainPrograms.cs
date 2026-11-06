namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> MainProgramCases()
    {
        yield return Passing("omitted-locals-preserve-unassigned-and-array-use-after-frame-reuse",
            ["function wide(a,b,c,d,e,f,g,h){b=7;c[\"k\"]=9;h=\"0003\";return a+b+c[\"k\"]+h}function narrow(x){return x}function probe(a,b,c){print \"[\"a\"]\",(a==0),(c==\"\");b[\"k\"]=4;return b[\"k\"]}BEGIN{print wide(1);print narrow(\"0002\");print probe();print wide(2);print probe()}"],
            [], "20\n0002\n[] 1 1\n4\n21\n[] 1 1\n4\n");
        yield return Passing("array-assignment-evaluates-rhs-before-field-destination",
            ["function rhs(){$1=\"new\";return \"0007\"}{a[\"new\"]=\"seed\";a[$1]=rhs();print (\"old\" in a),a[\"new\"],typeof(a[\"new\"]),length(a)}"],
            [], "0 0007 string 1\n", "old value\n");
        yield return Passing("array-assignment-alias-preserves-field-and-composite-spelling",
            ["function save(a,x){a[$1]=x;a[$1,$2]=x}{save(a,$3)}END{print a[\"café\"],typeof(a[\"café\"]),a[\"café\",\"part\"],length(a)}"],
            [], "0009 strnum 0009 2\n", "café part 0007\ncafé part 0009\n");
        yield return Passing("record-terminator-slots-retain-string-spelling-after-user-assignment",
            ["BEGIN{RS=\"7\";getline x;print x,\"[\"RT\"]\",typeof(RT),(RT==7);RT=100;RS=\":\";getline x;print x,\"[\"RT\"]\",typeof(RT)}"],
            [], "left [7] string 1\nright [:] string\n", "left7right:tail");
        yield return Passing("nested-captured-updates-and-postfix-results",
            ["BEGIN{a[\"x\"]=1;a[\"y\"]=2;a[\"z\"]=3;v=(a[\"x\"]+=(a[\"y\"]+=(a[\"z\"]++)));print v,a[\"x\"],a[\"y\"],a[\"z\"];w=a[\"x\"]+++a[\"y\"]+++a[\"z\"]++;print w,a[\"x\"],a[\"y\"],a[\"z\"]}"],
            [], "6 6 5 4\n15 7 6 5\n");
        var rules = string.Concat(Enumerable.Range(1, 37).Select(index =>
            "{a[\"k" + index + "\"]+=$2+" + index + ";s+=" + index + "}"));
        yield return Passing("many-rules-preserve-constants-and-shared-updates",
            [rules + "END{print a[\"k1\"],a[\"k37\"],s,typeof(a[\"k1\"])}"],
            [], "12 84 1406 number\n", "k 0007\nk 3\n");
        yield return Passing("nonlocal-next-skips-later-rules",
            ["function bump(k){a[k]++;if(NR%2)next}{a[$1]+=$2}{bump($1);a[$1]+=100}{tail++}END{print a[\"a\"],a[\"b\"],tail}"],
            [], "18 109 1\n", "a 0007\nb 8\na 9\n");
        yield return Passing("local-numeric-writes-retain-prior-spelling-and-type-transitions",
            ["function step(x,spelling){spelling=x;x+=1;x+=2;x=\"0005\";x+=1;x+=2;return spelling \":\" x}{print step($2)}"],
            [], "0007:8\n", "k 0007\n");
    }
}
