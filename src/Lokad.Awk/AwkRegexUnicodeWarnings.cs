namespace Lokad.Awk;

internal sealed class AwkRegexUnicodeWarnings(AwkMemoryAccounting governor, AwkCompatibilityMode mode,
    Action<string>? report, Action checkpoint, AwkRegexEscapeWarnings? regexEscapeWarnings = null)
{
    private readonly AwkRegexEscapeWarnings _operatorWarnings = regexEscapeWarnings ?? new AwkRegexEscapeWarnings();
    private Dictionary<object, Entry>? _sites;
    private int _capacity;
    private long _bufferBytes;

    public void Report(AwkRegexPattern pattern, AwkRegexCaseSensitivity sensitivity)
    {
        if (report is null || pattern.Origin == AwkRegexPatternOrigin.SourceLiteral)
            return;
        if (mode == AwkCompatibilityMode.Posix)
        {
            ReportWarnings(null);
            return;
        }
        var site = pattern.WarningSite ?? this;
        var previous = default(Entry);
        var found = _sites is not null && _sites.TryGetValue(site, out previous);
        var flag = sensitivity == AwkRegexCaseSensitivity.IgnoreCase ? 2 : 1;
        var sameText = found && previous.Pattern is string previousPattern &&
            (ReferenceEquals(previousPattern, pattern.Text) ||
             AwkRegexConstruction.Equals(previousPattern, pattern.Text, checkpoint));
        if (sameText && (previous.CompiledCases & flag) != 0)
        {
            ReportWarnings(null);
            return;
        }
        if (!(AwkRegexConstruction.IndexOf(pattern.Text, @"\u", 0, checkpoint) >= 0 ||
              AwkRegexConstruction.IndexOf(pattern.Text, @"\x", 0, checkpoint) >= 0) ||
            !AwkRegexEscapes.HasNumericWarnings(pattern.Text, mode, checkpoint))
        {
            ReportWarnings(null);
            if (found)
            {
                _sites?.Remove(site);
                governor.ReleaseCommitted(previous.Bytes);
            }
            return;
        }
        var bytes = AwkRuntimeMemory.EstimateDiagnosticMessageBytes(pattern.Text.Length);
        var grow = !found && (_sites?.Count ?? 0) >= _capacity;
        var capacity = grow
            ? AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedMultiply(Math.Max(2, _capacity), 2))
            : _capacity;
        var bufferBytes = grow
            ? AwkRuntimeMemory.EstimateDictionaryBufferBytes(AwkRuntimeMemory.CheckedToInt32(
                AwkRuntimeMemory.CheckedMultiply(capacity, 3)))
            : _bufferBytes;
        var reserved = AwkRuntimeMemory.CheckedAdd(bytes, grow ? bufferBytes : 0);
        governor.Reserve(reserved);
        Dictionary<object, Entry> next;
        try
        {
            if (grow)
            {
                checkpoint();
                next = new Dictionary<object, Entry>(capacity, ReferenceEqualityComparer.Instance);
                if (_sites is not null)
                {
                    var copied = 0;
                    foreach (var item in _sites)
                    {
                        if ((copied++ & 63) == 0) checkpoint();
                        next.Add(item.Key, item.Value);
                    }
                }
                checkpoint();
            }
            else
                next = _sites ?? throw new InvalidOperationException("Regex warning history is not initialized.");
            ReportWarnings(report);
            next[site] = new Entry(pattern.Text, (sameText ? previous.CompiledCases : 0) | flag, bytes);
        }
        catch
        {
            governor.ReleaseReserved(reserved);
            throw;
        }
        governor.CommitReserved(reserved);
        governor.ReleaseCommitted((grow ? _bufferBytes : 0) + (found ? previous.Bytes : 0));
        _sites = next;
        _capacity = capacity;
        _bufferBytes = bufferBytes;

        void ReportWarnings(Action<string>? unicodeReport) =>
            AwkRegexEscapes.ReportWarnings(pattern.Text, mode, _operatorWarnings,
                warning => report(warning.Message), unicodeReport, checkpoint);
    }

    public void Reset(object site)
    {
        if (_sites is not null && _sites.Remove(site, out var previous))
            governor.ReleaseCommitted(previous.Bytes);
    }

    private readonly record struct Entry(string? Pattern, int CompiledCases, long Bytes);
}
