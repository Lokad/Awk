namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> IncrementConcatenationCases()
    {
        yield return CompilerDiagnostic(Failing("postfix-does-not-make-prefix-result-assignable",
            ["BEGIN{print \"before\";print ++a++;print \"after\"}"], [], 2, "", "awk: invalid increment target.\n"));
        yield return CompilerDiagnostic(Failing("single-field-cannot-consume-two-postfix-mutations",
            ["BEGIN{print \"before\";print $a++++;print \"after\"}"], [], 2, "", "awk: invalid `print' statement.\n"));
        // Concatenation captures the earlier spelling before later field writes.
        // GNU 5.3.2 instead prints 22 for this order-sensitive expression.
        yield return Passing("field-postfix-concatenation-captures-spelling-before-later-record-mutation",
            ["BEGIN{$0=\"AAAAAAAABBBBBBBBCCCCCCCC\";x=$$$B++ \"\"*\"\"+$$B++N;print x}"], [], "12\n");
        yield return Passing("concatenate-string-before-prefix-increment",
            ["BEGIN{n=1;print \"n=\" ++n}"], [], "n=2\n");
        yield return Passing("concatenate-number-before-prefix-increment",
            ["BEGIN{n=1;v=1 ++n;print v,n}"], [], "12 2\n");
        yield return Passing("concatenate-regex-before-prefix-increment",
            ["BEGIN{n=1;v=/x/ ++n;print v,n}"], [], "02 2\n");
        yield return Passing("concatenate-group-before-prefix-increment",
            ["BEGIN{n=1;v=(n+1)++n;print v,n}"], [], "22 2\n");
        yield return Passing("concatenate-call-before-prefix-increment",
            ["function f(){return 7}BEGIN{n=1;v=f()++n;print v,n}"], [], "72 2\n");
        yield return Passing("concatenate-before-prefix-decrement",
            ["BEGIN{n=3;print \"n=\" --n,(9)--n,n}"], [], "n=2 91 1\n");
        yield return Passing("concatenate-after-variable-postfix-increment",
            ["BEGIN{a=1;b=\"x\";v=a++ b;print v,a}"], [], "1x 2\n");
        yield return Passing("concatenate-after-grouped-field-postfix-increment",
            ["BEGIN{$0=\"7 8\";i=1;n=3;v=$(i)++n;print v,$1,n}"], [], "73 8 3\n");
        yield return Passing("concatenate-after-called-field-postfix-increment",
            ["function idx(){return 1}BEGIN{$0=\"7 8\";n=3;v=$idx()++n;print v,$1,n}"], [], "73 8 3\n");
        yield return Passing("concatenate-after-literal-field-postfix-increment",
            ["BEGIN{$0=\"7 8\";n=3;v=$1++n;print v,$1,n}"], [], "73 8 3\n");
        yield return Passing("concatenate-after-string-field-postfix-increment",
            ["BEGIN{$0=\"7 8\";n=3;v=$\"1\"++n;print v,$1,n}"], [], "73 8 3\n");
    }
}
