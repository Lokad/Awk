namespace Lokad.Awk;

internal sealed class AwkRuntimeCounters
{
    private readonly bool _enabled;
    private readonly object _sync = new();

    private AwkRuntimeCounters(bool enabled)
    {
        _enabled = enabled;
    }

    public static AwkRuntimeCounters Disabled { get; } = new(enabled: false);

    public static AwkRuntimeCounters CreateEnabled() => new(enabled: true);

    public bool IsEnabled => _enabled;

    private long _fieldRuneBytesScanned;
    public long FieldRuneBytesScanned => Volatile.Read(ref _fieldRuneBytesScanned);
    internal void RecordFieldRuneBytesScanned(int bytes)
    {
        if (_enabled) Interlocked.Add(ref _fieldRuneBytesScanned, bytes);
    }

    public long FunctionFramesPrepared { get; private set; }
    public long FunctionSlotBufferAllocations { get; private set; }
    private long _functionArgumentSegments;
    public long FunctionArgumentSegments => Volatile.Read(ref _functionArgumentSegments);
    private long _callAssignments;
    public long CallAssignments => Volatile.Read(ref _callAssignments);
    private long _callNumericScratchBypasses;
    public long CallNumericScratchBypasses => Volatile.Read(ref _callNumericScratchBypasses);
    internal void RecordCallAssignment(bool bypassedNumericScratch)
    {
        if (!_enabled) return;
        Interlocked.Increment(ref _callAssignments);
        if (bypassedNumericScratch) Interlocked.Increment(ref _callNumericScratchBypasses);
    }
    internal void RecordFunctionArgumentSegment()
    {
        if (_enabled) Interlocked.Increment(ref _functionArgumentSegments);
    }
    private long _functionActivationSlotVisits;
    public long FunctionActivationSlotVisits => Volatile.Read(ref _functionActivationSlotVisits);
    private long _functionRetirementSlotVisits;
    public long FunctionRetirementSlotVisits => Volatile.Read(ref _functionRetirementSlotVisits);

    internal void RecordFunctionFramePrepared(bool grewSlots)
    {
        if (!_enabled) return;
        lock (_sync)
        {
            FunctionFramesPrepared++;
            if (grewSlots) FunctionSlotBufferAllocations++;
        }
    }

    internal void RecordFunctionActivationSlots(int count)
    {
        if (_enabled) Interlocked.Add(ref _functionActivationSlotVisits, count);
    }

    internal void RecordFunctionRetirementSlots(int count)
    {
        if (_enabled) Interlocked.Add(ref _functionRetirementSlotVisits, count);
    }

    // Entries into prepared record programs; loop iterations are RecordsRead.
    private long _boundRegionRuns;
    public long BoundRegionRuns => Volatile.Read(ref _boundRegionRuns);
    private long _boundFunctionRuns;
    public long BoundFunctionRuns => Volatile.Read(ref _boundFunctionRuns);
    private long _boundPhaseRuns;
    public long BoundPhaseRuns => Volatile.Read(ref _boundPhaseRuns);
    internal void RecordBoundPhaseRun()
    {
        if (_enabled) Interlocked.Increment(ref _boundPhaseRuns);
    }
    internal void RecordBoundFunctionRun()
    {
        if (_enabled) Interlocked.Increment(ref _boundFunctionRuns);
    }
    public void RecordBoundRegionRun()
    {
        if (_enabled) Interlocked.Increment(ref _boundRegionRuns);
    }

    private long _regexCoordinateBytes;
    public long RegexCoordinateBytes => Volatile.Read(ref _regexCoordinateBytes);
    public void RecordRegexCoordinateBytes(int bytes)
    {
        if (_enabled) Interlocked.Add(ref _regexCoordinateBytes, bytes);
    }

    public long BufferedInputRecords { get; private set; }
    public long InputRecordBufferReuses { get; private set; }
    public long BorrowedInputRecords { get; private set; }
    public long InputRecordBytesCopied { get; private set; }
    public long InputRecordPromotions { get; private set; }
    public void RecordBufferedInputRecord(bool reused, bool borrowed = false)
    {
        if (!_enabled) return;
        lock (_sync)
        {
            BufferedInputRecords++;
            if (borrowed) BorrowedInputRecords++;
            if (reused) InputRecordBufferReuses++;
        }
    }

