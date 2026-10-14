using System.Runtime.ExceptionServices;

namespace Lokad.Awk;

internal static partial class AwkRegexScalarPattern
{
    public static string Translate(string pattern, Action? checkpoint = null,
        AwkMemoryAccounting? governor = null, AwkMemoryLease? resultLease = null)
    {
        if (governor is null || !governor.IsEnabled)
            return TranslateCore(pattern, checkpoint);

        using var metadata = governor.CreateTransientLease();
        var measure = new ScalarMeasure(metadata, checkpoint);
        var allocation = measure.Measure(pattern);
        using var scratch = governor.ReserveTransient(allocation.ScratchBytes);
        using var ownedResult = resultLease is null ? governor.CreateTransientLease() : AwkMemoryLease.Disabled;
        var retained = resultLease ?? ownedResult;
        retained.ReserveAdditional(allocation.ResultBytes);
        try
        {
            var result = TranslateCore(pattern, checkpoint);
            var actualBytes = ReferenceEquals(result, pattern) ? 0 : StringBytes(result.Length);
            if (actualBytes > allocation.ResultBytes)
                throw new InvalidOperationException("Scalar translation exceeded its admitted output bound.");
            retained.Release(allocation.ResultBytes - actualBytes);
            return result;
        }
        catch
        {
            retained.Release(allocation.ResultBytes);
            throw;
        }
    }

    private static int CategoryClosing(ReadOnlySpan<char> pattern, int start, Action? checkpoint)
    {
        for (var index = start; index < pattern.Length; index += Math.Min(AwkRegexConstruction.ChunkCharacters, pattern.Length - index))
        {
            checkpoint?.Invoke();
            var found = pattern.Slice(index, Math.Min(AwkRegexConstruction.ChunkCharacters, pattern.Length - index)).IndexOf('}');
            if (found >= 0)
                return index + found;
        }
        return -1;
    }

    internal static void SortScalarRanges(List<ScalarRange> ranges, Action? checkpoint)
    {
        if (checkpoint is null)
        {
            ranges.Sort(static (left, right) => left.First.CompareTo(right.First));
            return;
        }
        var comparisons = 0;
        try
        {
            ranges.Sort((left, right) =>
            {
                if ((++comparisons & 1023) == 0) checkpoint();
                return left.First.CompareTo(right.First);
            });
        }
        catch (InvalidOperationException error) when (error.InnerException is OperationCanceledException or AwkRuntimeException)
        {
            if (error.InnerException is Exception cause)
                ExceptionDispatchInfo.Capture(cause).Throw();
            throw;
        }
    }

    private static long Add(long left, long right) => AwkRuntimeMemory.CheckedAdd(left, right);
    private static long Times(long value, int factor) => AwkRuntimeMemory.CheckedMultiply(value, factor);
    private static long StringBytes(long characters) => characters == 0 ? 0 : Add(24, Times(characters, 2));

    private readonly record struct ScalarAllocation(long ScratchBytes, long ResultBytes);

    // Only finite-memory execution needs this nonallocating sizing pass. Bounds cover
    // builder growth, coexisting strings and old/new range buffers; they deliberately
    // exceed CLR payload sizes. Shared category metadata has a separate admission.
    private sealed class ScalarMeasure(AwkMemoryLease metadata, Action? checkpoint)
    {
        private CategoryRange[]? _categories;

