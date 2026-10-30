using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk.Tests;

public sealed class AwkRegexNativePatternTests
{
    [Theory]
    [InlineData("[[:alpha:]]+😀$", @"[\p{L}]+😀\z", "é😀")]
    [InlineData(@"\360\237\230\200+", "😀+", "😀😀")]
    [InlineData("é\0😀", "é\0😀", "aé\0😀b")]
    [InlineData(@"\u1F600", "😀", "a😀b")]
    public void PreparationAndEquivalentByteKeysDoNotDecodePatterns(string source, string translated, string input)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var engine = new RecordingEngine();
        using var runtime = new AwkRegexRuntime(static () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, static () => { }, counters, regexEngine: engine);
        var first = AwkText.FromSourceLiteralString(source);
        var equalBytes = AwkText.FromHostBytes(first.AsMemory());
        var regex = runtime.Create(new AwkRegexPattern(first));
        Assert.Same(regex, runtime.Create(new AwkRegexPattern(equalBytes)));
        Assert.True(runtime.IsMatch(regex, AwkText.FromSourceLiteralString(input)));
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(translated), Assert.Single(engine.Patterns));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public void LiteralAndDynamicKeysRetainTheirDifferentMeaningsWithoutDecoding()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var engine = new RecordingEngine();
        using var runtime = new AwkRegexRuntime(static () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, static () => { }, counters, regexEngine: engine);
        var text = AwkText.FromSourceLiteralString("é.");
        var literal = runtime.CreateLiteral(new AwkRegexLiteralText(text));
        var expression = runtime.Create(new AwkRegexPattern(text));
        Assert.True(runtime.IsMatch(expression, AwkText.FromSourceLiteralString("éx")));
        Assert.False(runtime.IsMatch(literal, AwkText.FromSourceLiteralString("éx")));
        Assert.True(runtime.IsMatch(literal, AwkText.FromSourceLiteralString("é.")));
        Assert.Equal(2, engine.Patterns.Count);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    private sealed class RecordingEngine : IAwkRegexEngine
    {
        public List<byte[]> Patterns { get; } = [];

        public bool TryCompile(ReadOnlySpan<byte> patternUtf8, bool ignoreCase, IMemoryBudget? memoryBudget,
            CancellationToken cancellationToken, [NotNullWhen(true)] out IAwkCompiledRegex? compiledPattern)
        {
            Patterns.Add(patternUtf8.ToArray()); // This test observer owns its retained copy.
            return AwkUtf8RegexEngine.Instance.TryCompile(patternUtf8, ignoreCase, memoryBudget,
                cancellationToken, out compiledPattern);
        }
    }
}