    internal void RecordInputRecordCopy(int bytes, bool promotion = false)
    {
        if (!_enabled) return;
        lock (_sync)
        {
            InputRecordBytesCopied += bytes;
            if (promotion) InputRecordPromotions++;
        }
    }

    private long _recordsRead;
    public long RecordsRead => Volatile.Read(ref _recordsRead);

    private long _preparedSpecialScalarReads;
    public long PreparedSpecialScalarReads => Volatile.Read(ref _preparedSpecialScalarReads);
    private long _numericSpecialScalarReads;
    public long NumericSpecialScalarReads => Volatile.Read(ref _numericSpecialScalarReads);

    internal void RecordNumericSpecialScalarRead()
    {
        if (_enabled) Interlocked.Increment(ref _numericSpecialScalarReads);
    }
    private long _boundSpecialScalarReads;
    public long BoundSpecialScalarReads => Volatile.Read(ref _boundSpecialScalarReads);

    internal void RecordBoundSpecialScalarRead()
    {
        if (_enabled) Interlocked.Increment(ref _boundSpecialScalarReads);
    }

    internal void RecordPreparedSpecialScalarRead()
    {
        if (_enabled) Interlocked.Increment(ref _preparedSpecialScalarReads);
    }

    private long _arrayContainerCacheHits;
    public long ArrayContainerCacheHits => Volatile.Read(ref _arrayContainerCacheHits);

    private long _borrowedFieldTargetSnapshots;
    public long BorrowedFieldTargetSnapshots => Volatile.Read(ref _borrowedFieldTargetSnapshots);
    private long _numericArraySlotReads;
    public long NumericArraySlotReads => Volatile.Read(ref _numericArraySlotReads);
    private long _arrayCellCommits;
    public long ArrayCellCommits => Volatile.Read(ref _arrayCellCommits);
    private long _capturedPathAllocations;
    public long CapturedPathAllocations => Volatile.Read(ref _capturedPathAllocations);
    private long _capturedPathKeyBufferAllocations;
    public long CapturedPathKeyBufferAllocations => Volatile.Read(ref _capturedPathKeyBufferAllocations);

    internal void RecordCapturedPathAllocation()
    {
        if (_enabled) Interlocked.Increment(ref _capturedPathAllocations);
    }

    internal void RecordCapturedPathKeyBufferAllocation()
    {
        if (_enabled) Interlocked.Increment(ref _capturedPathKeyBufferAllocations);
    }

    // Comparer work, not the table's internal collision-probe count.
    public long ArrayBorrowedHashCalls { get; private set; }
    public long ArrayOwnedHashCalls { get; private set; }
    public long ArrayHashBytes { get; private set; }
    private long _arrayEqualityCalls;
    public long ArrayEqualityCalls => Volatile.Read(ref _arrayEqualityCalls);

    internal void RecordArrayHash(int bytes, bool borrowed)
    {
        if (!_enabled) return;
        lock (_sync)
        {
            if (borrowed) ArrayBorrowedHashCalls++;
            else ArrayOwnedHashCalls++;
            ArrayHashBytes += bytes;
        }
    }

    internal void RecordArrayEquality()
    {
        if (_enabled) Interlocked.Increment(ref _arrayEqualityCalls);
    }

    public void RecordArrayCellCommit()
    {
        if (_enabled) Interlocked.Increment(ref _arrayCellCommits);
    }

    public void RecordNumericArraySlotRead()
    {
        if (_enabled) Interlocked.Increment(ref _numericArraySlotReads);
    }

    public void RecordBorrowedFieldTargetSnapshot()
    {
        if (_enabled) Interlocked.Increment(ref _borrowedFieldTargetSnapshots);
    }

    public void RecordArrayContainerCacheHit()
    {
        if (_enabled) Interlocked.Increment(ref _arrayContainerCacheHits);
    }

