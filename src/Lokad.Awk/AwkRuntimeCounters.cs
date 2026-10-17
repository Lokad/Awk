namespace Lokad.Awk;

internal sealed class AwkRuntimeCounters
{
    private readonly bool _enabled;
    private readonly object _sync = new();
    private ulong _vmOpcodeMask;

    private AwkRuntimeCounters(bool enabled)
    {
        _enabled = enabled;
    }

    public static AwkRuntimeCounters Disabled { get; } = new(enabled: false);

    public static AwkRuntimeCounters CreateEnabled() => new(enabled: true);

    public bool IsEnabled => _enabled;

    public long RecordsRead { get; private set; }

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

    public long RegexDotNetTextMatches { get; private set; }

    public long RegexPatternGapFallbacks { get; private set; }

    public long RegexStringAdapterMatches { get; private set; }

    public long RegexPolicyInputBytesInspected { get; private set; }

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

    public long BoundGlobalArrayAccesses { get; private set; }

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

    public long ExecutionSteps { get; private set; }

    public long VmInstructions { get; private set; }

    public long VmBasicBlockEntries { get; private set; }

    public long VmFallbackCalls { get; private set; }

    public long VmPatternFallbacks { get; private set; }

    public long VmOpcodeKindsExecuted { get; private set; }

    public long VmBudgetChecks { get; private set; }

    public long VmMaxStackDepth { get; private set; }

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

    public long RuneSliceBytesScanned { get; private set; }

    public long RuneSliceIndexAllocations { get; private set; }

    public long RuneSliceIndexBytes { get; private set; }

    public long MemoryReserveCalls { get; private set; }

    public long MemoryCommitCalls { get; private set; }

    public long MemoryReleaseReservedCalls { get; private set; }

    public long MemoryReleaseCommittedCalls { get; private set; }

    public long MemoryReservedBytes { get; private set; }

    public long MemoryCommittedBytes { get; private set; }

    public long MemoryReleasedReservedBytes { get; private set; }

    public long MemoryReleasedCommittedBytes { get; private set; }

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

    public void RecordRegexDotNetTextMatch()
    {
        if (!_enabled)
            return;

        lock (_sync)
            RegexDotNetTextMatches++;
    }

    public void RecordRegexPatternGapFallback()
    {
        if (!_enabled)
            return;

        lock (_sync)
            RegexPatternGapFallbacks++;
    }

    public void RecordRegexStringAdapterMatch()
    {
        if (!_enabled)
            return;

        lock (_sync)
            RegexStringAdapterMatches++;
    }

    public void RecordRegexPolicyInputBytesInspected(int byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
            RegexPolicyInputBytesInspected += byteCount;
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

    public void RecordExecutionStep() => RecordExecutionSteps(1);

    public void RecordExecutionSteps(int count)
    {
        if (!_enabled)
            return;

        lock (_sync)
            ExecutionSteps += count;
    }

    public void RecordVmInstruction()
    {
        if (!_enabled)
            return;

        lock (_sync)
            VmInstructions++;
    }

    public void RecordVmOpcode(AwkOpcode opcode)
    {
        if (!_enabled)
            return;

        var opcodeIndex = (int)opcode;
        if ((uint)opcodeIndex >= sizeof(ulong) * 8)
            return;

        lock (_sync)
        {
            var opcodeBit = 1UL << opcodeIndex;
            if ((_vmOpcodeMask & opcodeBit) != 0)
                return;

            _vmOpcodeMask |= opcodeBit;
            VmOpcodeKindsExecuted++;
        }
    }

    public void RecordVmBasicBlockEntry()
    {
        if (!_enabled)
            return;

        lock (_sync)
            VmBasicBlockEntries++;
    }

    public void RecordVmFallbackCall()
    {
        if (!_enabled)
            return;

        lock (_sync)
            VmFallbackCalls++;
    }

    public void RecordVmPatternFallback()
    {
        if (!_enabled)
            return;

        lock (_sync)
            VmPatternFallbacks++;
    }

    public void RecordVmBudgetCheck()
    {
        if (!_enabled)
            return;

        lock (_sync)
            VmBudgetChecks++;
    }

    public void RecordVmStackDepth(long depth)
    {
        if (!_enabled)
            return;

        lock (_sync)
        {
            if (depth > VmMaxStackDepth)
                VmMaxStackDepth = depth;
        }
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

    public void RecordMemoryReserve(long byteCount, long liveBytes)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
        {
            MemoryReserveCalls++;
            MemoryReservedBytes += byteCount;
            RecordMemoryLiveBytes(liveBytes);
        }
    }

    public void RecordMemoryCommit(long byteCount, long liveBytes)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
        {
            MemoryCommitCalls++;
            MemoryCommittedBytes += byteCount;
            RecordMemoryLiveBytes(liveBytes);
        }
    }

    public void RecordMemoryReleaseReserved(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
        {
            MemoryReleaseReservedCalls++;
            MemoryReleasedReservedBytes += byteCount;
        }
    }

    public void RecordMemoryReleaseCommitted(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        lock (_sync)
        {
            MemoryReleaseCommittedCalls++;
            MemoryReleasedCommittedBytes += byteCount;
        }
    }

    private void RecordMemoryLiveBytes(long liveBytes)
    {
        if (liveBytes > MemoryPeakLiveBytes)
            MemoryPeakLiveBytes = liveBytes;
    }
}
