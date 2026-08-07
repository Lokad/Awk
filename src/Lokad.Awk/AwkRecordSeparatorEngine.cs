using System;
using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRecordSeparatorEngine
{
    private readonly AwkRecordSeparatorContext _context;

    public AwkRecordSeparatorEngine(
        AwkRuntimeState state,
        AwkRecordDescriptorReader descriptorReader,
        AwkRecordProcessor recordProcessor,
        CancellationToken cancellationToken)
    {
        _context = new AwkRecordSeparatorContext(
            state,
            descriptorReader,
            recordProcessor,
            cancellationToken);
    }

    public async ValueTask<AwkStatementFlow> ProcessDescriptorAsync(
        AwkFileDescriptor descriptor,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        var strategy = _context.GetCurrentStrategy();
        return await strategy
            .ProcessDescriptorAsync(descriptor, processRecordAsync)
            .ConfigureAwait(false);
    }
}

internal sealed class AwkRecordSeparatorContext(
    AwkRuntimeState state,
    AwkRecordDescriptorReader descriptorReader,
    AwkRecordProcessor recordProcessor,
    CancellationToken cancellationToken)
{
    private AwkRecordSeparatorStrategyCache _strategyCache;

    public AwkRuntimeState State { get; } = state;

    public AwkRecordDescriptorReader DescriptorReader { get; } = descriptorReader;

    public AwkRecordProcessor RecordProcessor { get; } = recordProcessor;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public AwkRecordSeparatorStrategy GetCurrentStrategy()
    {
        var version = State.RecordSeparatorVersion;
        if (_strategyCache.HasStrategy && _strategyCache.Version == version)
            return _strategyCache.Strategy;

        var strategy = AwkRecordSeparatorStrategy.Create(State.ResolveRecordSeparator(), this);
        _strategyCache = new AwkRecordSeparatorStrategyCache(strategy, version);
        return strategy;
    }

    public async ValueTask<AwkStatementFlow> ProcessUnreadTailWithCurrentSeparatorAsync(
        AwkText content,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        if (content.ByteLength == 0)
            return AwkStatementFlow.Continue;

        var strategy = GetCurrentStrategy();
        return await strategy.ProcessTextContentAsync(content, processRecordAsync).ConfigureAwait(false);
    }

    public static bool IsTerminalFlow(AwkStatementFlow flow) =>
        flow is AwkStatementFlow.ExitProgram or AwkStatementFlow.NextFile;
}

internal readonly struct AwkRecordSeparatorStrategyCache(
    AwkRecordSeparatorStrategy strategy,
    long version)
{
    public AwkRecordSeparatorStrategy Strategy { get; } = strategy;

    public long Version { get; } = version;

    public bool HasStrategy { get; } = true;
}

internal abstract class AwkRecordSeparatorStrategy
{
    protected AwkRecordSeparatorStrategy(AwkRecordSeparatorContext context)
    {
        Context = context;
        SeparatorVersion = context.State.RecordSeparatorVersion;
    }

    protected AwkRecordSeparatorContext Context { get; }

    protected long SeparatorVersion { get; }

    protected bool IsCurrentSeparatorVersion =>
        Context.State.RecordSeparatorVersion == SeparatorVersion;

    public static AwkRecordSeparatorStrategy Create(string separatorText, AwkRecordSeparatorContext context)
    {
        if (separatorText.Length == 0)
            return new AwkParagraphRecordSeparatorStrategy(context);

        if (separatorText == "\n")
            return new AwkNewlineRecordSeparatorStrategy(context);

        if (TryGetSingleByteSeparator(separatorText, out var separatorByte))
            return new AwkSingleByteRecordSeparatorStrategy(separatorText, separatorByte, context);

        if (TryGetSimpleByteSetRegexSeparator(separatorText, out var separatorBytes))
            return new AwkByteSetRecordSeparatorStrategy(separatorBytes, context);

        if (separatorText.Length == 1)
            return new AwkSingleCharacterRecordSeparatorStrategy(separatorText, context);

        return new AwkRegexRecordSeparatorStrategy(separatorText, context);
    }

    /// <summary>Splits descriptor input using the captured separator and invokes the record callback in order.</summary>
    public abstract ValueTask<AwkStatementFlow> ProcessDescriptorAsync(
        AwkFileDescriptor descriptor,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync);