    private long _printfSinkFormats;
    public long PrintfSinkFormats => Volatile.Read(ref _printfSinkFormats);

    public void RecordPrintfSinkFormat()
    {
        if (_enabled) Interlocked.Increment(ref _printfSinkFormats);
    }

    private long _bytesRead;
    public long BytesRead => Volatile.Read(ref _bytesRead);

    private long _fieldsMaterialized;
    public long FieldsMaterialized => Volatile.Read(ref _fieldsMaterialized);


    private long _fieldListBufferReuses;
    public long FieldListBufferReuses => Volatile.Read(ref _fieldListBufferReuses);
    private long _fieldPrefixStages;
    public long FieldPrefixStages => Volatile.Read(ref _fieldPrefixStages);
    private long _fieldPrefixExtensions;
    public long FieldPrefixExtensions => Volatile.Read(ref _fieldPrefixExtensions);
    public void RecordFieldPrefixExtension()
    {
        if (_enabled) Interlocked.Increment(ref _fieldPrefixExtensions);
    }

    private long _fieldDescriptorCopies;
    public long FieldDescriptorCopies => Volatile.Read(ref _fieldDescriptorCopies);

    private long _fieldValueCacheBufferAllocations;
    public long FieldValueCacheBufferAllocations => Volatile.Read(ref _fieldValueCacheBufferAllocations);

    public void RecordFieldValueCacheBufferAllocation()
    {
        if (_enabled) Interlocked.Increment(ref _fieldValueCacheBufferAllocations);
    }

    // Byte-native numeric reads only; ordinary owned-value classification is separate.
    private long _numericFieldByteParses;
    public long NumericFieldByteParses => Volatile.Read(ref _numericFieldByteParses);

    public void RecordNumericFieldByteParse()
    {
        if (_enabled) Interlocked.Increment(ref _numericFieldByteParses);
    }

    private long _fieldSplits;
    public long FieldSplits => Volatile.Read(ref _fieldSplits);

    public long WhitespaceFieldSearches { get; private set; }

    public long WhitespaceFieldSearchBytes { get; private set; }

    public long AsciiCaseConversionChunks { get; private set; }

    public long AsciiCaseConversionBytes { get; private set; }

    public long AsciiCasePreparationChunks { get; private set; }

    public long AsciiCasePreparationBytes { get; private set; }

    public void RecordAsciiCasePreparationChunk(int bytes)
    {
        if (!_enabled) return;
        lock (_sync)
        {
            AsciiCasePreparationChunks++;
            AsciiCasePreparationBytes += bytes;
        }
    }

    public void RecordAsciiCaseConversionChunk(int bytes)
    {
        if (!_enabled) return;
        lock (_sync)
        {
            AsciiCaseConversionChunks++;
            AsciiCaseConversionBytes += bytes;
        }
    }

    public void RecordWhitespaceFieldSearch(int bytes)
    {
        if (!_enabled) return;
        lock (_sync)
        {
            WhitespaceFieldSearches++;
            WhitespaceFieldSearchBytes += bytes;
        }
    }

    private long _valuesCreated;
    public long ValuesCreated => Volatile.Read(ref _valuesCreated);

    private long _preparedRecordTerminatorSlots;
    public long PreparedRecordTerminatorSlots => Volatile.Read(ref _preparedRecordTerminatorSlots);

    public void RecordPreparedRecordTerminatorSlot()
    {
        if (_enabled) Interlocked.Increment(ref _preparedRecordTerminatorSlots);
    }

    private long _stringDecodes;
    public long StringDecodes => Volatile.Read(ref _stringDecodes);

    private long _utf8Encodes;
    public long Utf8Encodes => Volatile.Read(ref _utf8Encodes);

    private long _outputBytes;
    public long OutputBytes => Volatile.Read(ref _outputBytes);

    private long _outputSinkCreations;
    public long OutputSinkCreations => Volatile.Read(ref _outputSinkCreations);

    private long _regexCompiles;
    public long RegexCompiles => Volatile.Read(ref _regexCompiles);

