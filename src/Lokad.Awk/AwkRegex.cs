using System;
using System.Text;
using System.Text.RegularExpressions;

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

    internal AwkRegex(string pattern, AwkRegexCaseSensitivity caseSensitivity, bool hasFixedCaseSensitivity = false,
        AwkRegexSelection? selection = null, string? erePattern = null, Action? checkpoint = null)
    {
        Pattern = pattern;
        ErePattern = erePattern ?? pattern;
        RequiresDotNetFallback = RequiresDotNetEngine(pattern, checkpoint);
        _caseSensitivity = caseSensitivity;
        checkpoint?.Invoke();
        Selection = selection ?? AwkRegexSelection.Analyze(pattern, checkpoint);
        _hasFixedCaseSensitivity = hasFixedCaseSensitivity;
        PatternHashCode = pattern.GetHashCode();
        checkpoint?.Invoke();
    }

    public string Pattern { get; }
    internal int PatternHashCode { get; }
    internal string ErePattern { get; }

    public bool RequiresDotNetFallback { get; }

    internal AwkRegexSelection Selection { get; }

    public AwkRegexCaseSensitivity ResolveCaseSensitivity(Func<AwkRegexCaseSensitivity> resolveCurrent) =>
        _hasFixedCaseSensitivity ? _caseSensitivity : resolveCurrent();

    public static AwkRegex Create(
        AwkRegexPattern pattern,
        AwkRegexCaseSensitivity caseSensitivity,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
    {
        var ere = TranslatePattern(pattern, compatibilityMode, checkpoint);
        return new AwkRegex(AwkRegexScalarPattern.Translate(ere, checkpoint), caseSensitivity,
            selection: AwkRegexSelection.Analyze(ere, checkpoint), erePattern: ere, checkpoint: checkpoint);
    }

    internal static bool RequiresDotNetEngine(string translatedPattern, Action? checkpoint = null) =>
        AwkRegexConstruction.IndexOf(translatedPattern, @"\A", 0, checkpoint) >= 0 ||
        AwkRegexConstruction.IndexOf(translatedPattern, @"\z", 0, checkpoint) >= 0;

    internal static RegexOptions ResolveOptions(AwkRegexCaseSensitivity caseSensitivity) =>
        RegexOptions.CultureInvariant |
        (caseSensitivity is AwkRegexCaseSensitivity.IgnoreCase
            ? RegexOptions.IgnoreCase
            : RegexOptions.None);

    public static string TranslatePattern(AwkRegexPattern pattern,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null, AwkMemoryLease? resultLease = null, AwkMemoryAccounting? governor = null)
    {
        var memory = governor ?? AwkMemoryAccounting.Disabled;
        using var ownedResults = resultLease is null ? memory.CreateTransientLease() : AwkMemoryLease.Disabled;
        return Translate(pattern.Text, compatibilityMode, checkpoint, resultLease ?? ownedResults, memory);
        static string Translate(string pattern, AwkCompatibilityMode mode, Action? checkpoint,
            AwkMemoryLease resultLease, AwkMemoryAccounting memory)
        {
            using var decoding = memory.ReserveTransient(memory.IsEnabled &&
                AwkRegexConstruction.IndexOf(pattern, '\\', 0, checkpoint) >= 0
                ? AwkRuntimeMemory.EstimateEscapeDecodingBytes(pattern.Length) : 0);
            var decoded = AwkRegexEscapes.Decode(pattern, mode, checkpoint);
            if (memory.IsEnabled && !ReferenceEquals(decoded, pattern))
                decoding.MoveTo(resultLease, AwkRuntimeMemory.EstimateUtf16StringBytes(decoded.Length));
            AwkRegexBracketClasses.Validate(decoded, checkpoint, resultLease);
            return TranslatePosixCharacterClasses(TranslateEscapes(decoded, mode, checkpoint, memory, resultLease),
                checkpoint, resultLease, memory);
            static string TranslateEscapes(string pattern, AwkCompatibilityMode mode, Action? checkpoint,
                AwkMemoryAccounting memory, AwkMemoryLease resultLease)
            {
                using var scratch = memory.CreateTransientLease();
                StringBuilder? builder = null;
                var bracketStart = -1;
                var bracketFirstItem = -1;
                var posixTokenEnd = -1;
                var groupDepth = 0;
                long nextCheckpoint = 0;
                for (var index = 0; index < pattern.Length; index++)
                {
                    AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
                    var current = pattern[index];
                    if (current != '\\' || index + 1 >= pattern.Length)
                    {
                        AppendPatternCharacter(current, index);
                        continue;
                    }

                    var output = GetBuilder(index);
                    index++;
                    var escaped = pattern[index];
                    if (bracketStart >= 0 || mode != AwkCompatibilityMode.GnuDefault)
                    {
                        AppendLiteral(output, escaped, bracketStart >= 0);
                        continue;
                    }

                    AppendOperator(output, escaped);
                }

                checkpoint?.Invoke();
                if (builder is null) return pattern;
                if (memory.IsEnabled)
                    resultLease.ReserveAdditional(AwkRuntimeMemory.EstimateUtf16StringBytes(builder.Length));
                return AwkRegexConstruction.Finish(builder, checkpoint);

                StringBuilder GetBuilder(int index)
                {
                    if (builder is not null)
                        return builder;
                    if (memory.IsEnabled)
                        scratch.ReserveAdditional(AwkRegexConstruction.EstimateDialectBuilderScratch(
                            pattern, 16, 0, quoteRightParenthesis: true, checkpoint));
                    builder = new StringBuilder(pattern.Length);
                    AwkRegexConstruction.Append(builder, pattern, 0, index, checkpoint);
                    return builder;
                }

                void AppendOperator(StringBuilder output, char escaped)
                {
                    switch (escaped)
                    {
                        case '<':
                            output.Append(@"\b(?=\w)");
                            break;
                        case '>':
                            output.Append(@"(?<=\w)\b");
                            break;
                        case 'b':
                            output.Append(@"\b");
                            break;
                        case 'B':
                            output.Append(@"\B");
                            break;
                        case 'w':
                            output.Append(@"[\p{L}\p{Nd}_]");
                            break;
                        case 'W':
                            output.Append(@"[^\p{L}\p{Nd}_]");
                            break;
                        case 's':
                            output.Append(@"\s");
                            break;
                        case 'S':
                            output.Append(@"\S");
                            break;
                        case '`':
                            output.Append(@"\A");
                            break;
                        case '\'':
                            output.Append(@"\z");
                            break;
                        case '/':
                            output.Append('/');
                            break;
                        default:
                            AppendLiteral(output, escaped, bracketStart >= 0);
                            break;
                    }
                }

                void AppendPatternCharacter(char value, int index)
                {
                    if (bracketStart < 0)
                    {
                        if (value == '[')
                        {
                            bracketStart = index;
                            bracketFirstItem = index + 1;
                            if (bracketFirstItem < pattern.Length && pattern[bracketFirstItem] == '^')
                                bracketFirstItem++;
                        }
                        else if (value == '(')
                            groupDepth++;
                        else if (value == ')')
                        {
                            if (groupDepth == 0)
                            {
                                AppendLiteral(GetBuilder(index), value, inBracket: false);
                                return;
                            }
                            groupDepth--;
                        }
                    }
                    else if (value == '[' && index + 1 < pattern.Length && pattern[index + 1] is ':' or '=' or '.')
                    {
                        var end = AwkRegexConstruction.IndexOf(pattern, pattern[index + 1] + "]", index + 2, checkpoint);
                        if (end >= 0)
                            posixTokenEnd = end + 1;
                    }
                    else if (value == ']' && index > bracketFirstItem && index > posixTokenEnd)
                        bracketStart = -1;
                    builder?.Append(value);
                }

                static void AppendLiteral(StringBuilder builder, char value, bool inBracket)
                {
                    if (inBracket && value is ']' or '-' or '^')
                        builder.Append('\\').Append(value);
                    else
                        builder.Append(Regex.Escape(value.ToString()));
                }
            }

            static string TranslatePosixCharacterClasses(string pattern, Action? checkpoint,
                AwkMemoryLease resultLease, AwkMemoryAccounting memory)
            {
                if (AwkRegexConstruction.IndexOf(pattern, "[:", 0, checkpoint) < 0 &&
                    AwkRegexConstruction.IndexOf(pattern, "[=", 0, checkpoint) < 0 &&
                    AwkRegexConstruction.IndexOf(pattern, "[.", 0, checkpoint) < 0)
                    return pattern;

                using var scratch = memory.CreateTransientLease();
                StringBuilder? builder = null;
                var copied = 0;
                long nextCheckpoint = 0;
                for (var index = 0; index < pattern.Length; index++)
                {
                    AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
                    if (pattern[index] == '\\')
                    {
                        index++;
                        continue;
                    }

                    if (pattern[index] != '[')
                        continue;

                    var end = FindBracketExpressionEnd(pattern, index + 1, checkpoint);
                    if (end < 0)
                        return pattern;

                    using var contentMemory = memory.ReserveTransient(
                        AwkRuntimeMemory.EstimateUtf16StringBytes(end - index - 1));
                    checkpoint?.Invoke();
                    var content = pattern.Substring(index + 1, end - index - 1);
                    checkpoint?.Invoke();
                    var translated = TranslatePosixClassTokens(content, checkpoint, resultLease, memory);
                    if (!AwkRegexConstruction.Equals(content, translated, checkpoint))
                    {
                        if (builder is null)
                        {
                            if (memory.IsEnabled)
                                scratch.ReserveAdditional(AwkRegexConstruction.EstimateDialectBuilderScratch(
                                    pattern, 0, 20, quoteRightParenthesis: false, checkpoint));
                            builder = new StringBuilder(pattern.Length);
                        }
                        AwkRegexConstruction.Append(builder, pattern, copied, index + 1 - copied, checkpoint);
                        AwkRegexConstruction.Append(builder, translated, 0, translated.Length, checkpoint);
                        copied = end;
                    }

                    index = end;
                }

                if (builder == null)
                    return pattern;

                AwkRegexConstruction.Append(builder, pattern, copied, pattern.Length - copied, checkpoint);
                if (memory.IsEnabled)
                    resultLease.ReserveAdditional(AwkRuntimeMemory.EstimateUtf16StringBytes(builder.Length));
                return AwkRegexConstruction.Finish(builder, checkpoint);

                static int FindBracketExpressionEnd(string pattern, int start, Action? checkpoint)
                {
                    var index = start;
                    long nextCheckpoint = 0;
                    var posixTokenMarker = '\0';
                    if (index < pattern.Length && pattern[index] == '^')
                        index++;

                    if (index < pattern.Length && pattern[index] == ']')
                        index++;

                    for (; index < pattern.Length; index++)
                    {
                        AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
                        if (pattern[index] == '\\')
                        {
                            index++;
                            continue;
                        }

                        if (posixTokenMarker != '\0')
                        {
                            if (pattern[index] == ']' && pattern[index - 1] == posixTokenMarker)
                                posixTokenMarker = '\0';

                            continue;
                        }

                        if (pattern[index] == '[' && index + 1 < pattern.Length)
                        {
                            var marker = pattern[index + 1];
                            if (marker is ':' or '=' or '.')
                            {
                                posixTokenMarker = marker;
                                index++;
                                continue;
                            }
                        }

                        if (pattern[index] == ']')
                            return index;
                    }

                    return -1;
                }

                static string TranslatePosixClassTokens(string content, Action? checkpoint,
                    AwkMemoryLease resultLease, AwkMemoryAccounting memory)
                {
                    var translated = TranslatePosixEquivalenceAndCollatingTokens(content, checkpoint, resultLease, memory);
                    translated = AwkRegexConstruction.Replace(translated, "[:alnum:]", "\\p{L}\\p{Nd}", checkpoint, resultLease);
                    translated = AwkRegexConstruction.Replace(translated, "[:alpha:]", "\\p{L}", checkpoint, resultLease);
                    translated = AwkRegexConstruction.Replace(translated, "[:blank:]", " \\t", checkpoint, resultLease);
                    translated = AwkRegexConstruction.Replace(translated, "[:cntrl:]", "\\p{Cc}", checkpoint, resultLease);
                    translated = AwkRegexConstruction.Replace(translated, "[:digit:]", "0-9", checkpoint, resultLease);
                    translated = AwkRegexConstruction.Replace(translated, "[:graph:]", "\\p{L}\\p{M}\\p{N}\\p{P}\\p{S}\\p{Cf}\\p{Co}", checkpoint, resultLease);
                    translated = AwkRegexConstruction.Replace(translated, "[:lower:]", "\\p{Ll}", checkpoint, resultLease);
                    translated = AwkRegexConstruction.Replace(translated, "[:print:]", "\\p{L}\\p{M}\\p{N}\\p{P}\\p{S}\\p{Cf}\\p{Co}\\p{Zs}", checkpoint, resultLease);
                    translated = AwkRegexConstruction.Replace(translated, "[:punct:]", "\\p{P}\\p{S}", checkpoint, resultLease);
                    translated = AwkRegexConstruction.Replace(translated, "[:space:]", "\\s", checkpoint, resultLease);
                    translated = AwkRegexConstruction.Replace(translated, "[:upper:]", "\\p{Lu}", checkpoint, resultLease);
                    translated = AwkRegexConstruction.Replace(translated, "[:xdigit:]", "0-9A-Fa-f", checkpoint, resultLease);
                    return translated;

                    static string TranslatePosixEquivalenceAndCollatingTokens(string content, Action? checkpoint,
                        AwkMemoryLease resultLease, AwkMemoryAccounting memory)
                    {
                        var tokenStart = AwkRegexConstruction.IndexOf(content, '[', 0, checkpoint);
                        if (tokenStart < 0)
                            return content;

                        using var scratch = memory.CreateTransientLease();
                        StringBuilder? builder = null;
                        var copied = 0;
                        long nextCheckpoint = 0;
                        for (var index = tokenStart; index + 4 < content.Length; index++)
                        {
                            AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
                            if (content[index] != '[' || content[index + 1] is not ('=' or '.'))
                                continue;

                            var marker = content[index + 1];
                            var closing = marker.ToString() + "]";
                            var closingIndex = AwkRegexConstruction.IndexOf(content, closing, index + 2, checkpoint);
                            if (closingIndex < 0)
                                continue;

                            var symbolStart = index + 2;
                            var symbolLength = closingIndex - symbolStart;
                            if (symbolLength == 1)
                            {
                                if (builder is null)
                                {
                                    if (memory.IsEnabled)
                                        scratch.ReserveAdditional(AwkRegexConstruction.EstimateDialectBuilderScratch(
                                            content, 0, 0, quoteRightParenthesis: false, checkpoint));
                                    builder = new StringBuilder(content.Length);
                                }
                                AwkRegexConstruction.Append(builder, content, copied, index - copied, checkpoint);
                                AppendBracketLiteral(builder, content[symbolStart]);
                                copied = closingIndex + 2;
                            }

                            index = closingIndex + 1;
                        }

                        if (builder == null)
                            return content;

                        AwkRegexConstruction.Append(builder, content, copied, content.Length - copied, checkpoint);
                        if (memory.IsEnabled)
                            resultLease.ReserveAdditional(AwkRuntimeMemory.EstimateUtf16StringBytes(builder.Length));
                        return AwkRegexConstruction.Finish(builder, checkpoint);

                        static void AppendBracketLiteral(StringBuilder builder, char value)
                        {
                            if (value is '\\' or ']' or '-' or '^')
                                builder.Append('\\');

                            builder.Append(value);
                        }
                    }
                }
            }
        }
        ;
    }
}
