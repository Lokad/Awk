namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> NonLvalueRegexGrammarCases()
    {
        yield return Passing("field-regex-nonlvalue-number",
            ["function f(a){return 2} BEGIN{$0=\"b 8\";x=8;y=2;z=4;a[8]=8;print 2 /=b/}"], [], "20\n");
        yield return Passing("field-regex-nonlvalue-additive-left",
            ["function f(a){return 2} BEGIN{$0=\"b 8\";x=8;y=2;z=4;a[8]=8;print x+1 /=b/}"], [], "90\n");
        yield return Passing("field-regex-nonlvalue-additive-right",
            ["function f(a){return 2} BEGIN{$0=\"b 8\";x=8;y=2;z=4;a[8]=8;print 1+x /=b/}"], [], "90\n");
        yield return Passing("field-regex-nonlvalue-additive-concatenation",
            ["function f(a){return 2} BEGIN{$0=\"b 8\";x=8;y=2;z=4;a[8]=8;print x+1 /=b/ z}"], [], "904\n");
        yield return Passing("field-regex-nonlvalue-multiplicative",
            ["function f(a){return 2} BEGIN{$0=\"b 8\";x=8;y=2;z=4;a[8]=8;print x*y /=b/}"], [], "160\n");
        yield return Passing("field-regex-nonlvalue-power",
            ["function f(a){return 2} BEGIN{$0=\"b 8\";x=8;y=2;z=4;a[8]=8;print x^y /=b/}"], [], "640\n");
        yield return Passing("field-regex-nonlvalue-call",
            ["function f(a){return 2} BEGIN{$0=\"b 8\";x=8;y=2;z=4;a[8]=8;print f() /=b/}"], [], "20\n");
        yield return Passing("field-regex-nonlvalue-call-argument",
            ["function f(a){return 2} BEGIN{$0=\"b 8\";x=8;y=2;z=4;a[8]=8;print f(x y) /=b/}"], [], "20\n");
        yield return Passing("field-regex-nonlvalue-string",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print \"P\" /=b/}"], [], "P1\n");
        yield return Passing("field-regex-nonlvalue-group",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print (x) /=b/}"], [], "21\n");
        yield return Passing("field-regex-nonlvalue-not",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print !x /=b/}"], [], "01\n");
        yield return Passing("field-regex-nonlvalue-minus",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print -x /=b/}"], [], "-21\n");
        yield return Passing("field-regex-nonlvalue-plus",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print +x /=b/}"], [], "21\n");
        yield return Passing("field-regex-nonlvalue-prefix-mutation",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print ++x /=b/}"], [], "31\n");
        yield return Passing("field-regex-nonlvalue-postfix-mutation",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print x++ /=b/}"], [], "21\n");
        yield return Passing("field-regex-nonlvalue-field-addition",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print $x+1 /=b/}"], [], "91\n");
        yield return Passing("field-regex-nonlvalue-field-multiplication",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print $x*2 /=b/}"], [], "161\n");
        yield return Passing("field-regex-nonlvalue-field-power",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print $x^2 /=b/}"], [], "641\n");
        yield return Passing("field-regex-nonlvalue-field-mutation-power",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print $++x^2 /=b/}"], [], "01\n");
        yield return Passing("field-regex-nonlvalue-bare-length",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print length /=b/}"], [], "41\n");
        yield return Passing("field-regex-nonlvalue-fixed-field-addition",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print $1+1 /=b/}"], [], "11\n");
        yield return Passing("field-regex-nonlvalue-fixed-field-multiplication",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print $1*2 /=b/}"], [], "01\n");
        yield return Passing("field-regex-nonlvalue-fixed-field-power",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print $1^2 /=b/}"], [], "01\n");
        yield return Passing("field-regex-nonlvalue-field-concatenation-control",
            ["function f(a){return 2} BEGIN{$0=\"=b 8\";x=2;y=2;print $1 c /=b/}"], [], "=b1\n");
        yield return Passing("field-regex-nonlvalue-signed-power-target-control",
            ["BEGIN{x=2;$0=\"2 4 8 16\";$+x^2/=2;print $0}"], [], "2 4 8 8\n");
        yield return Passing("field-regex-nonlvalue-nested-signed-power-target-control",
            ["BEGIN{x=2;$0=\"8 4 9 1\";$$+x^2/=2;print $0}"], [], "4 4 9 1\n");
        yield return Passing("field-regex-nonlvalue-not-power-target-control",
            ["BEGIN{x=0;$0=\"8 4\";$!x^2/=2;print $0}"], [], "4 4\n");
        yield return Passing("field-regex-nonlvalue-signed-mutation-power-target-control",
            ["BEGIN{x=-1;$0=\"8\";$-++x^2/=2;print $0,x}"], [], "4 0\n");
    }
}