    /// <summary>Splits an in-memory input using the captured separator and invokes the record callback in order.</summary>
    public abstract ValueTask<AwkStatementFlow> ProcessTextContentAsync(
        AwkText content,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync);

    protected Task<AwkText> ReadBufferedTextToEndAsync(
        AwkUtf8Builder buffer,
        AwkFileDescriptor descriptor) =>
        ReadBufferedTextToEndAsync(buffer, descriptor, reachedEof: false);

    protected async Task<AwkText> ReadBufferedTextToEndAsync(
        AwkUtf8Builder buffer,
        AwkFileDescriptor descriptor,
        bool reachedEof)
    {
        var buffered = buffer.CopyToAwkText(Context.State.Counters);
        if (reachedEof)
            return buffered;

        var tail = await Context.DescriptorReader
            .ReadToEndTextAsync(descriptor, Context.State.Limits, Context.State.MemoryGovernor)
            .ConfigureAwait(false);
        return buffered.Concat(tail, Context.State.Counters, Context.State.MemoryGovernor);
    }

    protected async Task<bool> AppendNextByteBatchAsync(
        AwkFileDescriptor descriptor,
        AwkUtf8Builder buffer)
    {
        var batch = await Context.DescriptorReader.ReadNextBatchAsync(descriptor).ConfigureAwait(false);
        buffer.Append(batch.Content);
        return batch.ReachedEof;
    }

    protected static bool TryGetSingleByteSeparator(string separatorText, out byte separator)
    {
        if (separatorText.Length == 1 && separatorText[0] <= 0x7F && separatorText[0] != '\n')
        {
            separator = (byte)separatorText[0];
            return true;
        }

        separator = 0;
        return false;
    }

    protected static bool TryGetSimpleByteSetRegexSeparator(string separatorText, out bool[] separators)
    {
        static bool IsAsciiLetter(char value) =>
            value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

        separators = [];
        if (separatorText.Length < 3 ||
            separatorText[0] != '[' ||
            separatorText[^1] != ']')
        {
            return false;
        }

        var set = new bool[256];
        var count = 0;
        for (var index = 1; index < separatorText.Length - 1; index++)
        {
            var ch = separatorText[index];
            if (ch > 0x7F ||
                ch is '\\' or '-' or '^' or '[' or ']' ||
                IsAsciiLetter(ch))
            {
                return false;
            }

            var b = (byte)ch;
            if (!set[b])
            {
                set[b] = true;
                count++;
            }
        }

        if (count == 0)
            return false;

        separators = set;
        return true;
    }
}

internal sealed class AwkNewlineRecordSeparatorStrategy(
    AwkRecordSeparatorContext context) : AwkRecordSeparatorStrategy(context)
{
    public override async ValueTask<AwkStatementFlow> ProcessDescriptorAsync(
        AwkFileDescriptor descriptor,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        var content = await Context.DescriptorReader
            .ReadToEndTextAsync(descriptor, Context.State.Limits, Context.State.MemoryGovernor)
            .ConfigureAwait(false);
        return await ProcessTextContentAsync(content, processRecordAsync).ConfigureAwait(false);
    }

    public override async ValueTask<AwkStatementFlow> ProcessTextContentAsync(
        AwkText content,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        if (!IsCurrentSeparatorVersion)
            return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(content, processRecordAsync).ConfigureAwait(false);

        var start = 0;
        var byteLength = content.ByteLength;
        for (var index = 0; index < byteLength; index++)
        {
            Context.State.ThrowIfCancellationRequested();
            if (content.AsSpan()[index] != (byte)'\n')
                continue;

            var recordLength = index - start;
            if (recordLength > 0 && content.AsSpan()[index - 1] == (byte)'\r')
                recordLength--;

            var record = content.CopyByteSlice(
                start,
                recordLength,
                Context.State.Counters,
                Context.State.MemoryGovernor);
            var flow = await Context.RecordProcessor.ProcessAsync(
                    record,
                    AwkRecordTerminator.NewLine,
                    processRecordAsync)
                .ConfigureAwait(false);
            if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                return flow;

            start = index + 1;
            if (!IsCurrentSeparatorVersion)
            {
                var remaining = content.CopyByteSlice(
                    start,
                    byteLength - start,
                    Context.State.Counters,
                    Context.State.MemoryGovernor);
                return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(remaining, processRecordAsync)
                    .ConfigureAwait(false);
            }
        }

        if (start == byteLength)
            return AwkStatementFlow.Continue;

        var finalRecord = content.CopyByteSlice(
            start,
            byteLength - start,
            Context.State.Counters,
            Context.State.MemoryGovernor);
        return await Context.RecordProcessor.ProcessAsync(
                finalRecord,
                AwkRecordTerminator.Empty,
                processRecordAsync)
            .ConfigureAwait(false);
    }
}

