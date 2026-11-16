using System.Globalization;
using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkDecimalNumberTests
{
    [Fact]
    public void DecimalPrefixesPreserveValueBitsAndByteExtents()
    {
        (string Text, double Number, int End)[] cases =
        [
            ("7.25", 7.25, 4), ("0007.25", 7.25, 7), ("+.25", .25, 4),
            ("-.25tail", -.25, 4), ("1.25.5", 1.25, 4), ("1.25\0tail", 1.25, 4),
            ("1.25é", 1.25, 4), ("10e+", 10, 2), ("10E-foo", 10, 2),
            ("1.25e+", 1.25, 4), ("-7.25E-2tail", -.0725, 8),
            ("1e309tail", double.PositiveInfinity, 5), ("1e-9999!", 0, 7),
            ("-0.0", Math.CopySign(0, -1), 4), ("-0e-9999", Math.CopySign(0, -1), 8),
            ("999999999999999", 999999999999999, 15),
            ("999999999999999.5tail", 999999999999999.5, 17)
        ];
        foreach (var mode in Enum.GetValues<AwkNumberParsingMode>())
        foreach (var (text, number, end) in cases)
        foreach (var prefix in new[] { "", " \t" })
        {
            AwkNumberParser.Parse(Encoding.UTF8.GetBytes(prefix + text), mode, out var result);
            Assert.Equal(AwkNumberParseStatus.Number, result.Status);
            Assert.Equal(prefix.Length + end, result.End);
            Assert.Equal(BitConverter.DoubleToUInt64Bits(number), BitConverter.DoubleToUInt64Bits(result.Number));
        }
        foreach (var text in new[] { "", ".", ".e10", "+.", "-e1", "  +", "x1.25", "\u00a01.25" })
        {
            AwkNumberParser.Parse(Encoding.UTF8.GetBytes(text), AwkNumberParsingMode.Decimal, out var result);
            Assert.Equal(AwkNumberParseStatus.NoNumber, result.Status);
            Assert.Equal(0, result.End);
        }
    }

    [Fact]
    public void DecimalRoundTripsPreserveBinary64ThroughNumericPrefixes()
    {
        var random = new Random(517);
        var bytes = new byte[sizeof(double)];
        for (var index = 0; index < 2048; index++)
        {
            random.NextBytes(bytes);
            var number = BitConverter.ToDouble(bytes);
            if (!double.IsFinite(number)) continue;
            var spelling = number.ToString("R", CultureInfo.InvariantCulture);
            foreach (var mode in Enum.GetValues<AwkNumberParsingMode>())
            foreach (var suffix in new[] { "", ":tail", ",5", "\0tail", "é" })
            {
                AwkNumberParser.Parse(Encoding.UTF8.GetBytes(spelling + suffix), mode, out var result);
                Assert.Equal(AwkNumberParseStatus.Number, result.Status);
                Assert.Equal(spelling.Length, result.End);
                Assert.Equal(BitConverter.DoubleToUInt64Bits(number), BitConverter.DoubleToUInt64Bits(result.Number));
            }
        }
    }
}
