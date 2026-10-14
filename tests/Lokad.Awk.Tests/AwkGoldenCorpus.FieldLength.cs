namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FieldLengthCases()
    {
        yield return Passing("field-length-input-text-and-empty-fields",
            ["BEGIN{$0=\"0007 alpha\";i=1;print length($(i++)),i,typeof($1);print length($1),$1;FS=\":\";$0=\"a::xyz:\";print length($1),length($2),length($3),length($4),length($5),NF}"],
            [], "4 2 strnum\n4 0007\n1 0 3 0 0 4\n");
        const string unicode = "BEGIN{$0=\"café 😀\";i=1;print length($(i++)),length($i),length($(i+1)),length($0);$2=\"é\";print length($2),$0}";
        yield return Passing("field-length-unicode-and-dirty-record", [unicode], [], "4 1 0 6\n1 café é\n");
        yield return Passing("field-length-byte-mode-and-dirty-record", ["-b", unicode], [], "5 4 0 10\n2 café é\n");
        yield return Passing("field-length-numeric-format-and-extended-nf",
            ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b c\";$1=1.25;i=1;print length($i),length($2);CONVFMT=\"%.4f\";print length($1);$2=\"é\";NF=4;print length($2),length($3),length($4),length($5);OFS=\"|\";print length($0),$0}"],
            [], "4 1\n6\n1 1 0 0\n11|1.2500 é c \n");
        yield return Passing("field-length-index-effects-change-record",
            ["function change(){$0=\"after next\";return 2} BEGIN{$0=\"before\";print length($(change())),NF,$0}"],
            [], "4 2 after next\n");
    }
}
