namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private const string ConversionEmitter = "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} ";

    private static IEnumerable<AwkGoldenCase> StrtonumConversionCases()
    {
        ConversionMatrixRow[] common =
        [
            new("\"0\"", "0|", "[0x0p+0] 1 0 0\n", "[0x0p+0] 1 0 0\n1\n"),
            new("\"-0\"", "-0|", "[-0x0p+0] 1 0 0\n", "[0x0p+0] 1 0 0\n1\n"),
            new("\"+0\"", "+0|", "[0x0p+0] 1 0 0\n", "[0x0p+0] 1 0 0\n1\n"),
            new("\"012\"", "012|", "[0x1.4p+3] 0 0 1\n", "[0x1.8p+3] 0 0 1\n1\n",
                RecordBuiltin: "[0x1.8p+3] 0 0 1\n", NonDecimalArithmetic: "[0x1.4p+3] 0 0 1\n1\n"),
            new("\"+012\"", "+012|", "[0x1.8p+3] 0 0 1\n", "[0x1.8p+3] 0 0 1\n1\n"),
            new("\"-012\"", "-012|", "[-0x1.8p+3] 0 1 0\n", "[-0x1.8p+3] 0 1 0\n1\n"),
            new("\" 012\"", " 012|", "[0x1.8p+3] 0 0 1\n", "[0x1.8p+3] 0 0 1\n1\n",
                NonDecimalBuiltin: "[0x1.4p+3] 0 0 1\n", NonDecimalArithmetic: "[0x1.4p+3] 0 0 1\n1\n"),
            new("\"\\v012\\f\"", "\u000b012\f|", "[0x1.8p+3] 0 0 1\n", "[0x1.8p+3] 0 0 1\n1\n",
                NonDecimalBuiltin: "[0x1.4p+3] 0 0 1\n", NonDecimalArithmetic: "[0x1.4p+3] 0 0 1\n1\n"),
            new("\"0109\"", "0109|", "[0x1.b4p+6] 0 0 1\n", "[0x1.b4p+6] 0 0 1\n1\n"),
            new("\"018\"", "018|", "[0x1.2p+4] 0 0 1\n", "[0x1.2p+4] 0 0 1\n1\n"),
            new("\"089\"", "089|", "[0x1.64p+6] 0 0 1\n", "[0x1.64p+6] 0 0 1\n1\n"),
            new("\"012e2\"", "012e2|", "[0x1.2cp+10] 0 0 1\n", "[0x1.2cp+10] 0 0 1\n1\n"),
            new("\"012e\"", "012e|", "[0x1.8p+3] 0 0 1\n", "[0x1.8p+3] 0 0 1\n0\n"),
            new("\"012.5\"", "012.5|", "[0x1.9p+3] 0 0 1\n", "[0x1.9p+3] 0 0 1\n1\n"),
            new("\"012z\"", "012z|", "[0x1.4p+3] 0 0 1\n", "[0x1.8p+3] 0 0 1\n0\n",
                NonDecimalArithmetic: "[0x1.4p+3] 0 0 1\n0\n"),
            new("\"012z.5\"", "012z.5|", "[0x1.4p+3] 0 0 1\n", "[0x1.8p+3] 0 0 1\n0\n",
                NonDecimalArithmetic: "[0x1.4p+3] 0 0 1\n0\n"),
            new("\"0x10\"", "0x10|", "[0x1p+4] 0 0 1\n", "[0x0p+0] 1 0 0\n0\n",
                NonDecimalArithmetic: "[0x1p+4] 0 0 1\n1\n", PosixArithmetic: "[0x1p+4] 0 0 1\n1\n"),
            new("\"0Xff\"", "0Xff|", "[0x1.fep+7] 0 0 1\n", "[0x0p+0] 1 0 0\n0\n",
                NonDecimalArithmetic: "[0x1.fep+7] 0 0 1\n1\n", PosixArithmetic: "[0x1.fep+7] 0 0 1\n1\n"),
            new("\"+0x10\"", "+0x10|", "[0x0p+0] 1 0 0\n", "[0x0p+0] 1 0 0\n0\n",
                NonDecimalBuiltin: "[0x1p+4] 0 0 1\n", NonDecimalArithmetic: "[0x1p+4] 0 0 1\n1\n", PosixArithmetic: "[0x1p+4] 0 0 1\n1\n"),
            new("\"-0x10\"", "-0x10|", "[0x0p+0] 1 0 0\n", "[0x0p+0] 1 0 0\n0\n",
                NonDecimalBuiltin: "[-0x1p+4] 0 1 0\n", NonDecimalArithmetic: "[-0x1p+4] 0 1 0\n1\n", PosixArithmetic: "[-0x1p+4] 0 1 0\n1\n"),
        ];
        for (var index = 0; index < common.Length; index++)
        {
            var row = common[index];
            var suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var literal = "BEGIN{emit(strtonum(" + row.Expression + "))}";
            const string record = "BEGIN{RS=\"|\"}{emit(strtonum($0))}";
            const string text = "BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}";
            const string arithmetic = "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}";
            var nonDecimal = row.NonDecimalBuiltin ?? row.Builtin;

            yield return MakeConversion($"literal-gnu-{suffix}", null, literal, null, row.Builtin);
            yield return MakeConversion($"record-gnu-{suffix}", null, record, row.Input, row.RecordBuiltin ?? row.Builtin);
            yield return MakeConversion($"string-gnu-{suffix}", null, text, row.Input, row.Builtin);
            yield return MakeConversion($"literal-nondecimal-{suffix}", "--non-decimal-data", literal, null, nonDecimal);
            yield return MakeConversion($"record-nondecimal-{suffix}", "--non-decimal-data", record, row.Input, nonDecimal);
            yield return MakeConversion($"string-nondecimal-{suffix}", "--non-decimal-data", text, row.Input, nonDecimal);
            yield return MakeConversion($"conversion-gnu-{suffix}", null, arithmetic, row.Input, row.Arithmetic);
            yield return MakeConversion($"conversion-nondecimal-{suffix}", "--non-decimal-data", arithmetic, row.Input,
                row.NonDecimalArithmetic ?? row.Arithmetic);
            yield return MakeConversion($"conversion-traditional-{suffix}", "--traditional", arithmetic, row.Input, row.Arithmetic);
            yield return MakeConversion($"conversion-posix-{suffix}", "--posix", arithmetic, row.Input, row.PosixArithmetic ?? row.Arithmetic);
        }

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-20",
                [ConversionEmitter + "BEGIN{emit(strtonum(\" 0x10\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-20",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\" 0x10\"))}"],
                [])
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-20",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-20",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-20",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-20",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-21",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"\\v0x10\\f\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-21",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"\\v0x10\\f\"))}"],
                [])
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-21",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b0x10\f|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-21",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b0x10\f|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-21",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b0x10\f|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-21",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b0x10\f|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-22",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x10z\"))}"],
                [])
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-22",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x10z\"))}"],
                [])
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-22",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10z|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-22",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10z|")
            .WithStdOut("[0x1p+4] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-22",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10z|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-22",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10z|")
            .WithStdOut("[0x1p+4] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-23",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-23",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-23",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-23",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-23",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-23",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-24",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0xz\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-24",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0xz\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-24",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-24",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-24",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-24",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-25",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x.\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-25",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x.\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-25",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-25",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.|")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-25",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-25",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-26",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x.p2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-26",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x.p2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-26",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-26",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-26",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-26",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-27",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0xp2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-27",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0xp2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-27",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-27",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-27",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-27",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-28",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1p2\"))}"],
                [])
            .WithStdOut("[0x1p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-28",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1p2\"))}"],
                [])
            .WithStdOut("[0x1p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-28",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-28",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p2|")
            .WithStdOut("[0x1p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-28",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-28",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p2|")
            .WithStdOut("[0x1p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-29",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8p2\"))}"],
                [])
            .WithStdOut("[0x1.8p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-29",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8p2\"))}"],
                [])
            .WithStdOut("[0x1.8p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-29",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-29",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-29",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-29",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-30",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-30",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-30",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-30",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8|")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-30",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-30",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-31",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8p\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-31",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8p\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-31",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-31",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-31",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-31",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-32",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8p+\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-32",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8p+\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-32",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p+|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-32",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p+|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-32",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p+|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-32",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p+|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-33",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8p-\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-33",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8p-\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-33",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p-|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-33",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p-|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-33",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p-|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-33",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p-|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-34",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8p2tail\"))}"],
                [])
            .WithStdOut("[0x1.8p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-34",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8p2tail\"))}"],
                [])
            .WithStdOut("[0x1.8p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-34",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2tail|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-34",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2tail|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-34",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2tail|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-34",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2tail|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-35",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8xyzp2\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-35",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8xyzp2\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-35",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyzp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-35",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyzp2|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-35",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyzp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-35",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyzp2|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-36",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8xyz\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-36",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.8xyz\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-36",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-36",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyz|")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-36",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-36",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyz|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-37",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"+0x1.8\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-37",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"+0x1.8\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-37",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-37",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-37",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-37",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-38",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"-0x1.8\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-38",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"-0x1.8\"))}"],
                [])
            .WithStdOut("[-0x1.8p+0] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-38",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-38",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8|")
            .WithStdOut("[-0x1.8p+0] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-38",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-38",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8|")
            .WithStdOut("[-0x1.8p+0] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-39",
                [ConversionEmitter + "BEGIN{emit(strtonum(\" 0x1.8\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-39",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\" 0x1.8\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-39",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-39",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x1.8|")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-39",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-39",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x1.8|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-40",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"+0x1.8p2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-40",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"+0x1.8p2\"))}"],
                [])
            .WithStdOut("[0x1.8p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-40",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-40",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8p2|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-40",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-40",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8p2|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-41",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"-0x1.8p2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-41",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"-0x1.8p2\"))}"],
                [])
            .WithStdOut("[-0x1.8p+2] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-41",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-41",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8p2|")
            .WithStdOut("[-0x1.8p+2] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-41",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-41",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8p2|")
            .WithStdOut("[-0x1.8p+2] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-42",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x.8p1\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-42",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x.8p1\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-42",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.8p1|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-42",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.8p1|")
            .WithStdOut("[0x1p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-42",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.8p1|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-42",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.8p1|")
            .WithStdOut("[0x1p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-43",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0Xf.FP-2\"))}"],
                [])
            .WithStdOut("[0x1.fep+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-43",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0Xf.FP-2\"))}"],
                [])
            .WithStdOut("[0x1.fep+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-43",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xf.FP-2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-43",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xf.FP-2|")
            .WithStdOut("[0x1.fep+1] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-43",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xf.FP-2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-43",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xf.FP-2|")
            .WithStdOut("[0x1.fep+1] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-44",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-44",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-44",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1074|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-44",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1074|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-44",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1074|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-44",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1074|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-45",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1p-1075\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-45",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1p-1075\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-45",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-45",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-45",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-45",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-46",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.0000000000001p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-46",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.0000000000001p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-46",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.0000000000001p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-46",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.0000000000001p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-46",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.0000000000001p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-46",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.0000000000001p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-47",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffffp1023\"))}"],
                [])
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-47",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffffp1023\"))}"],
                [])
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-47",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffffp1023|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-47",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffffp1023|")
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-47",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffffp1023|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-47",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffffp1023|")
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-48",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff8p1023\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-48",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff8p1023\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-48",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1023|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-48",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1023|")
            .WithStdOut("+inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-48",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1023|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-48",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1023|")
            .WithStdOut("+inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-49",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"1.2x\"))}"],
                [])
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-49",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"1.2x\"))}"],
                [])
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-49",
                [ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("1.2x|")
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-49",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("1.2x|")
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-49",
                ["--traditional", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("1.2x|")
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-49",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("1.2x|")
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-50",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"1e2x\"))}"],
                [])
            .WithStdOut("[0x1.9p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-50",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"1e2x\"))}"],
                [])
            .WithStdOut("[0x1.9p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-51",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"1e\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-51",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"1e\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-52",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"1e+\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-52",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"1e+\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-53",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"1e-\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-53",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"1e-\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-54",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"1e+2x\"))}"],
                [])
            .WithStdOut("[0x1.9p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-54",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"1e+2x\"))}"],
                [])
            .WithStdOut("[0x1.9p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-55",
                [ConversionEmitter + "BEGIN{emit(strtonum(\".5x\"))}"],
                [])
            .WithStdOut("[0x1p-1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-55",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\".5x\"))}"],
                [])
            .WithStdOut("[0x1p-1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-56",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"+.5z\"))}"],
                [])
            .WithStdOut("[0x1p-1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-56",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"+.5z\"))}"],
                [])
            .WithStdOut("[0x1p-1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-57",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"-2.5z\"))}"],
                [])
            .WithStdOut("[-0x1.4p+1] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-57",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"-2.5z\"))}"],
                [])
            .WithStdOut("[-0x1.4p+1] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-58",
                [ConversionEmitter + "BEGIN{emit(strtonum(\" 12abc \"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-58",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\" 12abc \"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-59",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"\\t12abc\\r\"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-59",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"\\t12abc\\r\"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-60",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"+inf\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-60",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"+inf\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-61",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"-inf\"))}"],
                [])
            .WithStdOut("-inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-61",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"-inf\"))}"],
                [])
            .WithStdOut("-inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-62",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"inf\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-62",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"inf\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-63",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"Infinity\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-63",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"Infinity\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-64",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"+nan\"))}"],
                [])
            .WithStdOut("nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-64",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"+nan\"))}"],
                [])
            .WithStdOut("nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-65",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"-NaN\"))}"],
                [])
            .WithStdOut("nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-65",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"-NaN\"))}"],
                [])
            .WithStdOut("nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-66",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"nan\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-66",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"nan\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-67",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"+infx\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-67",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"+infx\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-68",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"+nan(payload)\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-68",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"+nan(payload)\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-69",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-69",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-70",
                [ConversionEmitter + "BEGIN{emit(strtonum(\" \"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-70",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\" \"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-71",
                [ConversionEmitter + "BEGIN{emit(strtonum(\".\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-71",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\".\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-72",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"+\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-72",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"+\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-73",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"-\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-73",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"-\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-74",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"\u00a012\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-74",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{emit(strtonum(\"\u00a012\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-0",
                [ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=1/3;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x1.5555555555555p-2] 0 0 1\n[0x1.5555555555555p-2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-0",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=1/3;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x1.5555555555555p-2] 0 0 1\n[0x1.5555555555555p-2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-1",
                [ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=-0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[-0x0p+0] 1 0 0\n[-0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-1",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=-0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[-0x0p+0] 1 0 0\n[-0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-2",
                [ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-2",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-3",
                [ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=2^60;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x1p+60] 0 0 1\n[0x1p+60] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-3",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=2^60;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x1p+60] 0 0 1\n[0x1p+60] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-4",
                [ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=-2^60;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[-0x1p+60] 0 1 0\n[-0x1p+60] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-4",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=-2^60;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[-0x1p+60] 0 1 0\n[-0x1p+60] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-5",
                [ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=4.9406564584124654e-324;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-5",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=4.9406564584124654e-324;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-6",
                [ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=\"+nan\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("nan\nnan\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-6",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=\"+nan\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("nan\nnan\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-7",
                [ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=\"-nan\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("nan\nnan\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-7",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=\"-nan\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("nan\nnan\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-8",
                [ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=\"+inf\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("+inf\n+inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-8",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=\"+inf\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("+inf\n+inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-9",
                [ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=\"-inf\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("-inf\n-inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-9",
                ["--non-decimal-data", ConversionEmitter + "BEGIN{CONVFMT=\"%.1f\";x=\"-inf\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("-inf\n-inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-once",
                [ConversionEmitter + "function f(){i++;return 1/3}BEGIN{emit(strtonum(f()));print i}"],
                [])
            .WithStdOut("[0x1.5555555555555p-2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-unassigned",
                [ConversionEmitter + "BEGIN{emit(strtonum(x));print x==\"\"}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-fatal-after-output",
                ["BEGIN{print \"before\";x=strtonum(\"0x1.8\");print \"bad\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-0",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print s}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("0x1.8\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-1",
                ["--non-decimal-data", "BEGIN{print \"0x1.8\"}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("0x1.8\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-2",
                ["--non-decimal-data", "{print $0}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("0x1.8\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-3",
                ["--non-decimal-data", "{x=$0;print \"okay\"}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("okay\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-4",
                ["--non-decimal-data", "{print length($0)}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("5\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-5",
                ["--non-decimal-data", "{print typeof($0)}"],
                [])
            .WithStandardInput("0x1.8\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-6",
                ["--non-decimal-data", "{print $0==\"0x1.8\"}"],
                [])
            .WithStandardInput("0x1.8\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-7",
                ["--non-decimal-data", "{if($0)print \"okay\"}"],
                [])
            .WithStandardInput("0x1.8\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-8",
                ["--non-decimal-data", "{printf \"%s\\n\",$0}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("0x1.8\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-9",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print length(s)}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("5\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-10",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print(s==\"0x1.8\")}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-11",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";if(s)print \"okay\"}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("okay\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-12",
                ["--non-decimal-data", "BEGIN{print strtonum(\"+0x1.8\")}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("1.5\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-0",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-0",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-1",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-1",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p-1074|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-2",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008p-1023\"))}"],
                [])
            .WithStdOut("[0x0.8p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-2",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p-1023|")
            .WithStdOut("[0x0.8p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-3",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008p-1022\"))}"],
                [])
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-3",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p-1022|")
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-4",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008p0\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-4",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p0|")
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-5",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008p1023\"))}"],
                [])
            .WithStdOut("[0x1p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-5",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p1023|")
            .WithStdOut("[0x1p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-6",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-6",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p1024|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-7",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-7",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-8",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-8",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p-1074|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-9",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p-1023\"))}"],
                [])
            .WithStdOut("[0x0.8p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-9",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p-1023|")
            .WithStdOut("[0x0.8p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-10",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p-1022\"))}"],
                [])
            .WithStdOut("[0x1.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-10",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p-1022|")
            .WithStdOut("[0x1.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-11",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p0\"))}"],
                [])
            .WithStdOut("[0x1.0000000000001p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-11",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p0|")
            .WithStdOut("[0x1.0000000000001p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-12",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p1023\"))}"],
                [])
            .WithStdOut("[0x1.0000000000001p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-12",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p1023|")
            .WithStdOut("[0x1.0000000000001p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-13",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-13",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p1024|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-14",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000018p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-14",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-15",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000018p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-15",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p-1074|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-16",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000018p-1023\"))}"],
                [])
            .WithStdOut("[0x0.8000000000001p-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-16",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p-1023|")
            .WithStdOut("[0x0.8000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-17",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000018p-1022\"))}"],
                [])
            .WithStdOut("[0x1.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-17",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p-1022|")
            .WithStdOut("[0x1.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-18",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000018p0\"))}"],
                [])
            .WithStdOut("[0x1.0000000000002p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-18",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p0|")
            .WithStdOut("[0x1.0000000000002p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-19",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000018p1023\"))}"],
                [])
            .WithStdOut("[0x1.0000000000002p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-19",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p1023|")
            .WithStdOut("[0x1.0000000000002p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-20",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.00000000000018p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-20",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p1024|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-21",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff7p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-21",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-22",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff7p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-22",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p-1074|")
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-23",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff7p-1023\"))}"],
                [])
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-23",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p-1023|")
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-24",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff7p-1022\"))}"],
                [])
            .WithStdOut("[0x1.fffffffffffffp-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-24",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p-1022|")
            .WithStdOut("[0x1.fffffffffffffp-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-25",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff7p0\"))}"],
                [])
            .WithStdOut("[0x1.fffffffffffffp+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-25",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p0|")
            .WithStdOut("[0x1.fffffffffffffp+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-26",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff7p1023\"))}"],
                [])
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-26",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p1023|")
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-27",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff7p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-27",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p1024|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-28",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff8p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-28",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-29",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff8p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-29",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p-1074|")
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-30",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff8p-1023\"))}"],
                [])
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-30",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p-1023|")
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-31",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff8p-1022\"))}"],
                [])
            .WithStdOut("[0x1p-1021] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-31",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p-1022|")
            .WithStdOut("[0x1p-1021] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-32",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff8p0\"))}"],
                [])
            .WithStdOut("[0x1p+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-32",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p0|")
            .WithStdOut("[0x1p+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-33",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1023|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-34",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff8p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-34",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1024|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-35",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-35",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-36",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-36",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p-1074|")
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-37",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p-1023\"))}"],
                [])
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-37",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p-1023|")
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-38",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p-1022\"))}"],
                [])
            .WithStdOut("[0x1p-1021] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-38",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p-1022|")
            .WithStdOut("[0x1p-1021] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-39",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p0\"))}"],
                [])
            .WithStdOut("[0x1p+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-39",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p0|")
            .WithStdOut("[0x1p+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-40",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p1023\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-40",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p1023|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-41",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-41",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p1024|")
            .WithStdOut("+inf\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-199",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x30000002222225p-1077\"))}"],
                [])
            .WithStdOut("[0x0.6000000444445p-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-199",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x30000002222225p-1077|")
            .WithStdOut("[0x0.6000000444445p-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-200",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x0.7fffffffffffeap-1022\"))}"],
                [])
            .WithStdOut("[0x0.7ffffffffffffp-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-200",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x0.7fffffffffffeap-1022|")
            .WithStdOut("[0x0.7ffffffffffffp-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-201",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x0.7fffffffffffe9p-1022\"))}"],
                [])
            .WithStdOut("[0x0.7ffffffffffffp-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-201",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x0.7fffffffffffe9p-1022|")
            .WithStdOut("[0x0.7ffffffffffffp-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-202",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"0x0.70000000000002p-1022\"))}"],
                [])
            .WithStdOut("[0x0.7p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-202",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x0.70000000000002p-1022|")
            .WithStdOut("[0x0.7p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-203",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"-0x30000002222225p-1077\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-203",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("-0x30000002222225p-1077|")
            .WithStdOut("[-0x0.6000000444445p-1022] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-204",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"-0x0.7fffffffffffeap-1022\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-204",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("-0x0.7fffffffffffeap-1022|")
            .WithStdOut("[-0x0.7ffffffffffffp-1022] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-205",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"-0x0.7fffffffffffe9p-1022\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-205",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("-0x0.7fffffffffffe9p-1022|")
            .WithStdOut("[-0x0.7ffffffffffffp-1022] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-206",
                [ConversionEmitter + "BEGIN{emit(strtonum(\"-0x0.70000000000002p-1022\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-206",
                ["--posix", ConversionEmitter + "BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("-0x0.70000000000002p-1022|")
            .WithStdOut("[-0x0.7p-1022] 0 1 0\n")
            .Build();

    }

    private static AwkGoldenCase MakeConversion(string name, string? option, string program, string? input, string stdout)
    {
        var source = ConversionEmitter + program;
        var builder = AwkGoldenCaseBuilder.Create("conversion-" + name, option is null ? [source] : [option, source], []);
        if (input is not null) builder.WithStandardInput(input);
        return builder.WithStdOut(stdout).Build();
    }

    private readonly record struct ConversionMatrixRow(string Expression, string Input, string Builtin, string Arithmetic,
        string? RecordBuiltin = null, string? NonDecimalBuiltin = null, string? NonDecimalArithmetic = null,
        string? PosixArithmetic = null);
}