    private long _regexMatches;
    public long RegexMatches => Volatile.Read(ref _regexMatches);

    private long _regexGroupsMaterialized;
    public long RegexGroupsMaterialized => Volatile.Read(ref _regexGroupsMaterialized);

    private long _recordSeparatorResolutions;
    public long RecordSeparatorResolutions => Volatile.Read(ref _recordSeparatorResolutions);

    private long _recordSeparatorTailBytesMoved;
    public long RecordSeparatorTailBytesMoved => Volatile.Read(ref _recordSeparatorTailBytesMoved);

    private long _recordSeparatorBytesScanned;
    public long RecordSeparatorBytesScanned => Volatile.Read(ref _recordSeparatorBytesScanned);

    private long _separatorCaptureFallbacks;
    public long SeparatorCaptureFallbacks => Volatile.Read(ref _separatorCaptureFallbacks);

    private long _formattingFallbacks;
    public long FormattingFallbacks => Volatile.Read(ref _formattingFallbacks);

    private long _printfSignedIntegerFastPaths;
    public long PrintfSignedIntegerFastPaths => Volatile.Read(ref _printfSignedIntegerFastPaths);

    private long _singleValuePrintRuns;
    public long SingleValuePrintRuns => Volatile.Read(ref _singleValuePrintRuns);

    private long _smallValuePrintRuns;
    public long SmallValuePrintRuns => Volatile.Read(ref _smallValuePrintRuns);

    private long _fieldListPrintRuns;
    public long FieldListPrintRuns => Volatile.Read(ref _fieldListPrintRuns);

    public void RecordFieldListPrint()
    {
        if (_enabled) Interlocked.Increment(ref _fieldListPrintRuns);
    }

    public void RecordSmallValuePrint()
    {
        if (_enabled) Interlocked.Increment(ref _smallValuePrintRuns);
    }

    public void RecordSingleValuePrint()
    {
        if (_enabled) Interlocked.Increment(ref _singleValuePrintRuns);
    }

    public void RecordPrintfSignedIntegerFastPath()
    {
        if (_enabled) Interlocked.Increment(ref _printfSignedIntegerFastPaths);
    }

    private long _formattingStringInteropInputs;
    public long FormattingStringInteropInputs => Volatile.Read(ref _formattingStringInteropInputs);

    private long _stringInteropFallbacks;
    public long StringInteropFallbacks => Volatile.Read(ref _stringInteropFallbacks);


    private long _functionNameLookups;
    public long FunctionNameLookups => Volatile.Read(ref _functionNameLookups);
    public void RecordFunctionNameLookup()
    {
        if (_enabled) Interlocked.Increment(ref _functionNameLookups);
    }


    private long _boundScalarLengths;
    public long BoundScalarLengths => Volatile.Read(ref _boundScalarLengths);

    public void RecordBoundScalarLength()
    {
        if (!_enabled) return;
        Interlocked.Increment(ref _boundScalarLengths);
    }

    private long _numericLocalReads;
    public long NumericLocalReads => Volatile.Read(ref _numericLocalReads);

    private long _boundFrameNumberWrites;
    public long BoundFrameNumberWrites => Volatile.Read(ref _boundFrameNumberWrites);

    public void RecordBoundFrameNumberWrite()
    {
        if (_enabled) Interlocked.Increment(ref _boundFrameNumberWrites);
    }

    private long _scalarLocalValueReads;
    public long ScalarLocalValueReads => Volatile.Read(ref _scalarLocalValueReads);

    public void RecordScalarLocalValueRead()
    {
        if (_enabled) Interlocked.Increment(ref _scalarLocalValueReads);
    }

    private long _boundGlobalArrayAccesses;
    public long BoundGlobalArrayAccesses => Volatile.Read(ref _boundGlobalArrayAccesses);

    private long _boundLocalArrayAccesses;
    public long BoundLocalArrayAccesses => Volatile.Read(ref _boundLocalArrayAccesses);

