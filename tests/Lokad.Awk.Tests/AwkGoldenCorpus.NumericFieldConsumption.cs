namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> NumericFieldConsumptionCases()
    {
        yield return Passing("numeric-field-consumption-prefix-and-spelling",
            ["{a[\"x\"]+=$2;print a[\"x\"],$2,typeof($2)}"], [], "7 007 strnum\n12 5tail string\n12 café string\n", "k 007\nk 5tail\nk café\n");
        yield return Passing("numeric-field-consumption-operations",
            ["BEGIN{$0=\"key 2\";a[1]=8;a[1]+=$2;a[1]-=$2;a[1]*=$2;a[1]/=$2;a[1]%=$2;a[1]^=$2;print a[1]}"], [], "0\n");
        yield return Passing("numeric-field-consumption-cached-modified-and-record-zero",
            ["BEGIN{$0=\"007 tail\";v=$1;a[1]+=$1;$1=2.5;a[1]+=$1;CONVFMT=\"%.4f\";print a[1],$1;$0=3.5;a[1]+=$0;print a[1]}"], [], "9.5 2.5\n13\n");
        yield return Passing("numeric-field-consumption-target-before-index-effects",
            ["function change(){a[\"x\"]=100;$0=\"key 3\";return 2} BEGIN{a[\"x\"]=7;$0=\"key 1\";a[\"x\"]+=$(change());print a[\"x\"],$0}"], [], "10 key 3\n");
        yield return Passing("numeric-field-consumption-empty-and-out-of-range",
            ["BEGIN{FS=\":\";$0=\"k::3\";a[1]=7;a[1]+=$2;a[1]+=$4;print a[1];a[1]+=$3;print a[1]}"], [], "7\n10\n");
        const string script = "{a[1]+=$2;print a[1],$2}";
        yield return Passing("numeric-field-consumption-decimal", [script], [], "0 0x10\n7 007\n", "k 0x10\nk 007\n");
        yield return Passing("numeric-field-consumption-posix", ["--posix", script], [], "16 0x10\n23 007\n", "k 0x10\nk 007\n");
        yield return Passing("numeric-field-consumption-nondecimal", ["--non-decimal-data", script], [], "16 0x10\n23 007\n", "k 0x10\nk 007\n");
    }
}
