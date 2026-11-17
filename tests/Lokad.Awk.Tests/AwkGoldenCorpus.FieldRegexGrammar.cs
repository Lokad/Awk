namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FieldRegexGrammarCases()
    {
        yield return Passing("field-regex-grammar-field-leading-equals",
            ["BEGIN{$0=\"=b other\";print $/=b/}"], [], "=b\n");
        yield return Passing("field-regex-grammar-field-regex-control",
            ["BEGIN{$0=\"=b other\";print $(/=b/)}"], [], "=b\n");
        yield return Passing("field-regex-grammar-arithmetic-leading-equals",
            ["BEGIN{$0=\"=b\";print 1+/=b/}"], [], "2\n");
        yield return Passing("field-regex-grammar-assignment-leading-equals",
            ["BEGIN{$0=\"=b\";x=/=b/;print x}"], [], "1\n");
        yield return Passing("field-regex-grammar-division-assignment",
            ["BEGIN{x=8;x/=2;a[1]=8;a[1]/=2;print x,a[1]}"], [], "4 4\n");
        yield return Passing("field-regex-grammar-original-admitted-record",
            ["BEGIN{$0=\"abc\";print $/= b/ c /= d/}"], [], "abc0\n");
        yield return Passing("field-regex-grammar-original-matching-record",
            ["BEGIN{$0=\"= b = d\";print $/= b/ c /= d/}"], [], "=1\n");
        yield return Passing("field-regex-grammar-concatenated-variables",
            ["BEGIN{$0=\"=b\";x=\"X\";y=\"Y\";print x y /=b/}"], [], "XY1\n");
        yield return Passing("field-regex-grammar-concatenated-arithmetic",
            ["BEGIN{$0=\"=b\";x=2;y=\"Y\";print x+1 y /=b/}"], [], "3Y1\n");
        yield return Passing("field-regex-grammar-concatenated-call",
            ["function f(x){return \"F\" x} BEGIN{$0=\"=b\";x=\"X\";print x f(\"Y\") /=b/}"], [], "XFY1\n");
        yield return Passing("field-regex-grammar-concatenated-array",
            ["BEGIN{$0=\"=b\";a[1][2]=\"A\";x=\"X\";print a[1][2] x /=b/}"], [], "AX1\n");
        yield return Passing("field-regex-grammar-concatenated-group",
            ["BEGIN{$0=\"=b\";x=\"X\";y=\"Y\";z=\"Z\";print (x y) z /=b/}"], [], "XYZ1\n");
        yield return Passing("field-regex-grammar-concatenated-spaced-group",
            ["BEGIN{$0=\"=b\";x=\"X\";print x (\"Y\") /=b/}"], [], "XY1\n");
        yield return Passing("field-regex-grammar-concatenated-negation",
            ["BEGIN{$0=\"=b\";x=\"X\";y=0;print x !y /=b/}"], [], "X11\n");
        yield return Passing("field-regex-grammar-multiple-concatenated-patterns",
            ["BEGIN{$0=\"=b\";x=\"X\";print x x /=b/ x /=b/}"], [], "XX1X1\n");
        yield return Passing("field-regex-grammar-pattern-punctuation",
            ["BEGIN{$0=\"=};#\\\"\";x=\"X\";print x x /=[};#\"]/}"], [], "XX1\n");
        yield return Passing("field-regex-grammar-array-target",
            ["BEGIN{x=8;y=2;a[x y]=8;a[x y]/=2;print a[x y]}"], [], "4\n");
        yield return Passing("field-regex-grammar-field-call-target",
            ["function f(x){return 2} BEGIN{$0=\"b 8\";x=\"X\";y=\"Y\";$f(x y)/=2;print $0}"], [], "b 4\n");
        yield return Passing("field-regex-grammar-if-target",
            ["BEGIN{x=\"X\";y=\"Y\";z=8;if(x y) z/=2;print z}"], [], "4\n");
        yield return Passing("field-regex-grammar-while-target",
            ["BEGIN{z=8;while(i++<2) z/=2;print z,i}"], [], "2 3\n");
        yield return Passing("field-regex-grammar-for-target",
            ["BEGIN{z=8;for(i=0;i<2;i++) z/=2;print z,i}"], [], "2 2\n");
        yield return Passing("field-regex-grammar-logical-target",
            ["BEGIN{x=1;y=8;x && y/=2;print y}"], [], "4\n");
        yield return Passing("field-regex-grammar-comparison-target",
            ["BEGIN{x=1;y=8;r=x<y/=2;print y,r}"], [], "4 1\n");
        yield return Passing("field-regex-grammar-concatenated-division",
            ["BEGIN{x=\"X\";y=8;print x y/2/2}"], [], "X2\n");
        yield return Passing("field-regex-grammar-parenthesized-pattern",
            ["BEGIN{$0=\"=b\";x=\"X\";print x (/=b/)}"], [], "X1\n");
    }
}
