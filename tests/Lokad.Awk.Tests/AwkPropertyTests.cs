using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkPropertyTests
{
    [Fact]
    public async Task Awk_GeneratedArithmeticExpressionsEvaluateToReferenceValues()
    {
        var random = new Random(17828);
        for (var index = 0; index < 60; index++)
        {
            var expression = GenerateArithmeticExpression(random, depth: 3);
            var result = await RunAwkAsync("BEGIN{print " + expression.Text + "}", []);

            Assert.Equal(expression.Value + "\n", result.StdOut);
            Assert.Equal(string.Empty, result.StdErr);
        }
    }

    [Fact]
    public async Task Awk_GeneratedDefaultWhitespaceSplittingMatchesReference()
    {
        var random = new Random(17829);
        var input = new StringBuilder();
        var expected = new StringBuilder();
        for (var index = 0; index < 80; index++)
        {
            var fields = GenerateFields(random);
            input.Append(JoinWithRandomWhitespace(random, fields));
            input.Append('\n');
            expected.Append(fields.Count);
            expected.Append(' ');
            expected.Append(fields[0]);
            expected.Append(' ');
            expected.Append(fields[^1]);
            expected.Append('\n');
        }

        var result = await RunAwkAsync(
            "{print NF, $1, $NF}",
            [new AwkTestInputFile("/input.txt", input.ToString())],
            ["/input.txt"]);

        Assert.Equal(expected.ToString(), result.StdOut);
        Assert.Equal(string.Empty, result.StdErr);
    }

    [Fact]
    public void Awk_GeneratedPosixDigitRegexesMatchReferenceStrings()
    {
        var random = new Random(17830);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        for (var index = 0; index < 80; index++)
        {
            var prefix = GenerateWord(random, 2 + random.Next(4));
            var suffix = GenerateWord(random, 2 + random.Next(4));
            var regex = AwkRegex.Create(
                new AwkRegexPattern("^" + prefix + "[[:digit:]]+" + suffix + "$"),
                AwkRegexCaseSensitivity.CaseSensitive);

            Assert.True(state.IsRegexMatch(
                regex,
                AwkText.FromString(prefix + (1000 + index) + suffix)));
            Assert.False(state.IsRegexMatch(regex, AwkText.FromString(prefix + "abc" + suffix)));
        }
    }

    [Fact]
    public async Task Awk_GeneratedMalformedProgramsFailWithFriendlyDiagnostics()
    {
        var programs = GenerateMalformedPrograms().Distinct(StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(programs);

        for (var index = 0; index < programs.Length; index++)
        {
            var result = await AwkTestHarness
                .RunProgramAsync(programs[index], [], [], CancellationToken.None);

            Assert.Equal(2, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.StartsWith("awk:", result.StdErr);
            AwkTestHarness.AssertNoClrExceptionLeak(result.StdErr);
        }

        static IEnumerable<string> GenerateMalformedPrograms()
        {
            var validPrograms = new[]
            {
                "BEGIN{print 1}",
                "BEGIN{if (1) print 1}",
                "BEGIN{for (i = 0; i < 2; i++) print i}",
                "BEGIN{a[1] = 2; print a[1]}",
                "BEGIN{print (1 + 2)}",
                "BEGIN{print 1 ? 2 : 3}",
                "function f(x){return x + 1} BEGIN{print f(1)}",
                "BEGIN{switch (1) { case 1: print 1; break; default: print 0 }}"
            };

            for (var index = 0; index < validPrograms.Length; index++)
            {
                var program = validPrograms[index];
                yield return program[..^1];
                yield return program + "}";

                var closingParenthesis = program.IndexOf(')');
                if (closingParenthesis >= 0)
                {
                    yield return program.Remove(closingParenthesis, 1);
                }

                var closingBracket = program.IndexOf(']');
                if (closingBracket >= 0)
                {
                    yield return program.Remove(closingBracket, 1);
                }

                var firstBrace = program.IndexOf('{');
                if (firstBrace >= 0)
                {
                    yield return program.Insert(firstBrace + 1, "}");
                }
            }
        }
    }

    private static AwkGeneratedExpression GenerateArithmeticExpression(Random random, int depth)
    {
        if (depth == 0)
        {
            var value = random.Next(0, 20);
            return new AwkGeneratedExpression(value.ToString(), value);
        }

        var left = GenerateArithmeticExpression(random, depth - 1);
        var right = GenerateArithmeticExpression(random, depth - 1);
        return random.Next(3) switch
        {
            0 => new AwkGeneratedExpression("(" + left.Text + "+" + right.Text + ")", left.Value + right.Value),
            1 => new AwkGeneratedExpression("(" + left.Text + "-" + right.Text + ")", left.Value - right.Value),
            _ => new AwkGeneratedExpression("(" + left.Text + "*" + right.Text + ")", left.Value * right.Value)
        };
    }

    private static List<string> GenerateFields(Random random)
    {
        var count = 1 + random.Next(6);
        var fields = new List<string>(count);
        for (var index = 0; index < count; index++)
        {
            fields.Add(GenerateWord(random, 1 + random.Next(8)));
        }

        return fields;
    }

    private static string JoinWithRandomWhitespace(Random random, List<string> fields)
    {
        var builder = new StringBuilder();
        builder.Append(RandomWhitespace(random));
        for (var index = 0; index < fields.Count; index++)
        {
            if (index > 0)
                builder.Append(RandomWhitespace(random));

            builder.Append(fields[index]);
        }

        builder.Append(RandomWhitespace(random));
        return builder.ToString();
    }

    private static string RandomWhitespace(Random random)
    {
        var count = 1 + random.Next(4);
        var builder = new StringBuilder(count);
        for (var index = 0; index < count; index++)
        {
            builder.Append(random.Next(2) == 0 ? ' ' : '\t');
        }

        return builder.ToString();
    }

    private static string GenerateWord(Random random, int length)
    {
        var builder = new StringBuilder(length);
        for (var index = 0; index < length; index++)
        {
            builder.Append((char)('a' + random.Next(26)));
        }

        return builder.ToString();
    }

    private static async Task<AwkPropertyRunResult> RunAwkAsync(
        string program,
        IReadOnlyList<AwkTestInputFile> files)
    {
        return await RunAwkAsync(program, files, []).ConfigureAwait(false);
    }

    private static async Task<AwkPropertyRunResult> RunAwkAsync(
        string program,
        IReadOnlyList<AwkTestInputFile> files,
        IReadOnlyList<string> inputArguments)
    {
        var result = await AwkTestHarness
            .RunProgramAsync(program, files, inputArguments, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Equal(0, result.ExitCode);
        return new AwkPropertyRunResult(
            result.StdOut,
            result.StdErr);
    }

    private readonly struct AwkGeneratedExpression(string text, int value)
    {
        public string Text { get; } = text;

        public int Value { get; } = value;
    }

    private readonly struct AwkPropertyRunResult(string stdOut, string stdErr)
    {
        public string StdOut { get; } = stdOut;

        public string StdErr { get; } = stdErr;
    }
}
