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
            ["-v", "sep=0007", "BEGIN{SUBSEP=sep;OFS=sep;a[1,2]=9;print (\"100072\" in a),\"x\",\"y\"}"], [], "10007x0007y\n"),
        Passing("special-cell-nested-getline-counter-spelling",
            ["function readnext(line,code){code=getline line;print code,NR,FNR,line,NF}function work(){readnext()}BEGIN{NR=\"0007\";FNR=\"2.8tail\";$0=\"seed fields\";work();print NR,FNR,$0;exit}"],
            [], expectedStdOut: "1 8 3 row values 2\n8 3 seed fields\n", standardInput: "row values\n"),
        Passing("special-cell-function-symbol-alias-updates-global-counter",
            ["function setcounter(symbols){symbols[\"NR\"]=\"0007\"}function readnext(line){getline line;print NR,FNR,line}BEGIN{setcounter(SYMTAB);readnext();exit}"],
            [], expectedStdOut: "8 1 row values\n", standardInput: "row values\n"),
        Passing("special-cell-local-symbol-table-alias-keeps-own-name",
            ["function work(SYMTAB,line){SYMTAB[\"NR\"]=\"008\";getline line;print NR,FNR,SYMTAB[\"NR\"],line}BEGIN{p[\"NR\"]=41;work(p);print p[\"NR\"],NR;exit}"],
            [], expectedStdOut: "1 1 008 row values\n008 1\n", standardInput: "row values\n"),
        Passing("special-cell-function-case-assignment-and-symbol-write",
            ["function equal(){return (\"A\"==\"a\")+10*index(\"ABC\",\"b\")+100*(\"ABC\"~/b/)}function change(v){IGNORECASE=v;return equal()}BEGIN{print change(1);SYMTAB[\"IGNORECASE\"]=0;print equal()}"],
            [], "121\n0\n"),
        Passing("special-cell-posix-disabled-locals-survive-getline",
            ["--posix", "function work(IGNORECASE,RT,line){IGNORECASE=1;getline line;print (\"A\"==\"a\"),index(\"ABC\",\"b\"),RT,IGNORECASE,NR}BEGIN{RT=\"global\";work(0,\"local\");print RT;exit}"],
            [], expectedStdOut: "0 0 local 1 1\nglobal\n", standardInput: "row\n"),
        Passing("special-cell-traditional-disabled-locals-survive-getline",
            ["--traditional", "function work(IGNORECASE,RT,line){IGNORECASE=1;getline line;print (\"A\"==\"a\"),index(\"ABC\",\"b\"),RT,IGNORECASE,NR}BEGIN{RT=\"global\";work(0,\"local\");print RT;exit}"],
            [], expectedStdOut: "0 0 local 1 1\nglobal\n", standardInput: "row\n")
    ];
}