    private long _indexedLocalArrayAccesses;
    public long IndexedLocalArrayAccesses => Volatile.Read(ref _indexedLocalArrayAccesses);

    public void RecordIndexedLocalArrayAccess()
    {
        if (_enabled) Interlocked.Increment(ref _indexedLocalArrayAccesses);
    }

    private long _boundArrayUpdates;
    public long BoundArrayUpdates => Volatile.Read(ref _boundArrayUpdates);

    private long _arrayNameResolutions;
    public long ArrayNameResolutions => Volatile.Read(ref _arrayNameResolutions);

    private long _subarrayPromotions;
    public long SubarrayPromotions => Volatile.Read(ref _subarrayPromotions);

    public void RecordSubarrayPromotion()
    {
        if (_enabled) Interlocked.Increment(ref _subarrayPromotions);
    }

    public void RecordArrayNameResolution()
    {
        if (_enabled) Interlocked.Increment(ref _arrayNameResolutions);
    }

    public void RecordBoundArrayUpdate()
    {
        if (_enabled) Interlocked.Increment(ref _boundArrayUpdates);
    }

    public void RecordBoundLocalArrayAccess()
    {
        if (_enabled) Interlocked.Increment(ref _boundLocalArrayAccesses);
    }

    public void RecordBoundGlobalArrayAccess()
    {
        if (_enabled) Interlocked.Increment(ref _boundGlobalArrayAccesses);
    }

    private long _numericGlobalReads;
    public long NumericGlobalReads => Volatile.Read(ref _numericGlobalReads);

    private long _preparedNumericSlotReads;
    public long PreparedNumericSlotReads => Volatile.Read(ref _preparedNumericSlotReads);

    private long _indexedLocalArrayArguments;
    public long IndexedLocalArrayArguments => Volatile.Read(ref _indexedLocalArrayArguments);

    public void RecordIndexedLocalArrayArgument()
    {
        if (_enabled) Interlocked.Increment(ref _indexedLocalArrayArguments);
    }

    public void RecordPreparedNumericSlotRead()
    {
        if (_enabled) Interlocked.Increment(ref _preparedNumericSlotReads);
    }

    public void RecordNumericGlobalRead()
    {
        if (_enabled) Interlocked.Increment(ref _numericGlobalReads);
    }

    public void RecordNumericLocalRead()
    {
        if (!_enabled) return;
        Interlocked.Increment(ref _numericLocalReads);
    }


    private long _hostCalls;
    public long HostCalls => Volatile.Read(ref _hostCalls);

    private long _cancellationChecks;
    public long CancellationChecks => Volatile.Read(ref _cancellationChecks);

    private long _awkTextAsStringCalls;
    public long AwkTextAsStringCalls => Volatile.Read(ref _awkTextAsStringCalls);

    private long _awkTextAsStringDecodeMisses;
    public long AwkTextAsStringDecodeMisses => Volatile.Read(ref _awkTextAsStringDecodeMisses);

    // Owned UTF-8 payload creations, including compact text; not metadata objects.
    public long AwkTextAllocations { get; private set; }

    public long AwkTextSmallPayloadAllocations { get; private set; }

    public long AwkTextSmallPayloadBytes { get; private set; }

    public long AwkTextPayloadBytes { get; private set; }

    private long _byteTextComparisons;
    public long ByteTextComparisons => Volatile.Read(ref _byteTextComparisons);

    // Eager text-value factory calls; deferred numeric consumption is separate.
    private long _textValueClassifications;
    public long TextValueClassifications => Volatile.Read(ref _textValueClassifications);

    public void RecordTextValueClassification()
    {
        if (_enabled) Interlocked.Increment(ref _textValueClassifications);
    }

    private long _concatenationBytesCopied;
    public long ConcatenationBytesCopied => Volatile.Read(ref _concatenationBytesCopied);

    private long _runeSliceBytesScanned;
    public long RuneSliceBytesScanned => Volatile.Read(ref _runeSliceBytesScanned);

    public long RuneSliceIndexAllocations { get; private set; }

    public long RuneSliceIndexBytes { get; private set; }

