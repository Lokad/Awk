using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Lokad.Awk;

internal static class AwkRegex
{
    public static readonly TimeSpan MatchTimeout =
        TimeSpan.FromSeconds(AwkRuntimeLimits.DefaultRegexMatchTimeoutSeconds);

    public static Regex Create(
        AwkRegexPattern pattern,
        bool ignoreCase,
        RegexOptions extraOptions = RegexOptions.None) =>
        new(
            TranslatePattern(pattern),
            RegexOptions.CultureInvariant |
                (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) |
                extraOptions,
            MatchTimeout);

    public static string TranslatePattern(AwkRegexPattern pattern) =>
        Translate(pattern.Text);

    private static string Translate(string pattern) =>
        TranslatePosixCharacterClasses(TranslateEscapes(pattern));

    private static string TranslateEscapes(string pattern)
    {
        var slash = pattern.IndexOf('\\');
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
    }

    private static string TranslatePosixCharacterClasses(string pattern)
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
                continue;

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
    }

    private static int FindBracketExpressionEnd(string pattern, int start)
    {
        var index = start;
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

            if (pattern[index] == ']')
            {
                if (index > start && HasPosixBracketTokenStart(pattern, start, index, pattern[index - 1]))
                    continue;

                return index;
            }
        }

        return -1;
    }

    private static bool HasPosixBracketTokenStart(string pattern, int start, int end, char marker)
    {
        if (marker is not (':' or '=' or '.'))
            return false;

        for (var index = start; index + 1 < end; index++)
        {
            if (pattern[index] == '[' && pattern[index + 1] == marker)
                return true;
        }

        return false;
    }

    private static string TranslatePosixClassTokens(string content)
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
    }

    private static string TranslatePosixEquivalenceAndCollatingTokens(string content)
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
    }

    private static void AppendBracketLiteral(StringBuilder builder, char value)
    {
        if (value is '\\' or ']' or '-' or '^')
            builder.Append('\\');

        builder.Append(value);
    }

    private static void AppendLiteral(StringBuilder builder, char value) =>
        builder.Append(Regex.Escape(value.ToString()));
}
