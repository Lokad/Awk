using System.Collections.Generic;
using System.Text;

namespace Lokad.Awk;

internal static partial class AwkSourceLoader
{

    private static void AddSegment(List<AwkSourceSegment> segments, AwkSourceSegment segment, Action? checkpoint)
    {
        checkpoint?.Invoke();
        if (segment.Span.Length == 0)
            return;

        if (segments.Count > 0)
        {
            var previous = segments[^1];
            if (previous.SourceName == segment.SourceName &&
                previous.Kind == segment.Kind &&
                previous.Span.End == segment.Span.Start)
            {
                segments[^1] = new AwkSourceSegment(
                    previous.SourceName,
                    previous.Kind,
                    new AwkTextSpan(previous.Span.Start, previous.Span.Length + segment.Span.Length),
                    previous.SourceLineOffset,
                    previous.SourceColumnOffset,
                    previous.LineContinuations.Append(segment.LineContinuations, previous.Span.Length, checkpoint));
                return;
            }
        }

        segments.Add(segment);
    }

}
