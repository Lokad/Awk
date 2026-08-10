using System.Collections.Generic;

namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> UserFunctionCases()
    {
        yield return Posix(Passing(
            "user-function-return-expression",
            ["function inc(x){return x+1} BEGIN{print inc(2)}"],
            [],
            "3\n"));

        yield return Passing(
            "user-function-parameter-shadows-global",
            ["BEGIN{x=10; print plus(x), x} function plus(x){return x+1}"],
            [],
            "11 10\n");

        yield return Passing(
            "user-function-nested-scalar-parameter-retains-type",
            ["function inner(x){print typeof(x), x} function outer(x){inner(x)} BEGIN{outer(7)}"],
            [],
            "number 7\n");

        yield return Passing(
            "user-function-missing-parameter-becomes-unassigned-after-read",
            ["function inspect(x, copy){print typeof(x); copy=x; print typeof(x)} BEGIN{inspect()}"],
            [],
            "untyped\nunassigned\n");

        yield return Passing(
            "user-function-extra-parameter-is-uninitialized-local",
            ["function probe(x, local){return (local==0) \",\" (local==\"\") \",\" (local+2)} BEGIN{print probe(1); print probe(1, \"\")}"],
            [],
            "1,1,2\n0,1,2\n");

        yield return Passing(
            "user-function-recursive-return-expression",
            ["function fact(n){return n <= 1 ? 1 : n * fact(n - 1)} BEGIN{print fact(5)}"],
            [],
            "120\n");

        yield return Posix(Passing(
            "user-function-statement-body-return",
            ["function choose(a,b, m){if(a>b){m=a}else{m=b}; return m} BEGIN{print choose(3,5), choose(9,4)}"],
            [],
            "5 9\n"));

        yield return Passing(
            "user-function-loop-body-return",
            ["function sum(n, i, total){total=0; for(i=1;i<=n;i++){total+=i}; return total} BEGIN{print sum(4)}"],
            [],
            "10\n");

        yield return Passing(
            "user-function-print-side-effect",
            ["function emit(x){print \"emit\", x; return x+1} BEGIN{print \"value\", emit(4)}"],
            [],
            "emit 4\nvalue 5\n");

        yield return Failing(
            "user-function-getline-fails-after-prior-output",
            ["function fail(){print \"inside\"; getline x < \"/inet/tcp/0/localhost/80\"; return 1} BEGIN{print \"before\"; print fail(); print \"after\"}"],
            [],
            2,
            "before\ninside\n",
            "awk: /inet special files are unsupported in Lokad awk\n");

        yield return Posix(Passing(
            "user-function-array-parameter-by-reference",
            ["function fill(a){a[2]=9} BEGIN{x[1]=7; fill(x); print x[1], x[2]}"],
            [],
            "7 9\n"));

        yield return Passing(
            "user-function-untyped-parameter-promotes-array",
            ["function fill(a){a[1]=7} BEGIN{fill(x); print isarray(x), x[1]}"],
            [],
            "1 7\n");

        yield return Passing(
            "user-function-typeof-element-promotes-array-parameter",
            ["function inspect(a){print typeof(a[1]), isarray(a)} BEGIN{inspect(x); print isarray(x)}"],
            [],
            "untyped 1\n1\n");

        yield return Passing(
            "user-function-omitted-parameter-promotes-local-array",
            ["function fill(a){a[1]=7; print a[1], typeof(a)} BEGIN{fill(); print (\"a\" in SYMTAB)}"],
            [],
            "7 array\n0\n");

        yield return Passing(
            "user-function-same-name-untyped-argument-promotes-array",
            ["function fill(x){x[1]=7} BEGIN{fill(x); print x[1], typeof(x)}"],
            [],
            "7 array\n");

        yield return Passing(
            "user-function-forwards-untyped-array-argument",
            ["function inner(b){b[1]=9} function outer(a){inner(a); print a[1]} BEGIN{outer(x); print x[1]}"],
            [],
            "9\n9\n");

        yield return Passing(
            "user-function-forwards-same-name-untyped-array-argument",
            ["function inner(a){a[1]=9} function outer(a){inner(a); print a[1]} BEGIN{outer(x); print x[1]}"],
            [],
            "9\n9\n");

        yield return Passing(
            "user-function-forwards-existing-array-argument",
            ["function inner(b){b[2]=9} function outer(a){inner(a)} BEGIN{x[1]=7; outer(x); print x[1], x[2]}"],
            [],
            "7 9\n");

        yield return Passing(
            "user-function-untyped-scalar-argument-stays-local",
            ["function set(a){a=1} BEGIN{set(x); print typeof(x)}"],
            [],
            "untyped\n");

        yield return Passing(
            "user-function-mutual-recursion-statement-body",
            ["function even(n){if(n==0) return 1; return odd(n-1)} function odd(n){if(n==0) return 0; return even(n-1)} BEGIN{print even(6), odd(6)}"],
            [],
            "1 0\n");

        yield return Failing(
            "user-function-recursion-limit-fails",
            ["function loop(n){return loop(n+1)} BEGIN{print loop(0)}"],
            [],
            2,
            string.Empty,
            "awk: function recursion limit exceeded\n");

        yield return Failing(
            "user-function-scalar-parameter-array-use-fails",
            ["function fill(a){a[1]=7} BEGIN{x=1; fill(x)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n");

        yield return Passing(
            "user-function-bare-return",
            ["function empty(){return} BEGIN{print \"[\" empty() \"]\"}"],
            [],
            "[]\n");

        yield return Passing(
            "user-function-falls-off-end",
            ["function empty(){} BEGIN{print \"[\" empty() \"]\"}"],
            [],
            "[]\n");

        yield return Passing(
            "functab-builtins-and-user-functions",
            ["function inc(x){return x+1} BEGIN{print (\"length\" in FUNCTAB), FUNCTAB[\"length\"]; print (\"inc\" in FUNCTAB), FUNCTAB[\"inc\"]}"],
            [],
            "1 length\n1 inc\n");

        yield return CompilerDiagnostic(Failing(
            "functab-assignment-readonly",
            ["BEGIN{FUNCTAB[\"x\"]=\"y\"}"],
            [],
            2,
            string.Empty,
            "awk: cannot modify special array `FUNCTAB'\n"));

        yield return Passing(
            "symtab-scalar-bindings",
            ["BEGIN{SYMTAB[\"x\"]=\"y\"; print x; x=\"z\"; print SYMTAB[\"x\"]; print (\"FS\" in SYMTAB), (SYMTAB[\"FS\"]==\" \"); print typeof(SYMTAB[\"missing\"])}"],
            [],
            "y\nz\n1 1\nunassigned\n");

        yield return Passing(
            "symtab-special-scalar-assignment-updates-runtime-hook",
            ["BEGIN{SYMTAB[\"FS\"]=\":\"} {print $2}", "/file.txt"],
            [File("/file.txt", "a:b\n")],
            "b\n");

        yield return Failing(
            "symtab-invalid-reference-fails",
            ["BEGIN{SYMTAB[\"1bad\"]=\"y\"}"],
            [],
            2,
            string.Empty,
            "awk: invalid SYMTAB reference\n");

        yield return Failing(
            "symtab-scalar-context-fails",
            ["BEGIN{print SYMTAB}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `SYMTAB' in a scalar context\n");

        yield return Failing(
            "symtab-array-value-scalar-context-fails",
            ["BEGIN{a[1]=1; print SYMTAB[\"a\"]}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `a' in a scalar context\n");

        yield return CompilerDiagnostic(Failing(
            "symtab-delete-rejected",
            ["BEGIN{delete SYMTAB[\"x\"]}"],
            [],
            2,
            string.Empty,
            "awk: `delete' is not allowed with SYMTAB\n"));

        yield return CompilerDiagnostic(Failing(
            "user-function-call-whitespace-invalid",
            ["function inc(x){return x+1} BEGIN{print inc (2)}"],
            [],
            2,
            string.Empty,
            "awk: invalid function call.\n"));
    }
}
