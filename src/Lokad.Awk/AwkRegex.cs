using System;
using System.Text;
using System.Text.RegularExpressions;
using Utf8RegexEngine = Lokad.Utf8Regex.Utf8Regex;

namespace Lokad.Awk;

internal enum AwkRegexCaseSensitivity
{
    CaseSensitive,
    IgnoreCase
}

internal sealed class AwkRegex
{
    private readonly bool _hasFixedCaseSensitivity;

    private AwkRegex(string pattern, AwkRegexBackend backend, bool hasFixedCaseSensitivity = false,
        AwkRegexSelection? selection = null, string? erePattern = null)
    {
        Pattern = pattern;
        ErePattern = erePattern ?? pattern;
        RequiresDotNetFallback = RequiresDotNetEngine(pattern);
        Backend = backend;
        GroupCount = backend.GroupCount;
        Selection = selection ?? AwkRegexSelection.Analyze(pattern);
        _hasFixedCaseSensitivity = hasFixedCaseSensitivity;
    }

    public static readonly TimeSpan MatchTimeout =
        TimeSpan.FromSeconds(AwkRuntimeLimits.DefaultRegexMatchTimeoutSeconds);

    public string Pattern { get; }
    internal string ErePattern { get; }

    public bool RequiresDotNetFallback { get; }

    public int GroupCount { get; }

    internal AwkRegexSelection Selection { get; }

    internal AwkRegexBackend Backend { get; }

    public AwkRegexCaseSensitivity ResolveCaseSensitivity(Func<AwkRegexCaseSensitivity> resolveCurrent) =>
        _hasFixedCaseSensitivity ? Backend.SeedCaseSensitivity : resolveCurrent();

