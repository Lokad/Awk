using System.Collections.Generic;

namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ArrayCases()
    {
        yield return Posix(Passing(
            "split-defaults-to-fs",
            ["BEGIN{FS=\":\"} {split($0,a); print a[1], a[2]}", "/file.txt"],
            [File("/file.txt", "a:b\n")],
            "a b\n"));

        yield return Passing(
            "patsplit-statement",
            ["{patsplit($0,a,\"[0-9]+\"); print a[1], a[2]}", "/file.txt"],
            [File("/file.txt", "a12b345c\n")],
            "12 345\n");

        yield return Passing(
            "patsplit-defaults-to-fpat",
            ["BEGIN{FPAT=\"[A-Z]+|[0-9]+\"; n=patsplit(\"A-12-B\", a); print n, a[1], a[2], a[3]}"],
            [],
            "3 A 12 B\n");

        yield return Passing(
            "patsplit-default-fpat-whitespace",
            ["BEGIN{n=patsplit(\"a b\", a); print n, a[1], a[2]}"],
            [],
            "2 a b\n");

        yield return Posix(Passing(
            "array-membership-expression",
            ["BEGIN{a[\"x\"]=7; key=\"x\"; print (key in a), (\"y\" in a), ((\"x\" in a) ? \"yes\" : \"no\")}"],
            [],
            "1 0 yes\n"));

        yield return Passing(
            "expression-lists-in-subscripts-and-function-arguments",
            ["function sum(a,b,c){return a+b+c} BEGIN{a[1,2,3]=9; print a[1 SUBSEP 2 SUBSEP 3], sum(1,2,3)}"],
            [],
            "9 6\n");

        yield return Passing(
            "array-value-read-autovivifies",
            ["BEGIN{print (\"x\" in a); value=a[\"x\"]; print (\"x\" in a), length(a), \"[\" a[\"x\"] \"]\"}"],
            [],
            "0\n1 1 []\n");

        yield return Passing(
            "array-membership-does-not-autovivify",
            ["BEGIN{print (\"x\" in a); print length(a)}"],
            [],
            "0\n0\n");

        yield return Failing(
            "length-untyped-name-creates-scalar-context",
            ["BEGIN{print length(a); a[1]=1}"],
            [],
            2,
            "0\n",
            "awk: attempt to use scalar `a' as an array\n");

        yield return Passing(
            "length-missing-array-element-autovivifies",
            ["BEGIN{a[1]=1; print length(a[2]); print length(a)}"],
            [],
            "0\n2\n");

        yield return Failing(
            "length-scalar-as-array-element-fails",
            ["BEGIN{a=1; print length(a[1])}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n");

        yield return Failing(
            "posix-length-array-fails",
            ["--posix", "BEGIN{a[1]=1; print length(a)}"],
            [],
            2,
            string.Empty,
            "awk: length: received array argument\n");

        yield return Passing(
            "traditional-length-array-remains-supported",
            ["--traditional", "BEGIN{a[1]=1; print length(a)}"],
            [],
            "1\n");

        yield return Passing(
            "split-expression-return-value-and-regex-separator",
            ["BEGIN{n=split(\"a,b:c\", a, /[,:]/); print n, a[1], a[2], a[3]}"],
            [],
            "3 a b c\n");

        yield return Passing(
            "split-regex-captures-do-not-become-fields",
            ["BEGIN{n=split(\"a1b2c\", a, /([0-9])/); print n; for(i=1;i<=n;i++) print \"[\" a[i] \"]\"}"],
            [],
            "3\n[a]\n[b]\n[c]\n");

        yield return Passing(
            "split-regex-separator-array-return-value",
            ["BEGIN{n=split(\"::a:b::\", a, /:+/, s); print n; for(i=0;i<=n;i++) print \"s[\" i \"]=[\" s[i] \"]\"; for(i=1;i<=n;i++) print \"a[\" i \"]=[\" a[i] \"]\"}"],
            [],
            "4\ns[0]=[]\ns[1]=[::]\ns[2]=[:]\ns[3]=[::]\ns[4]=[]\na[1]=[]\na[2]=[a]\na[3]=[b]\na[4]=[]\n");

        yield return Passing(
            "split-default-whitespace-separator-array",
            ["BEGIN{split(\"  a  b \", a, \" \", s); for(i=0;i<=2;i++) print \"s[\" i \"]=[\" s[i] \"]\"; for(i=1;i<=2;i++) print \"a[\" i \"]=[\" a[i] \"]\"}"],
            [],
            "s[0]=[  ]\ns[1]=[  ]\ns[2]=[ ]\na[1]=[a]\na[2]=[b]\n");

        yield return Passing(
            "split-empty-separator-array",
            ["BEGIN{n=split(\"abc\", a, \"\", s); print n; for(i=0;i<=n;i++) print \"s[\" i \"]=[\" s[i] \"]\"; for(i=1;i<=n;i++) print \"a[\" i \"]=[\" a[i] \"]\"}"],
            [],
            "3\ns[0]=[]\ns[1]=[]\ns[2]=[]\ns[3]=[]\na[1]=[a]\na[2]=[b]\na[3]=[c]\n");

        yield return CompilerDiagnostic(Failing(
            "split-destination-expression-fails",
            ["BEGIN{split(\"abc\", 1+2)}"],
            [],
            2,
            string.Empty,
            "awk: argument 2 of `split' must be an array name.\n"));

        yield return Failing(
            "split-destination-scalar-fails",
            ["BEGIN{a=1; split(\"abc\", a)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n");

        yield return Passing(
            "patsplit-expression-return-value",
            ["BEGIN{n=patsplit(\"a12b345\", a, /[0-9]+/); print n, a[1], a[2]}"],
            [],
            "2 12 345\n");

        yield return Passing(
            "patsplit-separator-array-return-value",
            ["BEGIN{n=patsplit(\"aa-123--bb\", a, /[A-Za-z0-9]+/, s); print n; for(i=0;i<=n;i++) print \"s[\" i \"]=[\" s[i] \"]\"; for(i=1;i<=n;i++) print \"a[\" i \"]=[\" a[i] \"]\"}"],
            [],
            "3\ns[0]=[]\ns[1]=[-]\ns[2]=[--]\ns[3]=[]\na[1]=[aa]\na[2]=[123]\na[3]=[bb]\n");

        yield return Failing(
            "patsplit-separator-array-must-be-distinct",
            ["BEGIN{patsplit(\"a12b\", a, /[0-9]+/, a)}"],
            [],
            2,
            string.Empty,
            "awk: split separator array must be distinct from the destination array\n");

        yield return CompilerDiagnostic(Failing(
            "patsplit-destination-expression-fails",
            ["BEGIN{patsplit(\"abc\", 1+2, /[a-z]/)}"],
            [],
            2,
            string.Empty,
            "awk: argument 2 of `patsplit' must be an array name.\n"));

        yield return Failing(
            "patsplit-destination-scalar-fails",
            ["BEGIN{a=1; patsplit(\"abc\", a, /[a-z]/)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n");

        yield return Failing(
            "patsplit-default-fpat-invalid-regex-fails",
            ["BEGIN{FPAT=\"[\"; patsplit(\"abc\", a)}"],
            [],
            2,
            string.Empty,
            "awk: invalid regular expression\n");

        yield return Failing(
            "patsplit-explicit-invalid-regex-fails",
            ["BEGIN{patsplit(\"abc\", a, \"[\")}"],
            [],
            2,
            string.Empty,
            "awk: invalid regular expression\n");

        yield return Failing(
            "patsplit-array-pattern-fails",
            ["BEGIN{p[1]=1; patsplit(\"abc\", a, p)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `p' in a scalar context\n");

        yield return Failing(
            "split-separator-array-must-be-distinct",
            ["BEGIN{split(\"a:b\", a, \":\", a)}"],
            [],
            2,
            string.Empty,
            "awk: split separator array must be distinct from the destination array\n");

        yield return Failing(
            "split-default-fs-invalid-regex-fails",
            ["BEGIN{FS=\"[+\"; split(\"abc\", a)}"],
            [],
            2,
            string.Empty,
            "awk: invalid regular expression\n");

        yield return Failing(
            "split-explicit-invalid-regex-fails",
            ["BEGIN{split(\"abc\", a, \"[+\")}"],
            [],
            2,
            string.Empty,
            "awk: invalid regular expression\n");

        yield return Failing(
            "split-array-separator-fails",
            ["BEGIN{s[1]=1; split(\"abc\", a, s)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `s' in a scalar context\n");

        yield return PassingUnorderedStdOut(
            "single-statement-for-in-without-braces",
            ["{name=$0; ext=\"\"; if (match(name,/\\.([^.]+)$/ ,m)) ext=m[1]; else ext=\"(noext)\"; cnt[ext]++} END{for(e in cnt) print e, cnt[e]}", "/file.txt"],
            [File("/file.txt", "report.csv\nREADME\n")],
            "csv 1\n(noext) 1\n");
    }
}