internal abstract class AwkByteRecordSeparatorStrategy(
    AwkRecordSeparatorContext context) : AwkRecordSeparatorStrategy(context)
{
    public sealed override async ValueTask<AwkStatementFlow> ProcessDescriptorAsync(
        AwkFileDescriptor descriptor,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        using var buffer = new AwkUtf8Builder(
            Context.State.Limits,
            Context.State.MemoryGovernor,
            capacity: AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity);
        while (true)
        {
            Context.State.ThrowIfCancellationRequested();
            if (!IsCurrentSeparatorVersion)
            {
                var content = await ReadBufferedTextToEndAsync(buffer, descriptor).ConfigureAwait(false);
                return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(content, processRecordAsync)
                    .ConfigureAwait(false);
            }

            var batch = await Context.DescriptorReader.ReadNextBatchAsync(descriptor).ConfigureAwait(false);
            var batchContent = batch.Content;
            var segmentStart = 0;
            while (TryFindNextSeparator(batchContent.Span[segmentStart..], out var relativeIndex, out var separatorByte))
            {
                Context.State.ThrowIfCancellationRequested();
                var separatorIndex = segmentStart + relativeIndex;

                buffer.Append(batchContent.Slice(segmentStart, separatorIndex - segmentStart));
                var record = buffer.CopyToAwkText(Context.State.Counters);
                buffer.Clear();
                segmentStart = separatorIndex + 1;
                var flow = await Context.RecordProcessor.ProcessAsync(
                        record,
                        CreateTerminator(separatorByte),
                        processRecordAsync)
                    .ConfigureAwait(false);
                if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                    return flow;

                if (!IsCurrentSeparatorVersion)
                {
                    buffer.Append(batchContent.Slice(segmentStart));
                    var remaining = await ReadBufferedTextToEndAsync(buffer, descriptor).ConfigureAwait(false);
                    return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(remaining, processRecordAsync)
                        .ConfigureAwait(false);
                }
            }

            buffer.Append(batchContent.Slice(segmentStart));

            if (batch.ReachedEof)
            {
                if (buffer.Length == 0)
                    return AwkStatementFlow.Continue;

                return await Context.RecordProcessor.ProcessAsync(
                        buffer.CopyToAwkText(Context.State.Counters),
                        AwkRecordTerminator.Empty,
                        processRecordAsync)
                    .ConfigureAwait(false);
            }
        }
    }

    public sealed override async ValueTask<AwkStatementFlow> ProcessTextContentAsync(
        AwkText content,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        var byteLength = content.ByteLength;
        var start = 0;
        while (start < byteLength)
        {
            Context.State.ThrowIfCancellationRequested();
            if (!IsCurrentSeparatorVersion)
            {
                var tail = content.CopyByteSlice(
                    start,
                    byteLength - start,
                    Context.State.Counters,
                    Context.State.MemoryGovernor);
                return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(tail, processRecordAsync).ConfigureAwait(false);
            }

            if (!TryFindNextSeparator(content.AsSpan()[start..], out var relativeIndex, out var separatorByte))
                break;

            var separatorIndex = start + relativeIndex;

            var record = content.CopyByteSlice(
                start,
                separatorIndex - start,
                Context.State.Counters,
                Context.State.MemoryGovernor);
            var flow = await Context.RecordProcessor.ProcessAsync(
                    record,
                    CreateTerminator(separatorByte),
                    processRecordAsync)
                .ConfigureAwait(false);
            if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                return flow;

            start = separatorIndex + 1;
        }

        if (start >= byteLength)
            return AwkStatementFlow.Continue;

        return await Context.RecordProcessor.ProcessAsync(
                content.CopyByteSlice(
                    start,
                    byteLength - start,
                    Context.State.Counters,
                    Context.State.MemoryGovernor),
                AwkRecordTerminator.Empty,
                processRecordAsync)
            .ConfigureAwait(false);
    }

    private bool TryFindNextSeparator(
        ReadOnlySpan<byte> content,
        out int separatorIndex,
        out byte separatorByte)
    {
        const int searchWindowLength = 4 * 1024;
        var windowStart = 0;
        while (windowStart < content.Length)
        {
            Context.State.ThrowIfCancellationRequested();
            var windowLength = Math.Min(searchWindowLength, content.Length - windowStart);
            if (TryFindNextSeparatorCore(
                    content.Slice(windowStart, windowLength),
                    out var relativeIndex,
                    out separatorByte))
            {
                separatorIndex = windowStart + relativeIndex;
                return true;
            }

            windowStart += windowLength;
        }

        separatorIndex = -1;
        separatorByte = 0;
        return false;
    }

    /// <summary>Finds the first complete separator in a bounded byte window.</summary>
    /// <returns><see langword="true"/> only when both output values identify a separator in the window.</returns>
    protected abstract bool TryFindNextSeparatorCore(
        ReadOnlySpan<byte> content,
        out int separatorIndex,
        out byte separatorByte);

    /// <summary>Returns the byte-native <c>RT</c> value for a separator accepted by this strategy.</summary>
    protected abstract AwkRecordTerminator CreateTerminator(byte separatorByte);
}

