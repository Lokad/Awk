namespace Lokad.Awk;

internal enum AwkRegexCaseSensitivity
{
    CaseSensitive,
    IgnoreCase
}

// Immutable Awk preparation; execution caches own provider handles.
internal sealed class AwkRegex
{
    private readonly bool _hasFixedCaseSensitivity;
    private readonly AwkRegexCaseSensitivity _caseSensitivity;

    internal AwkRegex(AwkText pattern, AwkRegexCaseSensitivity caseSensitivity,
        bool hasFixedCaseSensitivity = false, Action? checkpoint = null)
    {
        Pattern = pattern;
        _caseSensitivity = caseSensitivity;
        _hasFixedCaseSensitivity = hasFixedCaseSensitivity;
        PatternHashCode = pattern.GetHashCode(checkpoint);
    }

    public AwkText Pattern { get; }
    internal int PatternHashCode { get; }

    public AwkRegexCaseSensitivity ResolveCaseSensitivity(Func<AwkRegexCaseSensitivity> resolveCurrent) =>
        _hasFixedCaseSensitivity ? _caseSensitivity : resolveCurrent();

    public static AwkRegex Create(AwkRegexPattern pattern, AwkRegexCaseSensitivity caseSensitivity,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault, Action? checkpoint = null) =>
        new(TranslatePattern(pattern, compatibilityMode, checkpoint), caseSensitivity, checkpoint: checkpoint);

    public static AwkText TranslatePattern(AwkRegexPattern pattern,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null, AwkMemoryLease? resultLease = null, AwkMemoryAccounting? governor = null)
    {
        var memory = governor ?? AwkMemoryAccounting.Disabled;
        using var ownedResults = resultLease is null ? memory.CreateTransientLease() : AwkMemoryLease.Disabled;
        var results = resultLease ?? ownedResults;
        var decoded = AwkRegexEscapes.Decode(pattern.Text, compatibilityMode, checkpoint, memory, results);
        AwkRegexBracketClasses.Validate(decoded, checkpoint, results);
        return TranslateClasses(TranslateEscapes(decoded, compatibilityMode, checkpoint, memory, results),
            checkpoint, memory, results);
    }

    private static AwkText TranslateEscapes(AwkText pattern, AwkCompatibilityMode mode, Action? checkpoint,
        AwkMemoryAccounting memory, AwkMemoryLease results)
    {
        var bytes = pattern.AsMemory(checkpoint);
        AwkUtf8Builder? builder = null;
        var bracketStart = -1;
        var firstItem = -1;
        var tokenEnd = -1;
        var groupDepth = 0;
        long nextCheckpoint = 0;
        try
        {
            for (var index = 0; index < bytes.Length; index++)
            {
                AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
                var value = bytes.Span[index];
                if (value == (byte)'\\' && index + 1 < bytes.Length)
                {
                    var output = GetBuilder(index);
                    var escaped = bytes.Span[++index];
                    if (bracketStart >= 0 || mode != AwkCompatibilityMode.GnuDefault)
                        AwkRegexConstruction.AppendLiteral(output, escaped, bracketStart >= 0);
                    else
                    {
                        switch (escaped)
                        {
                            case (byte)'<': output.Append(@"\b(?=\w)"u8); break;
                            case (byte)'>': output.Append(@"(?<=\w)\b"u8); break;
                            case (byte)'b': output.Append(@"\b"u8); break;
                            case (byte)'B': output.Append(@"\B"u8); break;
                            case (byte)'w': output.Append(@"[\p{L}\p{Nd}_]"u8); break;
                            case (byte)'W': output.Append(@"[^\p{L}\p{Nd}_]"u8); break;
                            case (byte)'s': output.Append(@"\s"u8); break;
                            case (byte)'S': output.Append(@"\S"u8); break;
                            case (byte)'`': output.Append(@"\A"u8); break;
                            case (byte)'\'': output.Append(@"\z"u8); break;
                            default: AwkRegexConstruction.AppendLiteral(output, escaped, inBracket: false); break;
                        }
                    }
                    continue;
                }
                if (bracketStart < 0)
                {
                    if (value == (byte)'.')
                    {
                        GetBuilder(index).Append(@"[\s\S]"u8);
                        continue;
                    }
                    if (value == (byte)'$')
                    {
                        GetBuilder(index).Append(@"\z"u8);
                        continue;
                    }
                    if (value == (byte)'[')
                    {
                        bracketStart = index;
                        firstItem = index + 1;
                        if (firstItem < bytes.Length && bytes.Span[firstItem] == (byte)'^') firstItem++;
                    }
                    else if (value == (byte)'(') groupDepth++;
                    else if (value == (byte)')')
                    {
                        if (groupDepth == 0)
                        {
                            AwkRegexConstruction.AppendLiteral(GetBuilder(index), value, inBracket: false);
                            continue;
                        }
                        groupDepth--;
                    }
                }
                else if (value == (byte)'[' && index + 1 < bytes.Length &&
                    bytes.Span[index + 1] is (byte)':' or (byte)'=' or (byte)'.')
                {
                    var end = AwkRegexConstruction.TokenEnd(bytes.Span, index, checkpoint);
                    if (end >= 0) tokenEnd = end + 1;
                }
                else if (value == (byte)']' && index > firstItem && index > tokenEnd)
                    bracketStart = -1;
                builder?.Append(value);
            }
            checkpoint?.Invoke();
            return builder is null ? pattern : AwkRegexConstruction.Finish(builder, results, checkpoint);
        }
        finally { builder?.Dispose(); }

        AwkUtf8Builder GetBuilder(int index)
        {
            if (builder is not null) return builder;
            builder = new AwkUtf8Builder(memory, bytes.Length);
            builder.Append(bytes.Span[..index], checkpoint);
            return builder;
        }
    }

