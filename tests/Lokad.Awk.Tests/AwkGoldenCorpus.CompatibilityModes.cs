namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] CompatibilityModeCases() =>
    [
        CompilerDiagnostic(Failing(
            "posix-gensub-is-undefined",
            ["--posix", "BEGIN{print gensub(\"a\", \"b\", \"g\", \"a\")}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: gensub\n")),

        Passing(
            "gnu-source-numeric-literals",
            ["BEGIN{print 010, 0x10, 09, 010.5, 0010}"],
            [],
            "8 16 9 10.5 8\n"),

        Passing(
            "posix-source-numeric-literals",
            ["--posix", "BEGIN{print 010, 0x10, 09, 010.5, 0010}"],
            [],
            "10 0 9 10.5 10\n"),

        Passing(
            "traditional-source-numeric-literals",
            ["--traditional", "BEGIN{print 010, 0x10, 09, 010.5, 0010}"],
            [],
            "10 0 9 10.5 10\n"),

        Passing(
            "gnu-field-numeric-literals",
            ["BEGIN{$0=\"a b c d e f g h i j\"; print $010, $08, $0x2}"],
            [],
            "h h b\n"),

        Passing(
            "posix-field-numeric-literals",
            ["--posix", "BEGIN{$0=\"a b c d e f g h i j\"; print $010, $08, $0x2}"],
            [],
            "j h a b c d e f g h i j\n"),

        Passing(
            "traditional-field-numeric-literals",
            ["--traditional", "BEGIN{$0=\"a b c d e f g h i j\"; print $010, $08, $0x2}"],
            [],
            "j h a b c d e f g h i j\n"),

        Passing(
            "default-input-non-decimal-data-is-decimal",
            ["{print $1+0, typeof($1)}"],
            [],
            "10 strnum\n0 string\n",
            "010\n0x10\n"),

        Passing(
            "non-decimal-data-input-values",
            ["--non-decimal-data", "{print $1+0, typeof($1)}"],
            [],
            "8 strnum\n16 strnum\n9 strnum\n10.5 strnum\n",
            "010\n0x10\n09\n010.5\n"),

        Passing(
            "posix-input-hex-values",
            ["--posix", "{print $1+0}"],
            [],
            "10\n16\n",
            "010\n0x10\n"),

        Passing(
            "traditional-input-values-remain-decimal",
            ["--traditional", "{print $1+0}"],
            [],
            "10\n0\n",
            "010\n0x10\n"),

        Passing(
            "non-decimal-data-string-conversion",
            ["--non-decimal-data", "BEGIN{x=\"010\"; y=\"0x10\"; print x+0, typeof(x), y+0, typeof(y)}"],
            [],
            "8 string 16 string\n"),

        Passing(
            "posix-string-hex-conversion",
            ["--posix", "BEGIN{x=\"0x10\"; print x+0}"],
            [],
            "16\n"),

        Passing(
            "traditional-string-hex-remains-decimal-prefix",
            ["--traditional", "BEGIN{x=\"0x10\"; print x+0}"],
            [],
            "0\n"),

        Passing(
            "non-decimal-data-variable-assignment",
            ["--non-decimal-data", "-v", "v=010", "BEGIN{print v+0, typeof(v)}"],
            [],
            "8 strnum\n"),

        CompilerDiagnostic(Failing(
            "traditional-strtonum-is-undefined",
            ["--traditional", "BEGIN{print strtonum(\"10\")}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: strtonum\n")),

        CompilerDiagnostic(Failing(
            "posix-bit-function-is-undefined",
            ["--posix", "BEGIN{print and(1, 1)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: and\n")),

        CompilerDiagnostic(Failing(
            "posix-typeof-is-undefined",
            ["--posix", "BEGIN{print typeof(1)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: typeof\n")),

        CompilerDiagnostic(Failing(
            "traditional-isarray-is-undefined",
            ["--traditional", "BEGIN{print isarray(a)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: isarray\n")),

        CompilerDiagnostic(Failing(
            "posix-patsplit-is-undefined",
            ["--posix", "BEGIN{print patsplit(\"a\", a)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: patsplit\n")),

        CompilerDiagnostic(Failing(
            "traditional-asort-is-undefined",
            ["--traditional", "BEGIN{a[1]=1; print asort(a)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: asort\n")),

        CompilerDiagnostic(Failing(
            "posix-asorti-is-undefined",
            ["--posix", "BEGIN{a[1]=1; print asorti(a)}"],
            [],
            2,
            string.Empty,
            "awk: undefined function: asorti\n"))
    ];
}