internal sealed class AwkSingleByteRecordSeparatorStrategy(
    string separatorText,
    byte separatorByte,
    AwkRecordSeparatorContext context) : AwkByteRecordSeparatorStrategy(context)
{
    private readonly AwkRecordTerminator _terminator = AwkRecordTerminator.FromText(
        AwkText.FromString(separatorText, context.State.Counters));

    protected override bool TryFindNextSeparatorCore(
        ReadOnlySpan<byte> content,
        out int separatorIndex,
        out byte matchedSeparatorByte)
    {
        separatorIndex = content.IndexOf(separatorByte);
        matchedSeparatorByte = separatorByte;
        return separatorIndex >= 0;
    }

    protected override AwkRecordTerminator CreateTerminator(byte matchedSeparatorByte) => _terminator;
}

internal sealed class AwkByteSetRecordSeparatorStrategy : AwkByteRecordSeparatorStrategy
{
    private readonly bool[] _separatorBytes;
    private readonly AwkRecordTerminator[] _terminators;

    public AwkByteSetRecordSeparatorStrategy(
        bool[] separatorBytes,
        AwkRecordSeparatorContext context)
        : base(context)
    {
        _separatorBytes = separatorBytes;
        _terminators = new AwkRecordTerminator[separatorBytes.Length];
        for (var index = 0; index < separatorBytes.Length; index++)
        {
            if (separatorBytes[index])
            {
                _terminators[index] = AwkRecordTerminator.FromText(
                    AwkText.FromTrustedOwnedBytes([(byte)index], context.State.Counters));
            }
        }
    }

    protected override bool TryFindNextSeparatorCore(
        ReadOnlySpan<byte> content,
        out int separatorIndex,
        out byte separatorByte)
    {
        for (var index = 0; index < content.Length; index++)
        {
            var candidate = content[index];
            if (_separatorBytes[candidate])
            {
                separatorIndex = index;
                separatorByte = candidate;
                return true;
            }
        }

        separatorIndex = -1;
        separatorByte = 0;
        return false;
    }

    protected override AwkRecordTerminator CreateTerminator(byte separatorByte) =>
        _terminators[separatorByte];
}

