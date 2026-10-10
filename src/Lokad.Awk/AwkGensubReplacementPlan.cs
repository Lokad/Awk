using System;
using System.Buffers;

namespace Lokad.Awk;

/// <summary>Owns scratch storage for a parsed GNU <c>gensub</c> replacement for one substitution operation.</summary>
internal readonly struct AwkGensubReplacementPlan : IDisposable
{
    private const byte EscapeByte = 0x5C;

    private readonly AwkText? _replacement;
    private readonly AwkGensubReplacementPart[]? _parts;
    private readonly int _partCount;
    private readonly AwkMemoryLease? _memoryLease;
    private readonly bool _pooled;

    private AwkGensubReplacementPlan(
        AwkText replacement,
        AwkGensubReplacementPart[] parts,
        int partCount,
        AwkRegexCapturePlan capturePlan,
        AwkMemoryLease memoryLease,
        bool pooled)
    {
        _replacement = replacement;
        _parts = parts;
        _partCount = partCount;
        CapturePlan = capturePlan;
        _memoryLease = memoryLease;
        _pooled = pooled;
    }

    public AwkRegexCapturePlan CapturePlan { get; }

    public static AwkGensubReplacementPlan Rent(
        AwkText replacement,
        Action? checkpoint = null,
        AwkMemoryGovernor? governor = null)
    {
        if (replacement.ByteLength == 0)
            return default;

        governor ??= AwkMemoryGovernor.Disabled;
        var bytes = replacement.AsSpan(checkpoint);
        var pooled = !governor.IsEnabled;
        // Finite budgets count the actual parts and allocate their exact capacity.
        // ArrayPool only promises a minimum length, so its capacity is not an admission bound.
        var capacity = pooled ? bytes.Length : Parse(bytes, [], checkpoint).PartCount;
        var memoryLease = governor.ReserveTransient(
            AwkRuntimeMemory.EstimateGensubReplacementPartArrayBytes(capacity));
        AwkGensubReplacementPart[]? parts = null;
        try
        {
            checkpoint?.Invoke();
            parts = pooled
                ? ArrayPool<AwkGensubReplacementPart>.Shared.Rent(capacity)
                : new AwkGensubReplacementPart[capacity];
            checkpoint?.Invoke();
            var parsed = Parse(bytes, parts, checkpoint);
            var capturePlan = parsed.MaximumGroupIndex < 0
                ? AwkRegexCapturePlan.None
                : AwkRegexCapturePlan.ThroughGroup(parsed.MaximumGroupIndex);
            return new AwkGensubReplacementPlan(
                replacement, parts, parsed.PartCount, capturePlan, memoryLease, pooled);
        }
        catch
        {
            if (pooled && parts is not null)
                ArrayPool<AwkGensubReplacementPart>.Shared.Return(parts);
            memoryLease.Dispose();
            throw;
        }
    }

    private static ParsedParts Parse(
        ReadOnlySpan<byte> bytes,
        Span<AwkGensubReplacementPart> parts,
        Action? checkpoint)
    {
        var partCount = 0;
        var maximumGroupIndex = -1;
        long nextCheckpoint = 0;
        for (var index = 0; index < bytes.Length; index++)
        {
            CheckProgress(index);
            var current = bytes[index];
            if (current == (byte)'&')
            {
                AddPart(parts, ref partCount, AwkGensubReplacementPart.WholeMatch());
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
                    CheckProgress(index);
                }

                AddPart(parts, ref partCount, AwkGensubReplacementPart.Literal(
                    literalStart, index - literalStart + 1));
                continue;
            }

            if (index + 1 >= bytes.Length)
            {
                AddPart(parts, ref partCount, AwkGensubReplacementPart.Literal(index, length: 1));
                continue;
            }

            var escapedIndex = ++index;
            var escaped = bytes[escapedIndex];
            if (escaped is >= (byte)'0' and <= (byte)'9')
            {
                var groupIndex = escaped - (byte)'0';
                AddPart(parts, ref partCount, AwkGensubReplacementPart.Group(groupIndex));
                maximumGroupIndex = Math.Max(maximumGroupIndex, groupIndex);
                continue;
            }

            AddPart(parts, ref partCount, AwkGensubReplacementPart.Literal(escapedIndex, length: 1));
        }

        checkpoint?.Invoke();
        return new ParsedParts(partCount, maximumGroupIndex);

        void CheckProgress(int offset)
        {
            if (offset < nextCheckpoint)
                return;
            checkpoint?.Invoke();
            nextCheckpoint = (long)offset + AwkExecutionGovernor.WorkChunkBytes;
        }

        static void AddPart(Span<AwkGensubReplacementPart> parts, ref int count, AwkGensubReplacementPart part)
        {
            if (!parts.IsEmpty)
                parts[count] = part;
            count++;
        }
    }

    private readonly record struct ParsedParts(int PartCount, int MaximumGroupIndex);

    public void Append(AwkRegexMatch match, AwkUtf8Builder builder, Action? checkpoint = null)
    {
        if (_parts is not { } parts || _replacement is not { } replacement)
            return;

        var replacementBytes = replacement.AsSpan(checkpoint);
        for (var index = 0; index < _partCount; index++)
        {
            if ((index & 1023) == 0)
                checkpoint?.Invoke();
            var part = parts[index];
            switch (part.Kind)
            {
                case AwkGensubReplacementPartKind.Literal:
                    builder.Append(replacementBytes.Slice(part.Start, part.Length), checkpoint);
                    break;
                case AwkGensubReplacementPartKind.WholeMatch:
                    builder.Append(match.Value, checkpoint);
                    break;
                case AwkGensubReplacementPartKind.Group:
                    if (part.GroupIndex < match.Groups.Count && match.Groups[part.GroupIndex].Success)
                        builder.Append(match.Groups[part.GroupIndex].Value, checkpoint);
                    break;
                default:
                    throw new InvalidOperationException("Unknown gensub replacement part.");
            }
        }
        checkpoint?.Invoke();
    }

    public void Dispose()
    {
        if (_pooled && _parts is { } parts)
            ArrayPool<AwkGensubReplacementPart>.Shared.Return(parts);
        _memoryLease?.Dispose();
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
