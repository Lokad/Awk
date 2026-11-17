namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ValueValidationRepairCases()
    {
        yield return Passing("function-table-includes-special-form-functions",
            ["function userfn(){}BEGIN{print(\"sub\" in FUNCTAB),(\"gsub\" in FUNCTAB),(\"split\" in FUNCTAB),(\"patsplit\" in FUNCTAB),(\"match\" in FUNCTAB),(\"asort\" in FUNCTAB),(\"asorti\" in FUNCTAB),(\"isarray\" in FUNCTAB),(\"typeof\" in FUNCTAB);print(\"getline\" in FUNCTAB),(\"print\" in FUNCTAB),(\"printf\" in FUNCTAB),(\"userfn\" in FUNCTAB)}"], [],
            "1 1 1 1 1 1 1 1 1\n0 0 0 1\n");
        yield return Passing("match-capture-arrays-keep-input-number-origins",
            ["BEGIN{match(\"0007 -0.25 7tail\",/^([^ ]+) ([^ ]+) ([^ ]+)$/,a);print typeof(a[0]),typeof(a[1]),a[1],(a[1]==7);print typeof(a[2]),a[2],(a[2]==-0.25);print typeof(a[3]),a[3],(a[3]==7)}"], [],
            "string strnum 0007 1\nstrnum -0.25 1\nstring 7tail 0\n");
        yield return CompilerDiagnostic(Failing("computed-field-rejects-posix-match-captures-before-effects",
            ["--posix", "BEGIN{print \"before\";print $(match(\"ab\",/a/,a))}"], [], 2, "",
            "awk: third argument of match is a GNU extension\n"));
        yield return CompilerDiagnostic(Failing("computed-field-validates-builtin-arity-before-effects",
            ["BEGIN{print \"before\";print $(substr(\"a\"))}"], [], 2, "",
            "awk: invalid number of arguments for substr\n"));
        yield return Passing("computed-field-calls-keep-single-evaluation",
            ["function idx(){n++;return 1}BEGIN{$0=\"A B\";print $(idx()),n}"], [], "A 1\n");
    }
}
