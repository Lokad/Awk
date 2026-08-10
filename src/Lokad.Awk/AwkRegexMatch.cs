using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Lokad.Utf8Regex;

namespace Lokad.Awk;

internal readonly record struct AwkRegexMatch(
    bool Success,
    int Index,
    int Length,
    int ByteIndex,
    int ByteLength,
    AwkText Value,
    IReadOnlyList<AwkRegexGroup> Groups)
{
    public static AwkRegexMatch NoMatch { get; } =
        new(false, 0, 0, 0, 0, AwkText.Empty, []);

    public static AwkRegexMatch FromMatch(
        Match match,
        string input,
        int byteIndex,
        int byteLength,
        bool useByteOffsets,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (!match.Success)
            return NoMatch;

        var groups = new AwkRegexGroup[match.Groups.Count];
        counters.RecordRegexGroupsMaterialized(groups.Length);
        for (var index = 0; index < groups.Length; index++)
        {
            groups[index] = index == 0
                ? new AwkRegexGroup(
                    true,
                    useByteOffsets ? byteIndex : match.Index,
                    useByteOffsets ? byteLength : match.Length,
                    AwkText.FromUtf16(match.ValueSpan, counters, governor))
                : FromGroup(
                    match.Groups[index],
                    input,
                    match.Index,
                    byteIndex,
                    useByteOffsets,
                    counters,
                    governor);
        }

        var value = groups.Length == 0
            ? AwkText.FromUtf16(match.ValueSpan, counters, governor)
            : groups[0].Value;
        return new AwkRegexMatch(
            true,
            useByteOffsets ? byteIndex : match.Index,
            useByteOffsets ? byteLength : match.Length,
            byteIndex,
            byteLength,
            value,
            groups);
    }

    public static AwkRegexMatch FromUtf8Match(
        Utf8MatchContext match,
        bool useByteOffsets,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (!match.Success)
            return NoMatch;

        var groups = new AwkRegexGroup[match.GroupCount];
        counters.RecordRegexGroupsMaterialized(groups.Length);
        for (var index = 0; index < groups.Length; index++)
        {
            groups[index] = FromUtf8Group(match.GetGroup(index), useByteOffsets, counters, governor);
        }

        var value = groups.Length == 0
            ? CreateText(match, counters, governor)
            : groups[0].Value;
        return new AwkRegexMatch(
            true,
            useByteOffsets ? match.IndexInBytes : match.IndexInUtf16,
            useByteOffsets ? match.LengthInBytes : match.LengthInUtf16,
            match.IndexInBytes,
            match.LengthInBytes,
            value,
            groups);
    }

    private static AwkRegexGroup FromGroup(
        Group group,
        string input,
        int matchUtf16Index,
        int matchByteIndex,
        bool useByteOffsets,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        static int ResolveGroupIndex(
            Group group,
            string input,
            int matchUtf16Index,
            int matchByteIndex,
            bool useByteOffsets)
        {
            if (!useByteOffsets)
                return group.Index;

            if (!group.Success)
                return 0;

            return matchByteIndex + Encoding.UTF8.GetByteCount(
                input.AsSpan(matchUtf16Index, group.Index - matchUtf16Index));
        }

        static int ResolveGroupLength(Group group, string input, bool useByteOffsets) =>
            useByteOffsets
                ? Encoding.UTF8.GetByteCount(input.AsSpan(group.Index, group.Length))
                : group.Length;

        return new AwkRegexGroup(
            group.Success,
            ResolveGroupIndex(group, input, matchUtf16Index, matchByteIndex, useByteOffsets),
            ResolveGroupLength(group, input, useByteOffsets),
            AwkText.FromUtf16(group.ValueSpan, counters, governor));
    }

    private static AwkRegexGroup FromUtf8Group(
        Utf8GroupContext group,
        bool useByteOffsets,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor) =>
        new(
            group.Success,
            useByteOffsets ? group.IndexInBytes : group.IndexInUtf16,
            useByteOffsets ? group.LengthInBytes : group.LengthInUtf16,
            CreateText(group, counters, governor));

    private static AwkText CreateText(Utf8MatchContext match, AwkRuntimeCounters counters, AwkMemoryGovernor governor) =>
        match.TryGetValueBytes(out var bytes)
            ? AwkText.CopyTrustedBytes(bytes, counters, governor)
            : AwkText.FromString(match.GetValueString(), counters, governor);

    private static AwkText CreateText(Utf8GroupContext group, AwkRuntimeCounters counters, AwkMemoryGovernor governor) =>
        group.TryGetValueBytes(out var bytes)
            ? AwkText.CopyTrustedBytes(bytes, counters, governor)
            : AwkText.FromString(group.GetValueString(), counters, governor);

}

internal readonly record struct AwkRegexGroup(
    bool Success,
    int Index,
    int Length,
    AwkText Value);

internal readonly record struct AwkRegexMatchPosition(
    bool Success,
    int Index,
    int Length)
{
    public static AwkRegexMatchPosition FromSpan(
        AwkRegexMatchSpan match,
        AwkRegexRuntime.AwkRegexCoordinatePolicy coordinatePolicy) =>
        coordinatePolicy == AwkRegexRuntime.AwkRegexCoordinatePolicy.Byte
            ? new AwkRegexMatchPosition(match.Success, match.ByteIndex, match.ByteLength)
            : new AwkRegexMatchPosition(match.Success, match.Utf16Index, match.Utf16Length);
}

internal readonly record struct AwkRegexMatchSpan(
    bool Success,
    int ByteIndex,
    int ByteLength,
    int Utf16Index,
    int Utf16Length)
{
    public static AwkRegexMatchSpan NoMatch { get; } = new(false, 0, 0, 0, 0);

    public static AwkRegexMatchSpan Match(int byteIndex, int byteLength) =>
        new(true, byteIndex, byteLength, byteIndex, byteLength);

    public static AwkRegexMatchSpan Match(
        int byteIndex,
        int byteLength,
        int utf16Index,
        int utf16Length) =>
        new(true, byteIndex, byteLength, utf16Index, utf16Length);
}

internal readonly record struct AwkRegexSearchOffset(int ByteIndex, int Utf16Index)
{
    public static AwkRegexSearchOffset Start { get; } = new(0, 0);

    public static AwkRegexSearchOffset AfterMatch(AwkText input, AwkRegexMatchSpan match)
    {
        if (match.ByteLength > 0 || match.ByteIndex >= input.ByteLength)
        {
            return new AwkRegexSearchOffset(
                match.ByteIndex + match.ByteLength,
                match.Utf16Index + match.Utf16Length);
        }

        var status = Rune.DecodeFromUtf8(input.AsSpan()[match.ByteIndex..], out var rune, out var bytesConsumed);
        return status == OperationStatus.Done && bytesConsumed > 0
            ? new AwkRegexSearchOffset(
                match.ByteIndex + bytesConsumed,
                match.Utf16Index + rune.Utf16SequenceLength)
            : new AwkRegexSearchOffset(match.ByteIndex + 1, match.Utf16Index + 1);
    }
}
