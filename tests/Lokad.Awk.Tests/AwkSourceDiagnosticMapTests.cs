namespace Lokad.Awk.Tests;

public sealed class AwkSourceDiagnosticMapTests
{
    [Fact]
    public void MapDiagnostics_ReturnsDefaultInlineDiagnosticsWithoutAllocatingAMappedBatch()
    {
        const string text = "BEGIN {";
        var sourceName = new AwkSourceName("<inline:1>");
        var segment = new AwkSourceSegment(
            sourceName,
            AwkSourceSegmentKind.Inline,
            new AwkTextSpan(0, text.Length),
            new AwkLineOffset(0),
            new AwkColumnOffset(0),
            AwkLineContinuationMap.Empty);
        var source = new AwkSourceUnit(sourceName, text, [segment]);
        AwkDiagnostic[] diagnostics = [new(new AwkTextSpan(text.Length, 0), "test")];

        var mapped = AwkSourceDiagnosticMap.MapDiagnostics(source, diagnostics);

        Assert.Same(diagnostics, mapped);
    }

    [Fact]
    public void MapDiagnostics_PreservesEveryCrLfBoundaryCoordinate()
    {
        const string text = "a\r\nb";
        var segment = new AwkSourceSegment(
            new AwkSourceName("/script.awk"),
            AwkSourceSegmentKind.File,
            new AwkTextSpan(0, text.Length),
            new AwkLineOffset(0),
            new AwkColumnOffset(0),
            AwkLineContinuationMap.Empty);
        var source = new AwkSourceUnit(segment.SourceName, text, [segment]);
        var diagnostics = Enumerable.Range(0, text.Length + 1)
            .Select(position => new AwkDiagnostic(new AwkTextSpan(position, 0), "test"))
            .ToArray();

        var mapped = AwkSourceDiagnosticMap.MapDiagnostics(source, diagnostics);

        Assert.Collection(
            mapped,
            diagnostic => AssertLocation(diagnostic, 1, 1),
            diagnostic => AssertLocation(diagnostic, 1, 2),
            diagnostic => AssertLocation(diagnostic, 2, 1),
            diagnostic => AssertLocation(diagnostic, 2, 1),
            diagnostic => AssertLocation(diagnostic, 2, 2));
    }

    [Fact]
    public void MapDiagnostics_IndexesRepeatedContinuationsAtOneOffset()
    {
        const string text = "ab";
        var continuations = new AwkLineContinuationMap(
            [new AwkLineContinuationPoint(1), new AwkLineContinuationPoint(1)]);
        var segment = new AwkSourceSegment(
            new AwkSourceName("/script.awk"),
            AwkSourceSegmentKind.File,
            new AwkTextSpan(0, text.Length),
            new AwkLineOffset(4),
            new AwkColumnOffset(2),
            continuations);
        var source = new AwkSourceUnit(segment.SourceName, text, [segment]);
        var diagnostics = Enumerable.Range(0, text.Length + 1)
            .Select(position => new AwkDiagnostic(new AwkTextSpan(position, 0), "test"))
            .ToArray();

        var mapped = AwkSourceDiagnosticMap.MapDiagnostics(source, diagnostics);

        Assert.Collection(
            mapped,
            diagnostic => AssertLocation(diagnostic, 5, 3),
            diagnostic => AssertLocation(diagnostic, 7, 1),
            diagnostic => AssertLocation(diagnostic, 7, 2));
    }

    private static void AssertLocation(AwkDiagnostic diagnostic, int line, int column)
    {
        var location = Assert.IsType<AwkSourceLocation>(diagnostic.SourceLocation);
        Assert.Equal("/script.awk", location.Name);
        Assert.Equal(line, location.Line);
        Assert.Equal(column, location.Column);
    }
}
