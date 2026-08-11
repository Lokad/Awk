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
    private AwkRegex(string pattern, AwkRegexBackend backend)
    {
        Pattern = pattern;
        RequiresDotNetFallback = RequiresDotNetEngine(pattern);
        Backend = backend;
        GroupCount = backend.GroupCount;
    }

    public static readonly TimeSpan MatchTimeout =
        TimeSpan.FromSeconds(AwkRuntimeLimits.DefaultRegexMatchTimeoutSeconds);

    public string Pattern { get; }

    public bool RequiresDotNetFallback { get; }

    public int GroupCount { get; }

    internal AwkRegexBackend Backend { get; }

    public static AwkRegex Create(
        AwkRegexPattern pattern,
        AwkRegexCaseSensitivity caseSensitivity)
    {
        var translatedPattern = TranslatePattern(pattern);
        return new AwkRegex(
            translatedPattern,
            AwkRegexBackend.DotNet(
                caseSensitivity,
                new Regex(
                    translatedPattern,
                    ResolveOptions(caseSensitivity),
                    MatchTimeout)));
    }

    internal static AwkRegex CreateDotNet(
        string translatedPattern,
        AwkRegexCaseSensitivity caseSensitivity) =>
        new(
            translatedPattern,
            AwkRegexBackend.DotNet(
                caseSensitivity,
                new Regex(translatedPattern, ResolveOptions(caseSensitivity), MatchTimeout)));

    internal static AwkRegex CreateUtf8(
        string translatedPattern,
        AwkRegexCaseSensitivity caseSensitivity) =>
        new(
            translatedPattern,
            AwkRegexBackend.Utf8(
                caseSensitivity,
                new Utf8RegexEngine(translatedPattern, ResolveOptions(caseSensitivity), MatchTimeout)));

    internal static bool RequiresDotNetEngine(string translatedPattern) =>
        translatedPattern.Contains(@"\A", StringComparison.Ordinal) ||
        translatedPattern.Contains(@"\z", StringComparison.Ordinal);

    internal static RegexOptions ResolveOptions(AwkRegexCaseSensitivity caseSensitivity) =>
        RegexOptions.CultureInvariant |
        (caseSensitivity is AwkRegexCaseSensitivity.IgnoreCase
            ? RegexOptions.IgnoreCase
            : RegexOptions.None);

    public static string TranslatePattern(AwkRegexPattern pattern)
    {
        return Translate(pattern.Text);
        static string Translate(string pattern)
        {
            return TranslatePosixCharacterClasses(TranslateEscapes(pattern));
            static string TranslateEscapes(string pattern)
            {
                var slash = pattern.IndexOf('\\', StringComparison.Ordinal);
                if (slash < 0)
                    return pattern;

                var builder = new StringBuilder(pattern.Length);
                builder.Append(pattern, 0, slash);

                for (var index = slash; index < pattern.Length; index++)
                {
                    var current = pattern[index];
                    if (current != '\\' || index + 1 >= pattern.Length)
                    {
                        builder.Append(current);
                        continue;
                    }

                    index++;
                    var escaped = pattern[index];
                    if (AwkEscapes.TryReadNumericEscape(pattern, ref index, pattern.Length, out var numeric))
                    {
                        AppendLiteral(builder, numeric);
                        continue;
                    }

                    switch (escaped)
                    {
                        case '<':
                            builder.Append(@"\b(?=\w)");
                            break;
                        case '>':
                            builder.Append(@"(?<=\w)\b");
                            break;
                        case 'y':
                            builder.Append(@"\b");
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
                            if (escaped is 'a' or 'b' or 'f' or 'n' or 'r' or 't' or 'v' &&
                                AwkEscapes.TryDecodeSimpleEscape(escaped, out var decoded))
                            {
                                AppendLiteral(builder, decoded);
                            }
                            else
                            {
                                builder.Append('\\');
                                builder.Append(escaped);
                            }
                            break;
                    }
                }

                return builder.ToString();

                static void AppendLiteral(StringBuilder builder, char value) =>
                    builder.Append(Regex.Escape(value.ToString()));
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
