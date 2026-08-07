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

    public static AwkRegexMatch FromMatch(Match match, string input, bool useByteOffsets, AwkRuntimeCounters counters)
    {
        if (!match.Success)
            return NoMatch;

        var groups = new AwkRegexGroup[match.Groups.Count];
        for (var index = 0; index < groups.Length; index++)
        {
            groups[index] = FromGroup(match.Groups[index], input, useByteOffsets, counters);
        }

        var value = AwkText.FromString(match.Value, counters);
        var byteIndex = ResolveByteIndex(input, match.Index);
        var byteLength = Encoding.UTF8.GetByteCount(match.Value);
        return new AwkRegexMatch(
            true,
            useByteOffsets ? byteIndex : match.Index,
            useByteOffsets ? byteLength : match.Length,
            byteIndex,
            byteLength,
            value,
            groups);
    }

    public static AwkRegexMatch FromMatchValue(
        Match match,
        string input,
        bool useByteOffsets,
        AwkRuntimeCounters counters)
    {
        if (!match.Success)
            return NoMatch;

        var value = AwkText.FromString(match.Value, counters);
        var byteIndex = ResolveByteIndex(input, match.Index);
        var byteLength = Encoding.UTF8.GetByteCount(match.Value);
        return new AwkRegexMatch(
            true,
            useByteOffsets ? byteIndex : match.Index,
            useByteOffsets ? byteLength : match.Length,
            byteIndex,
            byteLength,
            value,
            []);
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
        for (var index = 0; index < groups.Length; index++)
        {
            groups[index] = FromUtf8Group(match.GetGroup(index), useByteOffsets, counters, governor);
        }

        return new AwkRegexMatch(
            true,
            useByteOffsets ? match.IndexInBytes : match.IndexInUtf16,
            useByteOffsets ? match.LengthInBytes : match.LengthInUtf16,
            match.IndexInBytes,
            match.LengthInBytes,
            CreateText(match, counters, governor),
            groups);
    }

    public static AwkRegexMatch FromUtf8MatchValue(
        Utf8MatchContext match,
        bool useByteOffsets,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (!match.Success)
            return NoMatch;

        return new AwkRegexMatch(
            true,
            useByteOffsets ? match.IndexInBytes : match.IndexInUtf16,
            useByteOffsets ? match.LengthInBytes : match.LengthInUtf16,
            match.IndexInBytes,
            match.LengthInBytes,
            CreateText(match, counters, governor),
            []);
    }

    public static AwkRegexMatch FromUtf8ValueMatch(
        AwkText input,
        Utf8ValueMatch match,
        bool useByteOffsets,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (!match.Success)
            return NoMatch;

        var value = input.CopyByteSlice(match.IndexInBytes, match.LengthInBytes, counters, governor);
        return new AwkRegexMatch(
            true,
            useByteOffsets ? match.IndexInBytes : match.IndexInUtf16,
            useByteOffsets ? match.LengthInBytes : match.LengthInUtf16,
            match.IndexInBytes,
            match.LengthInBytes,
            value,
            []);
    }

    private static AwkRegexGroup FromGroup(Group group, string input, bool useByteOffsets, AwkRuntimeCounters counters) =>
        new(
            group.Success,
            ResolveIndex(input, group.Index, useByteOffsets),
            ResolveLength(group.Value, useByteOffsets),
            AwkText.FromString(group.Value, counters));

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
            ? AwkText.FromTrustedOwnedBytes(bytes.ToArray(), counters, governor)
            : AwkText.FromString(match.GetValueString(), counters, governor);

    private static AwkText CreateText(Utf8GroupContext group, AwkRuntimeCounters counters, AwkMemoryGovernor governor) =>
        group.TryGetValueBytes(out var bytes)
            ? AwkText.FromTrustedOwnedBytes(bytes.ToArray(), counters, governor)
            : AwkText.FromString(group.GetValueString(), counters, governor);

    private static int ResolveIndex(string input, int utf16Index, bool useByteOffsets) =>
        useByteOffsets ? ResolveByteIndex(input, utf16Index) : utf16Index;

    private static int ResolveByteIndex(string input, int utf16Index) =>
        Encoding.UTF8.GetByteCount(input.AsSpan(0, utf16Index));

    private static int ResolveLength(string value, bool useByteOffsets) =>
        useByteOffsets ? Encoding.UTF8.GetByteCount(value) : value.Length;

}

internal readonly record struct AwkRegexGroup(
    bool Success,
    int Index,
    int Length,
    AwkText Value);

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
}