    public static AwkRegex Create(
        AwkRegexPattern pattern,
        AwkRegexCaseSensitivity caseSensitivity,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
    {
        var ere = TranslatePattern(pattern, compatibilityMode);
        return CreateDotNet(AwkRegexScalarPattern.Translate(ere), caseSensitivity,
            selection: AwkRegexSelection.Analyze(ere), erePattern: ere);
    }

    internal static AwkRegex CreateDotNet(
        string translatedPattern,
        AwkRegexCaseSensitivity caseSensitivity,
        bool hasFixedCaseSensitivity = false,
        AwkRegexSelection? selection = null, string? erePattern = null) =>
        new(
            translatedPattern,
            AwkRegexBackend.DotNet(
                caseSensitivity,
                new Regex(translatedPattern, ResolveOptions(caseSensitivity), MatchTimeout)),
            hasFixedCaseSensitivity, selection, erePattern);

    internal static AwkRegex CreateUtf8(
        string translatedPattern,
        AwkRegexCaseSensitivity caseSensitivity,
        bool hasFixedCaseSensitivity = false,
        AwkRegexSelection? selection = null, string? erePattern = null) =>
        new(
            translatedPattern,
            AwkRegexBackend.Utf8(
                caseSensitivity,
                new Utf8RegexEngine(translatedPattern, ResolveOptions(caseSensitivity), MatchTimeout)),
            hasFixedCaseSensitivity, selection, erePattern);

    internal static bool RequiresDotNetEngine(string translatedPattern) =>
        translatedPattern.Contains(@"\A", StringComparison.Ordinal) ||
        translatedPattern.Contains(@"\z", StringComparison.Ordinal);

    internal static RegexOptions ResolveOptions(AwkRegexCaseSensitivity caseSensitivity) =>
        RegexOptions.CultureInvariant |
        (caseSensitivity is AwkRegexCaseSensitivity.IgnoreCase
            ? RegexOptions.IgnoreCase
            : RegexOptions.None);

    public static string TranslatePattern(AwkRegexPattern pattern,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
    {
        return Translate(pattern.Text, compatibilityMode);
        static string Translate(string pattern, AwkCompatibilityMode mode)
        {
            return TranslatePosixCharacterClasses(TranslateEscapes(pattern, mode));
            static string TranslateEscapes(string pattern, AwkCompatibilityMode mode)
            {
                var slash = pattern.IndexOf('\\', StringComparison.Ordinal);
                if (slash < 0)
                    return pattern;

                var builder = new StringBuilder(pattern.Length);
                var bracketStart = -1;
                var bracketFirstItem = -1;
                var posixTokenEnd = -1;
                var groupDepth = 0;
                for (var index = 0; index < slash; index++)
                    AppendPatternCharacter(pattern[index], index);

                for (var index = slash; index < pattern.Length; index++)
                {
                    var current = pattern[index];
                    if (current != '\\' || index + 1 >= pattern.Length)
                    {
                        AppendPatternCharacter(current, index);
                        continue;
                    }

                    index++;
                    var escaped = pattern[index];
                    if (!(mode == AwkCompatibilityMode.Posix && escaped == 'x') &&
                        AwkEscapes.TryReadNumericEscape(pattern, ref index, pattern.Length, out var numeric))
                    {
                        if (mode == AwkCompatibilityMode.Traditional && ".*+(){}[]|?^$\\".Contains(numeric))
                            AppendLiteral(builder, numeric, bracketStart >= 0);
                        else if (numeric == '\\' && index + 1 < pattern.Length)
                        {
                            // Numeric escapes are decoded before the ERE parser sees them.
                            // A resulting backslash introduces an ERE escape, not another
                            // awk numeric or string escape (\134y differs from \y).
                            index++;
                            AppendOperator(pattern[index], fromNumeric: true);
                        }
                        else
                            AppendPatternCharacter(numeric, index);
                        continue;
                    }

                    if (bracketStart >= 0 || mode != AwkCompatibilityMode.GnuDefault)
                    {
                        if (bracketStart >= 0 && mode == AwkCompatibilityMode.GnuDefault && escaped == 'y')
                            AppendLiteral(builder, 'b', inBracket: true);
                        else
                            AppendEscapedCharacter(escaped);
                        continue;
                    }

                    AppendOperator(escaped, fromNumeric: false);
                }

                return builder.ToString();

                void AppendOperator(char escaped, bool fromNumeric)
                {
                    if (fromNumeric && (bracketStart >= 0 || mode != AwkCompatibilityMode.GnuDefault))
                    {
                        AppendLiteral(builder, escaped, bracketStart >= 0);
                        return;
                    }

                    switch (escaped)
                    {
                        case '<':
                            builder.Append(@"\b(?=\w)");
                            break;
                        case '>':
                            builder.Append(@"(?<=\w)\b");
                            break;
                        case 'y' when !fromNumeric:
                        case 'b' when fromNumeric:
                            builder.Append(@"\b");
                            break;
                        case 'B':
                            builder.Append(@"\B");
                            break;
                        case 'w':
                            builder.Append(@"[\p{L}\p{Nd}_]");
                            break;
                        case 'W':
                            builder.Append(@"[^\p{L}\p{Nd}_]");
                            break;
                        case 's':
                            builder.Append(@"\s");
                            break;
                        case 'S':
                            builder.Append(@"\S");
                            break;
                        case '`':
                            builder.Append(@"\A");
                            break;
                        case '\'':
                            builder.Append(@"\z");
                            break;
                        case '/':
                            builder.Append('/');
                            break;
                        default:
                            if (fromNumeric)
                                AppendLiteral(builder, escaped, bracketStart >= 0);
                            else
                                AppendEscapedCharacter(escaped);
                            break;
                    }
                }

                void AppendEscapedCharacter(char escaped)
                {
                    if (escaped is 'a' or 'b' or 'f' or 'n' or 'r' or 't' or 'v' &&
                        AwkEscapes.TryDecodeSimpleEscape(escaped, out var decoded))
                        AppendLiteral(builder, decoded, bracketStart >= 0);
                    else
                        AppendLiteral(builder, escaped, bracketStart >= 0);
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
                                AppendLiteral(builder, value, inBracket: false);
                                return;
                            }
                            groupDepth--;
                        }
                    }
                    else if (value == '[' && index + 1 < pattern.Length && pattern[index + 1] is ':' or '=' or '.')
                    {
                        var end = pattern.IndexOf(pattern[index + 1] + "]", index + 2, StringComparison.Ordinal);
                        if (end >= 0)
                            posixTokenEnd = end + 1;
                    }
                    else if (value == ']' && index > bracketFirstItem && index > posixTokenEnd)
                        bracketStart = -1;
                    builder.Append(value);
                }

                static void AppendLiteral(StringBuilder builder, char value, bool inBracket)
                {
                    if (inBracket && value is ']' or '-' or '^')
                        builder.Append('\\').Append(value);
                    else
                        builder.Append(Regex.Escape(value.ToString()));
                }
            }

            static string TranslatePosixCharacterClasses(string pattern)
            {
                if (!pattern.Contains("[:", System.StringComparison.Ordinal) &&
                    !pattern.Contains("[=", System.StringComparison.Ordinal) &&
                    !pattern.Contains("[.", System.StringComparison.Ordinal))
                    return pattern;

                StringBuilder? builder = null;
                var copied = 0;
                for (var index = 0; index < pattern.Length; index++)
                {
                    if (pattern[index] == '\\')
                    {
                        index++;
                        continue;
                    }

                    if (pattern[index] != '[')
                        continue;

                    var end = FindBracketExpressionEnd(pattern, index + 1);
                    if (end < 0)
                        return pattern;

                    var content = pattern.Substring(index + 1, end - index - 1);
                    var translated = TranslatePosixClassTokens(content);
                    if (!string.Equals(content, translated, System.StringComparison.Ordinal))
                    {
                        builder ??= new StringBuilder(pattern.Length);
                        builder.Append(pattern, copied, index + 1 - copied);
                        builder.Append(translated);
                        copied = end;
                    }

                    index = end;
                }

                if (builder == null)
                    return pattern;

                builder.Append(pattern, copied, pattern.Length - copied);
                return builder.ToString();

                static int FindBracketExpressionEnd(string pattern, int start)
                {
                    var index = start;
                    var posixTokenMarker = '\0';
                    if (index < pattern.Length && pattern[index] == '^')
                        index++;

                    if (index < pattern.Length && pattern[index] == ']')
                        index++;

                    for (; index < pattern.Length; index++)
                    {
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

                static string TranslatePosixClassTokens(string content)
                {
                    return TranslatePosixEquivalenceAndCollatingTokens(content)
                        .Replace("[:alnum:]", "\\p{L}\\p{Nd}", System.StringComparison.Ordinal)
                        .Replace("[:alpha:]", "\\p{L}", System.StringComparison.Ordinal)
                        .Replace("[:blank:]", " \\t", System.StringComparison.Ordinal)
                        .Replace("[:cntrl:]", "\\p{Cc}", System.StringComparison.Ordinal)
                        .Replace("[:digit:]", "0-9", System.StringComparison.Ordinal)
                        .Replace("[:graph:]", "\\x21-\\x7E", System.StringComparison.Ordinal)
                        .Replace("[:lower:]", "\\p{Ll}", System.StringComparison.Ordinal)
                        .Replace("[:print:]", "\\x20-\\x7E", System.StringComparison.Ordinal)
                        .Replace("[:punct:]", "\\p{P}", System.StringComparison.Ordinal)
                        .Replace("[:space:]", "\\s", System.StringComparison.Ordinal)
                        .Replace("[:upper:]", "\\p{Lu}", System.StringComparison.Ordinal)
                        .Replace("[:xdigit:]", "0-9A-Fa-f", System.StringComparison.Ordinal);

                    static string TranslatePosixEquivalenceAndCollatingTokens(string content)
                    {
                        var tokenStart = content.IndexOf('[', System.StringComparison.Ordinal);
                        if (tokenStart < 0)
                            return content;

                        StringBuilder? builder = null;
                        var copied = 0;
                        for (var index = tokenStart; index + 4 < content.Length; index++)
                        {
                            if (content[index] != '[' || content[index + 1] is not ('=' or '.'))
                                continue;

                            var marker = content[index + 1];
                            var closing = marker.ToString() + "]";
                            var closingIndex = content.IndexOf(closing, index + 2, System.StringComparison.Ordinal);
                            if (closingIndex < 0)
                                continue;

                            var symbolStart = index + 2;
                            var symbolLength = closingIndex - symbolStart;
                            if (symbolLength == 1)
                            {
                                builder ??= new StringBuilder(content.Length);
                                builder.Append(content, copied, index - copied);
                                AppendBracketLiteral(builder, content[symbolStart]);
                                copied = closingIndex + 2;
                            }

                            index = closingIndex + 1;
                        }

                        if (builder == null)
                            return content;

                        builder.Append(content, copied, content.Length - copied);
                        return builder.ToString();

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

internal abstract record AwkRegexBackend(AwkRegexCaseSensitivity SeedCaseSensitivity)
{
    /// <summary>Gets the complete-match group plus every capturing group exposed by this backend.</summary>
    public abstract int GroupCount { get; }

    public static AwkRegexBackend DotNet(AwkRegexCaseSensitivity caseSensitivity, Regex value) =>
        new AwkDotNetRegexBackend(caseSensitivity, value);

    public static AwkRegexBackend Utf8(AwkRegexCaseSensitivity caseSensitivity, Utf8RegexEngine value) =>
        new AwkUtf8RegexBackend(caseSensitivity, value);
}

internal sealed record AwkDotNetRegexBackend : AwkRegexBackend
{
    public AwkDotNetRegexBackend(AwkRegexCaseSensitivity seedCaseSensitivity, Regex value)
        : base(seedCaseSensitivity)
    {
        Value = value;
    }

    public Regex Value { get; }

    public override int GroupCount => Value.GetGroupNumbers().Length;
}

internal sealed record AwkUtf8RegexBackend : AwkRegexBackend
{
    public AwkUtf8RegexBackend(AwkRegexCaseSensitivity seedCaseSensitivity, Utf8RegexEngine value)
        : base(seedCaseSensitivity)
    {
        Value = value;
    }

    public Utf8RegexEngine Value { get; }

    public override int GroupCount => Value.GetGroupNumbers().Length;
}
