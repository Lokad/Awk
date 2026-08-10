using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Lokad.Awk;

internal sealed class AwkByteFieldSplitter(
    Func<AwkRegexPattern, AwkRegex> createRegex,
    Func<AwkRegexLiteralText, AwkRegex> createLiteralRegex,
    Func<AwkRegex, AwkText, AwkRegexMatchSpanEnumerator> enumerateMatchSpans,
    Func<bool> isIgnoreCaseEnabled,
    Func<bool> charactersAsBytes,
    Action throwIfCancellationRequested,
    AwkMemoryGovernor memoryGovernor)
{
    public AwkTextSplitResult SplitBySeparator(
        AwkText line,
        AwkFieldSeparatorSpec separator,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        if (separator.UsesWhitespaceSemantics)
            return SplitWhitespace(line, captureSeparators, counters);

        if (separator.ByteText.ByteLength == 0)
            return SplitCharacters(line, captureSeparators, charactersAsBytes(), counters);

        if (TryGetSingleByteLiteralSeparator(separator.ByteText, out var separatorByte) && !isIgnoreCaseEnabled())
            return SplitSingleByteLiteral(line, separatorByte, captureSeparators, counters);

        var regex = separator.ByteText.RuneLength > 1
            ? createRegex(new AwkRegexPattern(separator.Text))
            : createLiteralRegex(new AwkRegexLiteralText(separator.Text));
        return SplitRegex(line, regex, captureSeparators, counters);
    }

    public IReadOnlyList<AwkFieldText> SplitRecordFieldsBySeparator(
        AwkText line,
        AwkFieldSeparatorSpec separator)
    {
        if (separator.UsesWhitespaceSemantics)
            return SplitWhitespaceRecordFields(line);

        if (separator.ByteText.ByteLength == 0)
            return SplitCharacterRecordFields(line, charactersAsBytes());

        if (TryGetSingleByteLiteralSeparator(separator.ByteText, out var separatorByte) && !isIgnoreCaseEnabled())
            return SplitSingleByteLiteralRecordFields(line, separatorByte);

        var regex = separator.ByteText.RuneLength > 1
            ? createRegex(new AwkRegexPattern(separator.Text))
            : createLiteralRegex(new AwkRegexLiteralText(separator.Text));
        return SplitRegexRecordFields(line, regex);
    }

    public IReadOnlyList<AwkFieldText> SplitParagraphRecordFieldsBySeparator(
        AwkText line,
        AwkFieldSeparatorSpec separator)
    {
        if (separator.UsesWhitespaceSemantics || separator.ByteText.RuneLength != 1)
            return SplitRecordFieldsBySeparator(line, separator);

        if (TryGetSingleByteLiteralSeparator(separator.ByteText, out var separatorByte) && !isIgnoreCaseEnabled())
            return SplitSingleByteParagraphRecordFields(line, separatorByte);

        var regex = createLiteralRegex(new AwkRegexLiteralText(separator.Text));
        return SplitRegexParagraphRecordFields(line, regex);
    }

    public AwkTextSplitResult Patsplit(
        AwkText input,
        string pattern,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        var regex = createRegex(new AwkRegexPattern(pattern));
        var fields = new List<AwkText>();
        var separators = captureSeparators ? new List<AwkText>() : null;
        var content = input.AsSpan();
        var position = 0;
        using var matches = enumerateMatchSpans(regex, input);
        while (matches.MoveNext(out var match))
        {
            throwIfCancellationRequested();
            if (separators != null)
                separators.Add(SliceTrustedFieldText(content, position, match.ByteIndex - position, counters));

            fields.Add(SliceTrustedFieldText(content, match.ByteIndex, match.ByteLength, counters));
            position = match.ByteIndex + match.ByteLength;
        }

        if (separators != null)
            separators.Add(SliceTrustedFieldText(content, position, content.Length - position, counters));

        return new AwkTextSplitResult(fields, separators ?? []);
    }

    public IReadOnlyList<AwkFieldText> PatsplitRecordFields(AwkText input, string pattern)
    {
        var regex = createRegex(new AwkRegexPattern(pattern));
        var fields = new List<AwkFieldText>();
        using var matches = enumerateMatchSpans(regex, input);
        while (matches.MoveNext(out var match))
        {
            throwIfCancellationRequested();
            fields.Add(AwkFieldText.FromRecordSlice(match.ByteIndex, match.ByteLength));
        }

        return fields;
    }

    private AwkTextSplitResult SplitWhitespace(
        AwkText line,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        var content = line.AsSpan();
        if (content.Length == 0)
            return new AwkTextSplitResult([], captureSeparators ? [AwkText.Empty] : []);

        if (!captureSeparators)
        {
            var uncapturedFields = ScanWhitespaceFields<OwnedListFieldSink, IReadOnlyList<AwkText>>(
                line,
                new OwnedListFieldSink(line, counters, memoryGovernor));
            return new AwkTextSplitResult(uncapturedFields, []);
        }

        var fields = new List<AwkText>();
        var separators = new List<AwkText>();
        var scanner = new AwkWhitespaceFieldScanner(line);
        while (scanner.MoveNext(out var field, out var precedingSeparator))
        {
            separators.Add(SliceTrustedFieldText(
                content,
                precedingSeparator.Start,
                precedingSeparator.Length,
                counters));

            fields.Add(SliceTrustedFieldText(content, field.Start, field.Length, counters));
        }

        separators.Add(SliceTrustedFieldText(
            content,
            scanner.TrailingSeparator.Start,
            scanner.TrailingSeparator.Length,
            counters));

        return new AwkTextSplitResult(fields, separators);
    }

    private static IReadOnlyList<AwkFieldText> SplitWhitespaceRecordFields(AwkText line) =>
        line.ByteLength == 0
            ? []
            : ScanWhitespaceFields<RecordListFieldSink, IReadOnlyList<AwkFieldText>>(
                line,
                new RecordListFieldSink());

    private AwkTextSplitResult SplitCharacters(
        AwkText line,
        bool captureSeparators,
        bool splitBytes,
        AwkRuntimeCounters counters)
    {
        if (line.ByteLength == 0)
            return new AwkTextSplitResult([], captureSeparators ? [AwkText.Empty] : []);

        var fields = ScanCharacterFields<OwnedListFieldSink, IReadOnlyList<AwkText>>(
            line,
            splitBytes,
            new OwnedListFieldSink(line, counters, memoryGovernor, splitBytes ? line.ByteLength : 0));

        if (!captureSeparators)
            return new AwkTextSplitResult(fields, []);

        var separators = new AwkText[fields.Count + 1];
        Array.Fill(separators, AwkText.Empty);
        return new AwkTextSplitResult(fields, separators);
    }

    private static IReadOnlyList<AwkFieldText> SplitCharacterRecordFields(AwkText line, bool splitBytes)
    {
        if (line.ByteLength == 0)
            return [];

        return ScanCharacterFields<RecordListFieldSink, IReadOnlyList<AwkFieldText>>(
            line,
            splitBytes,
            new RecordListFieldSink(splitBytes ? line.ByteLength : 0));
    }

    private AwkTextSplitResult SplitSingleByteLiteral(
        AwkText line,
        byte separator,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        var content = line.AsSpan();
        if (!captureSeparators)
        {
            var fieldCount = CountSingleByteFields(content, separator);
            var fieldArray = new AwkText[fieldCount];
            ScanSingleByteFields<OwnedArrayFieldSink, AwkText[]>(
                line,
                separator,
                new OwnedArrayFieldSink(line, counters, memoryGovernor, fieldArray));

            return new AwkTextSplitResult(fieldArray, []);
        }

        var fields = new List<AwkText>();
        var separators = new List<AwkText> { AwkText.Empty };
        var capturingScanner = new AwkSingleByteFieldScanner(line, separator);
        while (capturingScanner.MoveNext(out var field, out var trailingSeparator))
        {
            fields.Add(SliceTrustedFieldText(content, field.Start, field.Length, counters));
            separators.Add(SliceTrustedFieldText(
                content,
                trailingSeparator.Start,
                trailingSeparator.Length,
                counters));
        }

        return new AwkTextSplitResult(fields, separators);
    }

    private static AwkFieldText[] SplitSingleByteLiteralRecordFields(AwkText line, byte separator)
    {
        var content = line.AsSpan();
        var fieldCount = CountSingleByteFields(content, separator);
        var fields = new AwkFieldText[fieldCount];
        ScanSingleByteFields<RecordArrayFieldSink, AwkFieldText[]>(
            line,
            separator,
            new RecordArrayFieldSink(fields));

        return fields;
    }

    private static List<AwkFieldText> SplitSingleByteParagraphRecordFields(AwkText line, byte separator)
    {
        var content = line.AsSpan();
        var fields = new List<AwkFieldText>();
        var position = 0;
        for (var index = 0; index < content.Length; index++)
        {
            if (content[index] != separator && content[index] != (byte)'\n')
                continue;

            fields.Add(AwkFieldText.FromRecordSlice(position, index - position));
            position = index + 1;
        }

        fields.Add(AwkFieldText.FromRecordSlice(position, content.Length - position));
        return fields;
    }

    private AwkTextSplitResult SplitRegex(
        AwkText line,
        AwkRegex separator,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        if (!captureSeparators)
        {
            var spanFields = ScanRegexFields<OwnedListFieldSink, IReadOnlyList<AwkText>>(
                line,
                separator,
                new OwnedListFieldSink(line, counters, memoryGovernor));
            return new AwkTextSplitResult(spanFields, []);
        }

        var fields = new List<AwkText>();
        var separators = new List<AwkText> { AwkText.Empty };
        var content = line.AsSpan();
        var position = 0;
        using var matches = enumerateMatchSpans(separator, line);
        while (matches.MoveNext(out var match))
        {
            throwIfCancellationRequested();
            fields.Add(SliceTrustedFieldText(content, position, match.ByteIndex - position, counters));
            separators.Add(SliceTrustedFieldText(content, match.ByteIndex, match.ByteLength, counters));
            position = AdvanceAfterRegexSeparator(line, match.ByteIndex, match.ByteLength);
        }

        fields.Add(SliceTrustedFieldText(content, position, content.Length - position, counters));
        separators.Add(AwkText.Empty);
        return new AwkTextSplitResult(fields, separators);
    }

    private IReadOnlyList<AwkFieldText> SplitRegexRecordFields(AwkText line, AwkRegex separator)
        => ScanRegexFields<RecordListFieldSink, IReadOnlyList<AwkFieldText>>(
            line,
            separator,
            new RecordListFieldSink());

    private List<AwkFieldText> SplitRegexParagraphRecordFields(AwkText line, AwkRegex separator)
    {
        var content = line.AsSpan();
        var fields = new List<AwkFieldText>();
        var position = 0;
        using var matches = enumerateMatchSpans(separator, line);
        while (matches.MoveNext(out var match))
        {
            throwIfCancellationRequested();
            while (position < match.ByteIndex)
            {
                var newlineOffset = content[position..match.ByteIndex].IndexOf((byte)'\n');
                if (newlineOffset < 0)
                    break;

                fields.Add(AwkFieldText.FromRecordSlice(position, newlineOffset));
                position += newlineOffset + 1;
            }

            fields.Add(AwkFieldText.FromRecordSlice(position, match.ByteIndex - position));
            position = AdvanceAfterRegexSeparator(line, match.ByteIndex, match.ByteLength);
        }

        while (position < content.Length)
        {
            var newlineOffset = content[position..].IndexOf((byte)'\n');
            if (newlineOffset < 0)
                break;

            fields.Add(AwkFieldText.FromRecordSlice(position, newlineOffset));
            position += newlineOffset + 1;
        }

        fields.Add(AwkFieldText.FromRecordSlice(position, content.Length - position));
        return fields;
    }

    private static TFields ScanWhitespaceFields<TSink, TFields>(AwkText line, TSink sink)
        where TSink : struct, IFieldSink<TFields>
    {
        var scanner = new AwkWhitespaceFieldScanner(line);
        while (scanner.MoveNext(out var field, out _))
            sink.Add(field);

        return sink.Complete();
    }

    private static TFields ScanCharacterFields<TSink, TFields>(
        AwkText line,
        bool splitBytes,
        TSink sink)
        where TSink : struct, IFieldSink<TFields>
    {
        var scanner = new AwkCharacterFieldScanner(line, splitBytes);
        while (scanner.MoveNext(out var field))
            sink.Add(field);

        return sink.Complete();
    }

    private static TFields ScanSingleByteFields<TSink, TFields>(
        AwkText line,
        byte separator,
        TSink sink)
        where TSink : struct, IFieldSink<TFields>
    {
        var scanner = new AwkSingleByteFieldScanner(line, separator);
        while (scanner.MoveNext(out var field, out _))
            sink.Add(field);

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
            sink.Add(new AwkByteRange(position, match.ByteIndex - position));
            position = AdvanceAfterRegexSeparator(line, match.ByteIndex, match.ByteLength);
        }

        sink.Add(new AwkByteRange(position, line.ByteLength - position));
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
        private readonly AwkMemoryGovernor _memoryGovernor;
        private readonly List<AwkText> _fields;

        public OwnedListFieldSink(
            AwkText line,
            AwkRuntimeCounters counters,
            AwkMemoryGovernor memoryGovernor)
        {
            _line = line;
            _counters = counters;
            _memoryGovernor = memoryGovernor;
            _fields = [];
        }

        public OwnedListFieldSink(
            AwkText line,
            AwkRuntimeCounters counters,
            AwkMemoryGovernor memoryGovernor,
            int capacity)
        {
            _line = line;
            _counters = counters;
            _memoryGovernor = memoryGovernor;
            _fields = new List<AwkText>(capacity);
        }

        public void Add(AwkByteRange field) =>
            _fields.Add(_line.CopyByteSlice(
                field.Start,
                field.Length,
                _counters,
                _memoryGovernor));

        public IReadOnlyList<AwkText> Complete() => _fields;
    }

    private struct OwnedArrayFieldSink(
        AwkText line,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor memoryGovernor,
        AwkText[] fields) : IFieldSink<AwkText[]>
    {
        private int _next;

        public void Add(AwkByteRange field) =>
            fields[_next++] = line.CopyByteSlice(
                field.Start,
                field.Length,
                counters,
                memoryGovernor);

        public readonly AwkText[] Complete() => fields;
    }

    private readonly struct RecordListFieldSink : IFieldSink<IReadOnlyList<AwkFieldText>>
    {
        private readonly List<AwkFieldText> _fields;

        public RecordListFieldSink()
        {
            _fields = [];
        }

        public RecordListFieldSink(int capacity)
        {
            _fields = new List<AwkFieldText>(capacity);
        }

        public void Add(AwkByteRange field) =>
            _fields.Add(AwkFieldText.FromRecordSlice(field.Start, field.Length));

        public IReadOnlyList<AwkFieldText> Complete() => _fields;
    }

    private struct RecordArrayFieldSink(AwkFieldText[] fields) : IFieldSink<AwkFieldText[]>
    {
        private int _next;

        public void Add(AwkByteRange field) =>
            fields[_next++] = AwkFieldText.FromRecordSlice(field.Start, field.Length);

        public readonly AwkFieldText[] Complete() => fields;
    }

    private readonly record struct AwkByteRange(int Start, int Length);

    private struct AwkWhitespaceFieldScanner(AwkText line)
    {
        private int _position;

        public AwkByteRange TrailingSeparator { get; private set; }

        public bool MoveNext(out AwkByteRange field, out AwkByteRange precedingSeparator)
        {
            var content = line.AsSpan();
            var separatorStart = _position;
            while (_position < content.Length && IsDefaultFieldWhitespace(content[_position]))
                _position++;

            precedingSeparator = new AwkByteRange(separatorStart, _position - separatorStart);
            if (_position >= content.Length)
            {
                TrailingSeparator = precedingSeparator;
                field = default;
                return false;
            }

            var fieldStart = _position;
            while (_position < content.Length && !IsDefaultFieldWhitespace(content[_position]))
                _position++;

            field = new AwkByteRange(fieldStart, _position - fieldStart);
            return true;
        }
    }

    private struct AwkCharacterFieldScanner(AwkText line, bool splitBytes)
    {
        private int _position;

        public bool MoveNext(out AwkByteRange field)
        {
            var content = line.AsSpan();
            if (_position >= content.Length)
            {
                field = default;
                return false;
            }

            if (splitBytes)
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
    }

    private struct AwkSingleByteFieldScanner(AwkText line, byte separator)
    {
        private int _position;
        private bool _completed;

        public bool MoveNext(out AwkByteRange field, out AwkByteRange trailingSeparator)
        {
            if (_completed)
            {
                field = default;
                trailingSeparator = default;
                return false;
            }

            var content = line.AsSpan();
            var next = content[_position..].IndexOf(separator);
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

    private static bool IsDefaultFieldWhitespace(byte value) =>
        value is (byte)' ' or (byte)'\t' or (byte)'\n';

    private static int AdvanceAfterRegexSeparator(AwkText line, int byteIndex, int byteLength)
    {
        if (byteLength > 0 || byteIndex >= line.ByteLength)
            return byteIndex + byteLength;

        var status = Rune.DecodeFromUtf8(line.AsSpan()[byteIndex..], out _, out var bytesConsumed);
        return byteIndex + (status == OperationStatus.Done && bytesConsumed > 0 ? bytesConsumed : 1);
    }

    private static bool TryGetSingleByteLiteralSeparator(AwkText separator, out byte separatorByte)
    {
        if (separator.AsSpan() is [var value])
        {
            separatorByte = value;
            return true;
        }

        separatorByte = 0;
        return false;
    }

    private static int CountSingleByteFields(ReadOnlySpan<byte> content, byte separator)
    {
        var count = 1;
        for (var index = 0; index < content.Length; index++)
        {
            if (content[index] == separator)
                count++;
        }

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

        return AwkText.CopyTrustedBytes(content.Slice(start, length), counters, memoryGovernor);
    }
}