    public long MemoryPeakLiveBytes { get; private set; }

    public void RecordRecordRead()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _recordsRead);
    }

    public void RecordBytesRead(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        Interlocked.Add(ref _bytesRead, byteCount);
    }

    public void RecordFieldsMaterialized(int count)
    {
        if (!_enabled || count <= 0)
            return;

        Interlocked.Add(ref _fieldsMaterialized, count);
    }

    public void RecordFieldPrefixStage()
    {
        if (_enabled) Interlocked.Increment(ref _fieldPrefixStages);
    }

    public void RecordFieldDescriptorCopies(int count)
    {
        if (_enabled && count > 0) Interlocked.Add(ref _fieldDescriptorCopies, count);
    }

    public void RecordFieldListBufferReuse()
    {
        if (!_enabled) return;
        Interlocked.Increment(ref _fieldListBufferReuses);
    }

    public void RecordFieldSplit()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _fieldSplits);
    }

    public void RecordValueCreated()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _valuesCreated);
    }

    public void RecordStringDecode()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _stringDecodes);
    }

    public void RecordUtf8Encode()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _utf8Encodes);
    }

    public void RecordOutputBytes(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        Interlocked.Add(ref _outputBytes, byteCount);
    }

    public void RecordRegexCompile()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _regexCompiles);
    }

    public void RecordRegexMatch()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _regexMatches);
    }

    public void RecordRegexGroupsMaterialized(int count)
    {
        if (!_enabled || count <= 0)
            return;

        Interlocked.Add(ref _regexGroupsMaterialized, count);
    }

    public void RecordRecordSeparatorResolution()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _recordSeparatorResolutions);
    }

    public void RecordRecordSeparatorTailBytesMoved(int byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        Interlocked.Add(ref _recordSeparatorTailBytesMoved, byteCount);
    }

    public void RecordRecordSeparatorBytesScanned(int byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        Interlocked.Add(ref _recordSeparatorBytesScanned, byteCount);
    }

    public void RecordSeparatorCaptureFallback()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _separatorCaptureFallbacks);
    }

    public void RecordFormattingFallback()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _formattingFallbacks);
    }

    public void RecordFormattingStringInteropInput() => RecordFormattingStringInteropInput(1);

    public void RecordFormattingStringInteropInput(int count)
    {
        if (!_enabled || count <= 0)
            return;

        Interlocked.Add(ref _formattingStringInteropInputs, count);
    }

    public void RecordStringInteropFallback()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _stringInteropFallbacks);
    }


    public void RecordHostCall()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _hostCalls);
    }

    public void RecordCancellationCheck()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _cancellationChecks);
    }

    public void RecordAwkTextAsStringCall()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _awkTextAsStringCalls);
    }

    public void RecordOutputSinkCreation()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _outputSinkCreations);
    }

    public void RecordAwkTextAsStringDecodeMiss()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _awkTextAsStringDecodeMisses);
    }

    public void RecordAwkTextAllocation(int byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
        {
            AwkTextAllocations++;
            AwkTextPayloadBytes += byteCount;
            if (byteCount <= 16)
            {
                AwkTextSmallPayloadAllocations++;
                AwkTextSmallPayloadBytes += byteCount;
            }
        }
    }

    public void RecordByteTextComparison()
    {
        if (!_enabled)
            return;

        Interlocked.Increment(ref _byteTextComparisons);
    }

    public void RecordConcatenationBytesCopied(int byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        Interlocked.Add(ref _concatenationBytesCopied, byteCount);
    }

    public void RecordRuneSliceBytesScanned(int byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        Interlocked.Add(ref _runeSliceBytesScanned, byteCount);
    }

    public void RecordRuneSliceIndexAllocation(int byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
        {
            RuneSliceIndexAllocations++;
            RuneSliceIndexBytes += byteCount;
        }
    }

    public void RecordMemoryLiveBytes(long liveBytes)
    {
        if (!_enabled) return;
        lock (_sync)
            if (liveBytes > MemoryPeakLiveBytes)
                MemoryPeakLiveBytes = liveBytes;
    }
}
