namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ExponentiationAliasCases()
    {
        yield return Passing(
            "power-alias-gnu-original",
            ["BEGIN{print 2**3;x=2;x**=3;print x}"],
            [],
            "8\n8\n");

        yield return Passing(
            "power-alias-gnu-right-associative-mixed",
            ["BEGIN{print 2**3**2,2^3**2,2**3^2,(2**3)**2}"],
            [],
            "512 512 512 64\n");

        yield return Passing(
            "power-alias-gnu-unary-precedence",
            ["BEGIN{print -2**2,(-2)**2,2**-3,-2^2,2^-3,2**+3;print -2**2**3,(-2)**2**3}"],
            [],
            "-4 4 0.125 -4 0.125 8\n-256 256\n");

        yield return Passing(
            "power-alias-gnu-arithmetic-precedence",
            ["BEGIN{print 2*3**2,2**3*2,2**3/4,2**3%3,1+2**3,2**3-1;print 2 ** 3}"],
            [],
            "18 16 2 2 9 7\n8\n");

        yield return Passing(
            "power-alias-gnu-chained-compound",
            ["BEGIN{x=2;y=3;print (x**=y**=2),x,y;x=2;print (x**=3)+1,x}"],
            [],
            "512 512 9\n9 8\n");

        yield return Passing(
            "power-alias-gnu-one-time-targets",
            ["function p(){calls++;return 1}BEGIN{a[1]=2;print(a[p()]**=3),calls,a[1];$0=\"2 3\";print($(p())**=2),calls,$0;print(NF**=2),NF,$0}"],
            [],
            "8 1 8\n4 2 4 3\n4 4 4 3  \n");

        yield return Passing(
            "power-alias-gnu-function-effects",
            ["function base(){print \"base\";return 2}function exponent(){print \"exponent\";return 3}BEGIN{print base()**exponent();x=2;print(x**=exponent()),x}"],
            [],
            "base\nexponent\n8\nexponent\n8 8\n");

        yield return Passing(
            "power-alias-gnu-conditional-operands",
            ["BEGIN{x=2;print(0||x**=3),x;print(1||x**=99),x;print(0&&x**=99),x;print(1?x**=2:x**=99),x}"],
            [],
            "1 8\n1 8\n0 8\n64 64\n");

        yield return Passing(
            "power-alias-gnu-field-binding",
            ["BEGIN{$0=\"2 3\";i=1;print $i**$2,$(i**2),$1**-2;print ($1**=3),$0}"],
            [],
            "8 2 0.25\n8 8 3\n");

        yield return Passing(
            "power-alias-gnu-line-continuations",
            ["BEGIN{print 2**\\\n3;x=2;x**=\\\n3;print x}"],
            [],
            "8\n8\n");

        yield return Passing(
            "power-alias-traditional-original",
            ["--traditional", "BEGIN{print 2**3;x=2;x**=3;print x}"],
            [],
            "8\n8\n");

        yield return Passing(
            "power-alias-traditional-right-associative-mixed",
            ["--traditional", "BEGIN{print 2**3**2,2^3**2,2**3^2,(2**3)**2}"],
            [],
            "512 512 512 64\n");

        yield return Passing(
            "power-alias-traditional-unary-precedence",
            ["--traditional", "BEGIN{print -2**2,(-2)**2,2**-3,-2^2,2^-3,2**+3;print -2**2**3,(-2)**2**3}"],
            [],
            "-4 4 0.125 -4 0.125 8\n-256 256\n");

        yield return Passing(
            "power-alias-traditional-arithmetic-precedence",
            ["--traditional", "BEGIN{print 2*3**2,2**3*2,2**3/4,2**3%3,1+2**3,2**3-1;print 2 ** 3}"],
            [],
            "18 16 2 2 9 7\n8\n");

        yield return Passing(
            "power-alias-traditional-chained-compound",
            ["--traditional", "BEGIN{x=2;y=3;print (x**=y**=2),x,y;x=2;print (x**=3)+1,x}"],
            [],
            "512 512 9\n9 8\n");

        yield return Passing(
            "power-alias-traditional-one-time-targets",
            ["--traditional", "function p(){calls++;return 1}BEGIN{a[1]=2;print(a[p()]**=3),calls,a[1];$0=\"2 3\";print($(p())**=2),calls,$0;print(NF**=2),NF,$0}"],
            [],
            "8 1 8\n4 2 4 3\n4 4 4 3  \n");

        yield return Passing(
            "power-alias-traditional-function-effects",
            ["--traditional", "function base(){print \"base\";return 2}function exponent(){print \"exponent\";return 3}BEGIN{print base()**exponent();x=2;print(x**=exponent()),x}"],
            [],
            "base\nexponent\n8\nexponent\n8 8\n");

        yield return Passing(
            "power-alias-traditional-conditional-operands",
            ["--traditional", "BEGIN{x=2;print(0||x**=3),x;print(1||x**=99),x;print(0&&x**=99),x;print(1?x**=2:x**=99),x}"],
            [],
            "1 8\n1 8\n0 8\n64 64\n");

        yield return Passing(
            "power-alias-traditional-field-binding",
            ["--traditional", "BEGIN{$0=\"2 3\";i=1;print $i**$2,$(i**2),$1**-2;print ($1**=3),$0}"],
            [],
            "8 2 0.25\n8 8 3\n");

        yield return Passing(
            "power-alias-traditional-line-continuations",
            ["--traditional", "BEGIN{print 2**\\\n3;x=2;x**=\\\n3;print x}"],
            [],
            "8\n8\n");

        yield return Passing(
            "power-alias-input-pattern",
            ["($1**2>4){print NR,$1**3}END{print NR}"],
            [],
            "2 27\n3 64\n3\n", "2\n3\n4\n");

        yield return Passing(
            "power-caret-gnu-control",
            ["BEGIN{x=2;print 2^3^2,-2^2,2^-3,(x^=3),x}"],
            [],
            "512 -4 0.125 8 8\n");

        yield return Passing(
            "power-text-gnu-does-not-trigger-gating",
            ["BEGIN{print \"**\",\"**=\";print (\"***\"~/[*][*][*]/)} # ** **="],
            [],
            "** **=\n1\n");

        yield return Passing(
            "power-caret-traditional-control",
            ["--traditional", "BEGIN{x=2;print 2^3^2,-2^2,2^-3,(x^=3),x}"],
            [],
            "512 -4 0.125 8 8\n");

        yield return Passing(
            "power-text-traditional-does-not-trigger-gating",
            ["--traditional", "BEGIN{print \"**\",\"**=\";print (\"***\"~/[*][*][*]/)} # ** **="],
            [],
            "** **=\n1\n");

        yield return Passing(
            "power-caret-posix-control",
            ["--posix", "BEGIN{x=2;print 2^3^2,-2^2,2^-3,(x^=3),x}"],
            [],
            "512 -4 0.125 8 8\n");

        yield return Passing(
            "power-text-posix-does-not-trigger-gating",
            ["--posix", "BEGIN{print \"**\",\"**=\";print (\"***\"~/[*][*][*]/)} # ** **="],
            [],
            "** **=\n1\n");

        yield return CompilerDiagnostic(Failing(
            "power-alias-posix-reject-expression",
            ["--posix", "BEGIN{print 2**3}"],
            [],
            2,
            "",
            "awk: operator `**' is not allowed in POSIX mode.\n"));

        yield return CompilerDiagnostic(Failing(
            "power-alias-posix-reject-assignment",
            ["--posix", "BEGIN{x=2;x**=3;print x}"],
            [],
            2,
            "",
            "awk: operator `**=' is not allowed in POSIX mode.\n"));

        yield return CompilerDiagnostic(Failing(
            "power-alias-posix-reject-uncalled-function",
            ["--posix", "function f(){return 2**3}BEGIN{print \"unreachable\"}"],
            [],
            2,
            "",
            "awk: operator `**' is not allowed in POSIX mode.\n"));

        yield return CompilerDiagnostic(Failing(
            "power-alias-posix-reject-rule-pattern",
            ["--posix", "2**3{print \"unreachable\"}"],
            [],
            2,
            "",
            "awk: operator `**' is not allowed in POSIX mode.\n"));

        yield return CompilerDiagnostic(Failing(
            "power-alias-posix-reject-range-pattern",
            ["--posix", "1,2**3{print \"unreachable\"}"],
            [],
            2,
            "",
            "awk: operator `**' is not allowed in POSIX mode.\n"));

        yield return CompilerDiagnostic(Failing(
            "power-alias-posix-reject-unreachable-statement",
            ["--posix", "BEGIN{if(0)x**=2;print \"unreachable\"}"],
            [],
            2,
            "",
            "awk: operator `**=' is not allowed in POSIX mode.\n"));

    }
}
