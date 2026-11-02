namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> BoundConditionCases()
    {
        yield return Passing("bound-condition-preserves-mixed-short-circuit-call-order",
            ["function hit(v){calls++;return v}{calls=0;if((hit(0)||hit(1))&&(!hit(0)||hit(9)))print calls;calls=0;if(!((hit(0)&&hit(9))||hit(1)))print \"bad\";print calls}"],
            [], "3\n2\n", "row\n");
        yield return Passing("bound-condition-loops-preserve-tests-and-short-circuit-effects",
            ["function hit(v){calls++;return v}{calls=0;i=0;while(i<3&&(hit(i==1)||hit(i<2)))i++;print i,calls;for(j=0;!(j>=2||hit(0));j++){}print j,calls}"],
            [], "2 5\n2 7\n", "row\n");
        yield return Passing("bound-condition-numeric-truth-preserves-nan-and-signed-zero",
            ["{n=\"+nan\"+0;z=-0;if(n||1/0)print \"nan\";if(z&&1/0)print \"bad\";if(!(z||n))print \"bad\";print(0?n:7)}"],
            [], "nan\n7\n", "row\n");
        yield return Passing("bound-condition-text-comparison-keeps-spelling-and-native-unicode",
            ["BEGIN{FS=\":\"}{a=$1==\"007\";b=$2==\"nan\";c=$3==\"inf\";d=$4==\" \\t+1e2\";e=$5==\"😀\";print a,b,c,d,e;print typeof($1),$1}"],
            [], "1 1 1 1 1\nstrnum 007\n", "007:nan:inf: \t+1e2:😀\n");
    }
}