internal sealed class AwkSingleCharacterRecordSeparatorStrategy(
    string separatorText,
    AwkRecordSeparatorContext context) : AwkRecordSeparatorStrategy(context)
{
    private readonly AwkText _separator = AwkText.FromString(separatorText, context.State.Counters);

    public override async ValueTask<AwkStatementFlow> ProcessDescriptorAsync(
        AwkFileDescriptor descriptor,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        using var buffer = new AwkUtf8Builder(
            Context.State.Limits,
            Context.State.MemoryGovernor,
            capacity: AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity);
        while (true)
        {
            Context.State.ThrowIfCancellationRequested();
            if (!IsCurrentSeparatorVersion)
            {
                var remaining = await ReadBufferedTextToEndAsync(buffer, descriptor).ConfigureAwait(false);
                return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(remaining, processRecordAsync)
                    .ConfigureAwait(false);
            }

            var batch = await Context.DescriptorReader.ReadNextBatchAsync(descriptor).ConfigureAwait(false);
            buffer.Append(batch.Content);

            while (TryFindSeparator(buffer.AsSpan(), out var separatorIndex))
            {
                var record = AwkText.FromOwnedBytes(
                    buffer.AsSpan()[..separatorIndex].ToArray(),
                    Context.State.Counters,
                    Context.State.MemoryGovernor);
                buffer.RemovePrefix(separatorIndex + _separator.ByteLength);
                var flow = await Context.RecordProcessor.ProcessAsync(
                        record,
                        AwkRecordTerminator.FromText(_separator),
                        processRecordAsync)
                    .ConfigureAwait(false);
                if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                    return flow;

                if (!IsCurrentSeparatorVersion)
                {
                    var remaining = await ReadBufferedTextToEndAsync(buffer, descriptor).ConfigureAwait(false);
                    return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(remaining, processRecordAsync)
                        .ConfigureAwait(false);
                }
            }

            if (batch.ReachedEof)
            {
                if (buffer.Length == 0)
                    return AwkStatementFlow.Continue;

                return await Context.RecordProcessor.ProcessAsync(
                        buffer.CopyToAwkText(Context.State.Counters),
                        AwkRecordTerminator.Empty,
                        processRecordAsync)
                    .ConfigureAwait(false);
            }
        }
    }

    public override async ValueTask<AwkStatementFlow> ProcessTextContentAsync(
        AwkText content,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        var byteLength = content.ByteLength;
        var start = 0;
        while (start < byteLength)
        {
            Context.State.ThrowIfCancellationRequested();
            if (!IsCurrentSeparatorVersion)
            {
                var tail = content.CopyByteSlice(
                    start,
                    byteLength - start,
                    Context.State.Counters,
                    Context.State.MemoryGovernor);
                return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(tail, processRecordAsync).ConfigureAwait(false);
            }

            var remaining = content.AsSpan()[start..];
            var next = remaining.IndexOf(_separator.AsSpan());
            var recordLength = next < 0 ? remaining.Length : next;
            var record = content.CopyByteSlice(
                start,
                recordLength,
                Context.State.Counters,
                Context.State.MemoryGovernor);
            start = next < 0 ? byteLength : start + next + _separator.ByteLength;

            var terminator = next < 0
                ? AwkRecordTerminator.Empty
                : AwkRecordTerminator.FromText(_separator);
            var flow = await Context.RecordProcessor.ProcessAsync(record, terminator, processRecordAsync)
                .ConfigureAwait(false);
            if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                return flow;
        }

        return AwkStatementFlow.Continue;
    }

    private bool TryFindSeparator(ReadOnlySpan<byte> content, out int index)
    {
        index = content.IndexOf(_separator.AsSpan());
        return index >= 0;
    }

}

