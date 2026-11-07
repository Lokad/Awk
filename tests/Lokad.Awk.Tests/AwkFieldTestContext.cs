namespace Lokad.Awk.Tests;

// Real splitting services for owner/cache tests; the context retires provider
// caches after the frame's users. Runtime state owns these services in production.
internal sealed class AwkFieldTestContext : IDisposable
{
    private readonly AwkRegexRuntime _regex;

    public AwkFieldFrame Frame { get; }
    public AwkFieldSplitter Splitter { get; }

    public AwkFieldTestContext(AwkRuntimeCounters counters, AwkMemoryAccounting accounting,
        Func<AwkValue, AwkText>? format = null, Action? checkpoint = null, string separator = " ",
        Func<AwkText, AwkValue>? createInputValue = null, Func<AwkText>? resolveOutputFieldSeparator = null)
    {
        Action check = checkpoint ?? (static () => { });
        var monitor = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters);
        _regex = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, check, counters, monitor, accounting,
            regexEngine: AwkUtf8RegexEngine.Instance);
        Splitter = new AwkFieldSplitter(AwkFieldSeparatorSpec.FromText(separator), _regex.Create,
            _regex.CreateLiteral, _regex.EnumerateMatchSpans, () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, check, accounting, counters);
        Frame = new AwkFieldFrame(counters, accounting, format ?? (static value => value.ByteText),
            Splitter, createInputValue ?? (text => AwkValue.FromNumericString(text, AwkNumberParsingMode.Decimal, check)),
            text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal),
            resolveOutputFieldSeparator ?? (static () => AwkText.FromSourceLiteralString(" ")), check);
    }

    public void Dispose()
    {
        try { Frame.ResetRecord(); }
        finally { _regex.Dispose(); }
    }
}
