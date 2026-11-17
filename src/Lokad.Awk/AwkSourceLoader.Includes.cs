using System.Text;

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
        var current = BeginExpansion(plan, source, sourceName, sourceKind, includeDepth,
            parent: null, checkpoint, out var ready);
        if (current is null) return SourceExpansionResult.FromSource(ready);
        while (true)
        {
            checkpoint();
            if (!current.HasNextInclude)
            {
                var completed = current.Complete();
                if (current.Parent is not { } parent) return SourceExpansionResult.FromSource(completed);
                parent.CompleteInclude(completed);
                current = parent;
                continue;
            }
            if (!plan.AllowSourceIncludes)
                return SourceExpansionResult.Failure(AwkMessages.ErrorSourceIncludesDisabled,
                    AwkDiagnosticId.SourceLoadFailure);
            var directive = current.BeginInclude();
            var depth = checked(current.Depth + 1);
            var include = await TryReadIncludeSourceAsync(host, plan, directive.Path.Text, depth,
                resources, cancellationToken).ConfigureAwait(false);
            if (include is SourceExpansionResult.Failed) return include;
            if (include is SourceExpansionResult.Loaded loaded)
            {
                current.CompleteInclude(loaded.Source);
                continue;
            }
            if (include is not SourceExpansionResult.Unexpanded unexpanded)
                throw new InvalidOperationException("Unexpected include source result.");
            var script = unexpanded.Script;
            var child = BeginExpansion(plan, script.Text, script.SourceName, AwkSourceSegmentKind.Include,
                depth, current, checkpoint, out ready);
            if (child is null) current.CompleteInclude(ready);
            else current = child;
        }
    }

    private static IncludeExpansionFrame? BeginExpansion(AwkSourceLoadPlan plan, string source,
        AwkSourceName sourceName, AwkSourceSegmentKind sourceKind, int depth,
        IncludeExpansionFrame? parent, Action checkpoint, out AwkExpandedSource ready)
    {
        checkpoint();
        var normalization = AwkLineContinuations.NormalizeWithCheckpoint(source, checkpoint, plan.Memory, plan.CompatibilityMode);
        var normalizedSource = normalization.Text;
        IReadOnlyList<IncludeDirective> includes = plan.CompatibilityMode == AwkCompatibilityMode.GnuDefault &&
            normalizedSource.Contains("@include", StringComparison.Ordinal)
            ? FindIncludes(normalizedSource, plan.Memory, checkpoint) : [];
        checkpoint();
        if (includes.Count == 0)
        {
            ready = normalizedSource.Length == 0 ? AwkExpandedSource.Empty : AwkExpandedSource.Create(
                normalizedSource,
                [new AwkSourceSegment(sourceName, sourceKind, new(0, normalizedSource.Length),
                    new(0), new(0), normalization.LineContinuations)]);
            return null;
        }
        // The existing frontend owner funds the continuation, builder/list headers
        // and parent link before allocation; no per-node reservation is introduced.
        plan.Memory.AdmitSourceUnits(4);
        plan.Memory.AdmitTextCopy(normalizedSource.Length);
        checkpoint();
        ready = AwkExpandedSource.Empty;
        return new(plan, sourceName, sourceKind, normalization, includes, depth, parent, checkpoint);
    }

    private sealed class IncludeExpansionFrame(AwkSourceLoadPlan plan, AwkSourceName sourceName,
        AwkSourceSegmentKind sourceKind, AwkLineContinuationNormalization normalization,
        IReadOnlyList<IncludeDirective> includes, int depth, IncludeExpansionFrame? parent, Action checkpoint)
    {
        private string Source => normalization.Text;
        private readonly StringBuilder _builder = new(normalization.Text.Length);
        private readonly List<AwkSourceSegment> _segments = [];
        private int _sourceChunkStart;
        private int _sourceChunkLine;
        private int _sourceChunkColumn;
        private int _includePosition;

        internal IncludeExpansionFrame? Parent { get; } = parent;
        internal int Depth { get; } = depth;
        internal bool HasNextInclude => _includePosition < includes.Count;

        internal IncludeDirective BeginInclude()
        {
            var directive = includes[_includePosition];
            AppendSourceSegment(_sourceChunkStart, directive.Span.Start - _sourceChunkStart, _sourceChunkLine, _sourceChunkColumn,
                normalization.LineContinuations.Slice(_sourceChunkStart, directive.Span.Start - _sourceChunkStart, checkpoint));
            return directive;
        }

        internal void CompleteInclude(AwkExpandedSource source)
        {
            AppendExpandedSource(source);
            if (source.Text.Length > 0 && source.Text[^1] != '\n')
                AppendSyntheticNewLine(source.Segments.Count == 0 ? null : source.Segments[^1]);
            AdvanceSourcePosition(includes[_includePosition++].Span.End);
        }

        internal AwkExpandedSource Complete()
        {
            AppendSourceSegment(_sourceChunkStart, Source.Length - _sourceChunkStart, _sourceChunkLine, _sourceChunkColumn,
                normalization.LineContinuations.Slice(_sourceChunkStart, Source.Length - _sourceChunkStart, checkpoint));
            checkpoint();
            var expandedText = _builder.ToString();
            checkpoint();
            return AwkExpandedSource.Create(expandedText, _segments);
        }

        private void AdvanceSourcePosition(int end)
        {
            var lastLineStart = _sourceChunkStart - _sourceChunkColumn;
            for (var position = _sourceChunkStart; position < end; position++)
            {
                if ((position & 1023) == 0) checkpoint();
                if (Source[position] is not '\r' and not '\n') continue;
                if (Source[position] == '\r' && position + 1 < end && Source[position + 1] == '\n')
                    position++;
                _sourceChunkLine++;
                lastLineStart = position + 1;
            }
            var before = normalization.LineContinuations.CountAtOrBefore(_sourceChunkStart - 1, checkpoint);
            var after = normalization.LineContinuations.CountAtOrBefore(end - 1, checkpoint);
            _sourceChunkLine += after - before;
            if (after > before)
                lastLineStart = Math.Max(lastLineStart, normalization.LineContinuations.GetPoint(after - 1, checkpoint).NormalizedOffset);
            _sourceChunkStart = end;
            _sourceChunkColumn = end - lastLineStart;
        }

        private void AppendSourceSegment(
            int sourceStart,
            int sourceLength,
            int lineOffset,
            int columnOffset,
            AwkLineContinuationMap lineContinuations)
        {
            if (sourceLength == 0)
                return;

            var span = new AwkTextSpan(_builder.Length, sourceLength);
            plan.Memory.AdmitTextCopy(sourceLength);
            AwkParsingSupport.AppendText(_builder, Source.AsSpan(sourceStart, sourceLength), checkpoint);
            // Adjacent output can cross removed directives in the original source.
            // Keep the following chunk's original line/column instead of merging the gap.
            _segments.Add(new AwkSourceSegment(
                sourceName,
                sourceKind,
                span,
                new AwkLineOffset(lineOffset),
                new AwkColumnOffset(columnOffset),
                lineContinuations));
        }

        private void AppendExpandedSource(
            AwkExpandedSource source)
        {
            if (source.Text.Length == 0)
                return;

            var offset = _builder.Length;
            plan.Memory.AdmitTextCopy(source.Text.Length);
            AwkParsingSupport.AppendText(_builder, source.Text, checkpoint);
            for (var index = 0; index < source.Segments.Count; index++)
            {
                checkpoint();
                var segment = source.Segments[index];
                _segments.Add(new AwkSourceSegment(
                    segment.SourceName,
                    segment.Kind,
                    new AwkTextSpan(segment.Span.Start + offset, segment.Span.Length),
                    segment.SourceLineOffset,
                    segment.SourceColumnOffset,
                    segment.LineContinuations));
            }
        }

        private void AppendSyntheticNewLine(
            AwkSourceSegment? sourceSegment)
        {
            var start = _builder.Length;
            plan.Memory.AdmitTextCopy(1);
            _builder.Append('\n');
            if (sourceSegment is { } segment)
            {
                _segments.Add(new AwkSourceSegment(
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
    // survive into child include preparation, which reuses the frontend scratch estimate.
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