internal sealed class AwkRegexRecordSeparatorStrategy(
    string separatorText,
    AwkRecordSeparatorContext context) : AwkRecordSeparatorStrategy(context)
{
    private readonly Regex _separator = context.State.CreateRegex(new AwkRegexPattern(separatorText));

    public override async ValueTask<AwkStatementFlow> ProcessDescriptorAsync(
        AwkFileDescriptor descriptor,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        using var buffer = new AwkUtf8Builder(
            Context.State.Limits,
            Context.State.MemoryGovernor,
            capacity: AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity);
        var reachedEof = false;
        while (true)
        {
            Context.State.ThrowIfCancellationRequested();
            if (!IsCurrentSeparatorVersion)
            {
                var remainingContent = await ReadBufferedTextToEndAsync(buffer, descriptor, reachedEof).ConfigureAwait(false);
                return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(remainingContent, processRecordAsync)
                    .ConfigureAwait(false);
            }

            if (buffer.Length == 0 && !reachedEof)
            {
                reachedEof = await AppendNextByteBatchAsync(descriptor, buffer).ConfigureAwait(false);
                continue;
            }

            if (buffer.Length == 0)
                return AwkStatementFlow.Continue;

            var content = buffer.CopyToAwkText(Context.State.Counters);
            var match = Context.State.MatchRegexSpan(_separator, content);
            if (match.Success)
            {
                var record = content.CopyByteSlice(0, match.ByteIndex, Context.State.Counters, Context.State.MemoryGovernor);
                var terminator = CreateTerminator(content, match);
                buffer.RemovePrefix(AdvanceAfterRegexSeparator(content, match.ByteIndex, match.ByteLength));
                var flow = await Context.RecordProcessor.ProcessAsync(record, terminator, processRecordAsync)
                    .ConfigureAwait(false);
                if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                    return flow;

                continue;
            }

            if (reachedEof)
            {
                buffer.Clear();
                return await Context.RecordProcessor.ProcessAsync(content, AwkRecordTerminator.Empty, processRecordAsync)
                    .ConfigureAwait(false);
            }

            reachedEof = await AppendNextByteBatchAsync(descriptor, buffer).ConfigureAwait(false);
        }
    }

    public override async ValueTask<AwkStatementFlow> ProcessTextContentAsync(
        AwkText content,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        var remaining = content;
        while (remaining.ByteLength > 0)
        {
            Context.State.ThrowIfCancellationRequested();
            if (!IsCurrentSeparatorVersion)
            {
                return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(remaining, processRecordAsync)
                    .ConfigureAwait(false);
            }

            var match = Context.State.MatchRegexSpan(_separator, remaining);
            var record = match.Success
                ? remaining.CopyByteSlice(0, match.ByteIndex, Context.State.Counters, Context.State.MemoryGovernor)
                : remaining;
            var terminator = match.Success
                ? CreateTerminator(remaining, match)
                : AwkRecordTerminator.Empty;
            remaining = match.Success
                ? SliceAfterRegexSeparator(remaining, match.ByteIndex, match.ByteLength)
                : AwkText.Empty;

            var flow = await Context.RecordProcessor.ProcessAsync(record, terminator, processRecordAsync)
                .ConfigureAwait(false);
            if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                return flow;
        }

        return AwkStatementFlow.Continue;
    }

    private AwkText SliceAfterRegexSeparator(AwkText content, int byteIndex, int byteLength)
    {
        var next = AdvanceAfterRegexSeparator(content, byteIndex, byteLength);
        return next >= content.ByteLength
            ? AwkText.Empty
            : content.CopyByteSlice(
                next,
                content.ByteLength - next,
                Context.State.Counters,
                Context.State.MemoryGovernor);
    }

    private AwkRecordTerminator CreateTerminator(AwkText content, AwkRegexMatchSpan match)
    {
        if (match.ByteLength == 0)
            return AwkRecordTerminator.Empty;

        return AwkRecordTerminator.FromText(content.CopyByteSlice(
            match.ByteIndex,
            match.ByteLength,
            Context.State.Counters,
            Context.State.MemoryGovernor));
    }

    private static int AdvanceAfterRegexSeparator(AwkText content, int byteIndex, int byteLength)
    {
        if (byteLength > 0 || byteIndex >= content.ByteLength)
            return byteIndex + byteLength;

        var status = Rune.DecodeFromUtf8(content.AsSpan()[byteIndex..], out _, out var bytesConsumed);
        return byteIndex + (status == OperationStatus.Done && bytesConsumed > 0 ? bytesConsumed : 1);
    }

}

