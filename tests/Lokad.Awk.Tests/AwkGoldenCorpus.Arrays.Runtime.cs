namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] ArrayRuntimeCases() =>
    [
        Passing(
            "associative-array-count",
            ["{ext=\"(noext)\"; if(match($0,/\\.([^.]+)$/,m)){ext=m[1]} counts[ext]++} END{print counts[\"csv\"], counts[\"(noext)\"], counts[\"gz\"]}", "/file.txt"],
            [File("/file.txt", "report.csv\nREADME\narchive.tar.gz\n")],
            "1 1 1\n"),

        PassingUnorderedStdOut(
            "associative-array-iteration",
            ["{ext=\"(noext)\"; if(match($0,/\\.([^.]+)$/,m)){ext=m[1]} counts[ext]++} END{for(ext in counts){print ext, counts[ext]}}", "/file.txt"],
            [File("/file.txt", "report.csv\nREADME\narchive.tar.gz\n")],
            "csv 1\n(noext) 1\ngz 1\n"),

        Passing(
            "procinfo-sorted-in-index-order",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=10; PROCINFO[\"sorted_in\"]=\"@ind_str_asc\"; for(k in a) print k, a[k]}"],
            [],
            "a 10\nb 2\n"),

        Passing(
            "procinfo-sorted-in-value-order",
            ["BEGIN{a[\"x\"]=2; a[\"y\"]=10; PROCINFO[\"sorted_in\"]=\"@val_num_desc\"; for(k in a) print k, a[k]}"],
            [],
            "y 10\nx 2\n"),

        Failing(
            "procinfo-sorted-in-unknown-mode-fails",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=1; PROCINFO[\"sorted_in\"]=\"@bogus\"; for(k in a) print k}"],
            [],
            2,
            string.Empty,
            "awk: sort comparison function is not defined: @bogus\n"),

        Passing(
            "array-membership-condition",
            ["{if($1 in seen){next}else{seen[$1]=1; print $1}}", "/file.txt"],
            [File("/file.txt", "a 1\nb 2\na 3\n")],
            "a\nb\n"),

        Passing(
            "multidimensional-array-subsep",
            ["BEGIN{a[\"x\",\"y\"]=7; print a[\"x\",\"y\"]; if((\"x\",\"y\") in a) print \"present\"; SUBSEP=\"|\"; a[\"x\",\"y\"]=9; print a[\"x|y\"]; delete a[\"x\",\"y\"]; if((\"x\",\"y\") in a) print \"bad\"; else print \"deleted\"}"],
            [],
            "7\npresent\n9\ndeleted\n"),

        Passing(
            "valid-utf8-array-keys-and-subsep-composition",
            ["BEGIN{a[\"café\"]=7; print (\"café\" in a), a[\"café\"]; SUBSEP=\"é\"; a[\"caf\",\"\"]=9; print a[\"café\"]}"],
            [],
            "1 7\n9\n"),

        Passing(
            "split-array-values-preserve-numeric-string-comparison",
            ["BEGIN{split(\"00\", a); print typeof(a[1]), (a[1]==0), (a[1]==\"00\"), (a[1]?1:0)}"],
            [],
            "strnum 1 1 0\n"),

        CompilerDiagnostic(Failing(
            "arrays-of-arrays-unsupported",
            ["BEGIN{a[1][2]=3}"],
            [],
            2,
            string.Empty,
            "awk: arrays of arrays are unsupported in Lokad awk\n")),

        Passing(
            "arrays-of-arrays-token-in-string",
            ["BEGIN{print \"a[1][2]\"}"],
            [],
            "a[1][2]\n"),

        Passing(
            "delete-array-element",
            ["{a[$1]=$2} END{delete a[\"x\"]; for(k in a) print k, a[k]}", "/file.txt"],
            [File("/file.txt", "x 1\ny 2\n")],
            "y 2\n"),

        Passing(
            "delete-whole-array",
            ["{a[$1]=$2} END{delete a; for(k in a) print k; print \"done\"}", "/file.txt"],
            [File("/file.txt", "x 1\ny 2\n")],
            "done\n"),

        Passing(
            "asort-values-numeric-ascending",
            ["BEGIN{a[\"x\"]=3; a[\"y\"]=1; a[\"z\"]=2; asort(a,b,\"@val_num_asc\"); print b[1], b[2], b[3]}"],
            [],
            "1 2 3\n"),

        Passing(
            "asort-values-numeric-descending",
            ["BEGIN{a[\"x\"]=3; a[\"y\"]=10; a[\"z\"]=2; asort(a,b,\"@val_num_desc\"); print b[1], b[2], b[3]}"],
            [],
            "10 3 2\n"),

        Passing(
            "asort-values-string-descending",
            ["BEGIN{a[\"x\"]=\"aa\"; a[\"y\"]=\"b\"; a[\"z\"]=\"c\"; asort(a,b,\"@val_str_desc\"); print b[1], b[2], b[3]}"],
            [],
            "c b aa\n"),

        Passing(
            "asort-values-string-ascending",
            ["BEGIN{a[\"x\"]=\"aa\"; a[\"y\"]=\"b\"; a[\"z\"]=\"c\"; asort(a,b,\"@val_str_asc\"); print b[1], b[2], b[3]}"],
            [],
            "aa b c\n"),

        Passing(
            "asort-index-numeric-ascending",
            ["BEGIN{a[10]=\"x\"; a[2]=\"y\"; n=asort(a,b,\"@ind_num_asc\"); print n, b[1], b[2]}"],
            [],
            "2 y x\n"),

        Passing(
            "asort-expression-in-place-return-value",
            ["BEGIN{a[3]=\"c\"; a[1]=\"a\"; a[2]=\"b\"; n=asort(a); print n, a[1], a[2], a[3]}"],
            [],
            "3 a b c\n"),

        Passing(
            "asorti-expression-destination-return-value",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=1; n=asorti(a,k); print n, k[1], k[2]}"],
            [],
            "2 a b\n"),

        Passing(
            "asorti-index-numeric-descending",
            ["BEGIN{a[10]=\"x\"; a[2]=\"y\"; n=asorti(a,k,\"@ind_num_desc\"); print n, k[1], k[2]}"],
            [],
            "2 10 2\n"),

        Passing(
            "asorti-index-string-descending",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=1; n=asorti(a,k,\"@ind_str_desc\"); print n, k[1], k[2]}"],
            [],
            "2 b a\n"),

        Passing(
            "asorti-value-string-ascending",
            ["BEGIN{a[10]=\"x\"; a[2]=\"y\"; n=asorti(a,k,\"@val_str_asc\"); print n, k[1], k[2]}"],
            [],
            "2 10 2\n"),

        Passing(
            "asorti-expression-in-place-return-value",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=1; n=asorti(a); print n, a[1], a[2]}"],
            [],
            "2 a b\n"),

        Passing(
            "asort-user-comparator-by-value-length",
            ["function cmp(i1,v1,i2,v2){return length(v1)-length(v2)} BEGIN{a[\"x\"]=\"bbb\"; a[\"y\"]=\"a\"; a[\"z\"]=\"cc\"; n=asort(a,b,\"cmp\"); print n, b[1], b[2], b[3]}"],
            [],
            "3 a cc bbb\n"),

        Passing(
            "asorti-user-comparator-by-numeric-index",
            ["function cmp(i1,v1,i2,v2){return i2-i1} BEGIN{a[10]=\"x\"; a[2]=\"y\"; n=asorti(a,k,\"cmp\"); print n, k[1], k[2]}"],
            [],
            "2 10 2\n"),

        Passing(
            "asort-comparator-statement-body",
            ["function cmp(i1,v1,i2,v2, d){d=length(v1)-length(v2); if(d) return d; return i1-i2} BEGIN{a[2]=\"bb\"; a[1]=\"a\"; a[3]=\"cc\"; n=asort(a,b,\"cmp\"); print n, b[1], b[2], b[3]}"],
            [],
            "3 a bb cc\n"),

        Failing(
            "asort-unknown-mode-fails",
            ["BEGIN{a[\"b\"]=2; a[\"a\"]=1; asort(a,b,\"@bogus\")}"],
            [],
            2,
            string.Empty,
            "awk: sort comparison function is not defined: @bogus\n"),

        Failing(
            "asort-scalar-source-fails",
            ["BEGIN{x=1; asort(x)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `x' as an array\n"),

        Failing(
            "asort-scalar-destination-fails",
            ["BEGIN{a[1]=1; b=2; asort(a,b)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `b' as an array\n"),

        Failing(
            "asorti-scalar-source-fails",
            ["BEGIN{x=1; asorti(x)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use scalar `x' as an array\n"),

        Failing(
            "asort-order-array-fails",
            ["BEGIN{a[1]=1; order[1]=1; asort(a,b,order)}"],
            [],
            2,
            string.Empty,
            "awk: attempt to use array `order' in a scalar context\n")
    ];
}
