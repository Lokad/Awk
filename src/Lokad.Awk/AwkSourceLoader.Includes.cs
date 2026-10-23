using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static partial class AwkSourceLoader
{
    private static async Task<SourceExpansionResult> ExpandIncludesAsync(
        IAwkHost host,
        AwkSourceLoadPlan plan,
        string source,
        AwkSourceName sourceName,
        AwkSourceSegmentKind sourceKind,
        int includeDepth,
        AwkRuntimeResourceMonitor resources,
        CancellationToken cancellationToken)
    {
        Action checkpoint = cancellationToken.ThrowIfCancellationRequested;
        checkpoint();
        var normalization = AwkLineContinuations.Normalize(source, cancellationToken, plan.Memory, plan.CompatibilityMode);
        var normalizedSource = normalization.Text;
        IReadOnlyList<IncludeDirective> includes = plan.CompatibilityMode == AwkCompatibilityMode.GnuDefault &&
            normalizedSource.Contains("@include", StringComparison.Ordinal)
            ? FindIncludes(normalizedSource, plan.Memory, checkpoint) : [];
        if (includes.Count == 0)
        {
            checkpoint();
            if (normalizedSource.Length == 0)
                return SourceExpansionResult.FromSource(AwkExpandedSource.Empty);

            return SourceExpansionResult.FromSource(AwkExpandedSource.Create(
                normalizedSource,
                [new AwkSourceSegment(
                    sourceName,
                    sourceKind,
                    new AwkTextSpan(0, normalizedSource.Length),
                    new AwkLineOffset(0),
                    new AwkColumnOffset(0),
                    normalization.LineContinuations)]));
        }

        checkpoint();
        plan.Memory.AdmitTextCopy(normalizedSource.Length);
        var builder = new StringBuilder(normalizedSource.Length);
        var segments = new List<AwkSourceSegment>();
        var sourceChunkStart = 0;
        var sourceChunkLine = 0;
        var sourceChunkColumn = 0;
        foreach (var directive in includes)
        {
            checkpoint();
            if (!plan.AllowSourceIncludes)
            {
                return SourceExpansionResult.Failure(
                    AwkMessages.ErrorSourceIncludesDisabled,
                    AwkDiagnosticId.SourceLoadFailure);
            }

            AppendSourceSegment(
                builder,
                segments,
                sourceName,
                sourceKind,
                normalizedSource,
                sourceChunkStart,
                directive.Span.Start - sourceChunkStart,
                sourceChunkLine,
                sourceChunkColumn,
                normalization.LineContinuations.Slice(
                    sourceChunkStart,
                    directive.Span.Start - sourceChunkStart, checkpoint));
            var include = await TryReadIncludeAsync(
                    host,
                    plan,
                    directive.Path.Text,
                    checked(includeDepth + 1),
                    resources,
                    cancellationToken)
                .ConfigureAwait(false);
            if (include is SourceExpansionResult.Failed)
                return include;

            if (include is not SourceExpansionResult.Loaded loadedInclude)
                throw new InvalidOperationException("Unexpected source expansion result.");

            AppendExpandedSource(builder, segments, loadedInclude.Source);
            if (loadedInclude.Source.Text.Length > 0 && loadedInclude.Source.Text[^1] != '\n')
            {
                AwkSourceSegment? last = loadedInclude.Source.Segments.Count == 0
                    ? null
                    : loadedInclude.Source.Segments[^1];
                AppendSyntheticNewLine(builder, segments, last);
            }

            AdvanceSourcePosition(directive.Span.End);
        }

        AppendSourceSegment(
            builder,
            segments,
            sourceName,
            sourceKind,
            normalizedSource,
            sourceChunkStart,
            normalizedSource.Length - sourceChunkStart,
            sourceChunkLine,
            sourceChunkColumn,
            normalization.LineContinuations.Slice(
                sourceChunkStart,
                normalizedSource.Length - sourceChunkStart, checkpoint));

        checkpoint();
        var expandedText = builder.ToString();
        checkpoint();
        return SourceExpansionResult.FromSource(AwkExpandedSource.Create(expandedText, segments));

        void AdvanceSourcePosition(int end)
        {
            var lastLineStart = sourceChunkStart - sourceChunkColumn;
            for (var position = sourceChunkStart; position < end; position++)
            {
                if ((position & 1023) == 0) checkpoint();
                if (normalizedSource[position] is not '\r' and not '\n') continue;
                if (normalizedSource[position] == '\r' && position + 1 < end && normalizedSource[position + 1] == '\n')
                    position++;
                sourceChunkLine++;
                lastLineStart = position + 1;
            }
            var before = normalization.LineContinuations.CountAtOrBefore(sourceChunkStart - 1, checkpoint);
            var after = normalization.LineContinuations.CountAtOrBefore(end - 1, checkpoint);
            sourceChunkLine += after - before;
            if (after > before)
                lastLineStart = Math.Max(lastLineStart, normalization.LineContinuations.GetPoint(after - 1, checkpoint).NormalizedOffset);
            sourceChunkStart = end;
            sourceChunkColumn = end - lastLineStart;
        }

        void AppendSourceSegment(
            StringBuilder builder,
            List<AwkSourceSegment> segments,
            AwkSourceName sourceName,
            AwkSourceSegmentKind sourceKind,
            string sourceText,
            int sourceStart,
            int sourceLength,
            int lineOffset,
            int columnOffset,
            AwkLineContinuationMap lineContinuations)
        {
            if (sourceLength == 0)
                return;

            var span = new AwkTextSpan(builder.Length, sourceLength);
            plan.Memory.AdmitTextCopy(sourceLength);
            AwkParsingSupport.AppendText(builder, sourceText.AsSpan(sourceStart, sourceLength), checkpoint);
            // Adjacent output can cross removed directives in the original source.
            // Keep the following chunk's original line/column instead of merging the gap.
            segments.Add(new AwkSourceSegment(
                sourceName,
                sourceKind,
                span,
                new AwkLineOffset(lineOffset),
                new AwkColumnOffset(columnOffset),
                lineContinuations));
        }

        void AppendExpandedSource(
            StringBuilder builder,
            List<AwkSourceSegment> segments,
            AwkExpandedSource source)
        {
            if (source.Text.Length == 0)
                return;

            var offset = builder.Length;
            plan.Memory.AdmitTextCopy(source.Text.Length);
            AwkParsingSupport.AppendText(builder, source.Text, checkpoint);
            for (var index = 0; index < source.Segments.Count; index++)
            {
                checkpoint();
                var segment = source.Segments[index];
                segments.Add(new AwkSourceSegment(
                    segment.SourceName,
                    segment.Kind,
                    new AwkTextSpan(segment.Span.Start + offset, segment.Span.Length),
                    segment.SourceLineOffset,
                    segment.SourceColumnOffset,
                    segment.LineContinuations));
            }
        }

        void AppendSyntheticNewLine(
            StringBuilder builder,
            List<AwkSourceSegment> segments,
            AwkSourceSegment? sourceSegment)
        {
            var start = builder.Length;
            plan.Memory.AdmitTextCopy(1);
            builder.Append('\n');
            if (sourceSegment is { } segment)
            {
                segments.Add(new AwkSourceSegment(
                    segment.SourceName,
                    segment.Kind,
                    new AwkTextSpan(start, 1),
                    segment.SourceLineOffset,
                    segment.SourceColumnOffset,
                    AwkLineContinuationMap.Empty));
            }
        }
    }

    // Keep only admitted source spans/paths across host awaits. Lexer buffers must not
    // survive into recursive include preparation, which reuses the frontend scratch estimate.
    private static List<IncludeDirective> FindIncludes(string source, AwkFrontendMemory memory, Action checkpoint)
    {
        var tokens = AwkLexer.Tokenize(source, checkpoint, memory: memory);
        var includes = new List<IncludeDirective>();
        var depth = 0;
        for (var index = 0; index < tokens.Count; index++)
        {
            checkpoint();
            var token = tokens[index];
            if (token.Kind is AwkTokenKind.LeftBrace or AwkTokenKind.LeftBracket or AwkTokenKind.LeftParen)
                depth++;
            else if (token.Kind is AwkTokenKind.RightBrace or AwkTokenKind.RightBracket or AwkTokenKind.RightParen)
                depth = Math.Max(0, depth - 1);
            else if (depth == 0 && token.Kind == AwkTokenKind.At &&
                AwkDirectiveParser.TryParseAt(tokens, index, out var directive, checkpoint) &&
                AwkDirectiveParser.TryGetIncludePath(directive, out var path))
            {
                while (index + 1 < tokens.Count && tokens[index + 1].Span.Start < directive.Span.End)
                {
                    checkpoint();
                    index++;
                }
                // Consume only this directive's terminator, preserving subsequent source.
                var end = directive.Span.End;
                if (index + 1 < tokens.Count && tokens[index + 1].Kind is
                    AwkTokenKind.Semicolon or AwkTokenKind.NewLine or AwkTokenKind.EndOfFile)
                    end = tokens[++index].Span.End;
                memory.AdmitSourceUnits(2);
                includes.Add(new IncludeDirective(new AwkTextSpan(token.Span.Start, end - token.Span.Start), new AwkSourceName(path)));
            }
        }
        return includes;
    }

    private readonly record struct IncludeDirective(AwkTextSpan Span, AwkSourceName Path);
}
