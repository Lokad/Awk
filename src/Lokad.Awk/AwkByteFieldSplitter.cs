using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Lokad.Awk;

internal sealed class AwkByteFieldSplitter(
    Func<AwkRegexPattern, AwkRegexCaseSensitivity, AwkRegex> createRegex,
    Func<AwkRegexLiteralText, AwkRegexCaseSensitivity, AwkRegex> createLiteralRegex,
    Func<AwkRegex, AwkText, AwkRegexMatchSpanEnumerator> enumerateMatchSpans,
    Func<AwkRegexCaseSensitivity> resolveRegexCaseSensitivity,
    AwkStringUnitMode stringUnitMode,
    Action throwIfCancellationRequested,
    AwkMemoryAccounting memoryAccounting,
    AwkRuntimeCounters runtimeCounters,
    Action<object>? resetRegexWarningSite = null)
{
    private ResolvedFieldSeparatorPlan? _resolvedSeparator;

    public void ResetWarningHistory()
    {
        resetRegexWarningSite?.Invoke(this);
        _resolvedSeparator = null;
    }

    public void ValidateSeparator(AwkFieldSeparatorSpec separator) =>
        _ = ResolveSeparator(separator, runtimeCounters, resolveRegexCaseSensitivity(), prepareBothCases: true);

    public AwkTextSplitResult SplitBySeparator(
        AwkText line,
        AwkFieldSeparatorSpec separator,
        AwkSeparatorCaptureMode captureMode,
        AwkRuntimeCounters counters)
    {
        var plan = ResolveSeparator(separator, counters, resolveRegexCaseSensitivity());
        if (line.ByteLength == 0)
            return new AwkTextSplitResult([], []);

        return plan.Kind switch
        {
            FieldSeparatorKind.Whitespace => SplitWhitespace(line, captureMode, counters),
            FieldSeparatorKind.Characters => SplitCharacters(
                line,
                captureMode,
                stringUnitMode,
                counters),
            FieldSeparatorKind.SingleByte => SplitSingleByteLiteral(
                line,
                plan.SingleByte,
                captureMode,
                counters),
            FieldSeparatorKind.Regex => SplitRegex(line, plan.Regex, captureMode, counters),
            _ => throw new InvalidOperationException("Unknown field separator kind.")
        };

        AwkTextSplitResult SplitWhitespace(
            AwkText line,
            AwkSeparatorCaptureMode captureMode,
            AwkRuntimeCounters counters)
        {
            var content = line.AsSpan(throwIfCancellationRequested);
            if (content.Length == 0)
                return new AwkTextSplitResult([], []);

            if (captureMode is AwkSeparatorCaptureMode.Omit)
            {
                var uncapturedFields = ScanWhitespaceFields<OwnedListFieldSink, IReadOnlyList<AwkText>>(
                    line,
                    new OwnedListFieldSink(line, counters, memoryAccounting, throwIfCancellationRequested));
                return new AwkTextSplitResult(uncapturedFields, []);
            }

            var fields = new List<AwkText>();
            var separators = new List<AwkCapturedSeparator>();
            var scanner = new AwkWhitespaceFieldScanner(line, throwIfCancellationRequested, runtimeCounters);
            while (scanner.MoveNext(out var field, out var precedingSeparator))
            {
                if (precedingSeparator.Length > 0)
                    separators.Add(new AwkCapturedSeparator(fields.Count, SliceTrustedFieldText(
                        content,
                        precedingSeparator.Start,
                        precedingSeparator.Length,
                        counters)));

                fields.Add(SliceTrustedFieldText(content, field.Start, field.Length, counters));
            }

            if (scanner.TrailingSeparator.Length > 0)
                separators.Add(new AwkCapturedSeparator(fields.Count, SliceTrustedFieldText(
                    content,
                    scanner.TrailingSeparator.Start,
                    scanner.TrailingSeparator.Length,
                    counters)));

            return new AwkTextSplitResult(fields, separators);
        }

        AwkTextSplitResult SplitCharacters(
            AwkText line,
            AwkSeparatorCaptureMode captureMode,
            AwkStringUnitMode unitMode,
            AwkRuntimeCounters counters)
        {
            if (line.ByteLength == 0)
                return new AwkTextSplitResult([], []);

            var fields = ScanCharacterFields<OwnedListFieldSink, IReadOnlyList<AwkText>>(
                line,
                unitMode,
                new OwnedListFieldSink(
                    line,
                    counters,
                    memoryAccounting,
                    unitMode is AwkStringUnitMode.Byte ? line.ByteLength : 0,
                    throwIfCancellationRequested));

            if (captureMode is AwkSeparatorCaptureMode.Omit)
                return new AwkTextSplitResult(fields, []);

            var separators = new AwkCapturedSeparator[Math.Max(0, fields.Count - 1)];
            for (var index = 0; index < separators.Length; index++)
            {
                if ((index & 1023) == 0)
                    throwIfCancellationRequested();
                separators[index] = new AwkCapturedSeparator(index + 1, AwkText.Empty);
            }
            return new AwkTextSplitResult(fields, separators);
        }

        AwkTextSplitResult SplitSingleByteLiteral(
            AwkText line,
            byte separator,
            AwkSeparatorCaptureMode captureMode,
            AwkRuntimeCounters counters)
        {
            var content = line.AsSpan(throwIfCancellationRequested);
            if (captureMode is AwkSeparatorCaptureMode.Omit)
            {
                var fieldCount = CountSingleByteFields(content, separator);
                var fieldArray = new AwkText[fieldCount];
                ScanSingleByteFields<OwnedArrayFieldSink, AwkText[]>(
                    line,
                    separator,
                    new OwnedArrayFieldSink(line, counters, memoryAccounting, fieldArray, throwIfCancellationRequested));

                return new AwkTextSplitResult(fieldArray, []);
            }

            var fields = new List<AwkText>();
            var separators = new List<AwkCapturedSeparator>();
            var capturingScanner = new AwkSingleByteFieldScanner(line, separator, throwIfCancellationRequested);
            while (capturingScanner.MoveNext(out var field, out var trailingSeparator))
            {
                fields.Add(SliceTrustedFieldText(content, field.Start, field.Length, counters));
                if (trailingSeparator.Length > 0)
                    separators.Add(new AwkCapturedSeparator(fields.Count, SliceTrustedFieldText(
                        content,
                        trailingSeparator.Start,
                        trailingSeparator.Length,
                        counters)));
            }

            return new AwkTextSplitResult(fields, separators);
        }

        AwkTextSplitResult SplitRegex(
            AwkText line,
            AwkRegex separator,
            AwkSeparatorCaptureMode captureMode,
            AwkRuntimeCounters counters)
        {
            if (captureMode is AwkSeparatorCaptureMode.Omit)
            {
                var spanFields = ScanRegexFields<OwnedListFieldSink, IReadOnlyList<AwkText>>(
                    line,
                    separator,
                    new OwnedListFieldSink(line, counters, memoryAccounting, throwIfCancellationRequested));
                return new AwkTextSplitResult(spanFields, []);
            }

            var fields = new List<AwkText>();
            var separators = new List<AwkCapturedSeparator>();
            var content = line.AsSpan(throwIfCancellationRequested);
            var position = 0;
            using var matches = enumerateMatchSpans(separator, line);
            while (matches.MoveNext(out var match))
            {
                throwIfCancellationRequested();
                if (match.ByteLength == 0)
                    continue;
                fields.Add(SliceTrustedFieldText(content, position, match.ByteIndex - position, counters));
                separators.Add(new AwkCapturedSeparator(fields.Count,
                    SliceTrustedFieldText(content, match.ByteIndex, match.ByteLength, counters)));
                position = match.ByteIndex + match.ByteLength;
            }

            fields.Add(SliceTrustedFieldText(content, position, content.Length - position, counters));
            return new AwkTextSplitResult(fields, separators);
        }
    }

    // Reuse the complete splitters' scanners for prefix reads and their checkpoints.
    public int ExtendRecordFields(
        AwkText line, AwkFieldSeparatorSpec separator, int position, int targetCount,
        List<AwkFieldText> fields)
    {
        if (line.ByteLength == 0)
            return -1;
        if (separator.UsesWhitespaceSemantics)
        {
            var scanner = new AwkWhitespaceFieldScanner(line, throwIfCancellationRequested, runtimeCounters, position);
            while (fields.Count < targetCount && scanner.MoveNext(out var field, out _))
                fields.Add(AwkFieldText.FromRecordSlice(field.Start, field.Length));
            return scanner.Position == line.ByteLength ? -1 : scanner.Position;
        }

        var literal = separator.ByteText.AsSpan(throwIfCancellationRequested)[0];
        var literalScanner = new AwkSingleByteFieldScanner(line, literal, throwIfCancellationRequested, position);
        while (fields.Count < targetCount && literalScanner.MoveNext(out var field, out _))
            fields.Add(AwkFieldText.FromRecordSlice(field.Start, field.Length));
        return literalScanner.Completed ? -1 : literalScanner.Position;
    }

    public List<AwkFieldText> SplitRecordFieldsBySeparator(
        AwkText line,
        AwkFieldSeparatorSpec separator,
        AwkRegexCaseSensitivity caseSensitivity,
        List<AwkFieldText>? fieldBuffer = null)
    {
        var plan = ResolveSeparator(separator, runtimeCounters, caseSensitivity);
        if (line.ByteLength == 0)
            return [];

        return plan.Kind switch
        {
            FieldSeparatorKind.Whitespace => SplitWhitespaceRecordFields(line, fieldBuffer),
            FieldSeparatorKind.Characters => SplitCharacterRecordFields(
                line,
                stringUnitMode),
            FieldSeparatorKind.SingleByte => SplitSingleByteLiteralRecordFields(line, plan.SingleByte),
            FieldSeparatorKind.Regex => SplitRegexRecordFields(line, plan.Regex),
            _ => throw new InvalidOperationException("Unknown field separator kind.")
        };

        List<AwkFieldText> SplitSingleByteLiteralRecordFields(AwkText line, byte separator)
        {
            var content = line.AsSpan(throwIfCancellationRequested);
            var fieldCount = CountSingleByteFields(content, separator);
            return ScanSingleByteFields<RecordListFieldSink, List<AwkFieldText>>(
                line,
                separator,
                new RecordListFieldSink(fieldCount, fieldBuffer));
        }
    }

    public List<AwkFieldText> SplitParagraphRecordFieldsBySeparator(
        AwkText line,
        AwkFieldSeparatorSpec separator,
        AwkRegexCaseSensitivity caseSensitivity,
        List<AwkFieldText>? fieldBuffer = null)
    {
        var plan = ResolveSeparator(separator, runtimeCounters, caseSensitivity);
        if (line.ByteLength == 0)
            return [];

        return plan.Kind switch
        {
            FieldSeparatorKind.SingleByte => SplitSingleByteParagraphRecordFields(line, plan.SingleByte),
            FieldSeparatorKind.Whitespace => SplitWhitespaceRecordFields(line, fieldBuffer),
            FieldSeparatorKind.Characters => SplitCharacterRecordFields(
                line,
                stringUnitMode),
            FieldSeparatorKind.Regex => SplitRegexRecordFields(line, plan.Regex),
            _ => throw new InvalidOperationException("Unknown field separator kind.")
        };

        List<AwkFieldText> SplitSingleByteParagraphRecordFields(AwkText line, byte separator)
        {
            var content = line.AsSpan(throwIfCancellationRequested);
            var fields = new List<AwkFieldText>();
            var position = 0;
            for (var index = 0; index < content.Length; index++)
            {
                if ((index & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                    throwIfCancellationRequested();
                if (content[index] != separator && content[index] != (byte)'\n')
                    continue;

                fields.Add(AwkFieldText.FromRecordSlice(position, index - position));
                position = index + 1;
            }

            fields.Add(AwkFieldText.FromRecordSlice(position, content.Length - position));
            return fields;
        }

    }

    private FieldSeparatorPlan ResolveSeparator(
        AwkFieldSeparatorSpec separator,
        AwkRuntimeCounters counters,
        AwkRegexCaseSensitivity caseSensitivity,
        bool prepareBothCases = false)
    {
        if (separator.UsesWhitespaceSemantics)
            return FieldSeparatorPlan.Whitespace;

        var separatorText = separator.ByteText;
        if (separatorText.ByteLength == 0)
            return FieldSeparatorPlan.Characters;

        if (separatorText.GetRuneLength(throwIfCancellationRequested) == 1)
            caseSensitivity = AwkRegexCaseSensitivity.CaseSensitive;
        if (_resolvedSeparator is { } resolvedSeparator &&
            ReferenceEquals(resolvedSeparator.SeparatorText, separatorText) &&
            resolvedSeparator.CaseSensitivity == caseSensitivity &&
            resolvedSeparator.PatternOrigin == separator.PatternOrigin &&
            ReferenceEquals(resolvedSeparator.WarningSite, separator.WarningSite) &&
            (!prepareBothCases || resolvedSeparator.PreparedBothCases))
        {
            return resolvedSeparator.Plan;
        }

        FieldSeparatorPlan plan;
        if (TryGetSingleByteLiteralSeparator(separatorText, out var separatorByte) &&
            caseSensitivity is AwkRegexCaseSensitivity.CaseSensitive)
        {
            plan = FieldSeparatorPlan.FromSingleByte(separatorByte);
        }
        else
        {
            var regexText = separatorText.AsString(counters, throwIfCancellationRequested);
            AwkRegex regex;
            if (separatorText.GetRuneLength(throwIfCancellationRequested) > 1)
            {
                var pattern = new AwkRegexPattern(regexText, separator.PatternOrigin, separator.WarningSite ?? this);
                if (prepareBothCases)
                {
                    var sensitive = createRegex(pattern, AwkRegexCaseSensitivity.CaseSensitive);
                    var insensitive = createRegex(pattern, AwkRegexCaseSensitivity.IgnoreCase);
                    regex = caseSensitivity == AwkRegexCaseSensitivity.CaseSensitive ? sensitive : insensitive;
                }
                else
                    regex = createRegex(pattern, caseSensitivity);
            }
            else
                regex = createLiteralRegex(new AwkRegexLiteralText(regexText), caseSensitivity);
            plan = FieldSeparatorPlan.FromRegex(regex);
        }

        throwIfCancellationRequested();
        _resolvedSeparator = new ResolvedFieldSeparatorPlan(separatorText, caseSensitivity, plan,
            separator.PatternOrigin, separator.WarningSite, prepareBothCases);
        return plan;

        static bool TryGetSingleByteLiteralSeparator(AwkText separator, out byte separatorByte)
        {
            if (separator.ByteLength == 1 && separator.AsSpan() is [var value])
            {
                separatorByte = value;
                return true;
            }

            separatorByte = 0;
            return false;
        }
    }

    public AwkTextSplitResult Patsplit(
        AwkText input,
        AwkRegexPattern pattern,
        AwkSeparatorCaptureMode captureMode,
        AwkRuntimeCounters counters)
    {
        var regex = createRegex(pattern, resolveRegexCaseSensitivity());
        if (input.ByteLength == 0)
            return new AwkTextSplitResult([], []);

        var fields = new List<AwkText>();
        var separators = captureMode is AwkSeparatorCaptureMode.Capture
            ? new List<AwkCapturedSeparator>()
            : null;
        var content = input.AsSpan(throwIfCancellationRequested);
        var position = 0;
        var previousNonEmptyEnd = -1;
        using var matches = enumerateMatchSpans(regex, input);
        while (matches.MoveNext(out var match))
        {
            throwIfCancellationRequested();
            if (match.ByteLength == 0 && match.ByteIndex == previousNonEmptyEnd)
                continue;
            if (match.ByteLength > 0)
                previousNonEmptyEnd = match.ByteIndex + match.ByteLength;
            if (separators != null)
                separators.Add(new AwkCapturedSeparator(fields.Count,
                    SliceTrustedFieldText(content, position, match.ByteIndex - position, counters)));

            fields.Add(SliceTrustedFieldText(content, match.ByteIndex, match.ByteLength, counters));
            position = match.ByteIndex + match.ByteLength;
        }

        if (separators != null)
            separators.Add(new AwkCapturedSeparator(fields.Count,
                SliceTrustedFieldText(content, position, content.Length - position, counters)));

        return new AwkTextSplitResult(fields, separators ?? []);
    }

    public List<AwkFieldText> PatsplitRecordFields(
        AwkText input,
        AwkRegexPattern pattern,
        AwkRegexCaseSensitivity caseSensitivity)
    {
        var regex = createRegex(pattern, caseSensitivity);
        if (input.ByteLength == 0)
            return [];

        var fields = new List<AwkFieldText>();
        var previousNonEmptyEnd = -1;
        using var matches = enumerateMatchSpans(regex, input);
        while (matches.MoveNext(out var match))
        {
            throwIfCancellationRequested();
            if (match.ByteLength == 0 && match.ByteIndex == previousNonEmptyEnd)
                continue;
            if (match.ByteLength > 0)
                previousNonEmptyEnd = match.ByteIndex + match.ByteLength;
            fields.Add(AwkFieldText.FromRecordSlice(match.ByteIndex, match.ByteLength));
        }

        return fields;
    }

    private List<AwkFieldText> SplitWhitespaceRecordFields(AwkText line, List<AwkFieldText>? fieldBuffer = null) =>
        line.ByteLength == 0
            ? []
            : ScanWhitespaceFields<RecordListFieldSink, List<AwkFieldText>>(
                line,
                new RecordListFieldSink(0, fieldBuffer));

    private List<AwkFieldText> SplitCharacterRecordFields(
        AwkText line,
        AwkStringUnitMode unitMode)
    {
        if (line.ByteLength == 0)
            return [];

        return ScanCharacterFields<RecordListFieldSink, List<AwkFieldText>>(
            line,
            unitMode,
            new RecordListFieldSink(unitMode is AwkStringUnitMode.Byte ? line.ByteLength : 0));
    }

    private List<AwkFieldText> SplitRegexRecordFields(AwkText line, AwkRegex separator)
        => ScanRegexFields<RecordListFieldSink, List<AwkFieldText>>(
            line,
            separator,
            new RecordListFieldSink());

    private TFields ScanWhitespaceFields<TSink, TFields>(AwkText line, TSink sink)
        where TSink : struct, IFieldSink<TFields>
    {
        var scanner = new AwkWhitespaceFieldScanner(line, throwIfCancellationRequested, runtimeCounters);
        while (scanner.MoveNext(out var field, out _))
            sink.Add(field);

        throwIfCancellationRequested();
        return sink.Complete();
    }

    private TFields ScanCharacterFields<TSink, TFields>(
        AwkText line,
        AwkStringUnitMode unitMode,
        TSink sink)
        where TSink : struct, IFieldSink<TFields>
    {
        var scanner = new AwkCharacterFieldScanner(line, unitMode, throwIfCancellationRequested);
        while (scanner.MoveNext(out var field))
            sink.Add(field);

        throwIfCancellationRequested();
        return sink.Complete();
    }

    private TFields ScanSingleByteFields<TSink, TFields>(
        AwkText line,
        byte separator,
        TSink sink)
        where TSink : struct, IFieldSink<TFields>
    {
        var scanner = new AwkSingleByteFieldScanner(line, separator, throwIfCancellationRequested);
        while (scanner.MoveNext(out var field, out _))
            sink.Add(field);

        throwIfCancellationRequested();
        return sink.Complete();
    }

    private TFields ScanRegexFields<TSink, TFields>(
        AwkText line,
        AwkRegex separator,
        TSink sink)
        where TSink : struct, IFieldSink<TFields>
    {
        var position = 0;
        using var matches = enumerateMatchSpans(separator, line);
        while (matches.MoveNext(out var match))
        {
            throwIfCancellationRequested();
            if (match.ByteLength == 0)
                continue;
            sink.Add(new AwkByteRange(position, match.ByteIndex - position));
            position = match.ByteIndex + match.ByteLength;
        }

        sink.Add(new AwkByteRange(position, line.ByteLength - position));
        throwIfCancellationRequested();
        return sink.Complete();
    }

    private interface IFieldSink<out TFields>
    {
        void Add(AwkByteRange field);

        TFields Complete();
    }

    private readonly struct OwnedListFieldSink : IFieldSink<IReadOnlyList<AwkText>>
    {
        private readonly AwkText _line;
        private readonly AwkRuntimeCounters _counters;
        private readonly AwkMemoryAccounting _memoryAccounting;
        private readonly Action _checkpoint;
        private readonly List<AwkText> _fields;

        public OwnedListFieldSink(
            AwkText line,
            AwkRuntimeCounters counters,
            AwkMemoryAccounting memoryAccounting,
            Action checkpoint)
        {
            _checkpoint = checkpoint;
            _line = line;
            _counters = counters;
            _memoryAccounting = memoryAccounting;
            _fields = [];
        }

        public OwnedListFieldSink(
            AwkText line,
            AwkRuntimeCounters counters,
            AwkMemoryAccounting memoryAccounting,
            int capacity,
            Action checkpoint)
        {
            _checkpoint = checkpoint;
            _line = line;
            _counters = counters;
            _memoryAccounting = memoryAccounting;
            _fields = new List<AwkText>(capacity);
        }

        public void Add(AwkByteRange field) =>
            _fields.Add(_line.CopyByteSlice(
                field.Start,
                field.Length,
                _counters,
                _memoryAccounting,
                _checkpoint));

        public IReadOnlyList<AwkText> Complete() => _fields;
    }

    private struct OwnedArrayFieldSink(
        AwkText line,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting memoryAccounting,
        AwkText[] fields,
        Action checkpoint) : IFieldSink<AwkText[]>
    {
        private int _next;

        public void Add(AwkByteRange field) =>
            fields[_next++] = line.CopyByteSlice(
                field.Start,
                field.Length,
                counters,
                memoryAccounting,
                checkpoint);

        public readonly AwkText[] Complete() => fields;
    }

    private readonly struct RecordListFieldSink : IFieldSink<List<AwkFieldText>>
    {
        private readonly List<AwkFieldText> _fields;

        public RecordListFieldSink()
        {
            _fields = [];
        }

        public RecordListFieldSink(int capacity, List<AwkFieldText>? fields = null)
        {
            _fields = fields ?? new List<AwkFieldText>(capacity);
        }

        public void Add(AwkByteRange field) =>
            _fields.Add(AwkFieldText.FromRecordSlice(field.Start, field.Length));

        public List<AwkFieldText> Complete() => _fields;
    }

    private readonly record struct AwkByteRange(int Start, int Length);

    private readonly struct ResolvedFieldSeparatorPlan(
        AwkText? separatorText,
        AwkRegexCaseSensitivity caseSensitivity,
        FieldSeparatorPlan plan,
        AwkRegexPatternOrigin patternOrigin,
        object? warningSite,
        bool preparedBothCases)
    {
        public AwkText SeparatorText => separatorText ?? AwkText.Empty;

        public AwkRegexCaseSensitivity CaseSensitivity { get; } = caseSensitivity;

        public FieldSeparatorPlan Plan { get; } = plan;

        public AwkRegexPatternOrigin PatternOrigin { get; } = patternOrigin;

        public object? WarningSite { get; } = warningSite;

        public bool PreparedBothCases { get; } = preparedBothCases;
    }

    private readonly struct FieldSeparatorPlan
    {
        private readonly byte _singleByte;
        private readonly AwkRegex? _regex;

        private FieldSeparatorPlan(FieldSeparatorKind kind, byte singleByte, AwkRegex? regex)
        {
            Kind = kind;
            _singleByte = singleByte;
            _regex = regex;
        }

        public static FieldSeparatorPlan Whitespace { get; } =
            new(FieldSeparatorKind.Whitespace, 0, null);

        public static FieldSeparatorPlan Characters { get; } =
            new(FieldSeparatorKind.Characters, 0, null);

        public FieldSeparatorKind Kind { get; }

        public byte SingleByte =>
            Kind == FieldSeparatorKind.SingleByte
                ? _singleByte
                : throw new InvalidOperationException("The field separator is not a single byte.");

        public AwkRegex Regex =>
            Kind == FieldSeparatorKind.Regex && _regex != null
                ? _regex
                : throw new InvalidOperationException("The field separator is not a regular expression.");

        public static FieldSeparatorPlan FromSingleByte(byte value) =>
            new(FieldSeparatorKind.SingleByte, value, null);

        public static FieldSeparatorPlan FromRegex(AwkRegex regex) =>
            new(FieldSeparatorKind.Regex, 0, regex);
    }

    private enum FieldSeparatorKind
    {
        Whitespace,
        Characters,
        SingleByte,
        Regex
    }

    private struct AwkWhitespaceFieldScanner(AwkText line, Action checkpoint,
        AwkRuntimeCounters counters, int position = 0)
    {
        private int _position = position;
        private long _nextCheckpoint;

        public readonly int Position => _position;

        public AwkByteRange TrailingSeparator { get; private set; }

        public bool MoveNext(out AwkByteRange field, out AwkByteRange precedingSeparator)
        {
            var content = line.AsSpan(checkpoint);
            var separatorStart = _position;
            while (_position < content.Length && IsDefaultFieldWhitespace(content[_position]))
            {
                CheckProgress();
                _position++;
            }

            precedingSeparator = new AwkByteRange(separatorStart, _position - separatorStart);
            if (_position >= content.Length)
            {
                TrailingSeparator = precedingSeparator;
                field = default;
                return false;
            }

            var fieldStart = _position;
            while (_position < content.Length && !IsDefaultFieldWhitespace(content[_position]))
            {
                CheckProgress();
                // Never search beyond the next existing cancellation boundary.
                var end = (int)Math.Min(content.Length, _nextCheckpoint);
                var length = content[_position..end].IndexOfAny((byte)' ', (byte)'\t', (byte)'\n');
                counters.RecordWhitespaceFieldSearch(length < 0 ? end - _position : length + 1);
                if (length >= 0)
                {
                    _position += length;
                    break;
                }
                _position = end;
            }

            field = new AwkByteRange(fieldStart, _position - fieldStart);
            return true;

            static bool IsDefaultFieldWhitespace(byte value) =>
                value is (byte)' ' or (byte)'\t' or (byte)'\n';
        }
        private void CheckProgress()
        {
            if (_position < _nextCheckpoint)
                return;
            checkpoint();
            _nextCheckpoint = (long)_position + AwkExecutionGovernor.WorkChunkBytes;
        }
    }

    private struct AwkCharacterFieldScanner(AwkText line, AwkStringUnitMode unitMode, Action checkpoint)
    {
        private int _position;
        private long _nextCheckpoint;

        public bool MoveNext(out AwkByteRange field)
        {
            var content = line.AsSpan(checkpoint);
            if (_position >= content.Length)
            {
                field = default;
                return false;
            }

            CheckProgress();
            if (unitMode is AwkStringUnitMode.Byte)
            {
                if (content[_position] > 0x7F)
                {
                    throw new AwkRuntimeException(
                        AwkMessages.ErrorUnsupportedByteModeEmptyFieldSeparator,
                        AwkDiagnosticId.UnsupportedPolicy);
                }

                field = new AwkByteRange(_position, 1);
                _position++;
                return true;
            }

            var status = Rune.DecodeFromUtf8(content[_position..], out _, out var bytesConsumed);
            if (status != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");

            field = new AwkByteRange(_position, bytesConsumed);
            _position += bytesConsumed;
            return true;
        }
        private void CheckProgress()
        {
            if (_position < _nextCheckpoint)
                return;
            checkpoint();
            _nextCheckpoint = (long)_position + AwkExecutionGovernor.WorkChunkBytes;
        }
    }

    private struct AwkSingleByteFieldScanner(AwkText line, byte separator, Action checkpoint, int position = 0)
    {
        private int _position = position;
        private bool _completed;

        public readonly int Position => _position;
        public readonly bool Completed => _completed;

        public bool MoveNext(out AwkByteRange field, out AwkByteRange trailingSeparator)
        {
            if (_completed)
            {
                field = default;
                trailingSeparator = default;
                return false;
            }

            var content = line.AsSpan(checkpoint);
            var next = -1;
            for (var searchOffset = _position; searchOffset < content.Length;)
            {
                checkpoint();
                var length = Math.Min(AwkExecutionGovernor.WorkChunkBytes, content.Length - searchOffset);
                var found = content.Slice(searchOffset, length).IndexOf(separator);
                if (found >= 0)
                {
                    next = searchOffset - _position + found;
                    break;
                }
                searchOffset += length;
            }
            if (next < 0)
            {
                field = new AwkByteRange(_position, content.Length - _position);
                trailingSeparator = new AwkByteRange(content.Length, 0);
                _completed = true;
                return true;
            }

            field = new AwkByteRange(_position, next);
            trailingSeparator = new AwkByteRange(_position + next, 1);
            _position += next + 1;
            return true;
        }
    }

    private int CountSingleByteFields(ReadOnlySpan<byte> content, byte separator)
    {
        var count = 1;
        for (var index = 0; index < content.Length; index++)
        {
            if ((index & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                throwIfCancellationRequested();
            if (content[index] == separator)
                count++;
        }

        throwIfCancellationRequested();
        return count;
    }

    private AwkText SliceTrustedFieldText(
        ReadOnlySpan<byte> content,
        int start,
        int length,
        AwkRuntimeCounters counters)
    {
        if (length == 0)
            return AwkText.Empty;

        return AwkText.CopyTrustedBytes(content.Slice(start, length), counters, memoryAccounting, throwIfCancellationRequested);
    }
}
