namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> SpecialScalarBindingCases() =>
    [
        Passing("special-cell-subsep-symbol-assignment",
            ["BEGIN{SUBSEP=\":\";a[1,2]=7;SYMTAB[\"SUBSEP\"]=\"!\";a[1,2]+=3;print a[\"1:2\"],a[\"1!2\"]}"], [], "7 3\n"),
        Passing("special-cell-ofs-reconstruction-before-symbol-assignment",
            ["BEGIN{$0=\"a b\";OFS=\"|\";$1=\"c\";SYMTAB[\"OFS\"]=\":\";print $0;print \"x\",\"y\"}"], [], "c|b\nx:y\n"),
        Passing("special-cell-numeric-ofs-follows-current-convfmt",
            ["BEGIN{CONVFMT=\"%.2f\";OFS=1.25;ORS=\"!\\n\";print \"a\",\"b\";CONVFMT=\"%.3f\";print \"c\",\"d\"}"], [], "a1.25b!\nc1.250d!\n"),
        Passing("special-cell-numeric-string-separator-keeps-spelling",
            ["-v", "sep=0007", "BEGIN{SUBSEP=sep;OFS=sep;a[1,2]=9;print (\"100072\" in a),\"x\",\"y\"}"], [], "10007x0007y\n")
    ];
}
