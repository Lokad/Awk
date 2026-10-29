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

    public long RecordsRead { get; private set; }

    public long ArrayContainerCacheHits { get; private set; }

    public long BorrowedFieldTargetSnapshots { get; private set; }
    public long NumericArraySlotReads { get; private set; }

    public void RecordNumericArraySlotRead()
    {
        if (_enabled) lock (_sync) NumericArraySlotReads++;
    }

    public long BorrowedFieldSubstrings { get; private set; }

    public long BorrowedFieldIndexSearches { get; private set; }

    public void RecordBorrowedFieldIndexSearch()
    {
        if (_enabled) lock (_sync) BorrowedFieldIndexSearches++;
    }

    public void RecordBorrowedFieldSubstring()
    {
        if (_enabled) lock (_sync) BorrowedFieldSubstrings++;
    }

    public void RecordBorrowedFieldTargetSnapshot()
    {
        if (_enabled) lock (_sync) BorrowedFieldTargetSnapshots++;
    }

    public long SynchronousDoWhileLoopRuns { get; private set; }

    public void RecordSynchronousDoWhileLoop()
    {
        if (_enabled) lock (_sync) SynchronousDoWhileLoopRuns++;
    }

    public void RecordArrayContainerCacheHit()
    {
        if (_enabled) lock (_sync) ArrayContainerCacheHits++;
    }

    public long PrintfSinkFormats { get; private set; }

    public void RecordPrintfSinkFormat()
    {
        if (_enabled) lock (_sync) PrintfSinkFormats++;
    }

    public long BytesRead { get; private set; }

    public long FieldsMaterialized { get; private set; }

    public long SkippedEmptyStatementFlushes { get; private set; }

    public void RecordSkippedEmptyStatementFlush()
    {
        if (_enabled) lock (_sync) SkippedEmptyStatementFlushes++;
    }
    public long FieldListBufferReuses { get; private set; }

    public long FieldSplits { get; private set; }

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

    public long ValuesCreated { get; private set; }

    public long StringDecodes { get; private set; }

    public long Utf8Encodes { get; private set; }

    public long OutputBytes { get; private set; }

    public long OutputSinkCreations { get; private set; }

    public long RegexCompiles { get; private set; }

    public long RegexMatches { get; private set; }

    public long RegexGroupsMaterialized { get; private set; }

    public long RecordSeparatorResolutions { get; private set; }

    public long RecordSeparatorTailBytesMoved { get; private set; }

    public long RecordSeparatorBytesScanned { get; private set; }

    public long SeparatorCaptureFallbacks { get; private set; }

    public long FormattingFallbacks { get; private set; }

    public long PrintfSignedIntegerFastPaths { get; private set; }

    public long SingleValuePrintRuns { get; private set; }

    public long SmallValuePrintRuns { get; private set; }

    public long FieldListPrintRuns { get; private set; }

    public void RecordFieldListPrint()
    {
        if (_enabled) lock (_sync) FieldListPrintRuns++;
    }

    public void RecordSmallValuePrint()
    {
        if (_enabled) lock (_sync) SmallValuePrintRuns++;
    }

    public void RecordSingleValuePrint()
    {
        if (_enabled) lock (_sync) SingleValuePrintRuns++;
    }

    public void RecordPrintfSignedIntegerFastPath()
    {
        if (_enabled) lock (_sync) PrintfSignedIntegerFastPaths++;
    }

    public long FormattingStringInteropInputs { get; private set; }

    public long StringInteropFallbacks { get; private set; }

    public long SynchronousLoopRuns { get; private set; }

    public long SynchronousActionRuns { get; private set; }

    public long SynchronousConditionalRuns { get; private set; }

    public void RecordSynchronousConditional()
    {
        if (_enabled) lock (_sync) SynchronousConditionalRuns++;
    }

    public long SynchronousExpressionRuns { get; private set; }
    public long SynchronousFunctionRuns { get; private set; }

    public long BoundScalarArguments { get; private set; }

    public long BoundScalarLengths { get; private set; }

    public void RecordBoundScalarLength()
    {
        if (!_enabled) return;
        lock (_sync) BoundScalarLengths++;
    }

    public long NumericLocalReads { get; private set; }

    public long ScalarLocalValueReads { get; private set; }

    public void RecordScalarLocalValueRead()
    {
        if (_enabled) lock (_sync) ScalarLocalValueReads++;
    }

    public long BoundGlobalArrayAccesses { get; private set; }

    public long BoundLocalArrayAccesses { get; private set; }

    public long BoundArrayUpdates { get; private set; }

    public long ArrayNameResolutions { get; private set; }

    public long SubarrayPromotions { get; private set; }

    public void RecordSubarrayPromotion()
    {
        if (_enabled) lock (_sync) SubarrayPromotions++;
    }

    public void RecordArrayNameResolution()
    {
        if (_enabled) lock (_sync) ArrayNameResolutions++;
    }

    public void RecordBoundArrayUpdate()
    {
        if (_enabled) lock (_sync) BoundArrayUpdates++;
    }

    public void RecordBoundLocalArrayAccess()
    {
        if (_enabled) lock (_sync) BoundLocalArrayAccesses++;
    }

    public void RecordBoundGlobalArrayAccess()
    {
        if (_enabled) lock (_sync) BoundGlobalArrayAccesses++;
    }

    public long NumericGlobalReads { get; private set; }

    public long PreparedNumericSlotReads { get; private set; }

    public long BoundLocalScalarArguments { get; private set; }

    public void RecordBoundLocalScalarArgument()
    {
        if (_enabled) lock (_sync) BoundLocalScalarArguments++;
    }

    public void RecordPreparedNumericSlotRead()
    {
        if (_enabled) lock (_sync) PreparedNumericSlotReads++;
    }

    public void RecordNumericGlobalRead()
    {
        if (_enabled) lock (_sync) NumericGlobalReads++;
    }

    public void RecordNumericLocalRead()
    {
        if (!_enabled) return;
        lock (_sync) NumericLocalReads++;
    }

    public void RecordBoundScalarArgument()
    {
        if (!_enabled) return;
        lock (_sync) BoundScalarArguments++;
    }
    public long FunctionArgumentBufferRents { get; private set; }

    public long InlineFunctionFrames { get; private set; }

    public void RecordInlineFunctionFrame()
    {
        if (_enabled) lock (_sync) InlineFunctionFrames++;
    }

    public void RecordSynchronousFunction()
    {
        if (!_enabled) return;
        lock (_sync) SynchronousFunctionRuns++;
    }

    public void RecordFunctionArgumentBufferRent()
    {
        if (_enabled) lock (_sync) FunctionArgumentBufferRents++;
    }

    public long HostCalls { get; private set; }

    public long CancellationChecks { get; private set; }

    public long FunctionStackSuspensions { get; private set; }

    public void RecordFunctionStackSuspension()
    {
        if (!_enabled)
            return;

        lock (_sync)
            FunctionStackSuspensions++;
    }

    public long AwkTextAsStringCalls { get; private set; }

    public long AwkTextAsStringDecodeMisses { get; private set; }

    public long AwkTextAllocations { get; private set; }

    public long AwkTextSmallPayloadAllocations { get; private set; }

    public long AwkTextSmallPayloadBytes { get; private set; }

    public long AwkTextPayloadBytes { get; private set; }

    public long ByteTextComparisons { get; private set; }

    public long DeferredComparisonLiterals { get; private set; }

    public void RecordDeferredComparisonLiteral()
    {
        if (_enabled) lock (_sync) DeferredComparisonLiterals++;
    }

    public long ConcatenationBytesCopied { get; private set; }

    public long ConcatenationArgumentBufferRents { get; private set; }

    public void RecordConcatenationArgumentBufferRent()
    {
        if (_enabled) lock (_sync) ConcatenationArgumentBufferRents++;
    }

    public long RuneSliceBytesScanned { get; private set; }

    public long RuneSliceIndexAllocations { get; private set; }

    public long RuneSliceIndexBytes { get; private set; }

    public long MemoryPeakLiveBytes { get; private set; }

    public void RecordRecordRead()
    {
        if (!_enabled)
            return;

        lock (_sync)
            RecordsRead++;
    }

    public void RecordBytesRead(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
            BytesRead += byteCount;
    }

    public void RecordFieldsMaterialized(int count)
    {
        if (!_enabled || count <= 0)
            return;

        lock (_sync)
            FieldsMaterialized += count;
    }

    public void RecordFieldListBufferReuse()
    {
        if (!_enabled) return;
        lock (_sync) FieldListBufferReuses++;
    }

    public void RecordFieldSplit()
    {
        if (!_enabled)
            return;

        lock (_sync)
            FieldSplits++;
    }

    public void RecordValueCreated()
    {
        if (!_enabled)
            return;

        lock (_sync)
            ValuesCreated++;
    }

    public void RecordStringDecode()
    {
        if (!_enabled)
            return;

        lock (_sync)
            StringDecodes++;
    }

    public void RecordUtf8Encode()
    {
        if (!_enabled)
            return;

        lock (_sync)
            Utf8Encodes++;
    }

    public void RecordOutputBytes(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
            OutputBytes += byteCount;
    }

    public void RecordRegexCompile()
    {
        if (!_enabled)
            return;

        lock (_sync)
            RegexCompiles++;
    }

    public void RecordRegexMatch()
    {
        if (!_enabled)
            return;

        lock (_sync)
            RegexMatches++;
    }

    public void RecordRegexGroupsMaterialized(int count)
    {
        if (!_enabled || count <= 0)
            return;

        lock (_sync)
            RegexGroupsMaterialized += count;
    }

    public void RecordRecordSeparatorResolution()
    {
        if (!_enabled)
            return;

        lock (_sync)
            RecordSeparatorResolutions++;
    }

    public void RecordRecordSeparatorTailBytesMoved(int byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
            RecordSeparatorTailBytesMoved += byteCount;
    }

    public void RecordRecordSeparatorBytesScanned(int byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
            RecordSeparatorBytesScanned += byteCount;
    }

    public void RecordSeparatorCaptureFallback()
    {
        if (!_enabled)
            return;

        lock (_sync)
            SeparatorCaptureFallbacks++;
    }

    public void RecordFormattingFallback()
    {
        if (!_enabled)
            return;

        lock (_sync)
            FormattingFallbacks++;
    }

    public void RecordFormattingStringInteropInput() => RecordFormattingStringInteropInput(1);

    public void RecordFormattingStringInteropInput(int count)
    {
        if (!_enabled || count <= 0)
            return;

        lock (_sync)
            FormattingStringInteropInputs += count;
    }

    public void RecordStringInteropFallback()
    {
        if (!_enabled)
            return;

        lock (_sync)
            StringInteropFallbacks++;
    }

    public void RecordSynchronousLoop()
    {
        if (_enabled) lock (_sync) SynchronousLoopRuns++;
    }

    public void RecordSynchronousAction()
    {
        if (_enabled) lock (_sync) SynchronousActionRuns++;
    }

    public void RecordSynchronousExpression()
    {
        if (_enabled) lock (_sync) SynchronousExpressionRuns++;
    }

    public void RecordHostCall()
    {
        if (!_enabled)
            return;

        lock (_sync)
            HostCalls++;
    }

    public void RecordCancellationCheck()
    {
        if (!_enabled)
            return;

        lock (_sync)
            CancellationChecks++;
    }

    public void RecordAwkTextAsStringCall()
    {
        if (!_enabled)
            return;

        lock (_sync)
            AwkTextAsStringCalls++;
    }

    public void RecordOutputSinkCreation()
    {
        if (!_enabled)
            return;

        lock (_sync)
            OutputSinkCreations++;
    }

    public void RecordAwkTextAsStringDecodeMiss()
    {
        if (!_enabled)
            return;

        lock (_sync)
            AwkTextAsStringDecodeMisses++;
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

        lock (_sync)
            ByteTextComparisons++;
    }

    public void RecordConcatenationBytesCopied(int byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
            ConcatenationBytesCopied += byteCount;
    }

    public void RecordRuneSliceBytesScanned(int byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
            RuneSliceBytesScanned += byteCount;
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
