using System.Text;
using System.Text.RegularExpressions;

namespace Lokad.Awk;

/// <summary>Cancellation boundaries for Awk-owned regex preparation, without retaining caller callbacks.</summary>
internal static class AwkRegexConstruction
{
    internal static int BracketEnd(string pattern, int opening, Action? checkpoint = null)
    {
        var index = opening + 1;
        long nextCheckpoint = 0;
        if (index < pattern.Length && pattern[index] == '^')
            index++;
        if (index < pattern.Length && pattern[index] == ']')
            index++;
        for (; index < pattern.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
            if (pattern[index] == '\\')
                index++;
            else if (pattern[index] == ']')
                return index;
        }
        checkpoint?.Invoke();
        return -1;
    }

    internal const int ChunkCharacters = AwkRuntimeLimits.CancellationCheckBytes / sizeof(char);

    public static void CheckProgress(int position, ref long next, Action? checkpoint)
    {
        if (checkpoint is null || position < next)
            return;
        checkpoint();
        next = (long)position + ChunkCharacters;
    }

    public static int IndexOf(string text, char value, int start, Action? checkpoint)
    {
        if (checkpoint is null)
            return text.IndexOf(value, start);
        for (var position = start; position < text.Length;)
        {
            checkpoint();
            var length = Math.Min(ChunkCharacters, text.Length - position);
            var offset = text.AsSpan(position, length).IndexOf(value);
            checkpoint();
            if (offset >= 0)
                return position + offset;
            position += length;
        }
        checkpoint();
        return -1;
    }

    // Preparation callers use short fixed tokens. Overlap preserves matches crossing a window.
    public static int IndexOf(string text, string value, int start, Action? checkpoint)
    {
        if (checkpoint is null)
            return text.IndexOf(value, start, StringComparison.Ordinal);
        if (value.Length == 0)
        {
            checkpoint();
            return start;
        }
        for (var position = start; position < text.Length;)
        {
            checkpoint();
            var advance = Math.Min(ChunkCharacters, text.Length - position);
            var length = (int)Math.Min(text.Length - (long)position, advance + (long)value.Length - 1);
            var offset = text.AsSpan(position, length).IndexOf(value.AsSpan(), StringComparison.Ordinal);
            checkpoint();
            if (offset >= 0)
                return position + offset;
            position += advance;
        }
        checkpoint();
        return -1;
    }

    public static bool Equals(string left, string right, Action? checkpoint)
    {
        checkpoint?.Invoke();
        if (left.Length != right.Length)
            return false;
        if (checkpoint is null)
            return string.Equals(left, right, StringComparison.Ordinal);
        for (var position = 0; position < left.Length;)
        {
            checkpoint();
            var length = Math.Min(ChunkCharacters, left.Length - position);
            var equal = left.AsSpan(position, length).SequenceEqual(right.AsSpan(position, length));
            checkpoint();
            if (!equal)
                return false;
            position += length;
        }
        checkpoint();
        return true;
    }

    public static void Append(StringBuilder builder, string text, int start, int count, Action? checkpoint)
    {
        if (checkpoint is null)
        {
            builder.Append(text, start, count);
            return;
        }
        checkpoint();
        while (count > 0)
        {
            var length = Math.Min(ChunkCharacters, count);
            builder.Append(text, start, length);
            checkpoint();
            start += length;
            count -= length;
        }
    }

    public static string Finish(StringBuilder builder, Action? checkpoint)
    {
        checkpoint?.Invoke();
        var text = builder.ToString();
        checkpoint?.Invoke();
        return text;
    }