    private static AwkText TranslateClasses(AwkText pattern, Action? checkpoint,
        AwkMemoryAccounting memory, AwkMemoryLease results)
    {
        var bytes = pattern.AsMemory(checkpoint);
        AwkUtf8Builder? builder = null;
        var copied = 0;
        long nextCheckpoint = 0;
        try
        {
            for (var index = 0; index < bytes.Length; index++)
            {
                AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
                if (bytes.Span[index] == (byte)'\\') { index++; continue; }
                if (bytes.Span[index] != (byte)'[') continue;
                var end = FindBracketEnd(bytes.Span, index + 1, checkpoint);
                if (end < 0) return pattern;
                for (var token = index + 1; token < end; token++)
                {
                    AwkRegexConstruction.CheckProgress(token, ref nextCheckpoint, checkpoint);
                    if (bytes.Span[token] == (byte)'\\') { token++; continue; }
                    if (bytes.Span[token] != (byte)'[' || token + 1 >= end) continue;
                    var marker = bytes.Span[token + 1];
                    if (marker is not ((byte)':' or (byte)'=' or (byte)'.')) continue;
                    var close = AwkRegexConstruction.TokenEnd(bytes.Span, token, checkpoint);
                    if (close < 0 || close >= end) continue;
                    var symbol = bytes.Span[(token + 2)..close];
                    var replacement = marker == (byte)':' ? ClassExpansion(symbol) : default;
                    // Preserve the existing single-BMP-character equivalence/collating rule.
                    var single = marker != (byte)':' &&
                        System.Text.Rune.DecodeFromUtf8(symbol, out var scalar, out var consumed) ==
                            System.Buffers.OperationStatus.Done && consumed == symbol.Length && scalar.IsBmp;
                    if (!replacement.IsEmpty || single)
                    {
                        builder ??= new AwkUtf8Builder(memory, bytes.Length);
                        builder.Append(bytes.Span[copied..token], checkpoint);
                        if (single)
                        {
                            if (symbol.Length == 1 && symbol[0] is (byte)'\\' or (byte)']' or (byte)'-' or (byte)'^')
                                builder.Append((byte)'\\');
                            builder.Append(symbol, checkpoint);
                        }
                        else builder.Append(replacement, checkpoint);
                        copied = close + 2;
                    }
                    token = close + 1;
                }
                index = end;
            }
            if (builder is null) { checkpoint?.Invoke(); return pattern; }
            builder.Append(bytes.Span[copied..], checkpoint);
            return AwkRegexConstruction.Finish(builder, results, checkpoint);
        }
        finally { builder?.Dispose(); }
    }

    private static ReadOnlySpan<byte> ClassExpansion(ReadOnlySpan<byte> name) => name switch
    {
        _ when name.SequenceEqual("alnum"u8) => @"\p{L}\p{Nd}"u8,
        _ when name.SequenceEqual("alpha"u8) => @"\p{L}"u8,
        _ when name.SequenceEqual("blank"u8) => " \\t"u8,
        _ when name.SequenceEqual("cntrl"u8) => @"\p{Cc}"u8,
        _ when name.SequenceEqual("digit"u8) => "0-9"u8,
        _ when name.SequenceEqual("graph"u8) => @"\p{L}\p{M}\p{N}\p{P}\p{S}\p{Cf}\p{Co}"u8,
        _ when name.SequenceEqual("lower"u8) => @"\p{Ll}"u8,
        _ when name.SequenceEqual("print"u8) => @"\p{L}\p{M}\p{N}\p{P}\p{S}\p{Cf}\p{Co}\p{Zs}"u8,
        _ when name.SequenceEqual("punct"u8) => @"\p{P}\p{S}"u8,
        _ when name.SequenceEqual("space"u8) => @"\s"u8,
        _ when name.SequenceEqual("upper"u8) => @"\p{Lu}"u8,
        _ when name.SequenceEqual("xdigit"u8) => "0-9A-Fa-f"u8,
        _ => default
    };

    private static int FindBracketEnd(ReadOnlySpan<byte> bytes, int start, Action? checkpoint)
    {
        var index = start;
        long nextCheckpoint = start;
        if (index < bytes.Length && bytes[index] == (byte)'^') index++;
        if (index < bytes.Length && bytes[index] == (byte)']') index++;
        for (; index < bytes.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
            if (bytes[index] == (byte)'\\') { index++; continue; }
            if (bytes[index] == (byte)'[' && index + 1 < bytes.Length &&
                bytes[index + 1] is (byte)':' or (byte)'=' or (byte)'.')
            {
                var end = AwkRegexConstruction.TokenEnd(bytes, index, checkpoint);
                if (end >= 0) { index = end + 1; continue; }
            }
            if (bytes[index] == (byte)']') return index;
        }
        return -1;
    }
}
