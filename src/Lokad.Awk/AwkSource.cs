using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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
    public static AwkSourceLoadResult FromSources(IReadOnlyList<AwkSourceUnit> sources)
    {
        static AwkSourceUnit AssembleCompilationSource(IReadOnlyList<AwkSourceUnit> sources)
        {
            if (sources.Count == 0)
                return new AwkSourceUnit(new AwkSourceName("<program>"), string.Empty, []);

            if (sources.Count == 1)
                return sources[0];

            var textLength = sources.Count - 1;
            var segmentCount = 0;
            for (var index = 0; index < sources.Count; index++)
            {
                textLength = checked(textLength + sources[index].Text.Length);
                segmentCount = checked(segmentCount + sources[index].Segments.Count);
            }

            var text = new StringBuilder(textLength);
            var segments = new List<AwkSourceSegment>(segmentCount);
            for (var sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
            {
                if (sourceIndex > 0)
                    text.Append('\n');

                var source = sources[sourceIndex];
                var sourceOffset = text.Length;
                text.Append(source.Text);
                for (var segmentIndex = 0; segmentIndex < source.Segments.Count; segmentIndex++)
                {
                    var segment = source.Segments[segmentIndex];
                    segments.Add(new AwkSourceSegment(
                        segment.SourceName,
                        segment.Kind,
                        new AwkTextSpan(
                            checked(sourceOffset + segment.Span.Start),
                            segment.Span.Length),
                        segment.SourceLineOffset,
                        segment.SourceColumnOffset,
                        segment.LineContinuations));
                }
            }

            return new AwkSourceUnit(new AwkSourceName("<program>"), text.ToString(), segments);
        }

        return new Loaded(sources, AssembleCompilationSource(sources));
    }

    public static AwkSourceLoadResult Failure(ReadOnlyMemory<byte> errorMessage, AwkDiagnosticId id) =>
        new Failed(errorMessage, id);

    internal sealed record Loaded(
        IReadOnlyList<AwkSourceUnit> Sources,
        AwkSourceUnit CompilationSource) : AwkSourceLoadResult;

    internal sealed record Failed(
        ReadOnlyMemory<byte> ErrorMessage,
        AwkDiagnosticId Id) : AwkSourceLoadResult;
}
