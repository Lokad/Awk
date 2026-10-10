using System;
using System.Text;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal enum AwkParagraphLeaderPolicy
{
    Discard,
    Retain
}

/// <summary>
/// Pulls records from one descriptor while preserving unread input across <c>RS</c> changes.
/// </summary>
internal sealed class AwkRecordCursor : IAwkInputRecordReader, IDisposable
{
    private readonly AwkRuntimeState _state;
    private readonly Action _scanCheckpoint;
    private readonly AwkRecordDescriptorReader _descriptorReader;
    private readonly AwkFileDescriptor _descriptor;
    private readonly AwkParagraphLeaderPolicy _paragraphLeaderPolicy;
    private AsyncLineReader? _lineReader;
    private AwkUtf8Builder? _buffer;
    // A successful host read cannot be undone. Keep its borrowed bytes until append
    // commits, and do not issue another read while this batch is pending.
    private AwkRecordReadBatch _pendingBatch;
    private bool _hasPendingBatch;
    // All buffered-search positions are absolute indexes into _buffer. The
    // unread record begins at _bufferStart; _scanOffset is the earliest byte
    // that the active separator strategy has not ruled out.
    private int _bufferStart;
    private int _scanOffset;
    // Run and paragraph separators can end at a batch boundary. These bounds
    // retain that candidate so the next batch only extends the pending run.
    private int _pendingSeparatorStart = -1;
    private int _pendingSeparatorEnd = -1;
    private bool _descriptorReachedEof;
    private bool _hasConsumedDescriptorBytes;
    private bool _discardedParagraphLeader;
    private int _paragraphLeaderScanLength;
    // General regex searches borrow the current buffer. The optional reserved
    // prefix prevents ^ from re-anchoring after a record has been consumed;
    // the offset pair caches the byte-to-UTF-16 coordinate conversion.
    private AwkText? _regexInput;
    private int _regexInputPrefixLength;
    private int _regexOffsetBufferStart = -1;
    private int _regexOffsetUtf16Index;
    private SeparatorCache _separatorCache;

    public AwkRecordCursor(
        AwkRuntimeState state,
        AwkRecordDescriptorReader descriptorReader,
        AsyncLineReader lineReader,
        AwkParagraphLeaderPolicy paragraphLeaderPolicy = AwkParagraphLeaderPolicy.Discard,
        Action? scanCheckpoint = null)
    {
        _state = state;
        _descriptorReader = descriptorReader;
        _descriptor = lineReader.Descriptor;
        _paragraphLeaderPolicy = paragraphLeaderPolicy;
        _scanCheckpoint = scanCheckpoint ?? state.CancellationCheckpoint;
        _lineReader = lineReader;
    }

    public AwkFileDescriptor Descriptor => _descriptor;

