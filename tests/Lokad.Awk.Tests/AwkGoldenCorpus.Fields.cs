namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] FieldCases() =>
    [
        Posix(Passing(
            "field-separator",
            ["-F", ":", "{print $1}", "/file.txt"],
            [File("/file.txt", "a:b\nc:d\n")],
            "a\nc\n")),

        Posix(Passing(
            "regex-field-separator",
            ["-F", "[,:]", "{print $1, $2, $3}", "/file.txt"],
            [File("/file.txt", "a:b,c\n")],
            "a b c\n")),

        Passing(
            "joined-field-separator",
            ["-F:", "{print $2}", "/file.txt"],
            [File("/file.txt", "a:b\nc:d\n")],
            "b\nd\n"),

        Passing(
            "empty-option-field-separator",
            ["-F", "", "{print NF, $1, $2, $3}", "/file.txt"],
            [File("/file.txt", "abc\n")],
            "3 a b c\n"),

        Posix(Passing(
            "begin-fs-assignment-controls-fields",
            ["BEGIN{FS=\":\"} {print $2}", "/file.txt"],
            [File("/file.txt", "a:b\nc:d\n")],
            "b\nd\n")),

        Passing(
            "variable-option-fs-controls-fields",
            ["-v", "FS=:", "{print $2}", "/file.txt"],
            [File("/file.txt", "a:b\nc:d\n")],
            "b\nd\n"),

        Passing(
            "fs-assignment-affects-next-record",
            ["NR==1{FS=\":\"; next} {print $2}", "/file.txt"],
            [File("/file.txt", "a b\nc:d\n")],
            "d\n"),

        Passing(
            "fieldwidths-splitting",
            ["BEGIN{FIELDWIDTHS=\"2 3 3\"} {print $1, $2, $3, NF}", "/file.txt"],
            [File("/file.txt", "AA111BBB\n")],
            "AA 111 BBB 3\n"),

        Passing(
            "fieldwidths-skip-column",
            ["BEGIN{FIELDWIDTHS=\"1:2 2\"} {print $1, $2, NF}", "/file.txt"],
            [File("/file.txt", "ABCDE\n")],
            "BC DE 2\n"),

        Passing(
            "fpat-field-pattern",
            ["BEGIN{FPAT=\"[A-Z]+|[0-9]+\"} {print NF, $1, $2, $3}", "/file.txt"],
            [File("/file.txt", "A-12-B\n")],
            "3 A 12 B\n"),

        Passing(
            "empty-fs-character-splitting",
            ["BEGIN{FS=\"\"} {print NF, $1, $2, $3}", "/file.txt"],
            [File("/file.txt", "abc\n")],
            "3 a b c\n"),

        Passing(
            "literal-fs-preserves-empty-fields",
            ["BEGIN{FS=\",\"} {print NF, \"[\" $1 \"]\", \"[\" $2 \"]\", \"[\" $3 \"]\", \"[\" $4 \"]\"}", "/file.txt"],
            [File("/file.txt", "a,,c,\n")],
            "4 [a] [] [c] []\n"),

        Passing(
            "default-fs-space-tab-newline-groups",
            ["BEGIN{s=\" \\t\\na \\t b\\n \"; n=split(s,a,\" \"); print n; for(i=1;i<=n;i++) print \"[\" a[i] \"]\"}"],
            [],
            "2\n[a]\n[b]\n"),

        Passing(
            "default-fs-keeps-other-control-characters",
            ["BEGIN{s=\"a\\rb\\fc\\vd\"; n=split(s,a,\" \"); print n, index(a[1], \"\\r\"), index(a[1], \"\\f\"), index(a[1], \"\\v\")}"],
            [],
            "1 2 4 6\n"),

        Passing(
            "fs-assignment-switches-back-from-fieldwidths",
            ["BEGIN{FIELDWIDTHS=\"2 2\"; FS=\":\"} {print $1, $2, NF}", "/file.txt"],
            [File("/file.txt", "a:b\n")],
            "a b 2\n"),

        Passing(
            "utf8-multibyte-characters-count-as-characters",
            ["{print length($0), index($0,\"\u00e9\")}", "/file.txt"],
            [File("/file.txt", "caf\u00e9\n")],
            "4 4\n"),

        Passing(
            "utf8-combining-mark-counts-as-character",
            ["{print length($0), index($0,\"\u0301\"), substr($0,4,2)}", "/file.txt"],
            [File("/file.txt", "cafe\u0301\n")],
            "5 5 e\u0301\n"),

        Passing(
            "nonexistent-field-references",
            ["{i=5; print \"[\" $3 \"]\", \"[\" $(i) \"]\", NF}", "/file.txt"],
            [File("/file.txt", "a b\n")],
            "[] [] 2\n"),

        Passing(
            "dynamic-field-expression-reference",
            ["BEGIN{$0=\"a b c\"; print $(1+1), $NF, $0}"],
            [],
            "b c a b c\n"),

        Passing(
            "special-variable-prefix-identifiers",
            ["BEGIN{$0=\"a b c\"; NRx=42; NFx=2; NF_=1; NFIELD=3; print NRx, NR, $NFx, $NF_, $NFIELD, $NF}"],
            [],
            "42 0 b a c c\n"),

        Passing(
            "dynamic-field-post-increment",
            ["BEGIN{$0=\"1 2\"; i=2; print $i++, $2, $0; print $(i+0)}"],
            [],
            "2 3 1 3\n3\n"),

        Posix(Passing(
            "last-field-default-separator",
            ["{print $NF}", "/file.txt"],
            [File("/file.txt", "a b c\none two\n")],
            "c\ntwo\n")),

        Passing(
            "field-assignment-rebuilds-current-record",
            ["{$2=\"X\"; print $0}", "/file.txt"],
            [File("/file.txt", "a b c\n")],
            "a X c\n"),

        Passing(
            "field-assignment-extends-record",
            ["BEGIN{OFS=\"|\"} {i=4; $i=\"d\"; print NF, $0}", "/file.txt"],
            [File("/file.txt", "a b\n")],
            "4|a|b||d\n"),

        Passing(
            "field-zero-assignment-resplits-record",
            ["{$0=\"x y\"; print NF, $2}", "/file.txt"],
            [File("/file.txt", "a b c\n")],
            "2 y\n"),

        Passing(
            "nf-assignment-truncates-fields",
            ["{NF=2; print NF, $0}", "/file.txt"],
            [File("/file.txt", "a b c\n")],
            "2 a b\n"),

        Passing(
            "nf-assignment-extends-fields",
            ["{NF=4; print NF, $0}", "/file.txt"],
            [File("/file.txt", "a b\n")],
            "4 a b  \n"),

        Failing(
            "negative-field-reference-fails",
            ["BEGIN{print $(-1)}"],
            [],
            2,
            string.Empty,
            "awk: negative field index\n"),

        Failing(
            "negative-field-assignment-fails",
            ["BEGIN{$(-1)=\"x\"}"],
            [],
            2,
            string.Empty,
            "awk: negative field index\n"),

        Failing(
            "negative-nf-assignment-fails",
            ["BEGIN{NF=-1}"],
            [],
            2,
            string.Empty,
            "awk: negative field index\n"),

        Passing(
            "supplementary-utf8-characters-count-as-characters",
            ["BEGIN{s=\"\U0001F600a\"; print length(s), index(s,\"a\"), substr(s,1,1), substr(s,2,1); split(s,a,\"\"); print length(a), a[1], a[2]}"],
            [],
            "2 2 \U0001F600 a\n2 \U0001F600 a\n")
    ];
}