    public static string EscapeLiteral(string text, Action? checkpoint,
        AwkMemoryAccounting governor, AwkMemoryLease resultLease)
    {
        checkpoint?.Invoke();
        if (!governor.IsEnabled)
        {
            var sharedOrEscaped = Regex.Escape(text);
            checkpoint?.Invoke();
            return sharedOrEscaped;
        }
        // These are Regex.Escape's net10.0 metacharacters, including its ASCII
        // whitespace/comment escapes. Matching and escaping still use the BCL API.
        var escapedCharacters = 0;
        long progress = 0;
        for (var index = 0; index < text.Length; index++)
        {
            CheckProgress(index, ref progress, checkpoint);
            if (text[index] is '\t' or '\n' or '\f' or '\r' or ' ' or '#' or '$' or
                '(' or ')' or '*' or '+' or '.' or '?' or '[' or '\\' or '^' or '{' or '|')
                escapedCharacters = AwkRuntimeMemory.CheckedAdd(escapedCharacters, 1);
        }
        checkpoint?.Invoke();
        if (escapedCharacters == 0)
            return text;

        var length = AwkRuntimeMemory.CheckedAdd(text.Length, escapedCharacters);
        using var scratch = governor.ReserveTransient(
            AwkRuntimeMemory.EstimateRegexLiteralEscapeScratchBytes(text.Length, length));
        resultLease.ReserveAdditional(AwkRuntimeMemory.EstimateUtf16StringBytes(length));
        checkpoint?.Invoke();
        var escaped = Regex.Escape(text);
        checkpoint?.Invoke();
        return escaped;
    }

    public static long EstimateDialectBuilderScratch(string pattern, int escapeExpansion,
        int colonExpansion, bool quoteRightParenthesis, Action? checkpoint)
    {
        long maximumCharacters = pattern.Length;
        long progress = 0;
        for (var index = 0; index < pattern.Length; index++)
        {
            CheckProgress(index, ref progress, checkpoint);
            var extra = pattern[index] == '\\' ? escapeExpansion :
                pattern[index] == ':' ? colonExpansion :
                pattern[index] == '.' ? 5 : pattern[index] == '$' ? 1 :
                quoteRightParenthesis && pattern[index] == ')' ? 1 : 0;
            maximumCharacters = AwkRuntimeMemory.CheckedAdd(maximumCharacters, extra);
        }
        checkpoint?.Invoke();
        // Builder capacity/growth plus fixed literal/marker formatting scratch.
        return AwkRuntimeMemory.CheckedAdd(256, AwkRuntimeMemory.CheckedMultiply(maximumCharacters, 4));
    }

    public static string Replace(string text, string oldValue, string newValue, Action? checkpoint,
        AwkMemoryLease? resultLease = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(oldValue);
        if (checkpoint is null && resultLease is null)
            return text.Replace(oldValue, newValue, StringComparison.Ordinal);
        var index = IndexOf(text, oldValue, 0, checkpoint);
        if (index < 0 || string.Equals(oldValue, newValue, StringComparison.Ordinal))
            return text;
        var count = 0;
        do
        {
            count = AwkRuntimeMemory.CheckedAdd(count, 1);
            index = IndexOf(text, oldValue, index + oldValue.Length, checkpoint);
        } while (index >= 0);
        var remaining = text.Length - AwkRuntimeMemory.CheckedMultiply(oldValue.Length, count);
        var length = AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedAdd(remaining,
            AwkRuntimeMemory.CheckedMultiply(newValue.Length, count)));
        resultLease?.ReserveAdditional(AwkRuntimeMemory.EstimateUtf16StringBytes(length));
        checkpoint?.Invoke();
        // Fill one exact result string. Its charge remains with the caller's preparation lease.
        var result = string.Create(length, (Text: text, Old: oldValue, New: newValue, Checkpoint: checkpoint),
            static (destination, state) =>
            {
                var copied = 0;
                var written = 0;
                var index = IndexOf(state.Text, state.Old, 0, state.Checkpoint);
                while (index >= 0)
                {
                    var prefix = state.Text.AsSpan(copied, index - copied);
                    Copy(prefix, destination[written..], state.Checkpoint);
                    written += prefix.Length;
                    Copy(state.New.AsSpan(), destination[written..], state.Checkpoint);
                    written += state.New.Length;
                    copied = index + state.Old.Length;
                    index = IndexOf(state.Text, state.Old, copied, state.Checkpoint);
                }
                Copy(state.Text.AsSpan(copied), destination[written..], state.Checkpoint);
            });
        checkpoint?.Invoke();
        return result;
    }

    private static void Copy(ReadOnlySpan<char> source, Span<char> destination, Action? checkpoint)
    {
        checkpoint?.Invoke();
        for (var position = 0; position < source.Length;)
        {
            var length = Math.Min(ChunkCharacters, source.Length - position);
            source.Slice(position, length).CopyTo(destination[position..]);
            checkpoint?.Invoke();
            position += length;
        }
    }
}
