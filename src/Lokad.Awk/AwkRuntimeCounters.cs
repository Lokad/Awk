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

    public long FieldSplits { get; private set; }

    public long ValuesCreated { get; private set; }

    public long StringDecodes { get; private set; }

    public long Utf8Encodes { get; private set; }

    public long OutputBytes { get; private set; }

    public long OutputSinkCreations { get; private set; }

    public long RegexCompiles { get; private set; }

    public long RegexMatches { get; private set; }

    public long RegexDotNetTextFallbacks { get; private set; }

    public long RegexPatternGapFallbacks { get; private set; }

    public long RegexStringAdapterFallbacks { get; private set; }

    public long RecordSeparatorResolutions { get; private set; }

    public long SeparatorCaptureFallbacks { get; private set; }

    public long FormattingFallbacks { get; private set; }

    public long FormattingStringInteropInputs { get; private set; }

    public long StringInteropFallbacks { get; private set; }

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

    public long AwkTextAsStringCalls { get; private set; }

    public long AwkTextAsStringDecodeMisses { get; private set; }

    public long AwkTextAllocations { get; private set; }

    public long AwkTextSmallPayloadAllocations { get; private set; }

    public long AwkTextSmallPayloadBytes { get; private set; }

    public long AwkTextPayloadBytes { get; private set; }

    public long ByteTextComparisons { get; private set; }

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

    public void RecordRegexDotNetTextFallback()
    {
        if (!_enabled)
            return;

        lock (_sync)
            RegexDotNetTextFallbacks++;
    }

    public void RecordRegexPatternGapFallback()
    {
        if (!_enabled)
            return;

        lock (_sync)
            RegexPatternGapFallbacks++;
    }

    public void RecordRegexStringAdapterFallback()
    {
        if (!_enabled)
            return;

        lock (_sync)
            RegexStringAdapterFallbacks++;
    }

    public void RecordRecordSeparatorResolution()
    {
        if (!_enabled)
            return;

        lock (_sync)
            RecordSeparatorResolutions++;
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

    public void RecordHostCall()
    {
        if (!_enabled)
            return;

        lock (_sync)
            HostCalls++;
    }

    public void RecordExecutionStep()
    {
        if (!_enabled)
            return;

        lock (_sync)
            ExecutionSteps++;
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
