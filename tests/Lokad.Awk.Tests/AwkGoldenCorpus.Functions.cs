namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] BuiltinFunctionCases() =>
    [
        Passing(
            "common-string-and-int-functions",
            ["{print substr($1,2,3), toupper($2), tolower(\"MiX\"), int(3.8), int(-3.8)}", "/file.txt"],
            [File("/file.txt", "abcdef xy\n")],
            "bcd XY mix 3 -3\n"),

        Passing(
            "deterministic-numeric-functions",
            ["BEGIN{print int(sqrt(9)), int(sin(0)), int(cos(0)), int(exp(1)), int(log(exp(1))), int(atan2(0,-1))}"],
            [],
            "3 0 1 2 1 3\n"),

        Passing(
            "rand-default-seed",
            ["BEGIN{print int(rand()*1000000), int(rand()*1000000)}"],
            [],
            "924045 593908\n"),

        Passing(
            "srand-explicit-seed",
            ["BEGIN{old=srand(1); print int(rand()*1000000), int(rand()*1000000); print srand(2); print int(rand()*1000000); print srand(1); print int(rand()*1000000)}"],
            [],
            "924045 593908\n1\n893104\n2\n924045\n"),

        Passing(
            "srand-without-argument-returns-previous-seed",
            ["BEGIN{old=srand(7); print srand()}"],
            [],
            "7\n"),

        Passing(
            "time-functions-explicit-utc",
            ["BEGIN{print mktime(\"1970 01 02 03 04 05\", 1); print strftime(\"%Y-%m-%d %H:%M:%S\", 0, 1); print strftime(\"%s\", 86400, 1)}"],
            [],
            "97445\n1970-01-01 00:00:00\n86400\n"),

        Passing(
            "strftime-procinfo-default-format",
            ["BEGIN{PROCINFO[\"strftime\"]=\"%Y\"; y=strftime(); if(y ~ /^[0-9][0-9][0-9][0-9]$/) print \"year\"; else print y}"],
            [],
            "year\n"),

        Passing(
            "systime-returns-positive-unix-time",
            ["BEGIN{if(systime()>0) print \"positive\"; else print \"bad\"}"],
            [],
            "positive\n"),

        Passing(
            "bit-functions",
            ["BEGIN{print and(6,3), or(6,3), xor(6,3), lshift(3,2), rshift(8,1), compl(0)}"],
            [],
            "2 7 5 12 4 -1\n"),

        Failing(
            "bit-function-negative-operand-fails",
            ["BEGIN{print and(-1, 1)}"],
            [],
            2,
            string.Empty,
            "awk: negative bitwise operand\n"),

        Failing(
            "bit-function-negative-shift-fails",
            ["BEGIN{print lshift(1, -1)}"],
            [],
            2,
            string.Empty,
            "awk: negative bitwise operand\n"),

        Passing(
            "sprintf-function",
            ["BEGIN{msg=sprintf(\"%5d:%s\", 7, \"sku\"); print msg}"],
            [],
            "    7:sku\n"),

        Passing(
            "printf-common-conversions",
            ["BEGIN{printf \"%04d %.2f %x %o %c %-5s!\\n\", 7, 3.126, 255, 9, 65, \"x\"}"],
            [],
            "0007 3.13 ff 11 A x    !\n"),

        Passing(
            "printf-empty-and-numeric-string-conversions",
            ["BEGIN{printf \"[%d][%g][%s]\\n\", \"\", \"\", \"\"; split(\"00 1.5 x\", a); printf \"[%d][%.1f][%d][%s]\\n\", a[1], a[2], a[3], a[3]}"],
            [],
            "[0][0][]\n[0][1.5][0][x]\n"),

        Failing(
            "printf-too-few-arguments-fails",
            ["BEGIN{printf \"[%d][%s][%g]\\n\", 1}"],
            [],
            2,
            string.Empty,
            "awk: not enough arguments to satisfy format string\n"),

        Failing(
            "sprintf-too-few-arguments-fails",
            ["BEGIN{print sprintf(\"[%d][%s]\", 1)}"],
            [],
            2,
            string.Empty,
            "awk: not enough arguments to satisfy format string\n"),

        Passing(
            "sprintf-width-and-precision",
            ["BEGIN{print sprintf(\"%.3s:%06.2f\", \"abcdef\", 12.3)}"],
            [],
            "abc:012.30\n"),

        Passing(
            "sprintf-dynamic-width-precision-and-alternate",
            ["BEGIN{print sprintf(\"[%*.*f] [%#x] [%#o] [%+d] [% d]\", 8, 2, 3.5, 255, 8, 7, 7)}"],
            [],
            "[    3.50] [0xff] [010] [+7] [ 7]\n"),

        Passing(
            "printf-dynamic-width-precision-and-alternate",
            ["BEGIN{printf \"%*.*f|%#x|%#o|%*s!\\n\", 6, 2, 3.5, 15, 15, -6, \"hi\"}"],
            [],
            "  3.50|0xf|017|hi    !\n"),

        Passing(
            "printf-alternate-form-edge-flags",
            ["BEGIN{printf \"[%#.0o][%#.0x][%#.0f][%#.0g][%#8.0x][%-#8x][%08x][%-08x]\\n\", 0, 0, 3, 3, 31, 31, 31, 31}"],
            [],
            "[0][0][3.][3.][    0x1f][0x1f    ][0000001f][1f      ]\n"),

        Passing(
            "sprintf-sign-padding-string-precision-and-unknown-specifier",
            ["BEGIN{print sprintf(\"[%#.3g][%#.3G][%+08.2f][% 08.2f][%*.s][%.*s][%q]\", 12, 12, 3.5, 3.5, 5, \"abcdef\", 3, \"abcdef\", \"x\")}"],
            [],
            "[12.0][12.0][+0003.50][ 0003.50][     ][abc][%q]\n"),

        Passing(
            "printf-scientific-and-general-conversions",
            ["BEGIN{printf \"%.2e|%.2E|%.3g|%.3G|%s\\n\", 1234.5, 1234.5, 1234.5, 0.0012345, sprintf(\"%.2e\", 12.26)}"],
            [],
            "1.23e+03|1.23E+03|1.23e+03|0.00123|1.23e+01\n"),

        Passing(
            "strtonum-function",
            ["BEGIN{print strtonum(\"010\"), strtonum(\"0x10\"), strtonum(\"3.5\")}"],
            [],
            "8 16 3.5\n"),

        Passing(
            "strtonum-non-decimal-signs-and-full-consumption",
            ["BEGIN{print strtonum(\"+0x10\"), strtonum(\"-0x10\"), strtonum(\"010x\"), strtonum(\"12abc\")}"],
            [],
            "16 -16 0 0\n"),

        Passing(
            "signed-infinity-string-conversion",
            ["BEGIN{print \"+inf\"+0; print \"-Inf\"+0; print ((\"+inf\"+0)>999999), ((\"-inf\"+0)<-999999); print \"inf\"+0, \"Infinity\"+0, \"NaN\"+0, \"+infx\"+0}"],
            [],
            "+inf\n-inf\n1 1\n0 0 0 0\n"),

        Passing(
            "strtonum-signed-infinity-decisions",
            ["BEGIN{print strtonum(\"+inf\"), strtonum(\"-Inf\"), strtonum(\"inf\"), strtonum(\"Infinity\"), strtonum(\"NaN\"), strtonum(\"1e999\")}"],
            [],
            "+inf -inf 0 0 0 +inf\n"),

        Passing(
            "printf-signed-infinity-conversions",
            ["BEGIN{printf \"[%g][%G][%f][%F][%d][%X]\\n\", \"+inf\"+0, \"+inf\"+0, \"-inf\"+0, \"-inf\"+0, \"+inf\"+0, \"+inf\"+0}"],
            [],
            "[+inf][+INF][-inf][-INF][+inf][+INF]\n"),

        Passing(
            "gensub-function",
            ["BEGIN{print gensub(\"a\",\"X\",\"g\",\"banana\"); print gensub(\"a\",\"X\",2,\"banana\")}"],
            [],
            "bXnXnX\nbanXna\n"),

        PassingWithStdErr(
            "gensub-third-argument-warning",
            ["BEGIN{print gensub(/a/,\"X\",0,\"banana\"); print gensub(/a/,\"X\",\"x\",\"banana\"); print gensub(/a/,\"X\",\"2x\",\"banana\"); print gensub(/a/,\"X\",0.8,\"banana\")}"],
            [],
            "bXnana\nbXnana\nbanXna\nbXnana\n",
            "awk: warning: gensub: third argument `0' treated as 1\nawk: warning: gensub: third argument `x' treated as 1\n"),

        Passing(
            "gensub-replacement-backreferences",
            ["BEGIN{print gensub(\"([a-z]+)([0-9]+)\",\"\\\\2-\\\\1\",\"g\",\"a12 b34\"); print gensub(\"[0-9]+\",\"<&>\",1,\"a12b34\")}"],
            [],
            "12-a 34-b\na<12>b34\n"),

        Passing(
            "gensub-zero-capture-and-escaped-letter",
            ["BEGIN{print gensub(/(a)/,\"\\\\0-\\\\1\",\"g\",\"a\"); print gensub(/a/,\"\\\\q\",\"g\",\"a\")}"],
            [],
            "a-a\nq\n"),

        Passing(
            "gensub-regexp-literal-argument",
            ["BEGIN{print gensub(/a+/,\"X\",\"g\",\"baana\")}"],
            [],
            "bXnX\n"),

        CompilerDiagnostic(Failing(
            "gensub-too-few-arguments-fails",
            ["BEGIN{print gensub(\"a\",\"X\")}"],
            [],
            2,
            string.Empty,
            "awk: invalid number of arguments for gensub\n")),

        CompilerDiagnostic(Failing(
            "gensub-too-many-arguments-fails",
            ["BEGIN{print gensub(\"a\",\"X\",\"g\",\"banana\",\"extra\")}"],
            [],
            2,
            string.Empty,
            "awk: invalid number of arguments for gensub\n")),

        Failing(
            "gensub-array-argument-fails",
            ["BEGIN{a[1]=1; print gensub(a,\"X\",\"g\",\"banana\")}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `a' in a scalar context\n"),

        Passing(
            "string-octal-hex-and-alert-escapes",
            ["BEGIN{print \"A\\101\\x42\"; print length(\"\\a\")}"],
            [],
            "AAB\n1\n"),

        Passing(
            "string-hex-escape-consumes-two-digits",
            ["BEGIN{print \"[\" \"\\x4142\" \"]\"}"],
            [],
            "[A42]\n"),

        Passing(
            "logical-not-truthiness",
            ["BEGIN{x=0; y=\"\"; z=\"text\"; print !x, !1, !y, !z}"],
            [],
            "1 0 1 0\n"),

        Passing(
            "truthiness-distinguishes-numbers-and-strings",
            ["BEGIN{n=0; s0=\"0\"; s00=\"00\"; e=\"\"; t=\"text\"; a[1]=0; a[2]=\"0\"; print (n?1:0), (s0?1:0), (s00?1:0), (e?1:0), (u?1:0), (t?1:0), (a[1]?1:0), (a[2]?1:0)}"],
            [],
            "0 1 1 0 0 1 0 1\n"),

        Failing(
            "array-as-scalar-condition-fails",
            ["BEGIN{a[1]=1; if(a) print \"bad\"}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `a' in a scalar context\n"),

        Passing(
            "numeric-prefix-string-arithmetic",
            ["BEGIN{print \"12abc\"+1, \"  -3.5x\"+1, \"1e2x\"+1, \".5x\"+1, \"abc\"+1}"],
            [],
            "13 -2.5 101 1.5 1\n"),

        Passing(
            "match-capture-array",
            ["{ext=\"(noext)\"; if(match($0,/\\.([^.]+)$/,m)){ext=m[1]} print ext}", "/file.txt"],
            [File("/file.txt", "report.csv\nREADME\narchive.tar.gz\n")],
            "csv\n(noext)\ngz\n"),

        Passing(
            "match-rstart-and-rlength",
            ["BEGIN{print match(\"abc\",/b/), RSTART, RLENGTH; print match(\"abc\",/z/), RSTART, RLENGTH}"],
            [],
            "2 2 1\n0 0 -1\n"),

        Passing(
            "match-capture-array-metadata",
            ["BEGIN{if(match(\"abc\",/(a)(x)?(b)/,m)){for(i=0;i<=3;i++) print i, \"[\" m[i] \"]\", m[i,\"start\"], m[i,\"length\"]}}"],
            [],
            "0 [ab] 1 2\n1 [a] 1 1\n2 []  \n3 [b] 2 1\n"),

        Passing(
            "match-dynamic-regex",
            ["BEGIN{r=\"b+\"; print match(\"abbc\", r), RSTART, RLENGTH; if(match(\"file.txt\", \"\\\\.([^.]*)$\", m)) print m[1]}"],
            [],
            "2 2 2\ntxt\n"),

        CompilerDiagnostic(Failing(
            "match-capture-target-expression-fails",
            ["BEGIN{match(\"abc\", /a/, 1+2)}"],
            [],
            2,
            string.Empty,
            "awk: argument 3 of `match' must be an array name.\n")),

        Failing(
            "match-capture-target-scalar-fails",
            ["BEGIN{m=1; match(\"abc\", /a/, m)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `m' as an array\n"),
    ];
}
