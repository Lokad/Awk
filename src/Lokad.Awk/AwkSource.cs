using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lokad.Awk;

internal readonly struct AwkSourceUnit
{
    public AwkSourceUnit(AwkSourceName name, string text, IReadOnlyList<AwkSourceSegment> segments)
    {
        SourceName = name;
        Text = text;
        Segments = segments;
    }

    public AwkSourceName SourceName { get; }

    public string Text { get; }

    public IReadOnlyList<AwkSourceSegment> Segments { get; }
}

internal readonly struct AwkSourceSegment
{
    private readonly AwkLineContinuationMap _lineContinuations;

    public AwkSourceSegment(
        AwkSourceName name,
        AwkSourceSegmentKind kind,
        AwkTextSpan span,
        AwkLineOffset lineOffset,
        AwkColumnOffset columnOffset,
        AwkLineContinuationMap lineContinuations)
    {
        SourceName = name;
        Kind = kind;
        Span = span;
        SourceLineOffset = lineOffset;
        SourceColumnOffset = columnOffset;
        _lineContinuations = lineContinuations;
    }

    public AwkSourceName SourceName { get; }

    public AwkSourceSegmentKind Kind { get; }

    public AwkTextSpan Span { get; }

    public AwkLineOffset SourceLineOffset { get; }

    public AwkColumnOffset SourceColumnOffset { get; }

    public AwkLineContinuationMap LineContinuations => _lineContinuations ?? AwkLineContinuationMap.Empty;
}

internal readonly record struct AwkLineOffset
{
    public AwkLineOffset(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Line offsets must be zero or positive.");

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

internal readonly record struct AwkColumnOffset
{
    public AwkColumnOffset(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Column offsets must be zero or positive.");

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

internal enum AwkSourceSegmentKind
{
    Unknown,
    Inline,
    File,
    Include
}

internal sealed class AwkExpandedSource
{
    private AwkExpandedSource(string text, IReadOnlyList<AwkSourceSegment> segments)
    {
        Text = text;
        Segments = segments;
    }

    public string Text { get; }

    public IReadOnlyList<AwkSourceSegment> Segments { get; }

    public static AwkExpandedSource Empty { get; } = new(string.Empty, []);

    public static AwkExpandedSource Create(string text, IReadOnlyList<AwkSourceSegment> segments) =>
        new(text, segments);
}

internal sealed class AwkScriptFile
{
    public AwkScriptFile(string text, AwkSourceName displayName, AwkSourceIdentity identity)
    {
        Text = text;
        SourceName = displayName;
        SourceIdentity = identity;
    }

    public string Text { get; }

    public AwkSourceName SourceName { get; }

    public AwkSourceIdentity SourceIdentity { get; }
}

internal readonly record struct AwkSourceName(string Text)
{
    public override string ToString() => Text;
}

internal readonly record struct AwkSourceIdentity(string Text)
{
    public override string ToString() => Text;
}

internal abstract record AwkSourceLoadResult
{
    public static AwkSourceLoadResult FromSources(IReadOnlyList<AwkSourceUnit> sources) =>
        new Loaded(sources);

    public static AwkSourceLoadResult Failure(ReadOnlyMemory<byte> errorMessage, AwkDiagnosticId id) =>
        new Failed(errorMessage, id);

    internal sealed record Loaded(IReadOnlyList<AwkSourceUnit> Sources) : AwkSourceLoadResult;

    internal sealed record Failed(
        ReadOnlyMemory<byte> ErrorMessage,
        AwkDiagnosticId Id) : AwkSourceLoadResult;
}
