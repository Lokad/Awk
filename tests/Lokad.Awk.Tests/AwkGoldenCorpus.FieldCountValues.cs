namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] FieldCountValueCompatibilityRepairs() =>
    [
        Passing("field-values-survive-out-of-order-reads-truncation-and-growth",
            ["BEGIN{$0=\"a b c d e f g h\";g=$7;b=$2;f=$6;a=$1;d=$4;NF=4;print $1,$2,$4,g,b,f,a,d;$8=8;print ($6==\"\"),($7==\"\"),NF;$2=42;NF=2;print $0,$1,$2}"],
            [], "a b d g b f a d\n1 1 8\na 42 a 42\n"),
        Passing("nf-fractional-value-and-in-range-field-write",
            ["BEGIN{$0=\"a b c\";NF=1.8;print NF,typeof(NF),$1,$2,\"[\"$0\"]\";$1=\"z\";print NF,\"[\"$0\"]\"}"],
            [], "1.8 number a  [a]\n1.8 [z]\n"),
        Passing("nf-string-fraction-and-postfix-update",
            ["BEGIN{$0=\"a b c\";NF=\"02.8\";print NF,typeof(NF),\"[\"$0\"]\";print NF++;print NF,typeof(NF),\"[\"$0\"]\"}"],
            [], "02.8 string [a b]\n2.8\n3.8 number [a b ]\n"),
        Passing("nf-prefix-update-uses-fractional-value",
            ["BEGIN{$0=\"a b\";NF=1.8;print ++NF,NF,\"[\"$0\"]\";print --NF,NF,\"[\"$0\"]\"}"],
            [], "2.8 2.8 [a ]\n1.8 1.8 [a]\n"),
        Passing("nf-compound-update-uses-fractional-value",
            ["BEGIN{$0=\"a b c\";NF=1.8;NF+=1;print NF,\"[\"$0\"]\";NF*=2;print NF,\"[\"$0\"]\";NF/=2;print NF,\"[\"$0\"]\"}"],
            [], "2.8 [a ]\n5.6 [a    ]\n2.8 [a ]\n"),
        Passing("nf-negative-fraction-truncates-to-zero-fields",
            ["BEGIN{$0=\"a b c\";NF=-0.5;print NF,typeof(NF),\"[\"$0\"]\";print NF++,NF,\"[\"$0\"]\"}"],
            [], "-0.5 number []\n-0.5 0.5 []\n"),
        Passing("nf-nonnumeric-string-retains-type-until-update",
            ["BEGIN{$0=\"a b c\";NF=\"foo\";print NF,typeof(NF),\"[\"$0\"]\";NF++;print NF,typeof(NF),\"[\"$0\"]\"}"],
            [], "foo string []\n1 number []\n"),
        Passing("nf-field-growth-resets-assigned-type",
            ["BEGIN{$0=\"a b c\";NF=\"01.8\";$1=\"z\";print NF,typeof(NF);$2=\"y\";print NF,typeof(NF),\"[\"$0\"]\"}"],
            [], "01.8 string\n2 number [z y]\n"),
        Passing("nf-zero-match-field-reference-growth-resets-fraction",
            ["BEGIN{$0=\"a b c\";NF=1.8;print $2,\"[\"$0\"]\",NF;print sub(/x/,\"\",$2);print NF,\"[\"$0\"]\"}"],
            [], " [a] 1.8\n0\n2 [a]\n"),
        Passing("nf-reassigning-record-resets-assigned-value",
            ["BEGIN{$0=\"a b c\";NF=1.8;print length($0),NF,typeof(NF);$0=$0;print NF,typeof(NF)}"],
            [], "1 1.8 number\n1 number\n"),
        Passing("nf-new-record-resets-assigned-value",
            ["{print NF,typeof(NF),$0;NF=\"1.8\";print NF,typeof(NF),$0}"],
            [], "2 number a b\n1.8 string a\n3 number c d e\n1.8 string c\n", "a b\nc d e\n"),
        Passing("nf-deferred-external-fraction-retains-numeric-string-type",
            ["{print $0}END{print NF,typeof(NF),\"[\"$0\"]\"}", "a.txt", "NF=01.8"],
            [File("/a.txt", "a b c\n")], "a b c\n01.8 strnum [a]\n"),
        Passing("nf-symtab-read-shares-computed-and-assigned-value",
            ["BEGIN{$0=\"a b c\";print SYMTAB[\"NF\"],NF;NF=\"01.8\";print SYMTAB[\"NF\"],typeof(SYMTAB[\"NF\"]);$2=\"z\";print SYMTAB[\"NF\"],NF}"],
            [], "3 3\n01.8 string\n2 2\n"),
        AwkGoldenCaseBuilder.Create("nf-symtab-write-shares-record-field-value",
            ["BEGIN{$0=\"a b c\";SYMTAB[\"NF\"]=1.8;print NF,SYMTAB[\"NF\"],\"[\"$0\"]\";SYMTAB[\"NF\"]++;print NF,\"[\"$0\"]\"}"], [])
            .WithStdOut("1.8 1.8 [a]\n2.8 [a ]\n")
            .WithNondeterminismNote("Lokad uses one shared NF value for SYMTAB and ordinary access. GNU 5.2.1 stale indirect writes are a classified reference divergence.")
            .Build()
    ];
}
