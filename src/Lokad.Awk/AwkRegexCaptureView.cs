using System.Text.RegularExpressions;

namespace Lokad.Awk;

/// <summary>Borrowed UTF-16 coordinates while the selected match's lease is live.</summary>
internal readonly struct AwkRegexCaptureView
{
    private readonly GroupCollection? _groups;
    private readonly int[]? _groupMap;
    private readonly AwkPosixMatch? _posix;
    private readonly string? _input;

    public AwkRegexCaptureView(Match match, string input, int[]? groupMap)
    {
        _groups = match.Groups;
        _groupMap = groupMap;
        _posix = null;
        _input = input;
        Success = match.Success;
        Count = groupMap?.Length ?? match.Groups.Count;
    }

    public AwkRegexCaptureView(AwkPosixMatch match, string input)
    {
        _groups = null;
        _groupMap = null;
        _posix = match;
        _input = input;
        Success = match.Success;
        Count = match.GroupCount;
    }

    public bool Success { get; }
    public int Count { get; }
    public AwkRegexCapture Whole => _posix is { } match
        ? new(match.Success, match.Index, match.Length, _input ?? string.Empty)
        : this[0];

    public AwkRegexCapture this[int index]
    {
        get
        {
            if (_posix is { } match)
                return match.Capture(index, _input ?? string.Empty);
            if (_groups is { } groups)
            {
                var group = groups[_groupMap is { } map ? map[index] : index];
                return new(group.Success, group.Index, group.Length, _input ?? string.Empty);
            }
            return default;
        }
    }
}

internal readonly record struct AwkRegexCapture
{
    private readonly string? _input;
    public AwkRegexCapture(bool success, int index, int length, string input)
    {
        Success = success;
        Index = index;
        Length = length;
        _input = input.Length == 0 ? null : input;
    }
    public bool Success { get; }
    public int Index { get; }
    public int Length { get; }
    public string Input => _input ?? string.Empty;
    public ReadOnlySpan<char> ValueSpan => Success ? Input.AsSpan(Index, Length) : [];
}

internal sealed class AwkPosixMatch(
    bool success, int index, int length, int[] storage, int coordinatesAt,
    int groupCount, AwkMemoryLease memory) : IDisposable
{
    internal bool Success { get; } = success;
    internal int Index { get; } = index;
    internal int Length { get; } = length;
    internal int GroupCount { get; } = groupCount;

    internal AwkRegexCapture Capture(int group, string input)
    {
        var start = storage[coordinatesAt + group * 2];
        var end = storage[coordinatesAt + group * 2 + 1];
        return start < 0 ? default : new(true, start, end - start, input);
    }

    public void Dispose() => memory.Dispose();
}
