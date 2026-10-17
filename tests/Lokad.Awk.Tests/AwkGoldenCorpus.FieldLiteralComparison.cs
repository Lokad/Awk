namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FieldLiteralComparisonCases()
    {
        yield return Passing("field-literal-comparison-keeps-numeric-special-space-and-utf8-text",
            ["BEGIN{FS=\":\";$0=\"007:nan:inf: \\t+1e2:😀\";print $1==\"007\",$1==\"7\",$2==\"nan\",$3==\"inf\",$4==\" \\t+1e2\",$5==\"😀\"}"],
            [], "1 0 1 1 1 1\n");
        yield return Passing("field-literal-comparison-all-operators",
            ["BEGIN{$0=\"alpha z\";print ($1==\"alpha\"),($1!=\"alpha\"),($1<\"z\"),($1<=\"alpha\"),($1>\"a\"),($1>=\"alpha\")}"],
            [], "1 0 1 1 1 1\n");
        yield return Passing("field-literal-comparison-input-spelling-and-numeric-origin",
            ["{print $1==\"0007\",$1==\"7\",$1<\"8\",$1<8;print typeof($1),$1}"],
            [], "1 0 1 1\nstrnum 0007\n", "0007 alpha\n");
        yield return Passing("field-literal-comparison-cache-mutation-and-empty-fields",
            ["BEGIN{$0=\"0007 alpha\";v=$1;print $1==\"0007\",v==\"0007\";$1=7;print $1==\"7\",$1==\"0007\";$1=\"0007\";print $1==\"0007\";NF=4;print $3==\"\",$4==\"\",$5==\"\"}"],
            [], "1 1\n1 0\n1\n1 1 1\n");
        yield return Passing("field-literal-comparison-modified-numeric-format-and-record-zero",
            ["BEGIN{CONVFMT=\"%.2f\";$0=\"old\";$1=1.25;print $1==\"1.25\",$1==\"1.2500\";CONVFMT=\"%.4f\";print $1==\"1.2500\";print $0==\"1.2500\"}"],
            [], "1 0\n1\n1\n");
        const string unicode = "BEGIN{$0=\"café 😀\";print ($1==\"café\"),($2==\"😀\"),($2>\"é\");FS=\":\";$0=\"::\";print $1==\"\",$2==\"\",$3==\"\",$4==\"\",NF}";
        yield return Passing("field-literal-comparison-unicode-and-empty-separators", [unicode], [], "1 1 1\n1 1 1 1 3\n");
        yield return Passing("field-literal-comparison-byte-mode", ["-b", unicode], [], "1 1 1\n1 1 1 1 3\n");
        yield return Passing("field-literal-comparison-ignorecase-changes",
            ["BEGIN{IGNORECASE=1;$0=\"Alpha\";print $1==\"alpha\";IGNORECASE=0;print $1==\"alpha\";$0=\"alpha\";print $1==\"alpha\"}"],
            [], "1\n0\n1\n");
        yield return Passing("field-literal-comparison-index-effect-and-short-circuit",
            ["function change(){$0=\"after next\";i++;return 2} BEGIN{$0=\"before\";i=0;print $(change())==\"next\",i,$0;print ($1==\"after\"&&++i),i;print ($1==\"never\"&&++i),i}"],
            [], "1 1 after next\n1 2\n0 2\n");
        yield return Passing("field-literal-comparison-effectful-right-keeps-left-snapshot",
            ["function change(){$0=\"after next\";return \"old\"} BEGIN{$0=\"old rest\";print $1==change(),$0}"],
            [], "1 after next\n");
        const string classification = "{print $1==\"007\",$2==\"0x10\",$2==16}";
        yield return Passing("field-literal-comparison-decimal", [classification], [], "1 1 0\n", "007 0x10\n");
        yield return Passing("field-literal-comparison-posix", ["--posix", classification], [], "1 1 1\n", "007 0x10\n");
        yield return Passing("field-literal-comparison-traditional", ["--traditional", classification], [], "1 1 0\n", "007 0x10\n");
        yield return Passing("field-literal-comparison-nondecimal", ["--non-decimal-data", classification], [], "1 1 1\n", "007 0x10\n");
    }
}