internal sealed class AwkParagraphRecordSeparatorStrategy(
    AwkRecordSeparatorContext context) : AwkRecordSeparatorStrategy(context)
{
    public override async ValueTask<AwkStatementFlow> ProcessDescriptorAsync(
        AwkFileDescriptor descriptor,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        using var buffer = new AwkUtf8Builder(
            Context.State.Limits,
            Context.State.MemoryGovernor,
            capacity: AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity);
        var reachedEof = false;
        while (true)
        {
            Context.State.ThrowIfCancellationRequested();
            if (!IsCurrentSeparatorVersion)
            {
                var content = await ReadBufferedTextToEndAsync(buffer, descriptor, reachedEof).ConfigureAwait(false);
                return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(content, processRecordAsync)
                    .ConfigureAwait(false);
            }

            RemoveLeadingParagraphSeparators(buffer);
            if (buffer.Length == 0 && !reachedEof)
            {
                reachedEof = await AppendNextByteBatchAsync(descriptor, buffer).ConfigureAwait(false);
                continue;
            }

            if (TryFindParagraphSeparator(buffer.AsSpan(), out var separatorIndex, out var separatorLength))
            {
                var record = CreateParagraphRecord(
                    buffer,
                    separatorIndex,
                    Context.State.Counters,
                    Context.State.MemoryGovernor);
                var terminator = CreateParagraphTerminator(
                    buffer,
                    separatorIndex,
                    separatorLength,
                    Context.State.Counters,
                    Context.State.MemoryGovernor);
                buffer.RemovePrefix(separatorIndex + separatorLength);
                var flow = await Context.RecordProcessor.ProcessParagraphAsync(
                        record,
                        terminator,
                        processRecordAsync)
                    .ConfigureAwait(false);
                if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                    return flow;

                if (!IsCurrentSeparatorVersion)
                {
                    var content = await ReadBufferedTextToEndAsync(buffer, descriptor, reachedEof).ConfigureAwait(false);
                    return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(content, processRecordAsync)
                        .ConfigureAwait(false);
                }

                continue;
            }

            if (reachedEof)
            {
                if (buffer.Length == 0)
                    return AwkStatementFlow.Continue;

                var record = CreateFinalParagraphRecord(buffer, Context.State.Counters, Context.State.MemoryGovernor);
                buffer.Clear();
                return await Context.RecordProcessor.ProcessParagraphAsync(
                        record,
                        AwkRecordTerminator.Empty,
                        processRecordAsync)
                    .ConfigureAwait(false);
            }

            reachedEof = await AppendNextByteBatchAsync(descriptor, buffer).ConfigureAwait(false);
        }
    }

    public override async ValueTask<AwkStatementFlow> ProcessTextContentAsync(
        AwkText content,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        using var buffer = new AwkUtf8Builder(
            Context.State.Limits,
            Context.State.MemoryGovernor,
            capacity: content.ByteLength);
        buffer.Append(content);
        while (true)
        {
            Context.State.ThrowIfCancellationRequested();
            if (!IsCurrentSeparatorVersion)
            {
                return await Context.ProcessUnreadTailWithCurrentSeparatorAsync(
                        buffer.CopyToAwkText(Context.State.Counters),
                        processRecordAsync)
                    .ConfigureAwait(false);
            }

            RemoveLeadingParagraphSeparators(buffer);
            if (buffer.Length == 0)
                return AwkStatementFlow.Continue;

            if (TryFindParagraphSeparator(buffer.AsSpan(), out var separatorIndex, out var separatorLength))
            {
                var record = CreateParagraphRecord(
                    buffer,
                    separatorIndex,
                    Context.State.Counters,
                    Context.State.MemoryGovernor);
                var terminator = CreateParagraphTerminator(
                    buffer,
                    separatorIndex,
                    separatorLength,
                    Context.State.Counters,
                    Context.State.MemoryGovernor);
                buffer.RemovePrefix(separatorIndex + separatorLength);
                var flow = await Context.RecordProcessor.ProcessParagraphAsync(record, terminator, processRecordAsync)
                    .ConfigureAwait(false);
                if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                    return flow;

                continue;
            }

            var final = CreateFinalParagraphRecord(buffer, Context.State.Counters, Context.State.MemoryGovernor);
            buffer.Clear();
            return await Context.RecordProcessor.ProcessParagraphAsync(
                    final,
                    AwkRecordTerminator.Empty,
                    processRecordAsync)
                .ConfigureAwait(false);
        }
    }

    private static void RemoveLeadingParagraphSeparators(AwkUtf8Builder buffer)
    {
        var span = buffer.AsSpan();
        var removeLength = 0;
        while (TryReadNewline(span, removeLength, out var newlineLength))
            removeLength += newlineLength;

        buffer.RemovePrefix(removeLength);
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

    private static AwkText CreateParagraphRecord(
        AwkUtf8Builder buffer,
        int separatorIndex,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        var recordEnd = TrimTrailingRecordNewlines(buffer.AsSpan()[..separatorIndex]);
        return AwkText.FromOwnedBytes(buffer.AsSpan()[..recordEnd].ToArray(), counters, governor);
    }

    private static AwkText CreateFinalParagraphRecord(
        AwkUtf8Builder buffer,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        var recordEnd = TrimTrailingRecordNewlines(buffer.AsSpan());
        return AwkText.FromOwnedBytes(buffer.AsSpan()[..recordEnd].ToArray(), counters, governor);
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

    private static AwkRecordTerminator CreateParagraphTerminator(
        AwkUtf8Builder buffer,
        int separatorIndex,
        int separatorLength,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        return AwkRecordTerminator.FromText(AwkText.FromOwnedBytes(
            buffer.AsSpan().Slice(separatorIndex, separatorLength).ToArray(),
            counters,
            governor));
    }

}