        public ScalarAllocation Measure(string pattern)
        {
            long output = pattern.Length;
            long nestedScratch = 0;
            var rewritten = false;
            var aborted = false;
            long nextCheckpoint = 0;
            for (var index = 0; index < pattern.Length; index++)
            {
                AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
                var start = index;
                var end = index + 1;
                long replacement = -1;
                switch (pattern[index])
                {
                    case '\\':
                        if (++index < pattern.Length)
                        {
                            end = index + 1;
                            if (TryReadCategory(pattern, index, out var mask, out end, checkpoint) ||
                                TryShorthandCategory(pattern[index], out mask))
                            {
                                var ranges = CategoryCount(mask);
                                var positive = Add(BmpBoundary.Length + end - start, Times(ranges, 96));
                                replacement = Add(positive, 4);
                                nestedScratch = Math.Max(nestedScratch, AtomScratch(positive, ranges, end - start));
                            }
                        }
                        break;
                    case '.': replacement = AnyScalar.Length; break;
                    case '$': replacement = 2; break;
                    case '[':
                        end = AwkRegexSelection.BracketEnd(pattern, index, checkpoint);
                        if (end < 0)
                        {
                            aborted = true;
                            break;
                        }
                        end++;
                        var bracket = Bracket(pattern.AsSpan(index + 1, end - index - 2));
                        replacement = bracket.Replacement;
                        nestedScratch = Math.Max(nestedScratch, bracket.Scratch);
                        break;
                    case '(' when end < pattern.Length && pattern[end] == ')':
                        replacement = @"(\x00{0})".Length;
                        end++;
                        break;
                    default:
                        if (char.IsHighSurrogate(pattern[index]) && end < pattern.Length && char.IsLowSurrogate(pattern[end]))
                        {
                            end++;
                            replacement = 6;
                            nestedScratch = Math.Max(nestedScratch, 128);
                        }
                        break;
                }
                if (aborted) break;
                if (replacement >= 0)
                {
                    rewritten = true;
                    output = Add(output - (end - start), replacement);
                }
                index = end - 1;
            }
            checkpoint?.Invoke();
            // StringBuilder capacity can exceed length. Four bytes per character
            // admit UTF-16 payload and growth slack, including the original capacity.
            var outer = rewritten ? Add(128, Times(Math.Max(pattern.Length, output), 4)) : 0;
            return new(Add(outer, nestedScratch), rewritten && !aborted ? StringBytes(output) : 0);
        }

        private (long Replacement, long Scratch) Bracket(ReadOnlySpan<char> content)
        {
            if (content.Length > 0 && char.IsAsciiLetter(content[0]))
            {
                var lower = char.ToLowerInvariant(content[0]);
                var hasLower = false;
                var hasUpper = false;
                var sameLetter = true;
                long nextCheckpoint = 0;
                for (var scan = 0; scan < content.Length; scan++)
                {
                    AwkRegexConstruction.CheckProgress(scan, ref nextCheckpoint, checkpoint);
                    if (!char.IsAsciiLetter(content[scan]) || char.ToLowerInvariant(content[scan]) != lower)
                    {
                        sameLetter = false;
                        break;
                    }
                    hasLower |= content[scan] == lower;
                    hasUpper |= content[scan] != lower;
                }
                if (sameLetter) return (hasLower && hasUpper ? 7 : 1, Add(128, Times(content.Length, 2)));
            }
            var negated = content.StartsWith("^");
            var requiresTranslation = negated;
            long ranges = 0;
            long progress = 0;
            for (var index = negated ? 1 : 0; index < content.Length;)
            {
                AwkRegexConstruction.CheckProgress(index, ref progress, checkpoint);
                var scalar = ReadAtom(content, ref index, out var mask, checkpoint);
                var last = scalar;
                if (scalar >= 0 && index + 1 < content.Length && content[index] == '-')
                {
                    var end = index + 1;
                    var rangeLast = ReadAtom(content, ref end, out _, checkpoint);
                    if (rangeLast >= 0)
                    {
                        if (rangeLast < scalar) throw new ArgumentException("Invalid character range.");
                        last = rangeLast;
                        index = end;
                    }
                }
                if (last >= 0x10000)
                {
                    ranges = Add(ranges, 1);
                    requiresTranslation = true;
                }
                if (scalar < 0 && mask != 0)
                {
                    ranges = Add(ranges, CategoryCount(mask));
                    requiresTranslation = true;
                }
            }
            // A code-unit range needs at most thirteen characters. Each scalar
            // range emits at most three surrogate pairs of at most 31 characters;
            // sorting/coalescing can only reduce their combined bound.
            var bmp = Times(content.Length, 13);
            var positive = Add(Add(BmpBoundary.Length + 2, bmp), Times(ranges, 96));
            var scratch = Add(AtomScratch(positive, ranges, content.Length), Add(128, Times(bmp, 4)));
            var replacement = negated
                ? Add(positive, "(?:(?!(?:".Length + "))".Length + AnyScalar.Length + ")".Length)
                : Add(positive, 4);
            return (requiresTranslation ? replacement : -1, scratch);
        }

        private static long AtomScratch(long positive, long ranges, int contentLength) =>
            Add(Add(512, Times(positive, 8)), Add(Times(ranges, 32), Times(contentLength, 2)));

        private int CategoryCount(uint mask)
        {
            var categories = _categories;
            if (categories is null)
            {
                categories = GetCategoryRanges(checkpoint, metadata);
                _categories = categories;
            }
            var selected = 0;
            for (var index = 0; index < categories.Length; index++)
            {
                if ((index & 63) == 0) checkpoint?.Invoke();
                if ((mask & Bit(categories[index].Category)) != 0) selected++;
            }
            return selected;
        }
    }
}
