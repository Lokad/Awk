using System;
using System.Text;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>
/// Pulls records from one descriptor while preserving unread input across <c>RS</c> changes.
/// </summary>
internal sealed class AwkRecordCursor : IDisposable
{
    private readonly AwkRuntimeState _state;
    private readonly AwkRecordDescriptorReader _descriptorReader;
    private readonly AwkFileDescriptor _descriptor;
    private AsyncLineReader? _lineReader;
    private AwkUtf8Builder? _buffer;
    private int _bufferStart;
    private bool _descriptorReachedEof;
    private bool _hasConsumedDescriptorBytes;
    private AwkText? _regexInput;
    private int _regexInputPrefixLength;
    private int _regexOffsetBufferStart = -1;
    private int _regexOffsetUtf16Index;
    private SeparatorCache _separatorCache;

    public AwkRecordCursor(
        AwkRuntimeState state,
        AwkRecordDescriptorReader descriptorReader,
        AsyncLineReader lineReader)
    {
        _state = state;
        _descriptorReader = descriptorReader;
        _descriptor = lineReader.Descriptor;
        _lineReader = lineReader;
    }

    public AwkFileDescriptor Descriptor => _descriptor;

    public async ValueTask<AwkInputRecordResult> ReadAsync()
    {
        while (true)
        {
            _state.ThrowIfCancellationRequested();
            var separator = GetCurrentSeparator();
            if (_lineReader is not null && separator is NewlineSeparator)
                return await ReadDefaultNewlineAsync(_lineReader).ConfigureAwait(false);

            EnsureBufferedMode();
            var result = TryReadBuffered(separator);
            if (result.Status != BufferedReadStatus.NeedMoreData)
                return result.Record;

            await AppendNextBatchAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _buffer?.Dispose();
        _buffer = null;
        _lineReader = null;
        _regexInput = null;
    }

    private async ValueTask<AwkInputRecordResult> ReadDefaultNewlineAsync(AsyncLineReader reader)
    {
        if (await reader.ReachedEof().ConfigureAwait(false))
            return AwkInputRecordResult.EndOfFile;

        var line = AwkRecordLine.ReadUtf8DefaultNewline(reader.ReadLine(), _state.Counters);
        return AwkInputRecordResult.Record(
            line.Record,
            line.Terminator,
            AwkRecordFieldSplittingMode.Ordinary);
    }

    private void EnsureBufferedMode()
    {
        if (_buffer is not null)
            return;

        var tail = _lineReader?.TakeBufferedTail() ?? default;
        _lineReader = null;
        _buffer = new AwkUtf8Builder(
            _state.Limits,
            _state.MemoryGovernor,
            Math.Max(AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity, tail.Content.Length));
        _buffer.Append(tail.Content);
        _descriptorReachedEof = tail.DescriptorReachedEof;
    }

    private async Task AppendNextBatchAsync()
    {
        if (_descriptorReachedEof)
            throw new InvalidOperationException("Cannot append input after reaching end of file.");

        CompactBufferBeforeAppend();
        var batch = await _descriptorReader.ReadNextBatchAsync(_descriptor).ConfigureAwait(false);
        _buffer?.Append(batch.Content);
        _regexInput = null;
        _regexOffsetBufferStart = -1;
        _descriptorReachedEof = batch.ReachedEof;
    }

    private void CompactBufferBeforeAppend()
    {
        var buffer = _buffer ?? throw new InvalidOperationException("Buffered input is not initialized.");
        if (_bufferStart == 0)
            return;

        var remainingLength = buffer.Length - _bufferStart;
        if (remainingLength > 0)
            _state.Counters.RecordRecordSeparatorTailBytesMoved(remainingLength);

        buffer.RemovePrefix(_bufferStart);
        _bufferStart = 0;
        _regexInput = null;
        _regexOffsetBufferStart = -1;
    }

    private BufferedReadResult TryReadBuffered(RecordSeparator separator)
    {
        var buffer = _buffer ?? throw new InvalidOperationException("Buffered input is not initialized.");
        if (_bufferStart == buffer.Length)
        {
            buffer.Clear();
            _bufferStart = 0;
            _regexInput = null;
            _regexOffsetBufferStart = -1;
            return _descriptorReachedEof
                ? BufferedReadResult.EndOfFile
                : BufferedReadResult.NeedMoreData;
        }

        var content = buffer.AsSpan()[_bufferStart..];
        return separator switch
        {
            NewlineSeparator => TryReadNewline(content),
            ParagraphSeparator => TryReadParagraph(content),
            SingleByteSeparator singleByte => TryReadSingleByte(content, singleByte),
            ByteSetSeparator byteSet => TryReadByteSet(content, byteSet),
            LiteralSeparator literal => TryReadLiteral(content, literal),
            RegexSeparator regex => TryReadRegex(regex),
            _ => throw new InvalidOperationException("Unexpected awk record separator strategy.")
        };
    }

    private BufferedReadResult TryReadNewline(ReadOnlySpan<byte> content)
    {
        var separatorIndex = content.IndexOf((byte)'\n');
        if (separatorIndex < 0)
            return FinalRecordOrNeedMore(content, AwkRecordFieldSplittingMode.Ordinary);

        var recordLength = separatorIndex;
        if (recordLength > 0 && content[recordLength - 1] == (byte)'\r')
            recordLength--;

        return ConsumeRecord(
            content[..recordLength],
            separatorIndex + 1,
            AwkRecordTerminator.NewLine,
            AwkRecordFieldSplittingMode.Ordinary);
    }

    private BufferedReadResult TryReadSingleByte(
        ReadOnlySpan<byte> content,
        SingleByteSeparator separator)
    {
        var separatorIndex = content.IndexOf(separator.Value);
        return separatorIndex < 0
            ? FinalRecordOrNeedMore(content, AwkRecordFieldSplittingMode.Ordinary)
            : ConsumeRecord(
                content[..separatorIndex],
                separatorIndex + 1,
                separator.Terminator,
                AwkRecordFieldSplittingMode.Ordinary);
    }

    private BufferedReadResult TryReadByteSet(
        ReadOnlySpan<byte> content,
        ByteSetSeparator separator)
    {
        for (var index = 0; index < content.Length; index++)
        {
            var value = content[index];
            if (!separator.Contains(value))
                continue;

            return ConsumeRecord(
                content[..index],
                index + 1,
                separator.Terminator(value),
                AwkRecordFieldSplittingMode.Ordinary);
        }

        return FinalRecordOrNeedMore(content, AwkRecordFieldSplittingMode.Ordinary);
    }

    private BufferedReadResult TryReadLiteral(
        ReadOnlySpan<byte> content,
        LiteralSeparator separator)
    {
        var separatorIndex = content.IndexOf(separator.Value.AsSpan());
        return separatorIndex < 0
            ? FinalRecordOrNeedMore(content, AwkRecordFieldSplittingMode.Ordinary)
            : ConsumeRecord(
                content[..separatorIndex],
                separatorIndex + separator.Value.ByteLength,
                separator.Terminator,
                AwkRecordFieldSplittingMode.Ordinary);
    }

    private BufferedReadResult TryReadRegex(RegexSeparator separator)
    {
        var buffer = _buffer ?? throw new InvalidOperationException("Buffered input is not initialized.");
        var input = GetRegexInput(buffer);
        var searchOffset = GetRegexSearchOffset(buffer);
        var match = _state.MatchRegexSpan(
            separator.Value,
            input,
            searchOffset);
        if (!match.Success)
        {
            var remainingLength = buffer.Length - _bufferStart;
            return _descriptorReachedEof
                ? ConsumeExistingText(
                    input.CopyByteSlice(
                        searchOffset.ByteIndex,
                        remainingLength,
                        _state.Counters,
                        _state.MemoryGovernor),
                    remainingLength,
                    AwkRecordTerminator.Empty)
                : BufferedReadResult.NeedMoreData;
        }

        if (!_descriptorReachedEof && match.ByteIndex + match.ByteLength == input.ByteLength)
            return BufferedReadResult.NeedMoreData;

        var recordLength = match.ByteIndex - searchOffset.ByteIndex;
        var record = input.CopyByteSlice(
            searchOffset.ByteIndex,
            recordLength,
            _state.Counters,
            _state.MemoryGovernor);
        var terminator = match.ByteLength == 0
            ? AwkRecordTerminator.Empty
            : AwkRecordTerminator.FromText(input.CopyByteSlice(
                match.ByteIndex,
                match.ByteLength,
                _state.Counters,
                _state.MemoryGovernor));
        var next = AwkRegexSearchOffset.AfterMatch(input, match);
        var consumedLength = next.ByteIndex - searchOffset.ByteIndex;
        _bufferStart += consumedLength;
        _hasConsumedDescriptorBytes |= consumedLength > 0;
        _regexOffsetBufferStart = _bufferStart;
        _regexOffsetUtf16Index = next.Utf16Index;
        return BufferedReadResult.FromRecord(AwkInputRecordResult.Record(
            record,
            terminator,
            AwkRecordFieldSplittingMode.Ordinary));
    }

    private BufferedReadResult TryReadParagraph(ReadOnlySpan<byte> content)
    {
        var recordStart = SkipParagraphNewlines(content);
        _bufferStart += recordStart;
        _regexOffsetBufferStart = -1;
        content = content[recordStart..];
        if (content.IsEmpty)
        {
            return _descriptorReachedEof
                ? BufferedReadResult.EndOfFile
                : BufferedReadResult.NeedMoreData;
        }

        if (!TryFindParagraphSeparator(content, out var separatorIndex, out var separatorLength))
        {
            if (!_descriptorReachedEof)
                return BufferedReadResult.NeedMoreData;

            var recordLength = TrimTrailingRecordNewlines(content);
            return ConsumeRecord(
                content[..recordLength],
                content.Length,
                AwkRecordTerminator.Empty,
                AwkRecordFieldSplittingMode.Paragraph);
        }

        if (!_descriptorReachedEof && separatorIndex + separatorLength == content.Length)
            return BufferedReadResult.NeedMoreData;

        var terminator = AwkRecordTerminator.FromText(CreateText(
            content.Slice(separatorIndex, separatorLength)));
        return ConsumeRecord(
            content[..TrimTrailingRecordNewlines(content[..separatorIndex])],
            separatorIndex + separatorLength,
            terminator,
            AwkRecordFieldSplittingMode.Paragraph);
    }

    private BufferedReadResult FinalRecordOrNeedMore(
        ReadOnlySpan<byte> content,
        AwkRecordFieldSplittingMode fieldSplittingMode) =>
        _descriptorReachedEof
            ? ConsumeRecord(
                content,
                content.Length,
                AwkRecordTerminator.Empty,
                fieldSplittingMode)
            : BufferedReadResult.NeedMoreData;

    private BufferedReadResult ConsumeExistingText(
        AwkText record,
        int consumedLength,
        AwkRecordTerminator terminator)
    {
        _bufferStart += consumedLength;
        _hasConsumedDescriptorBytes |= consumedLength > 0;
        _regexOffsetBufferStart = -1;
        return BufferedReadResult.FromRecord(AwkInputRecordResult.Record(
            record,
            terminator,
            AwkRecordFieldSplittingMode.Ordinary));
    }

    private BufferedReadResult ConsumeRecord(
        ReadOnlySpan<byte> recordBytes,
        int consumedLength,
        AwkRecordTerminator terminator,
        AwkRecordFieldSplittingMode fieldSplittingMode)
    {
        var record = CreateText(recordBytes);
        _bufferStart += consumedLength;
        _hasConsumedDescriptorBytes |= consumedLength > 0;
        _regexOffsetBufferStart = -1;
        return BufferedReadResult.FromRecord(AwkInputRecordResult.Record(
            record,
            terminator,
            fieldSplittingMode));
    }

    private AwkText CreateText(ReadOnlySpan<byte> content) =>
        content.IsEmpty
            ? AwkText.Empty
            : AwkText.FromOwnedBytes(content.ToArray(), _state.Counters, _state.MemoryGovernor);

    private AwkText GetRegexInput(AwkUtf8Builder buffer)
    {
        if (_regexInput is not null)
            return _regexInput;

        _regexInputPrefixLength = _hasConsumedDescriptorBytes ? 1 : 0;
        if (_regexInputPrefixLength == 0)
        {
            _regexInput = CreateText(buffer.AsSpan());
            return _regexInput;
        }

        var content = buffer.AsSpan();
        var bytes = new byte[content.Length + 1];
        bytes[0] = 0x01;
        content.CopyTo(bytes.AsSpan(1));
        _regexInput = AwkText.FromOwnedBytes(bytes, _state.Counters, _state.MemoryGovernor);
        return _regexInput;
    }

    private AwkRegexSearchOffset GetRegexSearchOffset(AwkUtf8Builder buffer)
    {
        if (_regexOffsetBufferStart == _bufferStart)
        {
            return new AwkRegexSearchOffset(
                _regexInputPrefixLength + _bufferStart,
                _regexOffsetUtf16Index);
        }

        _regexOffsetBufferStart = _bufferStart;
        _regexOffsetUtf16Index = _regexInputPrefixLength +
            Encoding.UTF8.GetCharCount(buffer.AsSpan()[.._bufferStart]);
        return new AwkRegexSearchOffset(
            _regexInputPrefixLength + _bufferStart,
            _regexOffsetUtf16Index);
    }

    private RecordSeparator GetCurrentSeparator()
    {
        var version = _state.RecordSeparatorVersion;
        if (_separatorCache.HasValue && _separatorCache.Version == version)
            return _separatorCache.Separator;

        var separator = CreateSeparator(_state.ResolveRecordSeparator());
        _separatorCache = new SeparatorCache(separator, version);
        return separator;
    }

    private RecordSeparator CreateSeparator(string separatorText)
    {
        if (separatorText.Length == 0)
            return ParagraphSeparator.Instance;

        if (separatorText == "\n")
            return NewlineSeparator.Instance;

        if (separatorText.Length == 1 && separatorText[0] <= 0x7F)
        {
            var value = (byte)separatorText[0];
            return new SingleByteSeparator(
                value,
                AwkRecordTerminator.FromText(AwkText.FromTrustedOwnedBytes([value], _state.Counters)));
        }

        if (TryCreateByteSet(separatorText, out var byteSet))
            return byteSet;

        if (separatorText.Length == 1)
        {
            var value = AwkText.FromString(separatorText, _state.Counters);
            return new LiteralSeparator(value, AwkRecordTerminator.FromText(value));
        }

        return new RegexSeparator(_state.CreateRegex(new AwkRegexPattern(separatorText)));
    }

    private bool TryCreateByteSet(string separatorText, out ByteSetSeparator separator)
    {
        static bool IsAsciiLetter(char value) =>
            value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

        if (separatorText.Length < 3 || separatorText[0] != '[' || separatorText[^1] != ']')
        {
            separator = ByteSetSeparator.Empty;
            return false;
        }

        var values = new bool[256];
        var terminators = new AwkRecordTerminator[256];
        var count = 0;
        for (var index = 1; index < separatorText.Length - 1; index++)
        {
            var ch = separatorText[index];
            if (ch > 0x7F || ch is '\\' or '-' or '^' or '[' or ']' || IsAsciiLetter(ch))
            {
                separator = ByteSetSeparator.Empty;
                return false;
            }

            var value = (byte)ch;
            if (values[value])
                continue;

            values[value] = true;
            terminators[value] = AwkRecordTerminator.FromText(
                AwkText.FromTrustedOwnedBytes([value], _state.Counters));
            count++;
        }

        if (count == 0)
        {
            separator = ByteSetSeparator.Empty;
            return false;
        }

        separator = new ByteSetSeparator(values, terminators);
        return true;
    }

    private static int SkipParagraphNewlines(ReadOnlySpan<byte> content)
    {
        var position = 0;
        while (TryReadNewline(content, position, out var newlineLength))
            position += newlineLength;

        return position;
    }

    private static bool TryFindParagraphSeparator(
        ReadOnlySpan<byte> content,
        out int separatorIndex,
        out int separatorLength)
    {
        for (var index = 0; index < content.Length; index++)
        {
            if (!TryReadNewline(content, index, out var firstNewlineLength))
                continue;

            var position = index + firstNewlineLength;
            if (!TryReadNewline(content, position, out var secondNewlineLength))
                continue;

            position += secondNewlineLength;
            while (TryReadNewline(content, position, out var trailingNewlineLength))
                position += trailingNewlineLength;

            separatorIndex = index;
            separatorLength = position - index;
            return true;
        }

        separatorIndex = -1;
        separatorLength = 0;
        return false;
    }

    private static bool TryReadNewline(ReadOnlySpan<byte> content, int position, out int length)
    {
        if (position >= content.Length)
        {
            length = 0;
            return false;
        }

        if (content[position] == (byte)'\n')
        {
            length = 1;
            return true;
        }

        if (content[position] == (byte)'\r' &&
            position + 1 < content.Length &&
            content[position + 1] == (byte)'\n')
        {
            length = 2;
            return true;
        }

        length = 0;
        return false;
    }

    private static int TrimTrailingRecordNewlines(ReadOnlySpan<byte> content)
    {
        var end = content.Length;
        while (end > 0 && content[end - 1] == (byte)'\n')
        {
            end--;
            if (end > 0 && content[end - 1] == (byte)'\r')
                end--;
        }

        return end;
    }

    private enum BufferedReadStatus
    {
        NeedMoreData,
        Record,
        EndOfFile
    }

    private readonly record struct BufferedReadResult(
        BufferedReadStatus Status,
        AwkInputRecordResult Record)
    {
        public static BufferedReadResult NeedMoreData { get; } =
            new(BufferedReadStatus.NeedMoreData, AwkInputRecordResult.EndOfFile);

        public static BufferedReadResult EndOfFile { get; } =
            new(BufferedReadStatus.EndOfFile, AwkInputRecordResult.EndOfFile);

        public static BufferedReadResult FromRecord(AwkInputRecordResult record) =>
            new(BufferedReadStatus.Record, record);
    }

    private readonly record struct SeparatorCache(RecordSeparator Separator, long Version)
    {
        public bool HasValue { get; } = true;
    }

    private abstract record RecordSeparator;

    private sealed record NewlineSeparator : RecordSeparator
    {
        public static NewlineSeparator Instance { get; } = new();
    }

    private sealed record ParagraphSeparator : RecordSeparator
    {
        public static ParagraphSeparator Instance { get; } = new();
    }

    private sealed record SingleByteSeparator(
        byte Value,
        AwkRecordTerminator Terminator) : RecordSeparator;

    private sealed record ByteSetSeparator(
        bool[] Values,
        AwkRecordTerminator[] Terminators) : RecordSeparator
    {
        public static ByteSetSeparator Empty { get; } = new([], []);

        public bool Contains(byte value) => Values[value];

        public AwkRecordTerminator Terminator(byte value) => Terminators[value];
    }

    private sealed record LiteralSeparator(
        AwkText Value,
        AwkRecordTerminator Terminator) : RecordSeparator;

    private sealed record RegexSeparator(AwkRegex Value) : RecordSeparator;
}

internal readonly record struct AwkInputRecordResult(
    bool HasRecord,
    AwkText RecordText,
    AwkRecordTerminator Terminator,
    AwkRecordFieldSplittingMode FieldSplittingMode)
{
    public static AwkInputRecordResult EndOfFile { get; } =
        new(false, AwkText.Empty, AwkRecordTerminator.Empty, AwkRecordFieldSplittingMode.Ordinary);

    public static AwkInputRecordResult Record(
        AwkText record,
        AwkRecordTerminator terminator,
        AwkRecordFieldSplittingMode fieldSplittingMode) =>
        new(true, record, terminator, fieldSplittingMode);
}
