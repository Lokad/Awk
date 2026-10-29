namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> RecordValueCases() =>
    [
        Passing("record-value-helper-composite-prefix-before-later-call", ["function id(x){return x} function change(){$0=\"later tail\";return $1} BEGIN{$0=\"0007 kept\";a[id($1),change()]=1;print ((\"0007\" SUBSEP \"later\") in a),$0}"], [],
            "1 later tail\n"),
        Passing("record-value-returned-argument-key-after-record-replacement", ["function keep(x,y){return x} function change(){$0=\"later tail\";return \"key\"} BEGIN{$0=\"0007 kept\";a[keep($1,change()),\"x\"]=1;print ((\"0007\" SUBSEP \"x\") in a),$0}"], [],
            "1 later tail\n"),
        Passing("record-value-helper-arithmetic-local", ["function add(x,t){t=x+1;return t} {print add($2),\"[\"$2\"]\",typeof($2)}"], [],
            "8 [0007] strnum\n10 [9] strnum\n", "a 0007\nb 9\n"),
        Passing("record-value-return-keeps-spelling", ["function id(x){return x} {v=id($2);$0=\"q r\";print \"[\"v\"]\",typeof(v),(v==7),(v==\"7\")}"], [],
            "[0007] strnum 1 0\n", "a 0007\n"),
        Passing("record-value-earlier-argument-keeps-record", ["function f(x,y,t){t=x;$0=\"changed\";return t} BEGIN{$0=\"a 0007\";v=f($2,($0=\"b 9\"));print \"[\"v\"]\",typeof(v),$0}"], [],
            "[0007] strnum changed\n"),
        Passing("record-value-local-survives-getline", ["function old(x,t){t=x;getline;return t} {v=old($2);print \"[\"v\"]\",\"[\"$2\"]\";exit}"], [],
            "[0007] [99]\n", "a 0007\nb 99\n"),
        Passing("record-value-composite-prefix-before-mutation", ["BEGIN{$0=\"a b\";a[$1,($0=\"c d\")]=1;print a[\"a\",\"c d\"],$0}"], [],
            "1 c d\n"),
        Passing("record-value-cached-origin-after-rebuild", ["{x=typeof($2);$1=\"c\";print $0,typeof($2),(\"7\"==$2),($2==7)}"], [],
            "c 0007 strnum 0 1\n", "a 0007\n"),
        Passing("record-value-assigned-text-keeps-origin", ["BEGIN{$0=\"a 0007\";v=$2;$2=\"0007\";print(v==7),($2==7),typeof(v),typeof($2)}"], [],
            "1 0 strnum string\n")
    ];
}