    public async ValueTask<AwkInputRecordResult> ReadAsync()
    {
        _discardedParagraphLeader = false;
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

        async ValueTask<AwkInputRecordResult> ReadDefaultNewlineAsync(AsyncLineReader reader)
        {
            if (!await reader.TryBufferLineAsync().ConfigureAwait(false))
                return AwkInputRecordResult.EndOfFile;

            var line = AwkRecordLine.SplitDefaultNewline(reader.PeekLine());
            _state.Resources.ThrowIfRecordBytesExceeded(line.RecordBytes.Length);
            _state.Resources.ThrowIfValueBytesExceeded(line.RecordBytes.Length);
            var record = AwkText.CopyHostBytes(
                line.RecordBytes.Span,
                _state.Counters,
                _state.MemoryGovernor,
                _state.CancellationCheckpoint);
            _state.ThrowIfCancellationRequested();
            reader.ReadLine();
            return AwkInputRecordResult.Record(
                record,
                line.Terminator,
                AwkRecordFieldSplittingMode.Ordinary);
        }

        void EnsureBufferedMode()
        {
            if (_buffer is not null)
                return;

            var reader = _lineReader;
            var tail = reader?.PeekBufferedTail() ?? default;
            _state.ThrowIfCancellationRequested();
            // SOH stays outside the record bytes and the regex search window. Including it after
            // compaction keeps ^ anchored to the descriptor start rather than the compacted tail.
            var buffer = AwkUtf8Builder.CreateWithReservedPrefix(
                _state.Limits,
                _state.MemoryGovernor,
                Math.Max(AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity, tail.Content.Length),
                0x01);
            try
            {
                buffer.Append(tail.Content.Span, _state.CancellationCheckpoint);
                _state.ThrowIfCancellationRequested();
            }
            catch
            {
                buffer.Dispose();
                throw;
            }

            reader?.TakeBufferedTail();
            _buffer = buffer;
            _lineReader = null;
            _descriptorReachedEof = tail.DescriptorReachedEof;
        }

        async Task AppendNextBatchAsync()
        {
            if (_descriptorReachedEof)
                throw new InvalidOperationException("Cannot append input after reaching end of file.");

            CompactBufferBeforeAppend();
            if (!_hasPendingBatch)
            {
                _pendingBatch = await _descriptorReader.ReadNextBatchAsync(_descriptor).ConfigureAwait(false);
                _hasPendingBatch = true;
            }

            var buffer = _buffer ?? throw new InvalidOperationException("Buffered input is not initialized.");
            _state.ThrowIfCancellationRequested();
            // Drop borrowed regex views before append can resize their backing buffer.
            _regexInput = null;
            _regexOffsetBufferStart = -1;
            buffer.Append(_pendingBatch.Content.Span, _state.CancellationCheckpoint);
            _descriptorReachedEof = _pendingBatch.ReachedEof;
            _pendingBatch = default;
            _hasPendingBatch = false;

            void CompactBufferBeforeAppend()
            {
                var buffer = _buffer ?? throw new InvalidOperationException("Buffered input is not initialized.");
                if (_bufferStart == 0)
                    return;

                _state.ThrowIfCancellationRequested();
                var removedLength = _bufferStart;
                var remainingLength = buffer.Length - _bufferStart;
                if (remainingLength > 0)
                    _state.Counters.RecordRecordSeparatorTailBytesMoved(remainingLength);

                buffer.RemovePrefix(_bufferStart);
                _bufferStart = 0;
                _scanOffset = Math.Max(0, _scanOffset - removedLength);
                if (_pendingSeparatorStart >= 0)
                {
                    _pendingSeparatorStart -= removedLength;
                    _pendingSeparatorEnd -= removedLength;
                }
                _regexInput = null;
                _regexOffsetBufferStart = -1;
            }
        }

        BufferedReadResult TryReadBuffered(RecordSeparator separator)
        {
            var buffer = _buffer ?? throw new InvalidOperationException("Buffered input is not initialized.");
            if (_bufferStart == buffer.Length)
            {
                buffer.Clear();
                _bufferStart = 0;
                ResetSeparatorSearch();
                _regexInput = null;
                _regexOffsetBufferStart = -1;
                return _descriptorReachedEof
                    ? ParagraphEndOfFile(separator)
                    : BufferedReadResult.NeedMoreData;
            }

            var content = buffer.AsSpan()[_bufferStart..];
            return separator switch
            {
                NewlineSeparator => TryReadNewlineRecord(content),
                ParagraphSeparator => TryReadParagraph(content),
                SingleByteSeparator singleByte => TryReadSingleByte(content, singleByte),
                ByteSetSeparator byteSet => TryReadByteSet(content, byteSet),
                ByteRunSeparator byteRun => TryReadByteRun(content, byteRun),
                LiteralSeparator literal => TryReadLiteral(content, literal),
                RegexSeparator regex => TryReadRegex(regex),
                _ => throw new InvalidOperationException("Unexpected awk record separator strategy.")
            };

            BufferedReadResult TryReadNewlineRecord(ReadOnlySpan<byte> content)
            {
                var scanStart = GetRelativeScanStart(content);
                var candidate = content[scanStart..];
                var relativeIndex = IndexOfByte(candidate, (byte)'\n', _scanCheckpoint);
                RecordSeparatorScan(relativeIndex < 0 ? candidate.Length : relativeIndex + 1);
                var separatorIndex = relativeIndex < 0 ? -1 : scanStart + relativeIndex;
                if (separatorIndex < 0)
                {
                    MarkScannedThroughEnd(0);
                    return FinalRecordOrNeedMore(content, AwkRecordFieldSplittingMode.Ordinary);
                }

                var recordLength = separatorIndex;
                if (recordLength > 0 && content[recordLength - 1] == (byte)'\r')
                    recordLength--;

                return ConsumeRecord(
                    content[..recordLength],
                    separatorIndex + 1,
                    AwkRecordTerminator.NewLine,
                    AwkRecordFieldSplittingMode.Ordinary);
            }

            BufferedReadResult TryReadSingleByte(
                ReadOnlySpan<byte> content,
                SingleByteSeparator separator)
            {
                var scanStart = GetRelativeScanStart(content);
                var candidate = content[scanStart..];
                var relativeIndex = IndexOfByte(candidate, separator.Value, _scanCheckpoint);
                RecordSeparatorScan(relativeIndex < 0 ? candidate.Length : relativeIndex + 1);
                if (relativeIndex < 0)
                {
                    MarkScannedThroughEnd(0);
                    return FinalRecordOrNeedMore(content, AwkRecordFieldSplittingMode.Ordinary);
                }

                var separatorIndex = scanStart + relativeIndex;
                return ConsumeRecord(
                        content[..separatorIndex],
                        separatorIndex + 1,
                        separator.Terminator,
                        AwkRecordFieldSplittingMode.Ordinary);
            }

            BufferedReadResult TryReadByteSet(
                ReadOnlySpan<byte> content,
                ByteSetSeparator separator)
            {
                var scanStart = GetRelativeScanStart(content);
                for (var index = scanStart; index < content.Length; index++)
                {
                    if (((index - scanStart) & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                        _scanCheckpoint();
                    var value = content[index];
                    if (!separator.Contains(value))
                        continue;

                    RecordSeparatorScan(index - scanStart + 1);
                    return ConsumeRecord(
                        content[..index],
                        index + 1,
                        separator.Terminator(value),
                        AwkRecordFieldSplittingMode.Ordinary);
                }

                RecordSeparatorScan(content.Length - scanStart);
                MarkScannedThroughEnd(0);
                return FinalRecordOrNeedMore(content, AwkRecordFieldSplittingMode.Ordinary);
            }

            BufferedReadResult TryReadByteRun(
                ReadOnlySpan<byte> content,
                ByteRunSeparator separator)
            {
                if (_pendingSeparatorStart >= 0)
                    return ContinueByteRun(content, separator);

                var scanStart = GetRelativeScanStart(content);
                var separatorIndex = -1;
                for (var index = scanStart; index < content.Length; index++)
                {
                    if (((index - scanStart) & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                        _scanCheckpoint();
                    if (!separator.Contains(content[index]))
                        continue;

                    separatorIndex = index;
                    break;
                }

                if (separatorIndex < 0)
                {
                    RecordSeparatorScan(content.Length - scanStart);
                    MarkScannedThroughEnd(0);
                    return FinalRecordOrNeedMore(content, AwkRecordFieldSplittingMode.Ordinary);
                }

                var separatorEnd = separatorIndex;
                long nextRunCheckpoint = separatorEnd;
                while (separatorEnd < content.Length && separator.Contains(content[separatorEnd]))
                {
                    if (separatorEnd >= nextRunCheckpoint)
                    {
                        _scanCheckpoint();
                        nextRunCheckpoint = (long)separatorEnd + AwkExecutionGovernor.WorkChunkBytes;
                    }
                    separatorEnd++;
                }

                RecordSeparatorScan(separatorEnd - scanStart);
                if (separatorEnd == content.Length && !_descriptorReachedEof)
                {
                    _pendingSeparatorStart = _bufferStart + separatorIndex;
                    _pendingSeparatorEnd = _bufferStart + separatorEnd;
                    _scanOffset = _pendingSeparatorEnd;
                    return BufferedReadResult.NeedMoreData;
                }

                return ConsumeRecord(
                    content[..separatorIndex],
                    separatorEnd,
                    AwkRecordTerminator.FromText(CreateText(content[separatorIndex..separatorEnd])),
                    AwkRecordFieldSplittingMode.Ordinary);

                BufferedReadResult ContinueByteRun(
                    ReadOnlySpan<byte> content,
                    ByteRunSeparator separator)
                {
                    var separatorStart = _pendingSeparatorStart - _bufferStart;
                    var separatorEnd = _pendingSeparatorEnd - _bufferStart;
                    long nextRunCheckpoint = separatorEnd;
                    while (separatorEnd < content.Length && separator.Contains(content[separatorEnd]))
                    {
                        if (separatorEnd >= nextRunCheckpoint)
                        {
                            _scanCheckpoint();
                            nextRunCheckpoint = (long)separatorEnd + AwkExecutionGovernor.WorkChunkBytes;
                        }
                        separatorEnd++;
                    }

                    RecordSeparatorScan(separatorEnd - (_pendingSeparatorEnd - _bufferStart));
                    _pendingSeparatorEnd = _bufferStart + separatorEnd;
                    _scanOffset = _pendingSeparatorEnd;
                    if (separatorEnd == content.Length && !_descriptorReachedEof)
                        return BufferedReadResult.NeedMoreData;

                    return ConsumeRecord(
                        content[..separatorStart],
                        separatorEnd,
                        AwkRecordTerminator.FromText(CreateText(content[separatorStart..separatorEnd])),
                        AwkRecordFieldSplittingMode.Ordinary);
                }
            }

            BufferedReadResult TryReadLiteral(
                ReadOnlySpan<byte> content,
                LiteralSeparator separator)
            {
                var scanStart = GetRelativeScanStart(content);
                var candidate = content[scanStart..];
                var separatorBytes = separator.Value.AsSpan(_state.CancellationCheckpoint);
                var relativeIndex = IndexOfLiteral(candidate, separatorBytes, _scanCheckpoint);
                RecordSeparatorScan(relativeIndex < 0
                    ? candidate.Length
                    : relativeIndex + separatorBytes.Length);
                if (relativeIndex < 0)
                {
                    MarkScannedThroughEnd(separatorBytes.Length - 1);
                    return FinalRecordOrNeedMore(content, AwkRecordFieldSplittingMode.Ordinary);
                }

                var separatorIndex = scanStart + relativeIndex;
                return ConsumeRecord(
                        content[..separatorIndex],
                        separatorIndex + separator.Value.ByteLength,
                        separator.Terminator,
                        AwkRecordFieldSplittingMode.Ordinary);
            }

            BufferedReadResult TryReadRegex(RegexSeparator separator)
            {
                var buffer = _buffer ?? throw new InvalidOperationException("Buffered input is not initialized.");
                var input = GetRegexInput(buffer);
                var recordStart = GetRegexSearchOffset(buffer);
                var searchOffset = recordStart;
                AwkRegexMatchSpan match;
                while (true)
                {
                    _state.Resources.RecordRegexRecordSeparatorBytesScanned(
                        input.ByteLength - searchOffset.ByteIndex);
                    match = _state.MatchRegexSpan(separator.Value, input, searchOffset);
                    if (!match.Success || match.ByteLength > 0)
                        break;
                    if (match.ByteIndex >= input.ByteLength)
                    {
                        match = AwkRegexMatchSpan.NoMatch;
                        break;
                    }

                    searchOffset = AwkRegexSearchOffset.AfterMatch(input, match, _state.CancellationCheckpoint);
                }

                if (!match.Success)
                {
                    var remainingLength = buffer.Length - _bufferStart;
                    return _descriptorReachedEof
                        ? ConsumeExistingText(
                            input.CopyByteSlice(
                                recordStart.ByteIndex,
                                remainingLength,
                                _state.Counters,
                                _state.MemoryGovernor,
                                _state.CancellationCheckpoint),
                            remainingLength,
                            AwkRecordTerminator.Empty)
                        : BufferedReadResult.NeedMoreData;
                }

                if (!_descriptorReachedEof && match.ByteIndex + match.ByteLength == input.ByteLength)
                    return BufferedReadResult.NeedMoreData;

                var recordLength = match.ByteIndex - recordStart.ByteIndex;
                var record = input.CopyByteSlice(
                    recordStart.ByteIndex,
                    recordLength,
                    _state.Counters,
                    _state.MemoryGovernor,
                    _state.CancellationCheckpoint);
                var terminator = AwkRecordTerminator.FromText(input.CopyByteSlice(
                    match.ByteIndex,
                    match.ByteLength,
                    _state.Counters,
                    _state.MemoryGovernor,
                    _state.CancellationCheckpoint));
                var next = AwkRegexSearchOffset.AfterMatch(input, match, _state.CancellationCheckpoint);
                var consumedLength = next.ByteIndex - recordStart.ByteIndex;
                _bufferStart += consumedLength;
                _hasConsumedDescriptorBytes |= consumedLength > 0;
                _regexOffsetBufferStart = _bufferStart;
                _regexOffsetUtf16Index = next.Utf16Index;
                return BufferedReadResult.FromRecord(AwkInputRecordResult.Record(
                    record,
                    terminator,
                    AwkRecordFieldSplittingMode.Ordinary));

                BufferedReadResult ConsumeExistingText(
                    AwkText record,
                    int consumedLength,
                    AwkRecordTerminator terminator)
                {
                    _bufferStart += consumedLength;
                    _hasConsumedDescriptorBytes |= consumedLength > 0;
                    _regexOffsetBufferStart = -1;
                    ResetSeparatorSearch();
                    return BufferedReadResult.FromRecord(AwkInputRecordResult.Record(
                        record,
                        terminator,
                        AwkRecordFieldSplittingMode.Ordinary));
                }

                AwkText GetRegexInput(AwkUtf8Builder buffer)
                {
                    if (_regexInput is not null)
                        return _regexInput;

                    _regexInputPrefixLength = _hasConsumedDescriptorBytes ? 1 : 0;
                    _regexInput = buffer.BorrowAsAwkText(
                        _regexInputPrefixLength != 0
                            ? AwkUtf8BorrowRange.ReservedPrefixAndContent
                            : AwkUtf8BorrowRange.Content);
                    return _regexInput;
                }

                AwkRegexSearchOffset GetRegexSearchOffset(AwkUtf8Builder buffer)
                {
                    if (_regexOffsetBufferStart == _bufferStart)
                    {
                        return new AwkRegexSearchOffset(
                            _regexInputPrefixLength + _bufferStart,
                            _regexOffsetUtf16Index);
                    }

                    var utf16Index = _regexInputPrefixLength +
                        GetUtf16Length(buffer.AsSpan()[.._bufferStart], _scanCheckpoint);
                    _scanCheckpoint();
                    _regexOffsetUtf16Index = utf16Index;
                    _regexOffsetBufferStart = _bufferStart;
                    return new AwkRegexSearchOffset(
                        _regexInputPrefixLength + _bufferStart,
                        _regexOffsetUtf16Index);
                }
            }

            BufferedReadResult TryReadParagraph(ReadOnlySpan<byte> content)
            {
                var previous = new ParagraphReadPosition(
                    _bufferStart, _scanOffset, _pendingSeparatorStart, _pendingSeparatorEnd,
                    _hasConsumedDescriptorBytes, _discardedParagraphLeader,
                    _paragraphLeaderScanLength, _regexOffsetBufferStart);
                try
                {
                    return ReadParagraph(content);
                }
                catch
                {
                    // Buffered extraction does not mutate bytes. Restore its positions
                    // so a failed copy/admission cannot consume paragraph leaders.
                    _bufferStart = previous.BufferStart;
                    _scanOffset = previous.ScanOffset;
                    _pendingSeparatorStart = previous.PendingSeparatorStart;
                    _pendingSeparatorEnd = previous.PendingSeparatorEnd;
                    _hasConsumedDescriptorBytes = previous.HasConsumedDescriptorBytes;
                    _discardedParagraphLeader = previous.DiscardedParagraphLeader;
                    _paragraphLeaderScanLength = previous.ParagraphLeaderScanLength;
                    _regexOffsetBufferStart = previous.RegexOffsetBufferStart;
                    throw;
                }
            }

            BufferedReadResult ReadParagraph(ReadOnlySpan<byte> content)
            {
                if (_pendingSeparatorStart >= 0)
                    return ContinueParagraphSeparator(content);

                var previousLeaderLength = _paragraphLeaderScanLength;
                var recordStart = SkipParagraphNewlines(content, previousLeaderLength);
                _discardedParagraphLeader |= recordStart > 0;
                if (_paragraphLeaderPolicy == AwkParagraphLeaderPolicy.Retain)
                {
                    RecordSeparatorScan(recordStart - previousLeaderLength);
                    _hasConsumedDescriptorBytes |= recordStart > 0;
                    // GNU keeps all-blank pipe input buffered at paragraph EOF. A later
                    // RS change can read it; repeated paragraph reads only clear RT.
                    // Cache the scanned prefix so batch growth and repeated EOF are linear.
                    if (recordStart == content.Length ||
                        !_descriptorReachedEof && recordStart == content.Length - 1 &&
                        content[recordStart] == (byte)'\r')
                    {
                        _paragraphLeaderScanLength = recordStart;
                        return _descriptorReachedEof
                            ? ParagraphEndOfFile(ParagraphSeparator.Instance)
                            : BufferedReadResult.NeedMoreData;
                    }
                }

                content = content[recordStart..];
                if (content.IsEmpty)
                {
                    // Discarding an all-blank batch remains streaming progress. Once
                    // a record body exists, keep its leaders until that record commits.
                    _bufferStart += recordStart;
                    _regexOffsetBufferStart = -1;
                    if (recordStart > 0)
                        ResetSeparatorSearch();
                    return _descriptorReachedEof
                        ? ParagraphEndOfFile(ParagraphSeparator.Instance)
                        : BufferedReadResult.NeedMoreData;
                }

                _paragraphLeaderScanLength = recordStart;
                _discardedParagraphLeader = false;

                var scanStart = Math.Clamp(_scanOffset - (_bufferStart + recordStart), 0, content.Length);
                if (!TryFindParagraphSeparator(
                        content,
                        scanStart,
                        out var separatorIndex,
                        out var separatorLength))
                {
                    RecordSeparatorScan(content.Length - scanStart);
                    if (!_descriptorReachedEof)
                    {
                        MarkScannedThroughEnd(3);
                        return BufferedReadResult.NeedMoreData;
                    }

                    var recordLength = TrimTrailingRecordNewlines(content, _scanCheckpoint);
                    var trailingNewlines = content[recordLength..];
                    // An LF uses the shared terminator. Preflight both owned byte copies
                    // for CRLF before allocating either record or terminator text.
                    _state.MemoryGovernor.PreflightAllocation(
                        recordLength + (trailingNewlines.Length == 1 ? 0 : trailingNewlines.Length));
                    var finalTerminator = trailingNewlines.Length == 1
                        ? AwkRecordTerminator.NewLine
                        : AwkRecordTerminator.FromText(CreateText(trailingNewlines));
                    return ConsumeRecord(
                        content[..recordLength],
                        recordStart + content.Length,
                        finalTerminator,
                        AwkRecordFieldSplittingMode.Paragraph);
                }

                RecordSeparatorScan(separatorIndex + separatorLength - scanStart);
                if (!_descriptorReachedEof && separatorIndex + separatorLength == content.Length)
                {
                    _pendingSeparatorStart = _bufferStart + recordStart + separatorIndex;
                    _pendingSeparatorEnd = _bufferStart + recordStart + separatorIndex + separatorLength;
                    _scanOffset = _pendingSeparatorEnd;
                    return BufferedReadResult.NeedMoreData;
                }

                var terminator = AwkRecordTerminator.FromText(CreateText(
                    content.Slice(separatorIndex, separatorLength)));
                return ConsumeRecord(
                    content[..TrimTrailingRecordNewlines(content[..separatorIndex], _scanCheckpoint)],
                    recordStart + separatorIndex + separatorLength,
                    terminator,
                    AwkRecordFieldSplittingMode.Paragraph);

                BufferedReadResult ContinueParagraphSeparator(ReadOnlySpan<byte> content)
                {
                    var separatorStart = _pendingSeparatorStart - _bufferStart;
                    var separatorEnd = _pendingSeparatorEnd - _bufferStart;
                    var previousEnd = separatorEnd;
                    long nextNewlineCheckpoint = separatorEnd;
                    while (TryReadNewline(content, separatorEnd, out var newlineLength))
                    {
                        if (separatorEnd >= nextNewlineCheckpoint)
                        {
                            _scanCheckpoint();
                            nextNewlineCheckpoint = (long)separatorEnd + AwkExecutionGovernor.WorkChunkBytes;
                        }
                        separatorEnd += newlineLength;
                    }

                    RecordSeparatorScan(separatorEnd - previousEnd);
                    _pendingSeparatorEnd = _bufferStart + separatorEnd;
                    _scanOffset = _pendingSeparatorEnd;
                    if (!_descriptorReachedEof &&
                        (separatorEnd == content.Length ||
                            separatorEnd == content.Length - 1 && content[separatorEnd] == (byte)'\r'))
                    {
                        return BufferedReadResult.NeedMoreData;
                    }

                    var terminator = AwkRecordTerminator.FromText(CreateText(
                        content[separatorStart..separatorEnd]));
                    var recordBytes = content[_paragraphLeaderScanLength..separatorStart];
                    return ConsumeRecord(
                        recordBytes[..TrimTrailingRecordNewlines(recordBytes, _scanCheckpoint)],
                        separatorEnd,
                        terminator,
                        AwkRecordFieldSplittingMode.Paragraph);
                }

                int SkipParagraphNewlines(ReadOnlySpan<byte> content, int position)
                {
                    long nextNewlineCheckpoint = position;
                    while (TryReadNewline(content, position, out var newlineLength))
                    {
                        if (position >= nextNewlineCheckpoint)
                        {
                            _scanCheckpoint();
                            nextNewlineCheckpoint = (long)position + AwkExecutionGovernor.WorkChunkBytes;
                        }
                        position += newlineLength;
                    }

                    return position;
                }

                bool TryFindParagraphSeparator(
                    ReadOnlySpan<byte> content,
                    int start,
                    out int separatorIndex,
                    out int separatorLength)
                {
                    for (var index = start; index < content.Length; index++)
                    {
                        if (((index - start) & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                            _scanCheckpoint();
                        if (!TryReadNewline(content, index, out var firstNewlineLength))
                            continue;

                        var position = index + firstNewlineLength;
                        if (!TryReadNewline(content, position, out var secondNewlineLength))
                            continue;

                        position += secondNewlineLength;
                        long nextNewlineCheckpoint = position;
                        while (TryReadNewline(content, position, out var trailingNewlineLength))
                        {
                            if (position >= nextNewlineCheckpoint)
                            {
                                _scanCheckpoint();
                                nextNewlineCheckpoint = (long)position + AwkExecutionGovernor.WorkChunkBytes;
                            }
                            position += trailingNewlineLength;
                        }

                        separatorIndex = index;
                        separatorLength = position - index;
                        return true;
                    }

                    separatorIndex = -1;
                    separatorLength = 0;
                    return false;
                }
            }
        }

        RecordSeparator GetCurrentSeparator()
        {
            var version = _state.RecordSeparatorVersion;
            if (_separatorCache.IsCurrent(version))
                return _separatorCache.Value;

            var separator = CreateSeparator(_state.ResolveRecordSeparator());
            _scanCheckpoint();
            _separatorCache = new SeparatorCache(separator, version);
            _discardedParagraphLeader = false;
            ResetSeparatorSearch();
            return separator;

            RecordSeparator CreateSeparator(AwkText separatorText)
            {
                var separatorBytes = separatorText.AsSpan(_state.CancellationCheckpoint);
                if (separatorBytes.Length == 0)
                    return ParagraphSeparator.Instance;

                if (_state.CompatibilityMode == AwkCompatibilityMode.Posix)
                {
                    var first = separatorBytes[0];
                    if (first > 0x7F)
                    {
                        // GNU's multibyte POSIX scanner selects the first byte and consumes
                        // the complete input character with that prefix, including a different rune.
                        var (minimum, maximum) = first switch
                        {
                            <= 0xDF => ((first & 0x1F) << 6, ((first & 0x1F) << 6) + 0x3F),
                            <= 0xEF => (Math.Max(0x800, (first & 0x0F) << 12),
                                first == 0xED ? 0xD7FF : ((first & 0x0F) << 12) + 0xFFF),
                            _ => (Math.Max(0x10000, (first & 0x07) << 18),
                                Math.Min(0x10FFFF, ((first & 0x07) << 18) + 0x3FFFF))
                        };
                        var pattern = "[" + char.ConvertFromUtf32(minimum) + "-" + char.ConvertFromUtf32(maximum) + "]";
                        return new RegexSeparator(_state.CreateRegex(new AwkRegexPattern(pattern, warningSite: this)));
                    }
                    return first == (byte)'\n'
                        ? NewlineSeparator.Instance
                        : new SingleByteSeparator(first,
                            AwkRecordTerminator.FromAsciiByte(first));
                }

                if (separatorBytes is [(byte)'\n'])
                    return NewlineSeparator.Instance;

                if (separatorBytes.Length == 1)
                {
                    var value = separatorBytes[0];
                    return new SingleByteSeparator(
                        value,
                        AwkRecordTerminator.FromAsciiByte(value));
                }

                if (TryCreateByteSet(separatorBytes, out var byteSet))
                    return byteSet;

                if (TryCreateByteRun(separatorBytes, _scanCheckpoint, out var byteRun))
                    return byteRun;

                if (_state.ResolveRegexCaseSensitivity() is AwkRegexCaseSensitivity.CaseSensitive &&
                    (separatorText.GetRuneLength(_state.CancellationCheckpoint) == 1 || IsLiteralPattern(separatorBytes, _scanCheckpoint)))
                    return new LiteralSeparator(separatorText, AwkRecordTerminator.FromText(separatorText));

                return new RegexSeparator(_state.CreateRegex(
                    new AwkRegexPattern(separatorText.AsString(_state.Counters, _state.CancellationCheckpoint), warningSite: _state),
                    _state.ResolveRegexCaseSensitivity()));

                bool TryCreateByteSet(ReadOnlySpan<byte> separatorText, out ByteSetSeparator separator)
                {
                    if (!TryCreateByteMembership(separatorText, 0, separatorText.Length, _scanCheckpoint, out var membership))
                    {
                        separator = ByteSetSeparator.Empty;
                        return false;
                    }

                    separator = new ByteSetSeparator(membership);
                    return true;
                }


            }
        }

        BufferedReadResult ParagraphEndOfFile(RecordSeparator separator) =>
            _discardedParagraphLeader && separator is ParagraphSeparator
                ? BufferedReadResult.EndOfFileClearingTerminator
                : BufferedReadResult.EndOfFile;
    }

    public void Dispose()
    {
        _buffer?.Dispose();
        _buffer = null;
        _lineReader = null;
        _regexInput = null;
        _pendingBatch = default;
        _hasPendingBatch = false;
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

    private int GetRelativeScanStart(ReadOnlySpan<byte> content) =>
        Math.Clamp(_scanOffset - _bufferStart, 0, content.Length);

    private void MarkScannedThroughEnd(int preservedTailLength)
    {
        var buffer = _buffer ?? throw new InvalidOperationException("Buffered input is not initialized.");
        _scanOffset = Math.Max(_bufferStart, buffer.Length - preservedTailLength);
    }

    private void RecordSeparatorScan(int byteCount)
    {
        _state.Resources.RecordByteWork(byteCount);
        _state.Counters.RecordRecordSeparatorBytesScanned(byteCount);
    }

    private void ResetSeparatorSearch()
    {
        _scanOffset = _bufferStart;
        _pendingSeparatorStart = -1;
        _pendingSeparatorEnd = -1;
        _paragraphLeaderScanLength = 0;
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
        ResetSeparatorSearch();
        return BufferedReadResult.FromRecord(AwkInputRecordResult.Record(
            record,
            terminator,
            fieldSplittingMode));
    }

    private AwkText CreateText(ReadOnlySpan<byte> content) =>
        content.IsEmpty
            ? AwkText.Empty
            : AwkText.CopyHostBytes(content, _state.Counters, _state.MemoryGovernor, _state.CancellationCheckpoint);

    internal static void ValidateSeparator(AwkRuntimeState state, AwkText value)
    {
        if (state.CompatibilityMode == AwkCompatibilityMode.Posix || value.ByteLength <= 1)
            return;
        var bytes = value.AsSpan(state.CancellationCheckpoint);
        // Known byte-native forms have no escape diagnostics or case-dependent grammar.
        if (value.GetRuneLength(state.CancellationCheckpoint) == 1 || IsLiteralPattern(bytes, state.CancellationCheckpoint) ||
            TryCreateByteMembership(bytes, 0, bytes.Length, state.CancellationCheckpoint, out _) ||
            TryCreateByteRun(bytes, state.CancellationCheckpoint, out _))
        {
            state.CancellationCheckpoint();
            return;
        }
        var pattern = new AwkRegexPattern(value.AsString(state.Counters, state.CancellationCheckpoint), warningSite: state);
        _ = state.CreateRegex(pattern, AwkRegexCaseSensitivity.CaseSensitive);
        _ = state.CreateRegex(pattern, AwkRegexCaseSensitivity.IgnoreCase);
    }

    private static bool TryCreateByteRun(ReadOnlySpan<byte> separatorText, Action checkpoint, out ByteRunSeparator separator)
    {
        var start = 0;
        var length = separatorText.Length;
        var hasOneOrMoreQuantifier = length > 0 && separatorText[length - 1] == '+';
        if (hasOneOrMoreQuantifier)
            length--;

        if (length >= 2 && separatorText[start] == '(' && separatorText[start + length - 1] == ')')
        {
            start++;
            length -= 2;
            if (length >= 2 && separatorText[start] == '?' && separatorText[start + 1] == ':')
            {
                start += 2;
                length -= 2;
            }

            if (length > 0 && separatorText[start + length - 1] == '+')
            {
                hasOneOrMoreQuantifier = true;
                length--;
            }
        }

        if (hasOneOrMoreQuantifier &&
            TryCreateByteMembership(separatorText, start, length, checkpoint, out var membership))
        {
            separator = new ByteRunSeparator(membership);
            return true;
        }

        separator = ByteRunSeparator.Empty;
        return false;
    }

    private static bool IsLiteralPattern(ReadOnlySpan<byte> pattern, Action checkpoint)
    {
        for (var index = 0; index < pattern.Length; index++)
        {
            if ((index & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                checkpoint();
            if (IsRegexMetacharacter(pattern[index]))
                return false;
        }

        return true;
    }

    private static bool IsRegexMetacharacter(byte value) =>
        value is (byte)'\\' or (byte)'.' or (byte)'^' or (byte)'$' or (byte)'[' or (byte)']' or
            (byte)'(' or (byte)')' or (byte)'|' or (byte)'*' or (byte)'+' or (byte)'?' or (byte)'{' or (byte)'}';

    private static bool TryCreateByteMembership(
        ReadOnlySpan<byte> separatorText,
        int start,
        int length,
        Action checkpoint,
        out ByteMembership membership)
    {
        static bool TryAddTerm(
            ReadOnlySpan<byte> text,
            int termStart,
            int termLength,
            Action checkpoint,
            ref ByteMembership values)
        {
            static bool IsAsciiLetter(byte value) =>
                value is >= (byte)'A' and <= (byte)'Z' or >= (byte)'a' and <= (byte)'z';

            if (termLength == 1)
            {
                var value = text[termStart];
                if (IsAsciiLetter(value) || IsRegexMetacharacter(value))
                    return false;

                values = values.Add(value);
                return true;
            }

            if (termLength < 3 || text[termStart] != '[' || text[termStart + termLength - 1] != ']')
                return false;

            var termEnd = termStart + termLength - 1;
            for (var index = termStart + 1; index < termEnd; index++)
            {
                if (((index - termStart - 1) & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                    checkpoint();
                var ch = text[index];
                if (ch is (byte)'\\' or (byte)'-' or (byte)'^' or (byte)'[' or (byte)']' || IsAsciiLetter(ch))
                    return false;

                values = values.Add(ch);
            }

            return values.Count > 0;

        }

        membership = default;
        var end = start + length;
        var termStart = start;
        long nextTermCheckpoint = start;
        while (termStart < end)
        {
            if (termStart >= nextTermCheckpoint)
            {
                checkpoint();
                nextTermCheckpoint = (long)termStart + AwkExecutionGovernor.WorkChunkBytes;
            }
            int termLength;
            if (separatorText[termStart] == '[')
            {
                var relativeClosingBracket = IndexOfByte(separatorText[(termStart + 1)..end], (byte)']', checkpoint);
                if (relativeClosingBracket < 0)
                    return false;

                var closingBracket = termStart + 1 + relativeClosingBracket;
                termLength = closingBracket - termStart + 1;
            }
            else
            {
                termLength = 1;
            }

            if (!TryAddTerm(separatorText, termStart, termLength, checkpoint, ref membership))
            {
                membership = default;
                return false;
            }

            termStart += termLength;
            if (termStart == end)
                return true;

            if (separatorText[termStart] != '|')
            {
                membership = default;
                return false;
            }

            termStart++;
        }

        membership = default;
        return false;
    }

    private readonly record struct ByteMembership(ulong Low, ulong High, int Count)
    {
        public static ByteMembership Single(byte value) => default(ByteMembership).Add(value);

        public ByteMembership Add(byte value)
        {
            if (value > 0x7F || Contains(value))
                return this;

            return value < 64
                ? new ByteMembership(Low | 1UL << value, High, Count + 1)
                : new ByteMembership(Low, High | 1UL << (value - 64), Count + 1);
        }

        public bool Contains(byte value) =>
            value < 64
                ? (Low & 1UL << value) != 0
                : value <= 0x7F && (High & 1UL << (value - 64)) != 0;


    }

    private static int IndexOfByte(ReadOnlySpan<byte> content, byte value, Action checkpoint)
    {
        var offset = 0;
        while (offset < content.Length)
        {
            checkpoint();
            var length = Math.Min(AwkExecutionGovernor.WorkChunkBytes, content.Length - offset);
            var found = content.Slice(offset, length).IndexOf(value);
            if (found >= 0)
                return offset + found;
            offset += length;
        }
        return -1;
    }

    private static int IndexOfLiteral(ReadOnlySpan<byte> content, ReadOnlySpan<byte> value, Action checkpoint)
    {
        if (value.Length > AwkExecutionGovernor.WorkChunkBytes)
        {
            // Keep the framework's long-needle algorithm rather than repeatedly
            // searching a large overlap. This remains an indivisible boundary.
            checkpoint();
            var found = content.IndexOf(value);
            checkpoint();
            return found;
        }
        var offset = 0;
        var candidates = content.Length - value.Length + 1;
        while (offset < candidates)
        {
            checkpoint();
            var candidateCount = Math.Min(AwkExecutionGovernor.WorkChunkBytes, candidates - offset);
            // Include the overlap so a separator may start at the last candidate.
            // Large needles remain a framework boundary, not a new local matcher.
            var found = content.Slice(offset, candidateCount + value.Length - 1).IndexOf(value);
            if (found >= 0)
                return offset + found;
            offset += candidateCount;
        }
        return -1;
    }

    private static int GetUtf16Length(ReadOnlySpan<byte> bytes, Action checkpoint)
    {
        var length = 0;
        var offset = 0;
        while (offset < bytes.Length)
        {
            checkpoint();
            var end = offset + Math.Min(AwkExecutionGovernor.WorkChunkBytes, bytes.Length - offset);
            // Input is strict UTF-8 and the record offset is a scalar boundary.
            while (end < bytes.Length && (bytes[end] & 0xC0) == 0x80)
                end--;
            length += Encoding.UTF8.GetCharCount(bytes[offset..end]);
            offset = end;
        }
        return length;
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

    private static int TrimTrailingRecordNewlines(ReadOnlySpan<byte> content, Action checkpoint)
    {
        var end = content.Length;
        long nextCheckpoint = end;
        while (end > 0 && content[end - 1] == (byte)'\n')
        {
            if (end <= nextCheckpoint)
            {
                checkpoint();
                nextCheckpoint = (long)end - AwkExecutionGovernor.WorkChunkBytes;
            }
            end--;
            if (end > 0 && content[end - 1] == (byte)'\r')
                end--;
        }

        return end;
    }

    private readonly record struct ParagraphReadPosition(
        int BufferStart,
        int ScanOffset,
        int PendingSeparatorStart,
        int PendingSeparatorEnd,
        bool HasConsumedDescriptorBytes,
        bool DiscardedParagraphLeader,
        int ParagraphLeaderScanLength,
        int RegexOffsetBufferStart);

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

        public static BufferedReadResult EndOfFileClearingTerminator { get; } =
            new(BufferedReadStatus.EndOfFile, AwkInputRecordResult.EndOfFileClearingTerminator);

        public static BufferedReadResult FromRecord(AwkInputRecordResult record) =>
            new(BufferedReadStatus.Record, record);
    }

    private readonly record struct SeparatorCache
    {
        private readonly SeparatorCacheKind _kind;
        private readonly RecordSeparator? _value;

        public SeparatorCache(RecordSeparator value, long version)
        {
            _kind = SeparatorCacheKind.Value;
            _value = value;
            Version = version;
        }

        public long Version { get; }

        public RecordSeparator Value =>
            _kind == SeparatorCacheKind.Value && _value is { } value
                ? value
                : throw new InvalidOperationException("The record-separator cache is empty.");

        public bool IsCurrent(long version) =>
            _kind == SeparatorCacheKind.Value && Version == version;
    }

    private enum SeparatorCacheKind
    {
        Empty,
        Value
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

    private sealed record ByteSetSeparator(ByteMembership Values) : RecordSeparator
    {
        public static ByteSetSeparator Empty { get; } = new(default(ByteMembership));

        public bool Contains(byte value) => Values.Contains(value);

        public AwkRecordTerminator Terminator(byte value) => AwkRecordTerminator.FromAsciiByte(value);
    }

    private sealed record ByteRunSeparator(ByteMembership Values) : RecordSeparator
    {
        public static ByteRunSeparator Empty { get; } = new(default(ByteMembership));

        public bool Contains(byte value) => Values.Contains(value);
    }

    private sealed record LiteralSeparator(
        AwkText Value,
        AwkRecordTerminator Terminator) : RecordSeparator;

    private sealed record RegexSeparator(AwkRegex Value) : RecordSeparator;
}

internal readonly record struct AwkInputRecordResult
{
    private readonly AwkInputRecordResultKind _kind;
    private readonly AwkText? _recordText;

    private AwkInputRecordResult(
        AwkInputRecordResultKind kind,
        AwkText recordText,
        AwkRecordTerminator terminator,
        AwkRecordFieldSplittingMode fieldSplittingMode,
        bool clearsRecordTerminator = false)
    {
        _kind = kind;
        _recordText = recordText;
        Terminator = terminator;
        FieldSplittingMode = fieldSplittingMode;
        ClearsRecordTerminator = clearsRecordTerminator;
    }

    public bool HasRecord => _kind == AwkInputRecordResultKind.Record;

    public bool ClearsRecordTerminator { get; }

    public AwkText RecordText => _recordText ?? AwkText.Empty;

    public AwkRecordTerminator Terminator { get; }

    public AwkRecordFieldSplittingMode FieldSplittingMode { get; }

    public static AwkInputRecordResult EndOfFile { get; } =
        new(
            AwkInputRecordResultKind.EndOfFile,
            AwkText.Empty,
            AwkRecordTerminator.Empty,
            AwkRecordFieldSplittingMode.Ordinary);

    public static AwkInputRecordResult EndOfFileClearingTerminator { get; } =
        new(AwkInputRecordResultKind.EndOfFile, AwkText.Empty, AwkRecordTerminator.Empty,
            AwkRecordFieldSplittingMode.Ordinary, clearsRecordTerminator: true);

    public static AwkInputRecordResult Record(
        AwkText record,
        AwkRecordTerminator terminator,
        AwkRecordFieldSplittingMode fieldSplittingMode) =>
        new(AwkInputRecordResultKind.Record, record, terminator, fieldSplittingMode);

    private enum AwkInputRecordResultKind
    {
        EndOfFile,
        Record
    }
}
