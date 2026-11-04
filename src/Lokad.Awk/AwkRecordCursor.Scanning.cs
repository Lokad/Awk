using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal sealed partial class AwkRecordCursor
{
    private BufferedReadStatus TryReadBuffered(RecordSeparator separator, AwkRecordStorage? recordStorage)
    {
        var buffer = _buffer ?? throw new InvalidOperationException("Buffered input is not initialized.");
        if (_bufferStart == buffer.Length)
        {
            buffer.Clear();
            _validatedRegexBytes = 0;
            _bufferStart = 0;
            ResetSeparatorSearch();
            ClearRegexInput();
            return _descriptorReachedEof
                ? ParagraphEndOfFile(separator)
                : BufferedReadStatus.NeedMoreData;
        }

        var content = buffer.AsSpan()[_bufferStart..];
        return separator switch
        {
            CsvSeparator => TryReadCsvRecord(content),
            NewlineSeparator => TryReadNewlineRecord(content, recordStorage),
            ParagraphSeparator => TryReadParagraph(content),
            SingleByteSeparator singleByte => TryReadSingleByte(content, singleByte),
            ByteSetSeparator byteSet => TryReadByteSet(content, byteSet),
            ByteRunSeparator byteRun => TryReadByteRun(content, byteRun),
            LiteralSeparator literal => TryReadLiteral(content, literal),
            RegexSeparator regex => TryReadRegex(regex),
            _ => throw new InvalidOperationException("Unexpected awk record separator strategy.")
        };

        BufferedReadStatus TryReadCsvRecord(ReadOnlySpan<byte> content)
        {
            var start = GetRelativeScanStart(content);
            long nextCheckpoint = start;
            for (var i = start; i < content.Length; i++)
            {
                if (i >= nextCheckpoint)
                {
                    _scanCheckpoint();
                    nextCheckpoint = (long)i + AwkRuntimeLimits.CancellationCheckBytes;
                }
                if (content[i] == (byte)'"') _csvInQuote = !_csvInQuote;
                if (content[i] == (byte)'\n' && !_csvInQuote)
                {
                    RecordSeparatorScan(i - start + 1);
                    return ConsumeRecord(content[..i], i + 1, AwkRecordTerminator.NewLine,
                        AwkRecordFieldSplittingMode.Ordinary);
                }
            }
            RecordSeparatorScan(content.Length - start);
            MarkScannedThroughEnd(0);
            return FinalRecordOrNeedMore(content, AwkRecordFieldSplittingMode.Ordinary);
        }

        BufferedReadStatus TryReadNewlineRecord(ReadOnlySpan<byte> content, AwkRecordStorage? storage)
        {
            var scanStart = GetRelativeScanStart(content);
            var candidate = content[scanStart..];
            var relativeIndex = IndexOfByte(candidate, (byte)'\n', _scanCheckpoint);
            RecordSeparatorScan(relativeIndex < 0 ? candidate.Length : relativeIndex + 1);
            var separatorIndex = relativeIndex < 0 ? -1 : scanStart + relativeIndex;
            if (separatorIndex < 0)
            {
                MarkScannedThroughEnd(0);
                return FinalRecordOrNeedMore(content, AwkRecordFieldSplittingMode.Ordinary, storage);
            }

            var recordLength = separatorIndex;
            if (recordLength > 0 && content[recordLength - 1] == (byte)'\r')
                recordLength--;

            return ConsumeRecord(
                content[..recordLength],
                separatorIndex + 1,
                AwkRecordTerminator.NewLine,
                AwkRecordFieldSplittingMode.Ordinary, storage);
        }

        BufferedReadStatus TryReadSingleByte(
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

        BufferedReadStatus TryReadByteSet(
            ReadOnlySpan<byte> content,
            ByteSetSeparator separator)
        {
            var scanStart = GetRelativeScanStart(content);
            for (var index = scanStart; index < content.Length; index++)
            {
                if (((index - scanStart) & (AwkRuntimeLimits.CancellationCheckBytes - 1)) == 0)
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

        BufferedReadStatus TryReadByteRun(
            ReadOnlySpan<byte> content,
            ByteRunSeparator separator)
        {
            if (_pendingSeparatorStart >= 0)
                return ContinueByteRun(content, separator);

            var scanStart = GetRelativeScanStart(content);
            var separatorIndex = -1;
            for (var index = scanStart; index < content.Length; index++)
            {
                if (((index - scanStart) & (AwkRuntimeLimits.CancellationCheckBytes - 1)) == 0)
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
                    nextRunCheckpoint = (long)separatorEnd + AwkRuntimeLimits.CancellationCheckBytes;
                }
                separatorEnd++;
            }

            RecordSeparatorScan(separatorEnd - scanStart);
            if (separatorEnd == content.Length && !_descriptorReachedEof)
            {
                _pendingSeparatorStart = _bufferStart + separatorIndex;
                _pendingSeparatorEnd = _bufferStart + separatorEnd;
                _scanOffset = _pendingSeparatorEnd;
                return BufferedReadStatus.NeedMoreData;
            }

            return ConsumeRecord(
                content[..separatorIndex],
                separatorEnd,
                AwkRecordTerminator.FromText(CreateText(content[separatorIndex..separatorEnd])),
                AwkRecordFieldSplittingMode.Ordinary);

            BufferedReadStatus ContinueByteRun(
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
                        nextRunCheckpoint = (long)separatorEnd + AwkRuntimeLimits.CancellationCheckBytes;
                    }
                    separatorEnd++;
                }

                RecordSeparatorScan(separatorEnd - (_pendingSeparatorEnd - _bufferStart));
                _pendingSeparatorEnd = _bufferStart + separatorEnd;
                _scanOffset = _pendingSeparatorEnd;
                if (separatorEnd == content.Length && !_descriptorReachedEof)
                    return BufferedReadStatus.NeedMoreData;

                return ConsumeRecord(
                    content[..separatorStart],
                    separatorEnd,
                    AwkRecordTerminator.FromText(CreateText(content[separatorStart..separatorEnd])),
                    AwkRecordFieldSplittingMode.Ordinary);
            }
        }

        BufferedReadStatus TryReadLiteral(
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

        BufferedReadStatus TryReadRegex(RegexSeparator separator)
        {
            var buffer = _buffer ?? throw new InvalidOperationException("Buffered input is not initialized.");
            var input = GetRegexInput(buffer);
            if (input.ByteLength == _bufferStart && !_descriptorReachedEof)
                return BufferedReadStatus.NeedMoreData;
            var recordStart = _bufferStart;
            var searchOffset = recordStart;
            var regex = _hasConsumedDescriptorBytes ? separator.Continuing : separator.Value;
            using var search = _state.OpenRecordSeparatorSearch(regex, input, recordStart);
            AwkRegexMatchSpan match;
            while (true)
            {
                _state.Resources.RecordRegexRecordSeparatorBytesScanned(
                    input.ByteLength - searchOffset);
                match = search.MatchSpan(searchOffset);
                if (!match.Success || match.ByteLength > 0)
                    break;
                if (match.ByteIndex >= input.ByteLength)
                {
                    match = AwkRegexMatchSpan.NoMatch;
                    break;
                }

                searchOffset = AwkRegexMatchSpan.AfterMatch(input, match, _state.CancellationCheckpoint);
            }

            if (!match.Success)
            {
                var remainingLength = buffer.Length - _bufferStart;
                return _descriptorReachedEof
                    ? ConsumeExistingText(
                        input.CopyByteSlice(
                            recordStart,
                            remainingLength,
                            _state.Counters,
                            _state.MemoryAccounting,
                            _state.CancellationCheckpoint),
                        remainingLength,
                        AwkRecordTerminator.Empty)
                    : BufferedReadStatus.NeedMoreData;
            }

            if (!_descriptorReachedEof && match.ByteIndex + match.ByteLength == input.ByteLength)
                return BufferedReadStatus.NeedMoreData;

            var recordLength = match.ByteIndex - recordStart;
            var record = input.CopyByteSlice(
                recordStart,
                recordLength,
                _state.Counters,
                _state.MemoryAccounting,
                _state.CancellationCheckpoint);
            var terminator = AwkRecordTerminator.FromText(input.CopyByteSlice(
                match.ByteIndex,
                match.ByteLength,
                _state.Counters,
                _state.MemoryAccounting,
                _state.CancellationCheckpoint));
            var next = AwkRegexMatchSpan.AfterMatch(input, match, _state.CancellationCheckpoint);
            var consumedLength = next - recordStart;
            _bufferStart += consumedLength;
            _hasConsumedDescriptorBytes |= consumedLength > 0;
            return PublishRecord(AwkInputRecordResult.Record(
                record,
                terminator,
                AwkRecordFieldSplittingMode.Ordinary));

            BufferedReadStatus ConsumeExistingText(
                AwkText record,
                int consumedLength,
                AwkRecordTerminator terminator)
            {
                _bufferStart += consumedLength;
                _hasConsumedDescriptorBytes |= consumedLength > 0;
                ResetSeparatorSearch();
                return PublishRecord(AwkInputRecordResult.Record(
                    record,
                    terminator,
                    AwkRecordFieldSplittingMode.Ordinary));
            }

            AwkText GetRegexInput(AwkUtf8Builder buffer)
            {
                if (_regexInput is not null)
                    return _regexInput;

                var bytes = buffer.AsSpan();
                var offset = _validatedRegexBytes;
                var nextCheckpoint = offset;
                while (offset < bytes.Length)
                {
                    if (offset >= nextCheckpoint)
                    {
                        _scanCheckpoint();
                        nextCheckpoint = offset + Math.Min(AwkRuntimeLimits.CancellationCheckBytes, bytes.Length - offset);
                        if (Ascii.IsValid(bytes[offset..nextCheckpoint]))
                        {
                            offset = nextCheckpoint;
                            continue;
                        }
                    }
                    var status = Rune.DecodeFromUtf8(bytes[offset..], out _, out var consumed);
                    if (status == OperationStatus.NeedMoreData && !_descriptorReachedEof) break;
                    if (status != OperationStatus.Done)
                        throw new AwkRuntimeException(AwkMessages.ErrorInvalidUtf8Input);
                    offset += consumed;
                }
                _validatedRegexBytes = offset;
                _regexInput = buffer.BorrowAsAwkText(offset);
                return _regexInput;
            }


        }

        BufferedReadStatus TryReadParagraph(ReadOnlySpan<byte> content)
        {
            var previous = new ParagraphReadPosition(
                _bufferStart, _scanOffset, _pendingSeparatorStart, _pendingSeparatorEnd,
                _hasConsumedDescriptorBytes, _discardedParagraphLeader,
                _paragraphLeaderScanLength);
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
                throw;
            }
        }

        BufferedReadStatus ReadParagraph(ReadOnlySpan<byte> content)
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
                        : BufferedReadStatus.NeedMoreData;
                }
            }

            content = content[recordStart..];
            if (content.IsEmpty)
            {
                // Discarding an all-blank batch remains streaming progress. Once
                // a record body exists, keep its leaders until that record commits.
                _bufferStart += recordStart;
                if (recordStart > 0)
                    ResetSeparatorSearch();
                return _descriptorReachedEof
                    ? ParagraphEndOfFile(ParagraphSeparator.Instance)
                    : BufferedReadStatus.NeedMoreData;
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
                    return BufferedReadStatus.NeedMoreData;
                }

                var recordLength = TrimTrailingRecordNewlines(content, _scanCheckpoint);
                var trailingNewlines = content[recordLength..];
                // An LF uses the shared terminator. Preflight both owned byte copies
                // for CRLF before allocating either record or terminator text.
                using var allocationPeak = _state.MemoryAccounting.ReserveAllocationPeak(
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
                return BufferedReadStatus.NeedMoreData;
            }

            var terminator = AwkRecordTerminator.FromText(CreateText(
                content.Slice(separatorIndex, separatorLength)));
            return ConsumeRecord(
                content[..TrimTrailingRecordNewlines(content[..separatorIndex], _scanCheckpoint)],
                recordStart + separatorIndex + separatorLength,
                terminator,
                AwkRecordFieldSplittingMode.Paragraph);

            BufferedReadStatus ContinueParagraphSeparator(ReadOnlySpan<byte> content)
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
                        nextNewlineCheckpoint = (long)separatorEnd + AwkRuntimeLimits.CancellationCheckBytes;
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
                    return BufferedReadStatus.NeedMoreData;
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
                        nextNewlineCheckpoint = (long)position + AwkRuntimeLimits.CancellationCheckBytes;
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
                    if (((index - start) & (AwkRuntimeLimits.CancellationCheckBytes - 1)) == 0)
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
                            nextNewlineCheckpoint = (long)position + AwkRuntimeLimits.CancellationCheckBytes;
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
}
