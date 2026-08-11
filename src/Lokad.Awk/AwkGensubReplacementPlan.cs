using System;
using System.Buffers;

namespace Lokad.Awk;

/// <summary>Owns a pooled, parsed GNU <c>gensub</c> replacement for one substitution operation.</summary>
internal readonly struct AwkGensubReplacementPlan : IDisposable
{
    private const byte EscapeByte = 0x5C;

    private readonly AwkText? _replacement;
    private readonly AwkGensubReplacementPart[]? _parts;
    private readonly int _partCount;

    private AwkGensubReplacementPlan(
        AwkText replacement,
        AwkGensubReplacementPart[] parts,
        int partCount,
        AwkRegexCapturePlan capturePlan)
    {
        _replacement = replacement;
        _parts = parts;
        _partCount = partCount;
        CapturePlan = capturePlan;
    }

    public AwkRegexCapturePlan CapturePlan { get; }

    public static AwkGensubReplacementPlan Rent(AwkText replacement)
    {
        if (replacement.ByteLength == 0)
            return default;

        var bytes = replacement.AsSpan();
        var parts = ArrayPool<AwkGensubReplacementPart>.Shared.Rent(bytes.Length);
        var partCount = 0;
        var maximumGroupIndex = -1;
        try
        {
            for (var index = 0; index < bytes.Length; index++)
            {
                var current = bytes[index];
                if (current == (byte)'&')
                {
                    parts[partCount++] = AwkGensubReplacementPart.WholeMatch();
                    maximumGroupIndex = Math.Max(maximumGroupIndex, 0);
                    continue;
                }

                if (current != EscapeByte)
                {
                    var literalStart = index;
                    while (index + 1 < bytes.Length &&
                        bytes[index + 1] is not (byte)'&' and not EscapeByte)
                    {
                        index++;
                    }

                    parts[partCount++] = AwkGensubReplacementPart.Literal(
                        literalStart,
                        index - literalStart + 1);
                    continue;
                }

                if (index + 1 >= bytes.Length)
                {
                    parts[partCount++] = AwkGensubReplacementPart.Literal(index, length: 1);
                    continue;
                }

                var escapedIndex = ++index;
                var escaped = bytes[escapedIndex];
                if (escaped is >= (byte)'0' and <= (byte)'9')
                {
                    var groupIndex = escaped - (byte)'0';
                    parts[partCount++] = AwkGensubReplacementPart.Group(groupIndex);
                    maximumGroupIndex = Math.Max(maximumGroupIndex, groupIndex);
                    continue;
                }

                parts[partCount++] = AwkGensubReplacementPart.Literal(escapedIndex, length: 1);
            }

            var capturePlan = maximumGroupIndex < 0
                ? AwkRegexCapturePlan.None
                : AwkRegexCapturePlan.ThroughGroup(maximumGroupIndex);
            return new AwkGensubReplacementPlan(replacement, parts, partCount, capturePlan);
        }
        catch
        {
            ArrayPool<AwkGensubReplacementPart>.Shared.Return(parts);
            throw;
        }
    }

    public void Append(AwkRegexMatch match, AwkUtf8Builder builder)
    {
        if (_parts is not { } parts || _replacement is not { } replacement)
            return;

        var replacementBytes = replacement.AsSpan();
        for (var index = 0; index < _partCount; index++)
        {
            var part = parts[index];
            switch (part.Kind)
            {
                case AwkGensubReplacementPartKind.Literal:
                    builder.Append(replacementBytes.Slice(part.Start, part.Length));
                    break;
                case AwkGensubReplacementPartKind.WholeMatch:
                    builder.Append(match.Value);
                    break;
                case AwkGensubReplacementPartKind.Group:
                    if (part.GroupIndex < match.Groups.Count && match.Groups[part.GroupIndex].Success)
                        builder.Append(match.Groups[part.GroupIndex].Value);
                    break;
                default:
                    throw new InvalidOperationException("Unknown gensub replacement part.");
            }
        }
    }

    public void Dispose()
    {
        if (_parts is { } parts)
            ArrayPool<AwkGensubReplacementPart>.Shared.Return(parts);
    }

    private readonly struct AwkGensubReplacementPart
    {
        private AwkGensubReplacementPart(
            AwkGensubReplacementPartKind kind,
            int start,
            int length,
            int groupIndex)
        {
            Kind = kind;
            Start = start;
            Length = length;
            GroupIndex = groupIndex;
        }

        public AwkGensubReplacementPartKind Kind { get; }

        public int Start { get; }

        public int Length { get; }

        public int GroupIndex { get; }

        public static AwkGensubReplacementPart Literal(int start, int length) =>
            new(AwkGensubReplacementPartKind.Literal, start, length, groupIndex: 0);

        public static AwkGensubReplacementPart WholeMatch() =>
            new(AwkGensubReplacementPartKind.WholeMatch, start: 0, length: 0, groupIndex: 0);

        public static AwkGensubReplacementPart Group(int groupIndex) =>
            new(AwkGensubReplacementPartKind.Group, start: 0, length: 0, groupIndex);
    }

    private enum AwkGensubReplacementPartKind
    {
        Literal,
        WholeMatch,
        Group
    }
}
