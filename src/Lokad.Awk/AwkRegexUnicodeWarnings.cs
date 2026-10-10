namespace Lokad.Awk;

internal sealed class AwkRegexUnicodeWarnings(AwkMemoryGovernor governor, AwkCompatibilityMode mode,
    Action<string>? report, Action checkpoint)
{
    private Dictionary<object, Entry>? _sites;
    private int _capacity;
    private long _bufferBytes;

    public void Report(AwkRegexPattern pattern, AwkRegexCaseSensitivity sensitivity)
    {
        if (report is null || mode == AwkCompatibilityMode.Posix || pattern.Origin == AwkRegexPatternOrigin.SourceLiteral)
            return;
        var site = pattern.WarningSite ?? this;
        var previous = default(Entry);
        var found = _sites is not null && _sites.TryGetValue(site, out previous);
        var flag = sensitivity == AwkRegexCaseSensitivity.IgnoreCase ? 2 : 1;
        var sameText = found && string.Equals(previous.Pattern, pattern.Text, StringComparison.Ordinal);
        if (sameText && (previous.CompiledCases & flag) != 0)
            return;
        if (!pattern.Text.Contains("\\u", StringComparison.Ordinal) ||
            !AwkRegexEscapes.HasUnicodeWarnings(pattern.Text, mode, checkpoint))
        {
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
                next = new Dictionary<object, Entry>(capacity, ReferenceEqualityComparer.Instance);
                if (_sites is not null)
                    foreach (var item in _sites)
                        next.Add(item.Key, item.Value);
            }
            else
                next = _sites ?? throw new InvalidOperationException("Regex warning history is not initialized.");
            AwkRegexEscapes.ReportUnicodeWarnings(pattern.Text, mode, report, checkpoint);
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
    }

    private readonly record struct Entry(string? Pattern, int CompiledCases, long Bytes);
}
