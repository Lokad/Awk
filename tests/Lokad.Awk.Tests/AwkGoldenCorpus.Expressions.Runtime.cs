namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] ExpressionRuntimeCases() =>
    [
        Passing(
            "compound-assignment-sum",
            ["{sum+=$1} END{print sum}", "/file.txt"],
            [File("/file.txt", "10 a\n5 b\n20 c\n")],
            "35\n"),

        Passing(
            "binary-addition-sum",
            ["{sum=sum+$1} END{print sum}", "/file.txt"],
            [File("/file.txt", "10 a\n5 b\n20 c\n")],
            "35\n"),

        Posix(Passing(
            "concatenated-numeric-text-retains-string-semantics",
            ["BEGIN{x=\"01\" \"\"; print x+1, (x==1), (x==\"01\")}"],
            [],
            "2 0 1\n")),

        Passing(
            "compound-assignment-operators",
            ["BEGIN{x=10; x-=3; y=4; y*=5; z=20; z/=4; r=17; r%=5; print x, y, z, r}"],
            [],
            "7 20 5 2\n"),

        Passing(
            "decrement-operators",
            ["BEGIN{x=3; print --x; x--; a[\"k\"]=5; a[\"k\"]--; print x, a[\"k\"]}"],
            [],
            "2\n1 4\n"),

        Passing(
            "field-post-decrement",
            ["{$2--; print $0}", "/file.txt"],
            [File("/file.txt", "a 3 c\n")],
            "a 2 c\n"),

        Passing(
            "exponentiation-and-assignment",
            ["BEGIN{x=2^3; y=2; y^=4; print x, y, 2^3^2}"],
            [],
            "8 16 512\n"),

        Passing(
            "regex-match-operator",
            ["$2 ~ /^a/ {print $1}", "/file.txt"],
            [File("/file.txt", "10 apple\n5 berry\n20 apricot\n")],
            "10\n20\n"),

        Passing(
            "regex-not-match-operator",
            ["$2 !~ /^a/ {print $1}", "/file.txt"],
            [File("/file.txt", "10 apple\n5 berry\n20 apricot\n")],
            "5\n"),

        Passing(
            "regex-literal-scalar-expression-contexts",
            ["function keep(value){return value} {hit=/foo/; print /foo/, !/foo/, (/foo/ ? \"yes\" : \"no\"), hit, keep(/foo/)}", "/file.txt"],
            [File("/file.txt", "foo\nbar\n")],
            "1 0 yes 1 1\n0 1 no 0 0\n"),

        Passing(
            "regex-literal-direct-and-grouped-match-operands",
            ["{value=\"foo\"; print (value ~ /foo/), (value ~ (/foo/)), (value !~ /bar/)}", "/file.txt"],
            [File("/file.txt", "foo\nbar\n")],
            "1 0 1\n1 0 1\n"),

        Passing(
            "ignorecase-regex-literal-scalar-expression",
            ["BEGIN{IGNORECASE=1} {print /foo/}", "/file.txt"],
            [File("/file.txt", "Foo\nbar\n")],
            "1\n0\n"),

        Passing(
            "ignorecase-regex-matching",
            ["BEGIN{IGNORECASE=1} /foo/{print \"rule\", $0} $0 ~ /BAR/{print \"op\", $0} {if(match($0,/baz/)) print \"match\", RSTART}", "/file.txt"],
            [File("/file.txt", "Foo\nbar\nBAZ\n")],
            "rule Foo\nop bar\nmatch 1\n"),

        Passing(
            "ignorecase-substitution",
            ["BEGIN{IGNORECASE=1} {sub(/foo/,\"x\"); gsub(/bar/,\"y\"); print $0}", "/file.txt"],
            [File("/file.txt", "FOO BAR bar\n")],
            "x y y\n"),

        Passing(
            "ignorecase-split-and-field-splitting",
            ["BEGIN{IGNORECASE=1; split(\"aXb\", parts, \"x\"); print parts[1], parts[2]; FS=\"x\"} {print $1, $2}", "/file.txt"],
            [File("/file.txt", "cXd\n")],
            "a b\nc d\n"),

        Passing(
            "parenthesized-arithmetic",
            ["{sum=(sum+$1)} END{print sum}", "/file.txt"],
            [File("/file.txt", "10 a\n5 b\n20 c\n")],
            "35\n"),

        Passing(
            "print-numbers-use-ofmt",
            ["BEGIN{print 1/3; OFMT=\"%.2f\"; print 1/3; print \"1.2345\"}"],
            [],
            "0.333333\n0.33\n1.2345\n"),

        Passing(
            "print-numeric-field-expression-uses-ofmt",
            ["BEGIN{OFMT=\"%.2f\"} {print $1+0, $1}", "/file.txt"],
            [File("/file.txt", "1.2345\n")],
            "1.23 1.2345\n"),

        Passing(
            "print-assigned-numbers-use-ofmt",
            ["BEGIN{OFMT=\"%.2f\"; x=1/3; y=\"1.2345\"; print x; print y}"],
            [],
            "0.33\n1.2345\n"),

        Passing(
            "custom-number-formats-preserve-source-precision",
            ["BEGIN{OFMT=\"%.12g\"; print 1/3; CONVFMT=\"%.12g\"; print (1/3 \"\")}"],
            [],
            "0.333333333333\n0.333333333333\n"),

        Passing(
            "concat-numbers-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; print (1/3 \"\"); print (12 \"\")}"],
            [],
            "0.33\n12\n"),

        Passing(
            "concat-assigned-numbers-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; x=1/3; y=\"1\"; print \"v=\" x; print \"v=\" y}"],
            [],
            "v=0.33\nv=1\n"),

        Passing(
            "multipart-concat-preserves-formatting-order",
            ["function changefmt(){CONVFMT=\"%.2f\"; return \"b\"} BEGIN{CONVFMT=\"%.4f\"; print ((1/3) \"a\" changefmt())}"],
            [],
            "0.3333ab\n"),

        Passing(
            "implicit-string-conversions-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; x=1.2345; print length(x), index(x,\"23\"), substr(x,1,4), (x ~ /^1\\.23$/), match(x,/^1\\.23$/), tolower(x)}"],
            [],
            "4 3 1.23 1 1 1.23\n"),

        Passing(
            "comparison-regex-switch-and-substitution-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; n=1.234567; print (n==\"1.23\"), (n==\"1.23457\"), (\"1.23\" ~ n), (n ~ \"^1.23$\"); switch(n){case /^1\\.23$/: print \"switch\"}; x=n; sub(/23/,\"XX\",x); print x}"],
            [],
            "1 0 1 1\nswitch\n1.XX\n"),

        Passing(
            "special-separators-and-fields-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; n=1.234567; FS=n; $0=\"a1.23b\"; print NF,$1,$2; OFS=n; print \"a\",\"b\"; SUBSEP=n; a[\"x\",\"y\"]=7; print (\"x1.23y\" in a); OFS=\" \"; $0=n; FS=\"\\\\.\"; print NF; FS=\" \"; $0=\"a b\"; $1=n; print \"[\" $0 \"]\"}"],
            [],
            "2 a b\na1.23b\n1\n2\n[1.23 b]\n"),

        Passing(
            "record-and-output-separators-use-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\"; RS=1.234567; ORS=1.234567; print \"x\"; ORS=\"\\n\"} {print \"[\" $0 \"]\"}", "/file.txt"],
            [File("/file.txt", "a1.23b")],
            "x1.23[a]\n[b]\n"),

        Passing(
            "comparison-logical-and-ternary-expressions",
            ["BEGIN{x=1; print (\"b\" > \"a\"), (2 <= 2), (2 != 3); print (0 && ++x), x; print (1 || ++x), x; print (x ? \"yes\" : ++y), y+0}"],
            [],
            "1 1 1\n0 1\n1 1\nyes 0\n"),

        Passing(
            "postfix-update-expressions",
            ["BEGIN{x=1; print x++, x; a[\"k\"]=3; print a[\"k\"]++, a[\"k\"]; $0=\"a b\"; print $2++, $2}"],
            [],
            "1 2\n3 4\nb 1\n"),

        Passing(
            "mutation-expression-target-index-evaluates-once",
            ["BEGIN{i=0; a[1]=10; print ++a[++i], i, a[1], a[2]; i=0; a[1]=20; print a[++i]++, i, a[1], a[2]; i=0; a[1]=30; print (a[++i] += 5), i, a[1], a[2]}"],
            [],
            "11 1 11 \n20 1 21 \n35 1 35 \n"),

        Passing(
            "scientific-notation-numeric-constants",
            ["BEGIN{print 1e3, 2.5e2, 1E-1}"],
            [],
            "1000 250 0.1\n"),

        Passing(
            "dot-form-numeric-constants",
            ["BEGIN{print .5, 1., .5e1, 1.e1}"],
            [],
            "0.5 1 5 10\n"),

        Passing(
            "hexadecimal-numeric-constants",
            ["BEGIN{print 0x10, 0Xf}"],
            [],
            "16 15\n"),

        Passing(
            "octal-numeric-constants",
            ["BEGIN{print 010, 07, 00, 08, 09}"],
            [],
            "8 7 0 8 9\n"),

        Passing(
            "else-clause",
            ["{if($1>10){print $2}else{print \"small\"}}", "/file.txt"],
            [File("/file.txt", "10 a\n5 b\n20 c\n")],
            "small\nsmall\nc\n"),

        Passing(
            "length-function",
            ["{print length($0)}", "/file.txt"],
            [File("/file.txt", "alpha\nbb\n")],
            "5\n2\n"),

        Passing(
            "length-without-argument",
            ["BEGIN{print length()} {print length()}", "/file.txt"],
            [File("/file.txt", "alpha\nbb\n")],
            "0\n5\n2\n"),

        Passing(
            "length-keyword-without-parentheses",
            ["{print length}", "/file.txt"],
            [File("/file.txt", "alpha\nbb\n")],
            "5\n2\n"),

        Passing(
            "length-array",
            ["BEGIN{a[\"x\"]=1; a[\"y\"]=2; print length(a), length(\"abc\")}"],
            [],
            "2 3\n"),

        Passing(
            "type-functions",
            ["BEGIN{a[1]=1; x=3; y=\"abc\"; print isarray(a), isarray(x), typeof(a), typeof(x), typeof(y)}"],
            [],
            "1 0 array number string\n"),

        Passing(
            "typeof-uninitialized-does-not-fix-type",
            ["BEGIN{print typeof(a), isarray(a); a[1]=1; print a[1]}"],
            [],
            "untyped 0\n1\n"),

        Passing(
            "typeof-array-element-does-not-autovivify",
            ["BEGIN{a[2]=1; print typeof(a[1]), (1 in a), (2 in a); print typeof(a[2])}"],
            [],
            "untyped 0 1\nnumber\n"),

        Passing(
            "typeof-array-element-materializes-array-not-element",
            ["BEGIN{print typeof(a[1]), isarray(a), (1 in a)}"],
            [],
            "untyped 1 0\n"),

        Passing(
            "typeof-numeric-string-provenance",
            ["BEGIN{split(\"00\", a); x=a[1]; print typeof(a[1]), typeof(x); x=x \"\"; print typeof(x)}"],
            [],
            "strnum strnum\nstring\n"),

        Passing(
            "uninitialized-scalar-is-empty-string-and-zero",
            ["BEGIN{print \"[\" x \"]\", x+2, (x?1:0), (x==0), (x==\"\"), (x<1); y=\"\"; print (y==0), (y==\"\"), (y<1)}"],
            [],
            "[] 2 0 1 1 1\n0 1 1\n"),

        Passing(
            "uninitialized-array-element-is-empty-string-and-zero",
            ["BEGIN{print \"[\" a[1] \"]\", a[1]+2, (a[1]?1:0), (a[1]==0), (a[1]==\"\"); a[1]=\"\"; print (a[1]==0), (a[1]==\"\")}"],
            [],
            "[] 2 0 1 1\n0 1\n"),

        Passing(
            "numeric-string-input-fields-compare-numerically",
            ["{print typeof($1), ($1==0), ($1==\"00\"), ($1?1:0); x=\"00\"; print typeof(x), (x==0), (x==\"00\"), (x?1:0)}", "/file.txt"],
            [File("/file.txt", "00\n")],
            "strnum 1 1 0\nstring 0 1 1\n"),

        Passing(
            "numeric-string-split-results-compare-numerically",
            ["BEGIN{split(\"00 x\", a); print typeof(a[1]), (a[1]==0), (a[1]==\"00\"), (a[1]?1:0); print typeof(a[2]), (a[2]==0), (a[2]==\"x\"), (a[2]?1:0)}"],
            [],
            "strnum 1 1 0\nstring 0 1 1\n"),

        Failing(
            "scalar-as-array-assignment-fails",
            ["BEGIN{a=1; a[1]=2}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n"),

        Failing(
            "scalar-as-array-read-fails",
            ["BEGIN{a=1; print a[1]}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n"),

        Failing(
            "scalar-context-then-array-fails",
            ["BEGIN{x=a; a[1]=2}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `a' as an array\n"),

        Failing(
            "array-as-scalar-assignment-fails",
            ["BEGIN{a[1]=2; a=3}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `a' in a scalar context\n"),

        Failing(
            "array-as-scalar-print-fails",
            ["BEGIN{a[1]=2; print a}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `a' in a scalar context\n")
    ];
}
