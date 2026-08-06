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

    public async ValueTask<AwkStatementFlow> ProcessCustomTextContentAsync(
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
    protected AwkRecordSeparatorStrategy(string separatorText, AwkRecordSeparatorContext context)
    {
        SeparatorText = separatorText;
        Context = context;
        SeparatorVersion = context.State.RecordSeparatorVersion;
    }

    protected string SeparatorText { get; }

    protected AwkRecordSeparatorContext Context { get; }

    protected long SeparatorVersion { get; }

    protected bool IsCurrentSeparatorVersion =>
        Context.State.RecordSeparatorVersion == SeparatorVersion;

    public static AwkRecordSeparatorStrategy Create(string separatorText, AwkRecordSeparatorContext context)
    {
        if (separatorText.Length == 0)
            return new AwkParagraphRecordSeparatorStrategy(separatorText, context);

        if (separatorText == "\n")
            return new AwkNewlineRecordSeparatorStrategy(separatorText, context);

        if (TryGetSingleByteSeparator(separatorText, out var separatorByte))
            return new AwkSingleByteRecordSeparatorStrategy(separatorText, separatorByte, context);

        if (TryGetSimpleByteSetRegexSeparator(separatorText, out var separatorBytes))
            return new AwkByteSetRecordSeparatorStrategy(separatorText, separatorBytes, context);

        if (separatorText.Length == 1)
            return new AwkSingleCharacterRecordSeparatorStrategy(separatorText, context);

        return new AwkRegexRecordSeparatorStrategy(separatorText, context);
    }

    public abstract ValueTask<AwkStatementFlow> ProcessDescriptorAsync(
        AwkFileDescriptor descriptor,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync);

    public abstract ValueTask<AwkStatementFlow> ProcessTextContentAsync(
        AwkText content,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync);

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

    private static bool IsAsciiLetter(char ch) =>
        ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
}

internal sealed class AwkNewlineRecordSeparatorStrategy(
    string separatorText,
    AwkRecordSeparatorContext context) : AwkRecordSeparatorStrategy(separatorText, context)
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
            return await Context.ProcessCustomTextContentAsync(content, processRecordAsync).ConfigureAwait(false);

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
                return await Context.ProcessCustomTextContentAsync(remaining, processRecordAsync)
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

internal sealed class AwkSingleByteRecordSeparatorStrategy(
    string separatorText,
    byte separatorByte,
    AwkRecordSeparatorContext context) : AwkRecordSeparatorStrategy(separatorText, context)
{
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
                var content = await ReadBufferedTextToEndAsync(buffer, descriptor).ConfigureAwait(false);
                return await Context.ProcessCustomTextContentAsync(content, processRecordAsync)
                    .ConfigureAwait(false);
            }

            var batch = await Context.DescriptorReader.ReadNextBatchAsync(descriptor).ConfigureAwait(false);
            var batchContent = batch.Content;
            var segmentStart = 0;
            for (var index = 0; index < batchContent.Length; index++)
            {
                Context.State.ThrowIfCancellationRequested();
                if (batchContent.Span[index] != separatorByte)
                    continue;

                buffer.Append(batchContent.Slice(segmentStart, index - segmentStart));
                var record = buffer.CopyToAwkText(Context.State.Counters);
                buffer.Clear();
                segmentStart = index + 1;
                var flow = await Context.RecordProcessor.ProcessAsync(
                        record,
                        AwkRecordTerminator.FromText(SeparatorText),
                        processRecordAsync)
                    .ConfigureAwait(false);
                if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                    return flow;

                if (!IsCurrentSeparatorVersion)
                {
                    buffer.Append(batchContent.Slice(segmentStart));
                    var remaining = await ReadBufferedTextToEndAsync(buffer, descriptor).ConfigureAwait(false);
                    return await Context.ProcessCustomTextContentAsync(remaining, processRecordAsync)
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

    public override async ValueTask<AwkStatementFlow> ProcessTextContentAsync(
        AwkText content,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        if (!IsCurrentSeparatorVersion)
            return await Context.ProcessCustomTextContentAsync(content, processRecordAsync).ConfigureAwait(false);

        var byteLength = content.ByteLength;
        var start = 0;
        for (var index = 0; index < byteLength; index++)
        {
            Context.State.ThrowIfCancellationRequested();
            if (content.AsSpan()[index] != separatorByte)
                continue;

            var record = content.CopyByteSlice(
                start,
                index - start,
                Context.State.Counters,
                Context.State.MemoryGovernor);
            var flow = await Context.RecordProcessor.ProcessAsync(
                    record,
                    AwkRecordTerminator.FromText(SeparatorText),
                    processRecordAsync)
                .ConfigureAwait(false);
            if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                return flow;

            start = index + 1;
            if (!IsCurrentSeparatorVersion)
            {
                var tail = content.CopyByteSlice(
                    start,
                    byteLength - start,
                    Context.State.Counters,
                    Context.State.MemoryGovernor);
                return await Context.ProcessCustomTextContentAsync(tail, processRecordAsync).ConfigureAwait(false);
            }
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

    private async Task<AwkText> ReadBufferedTextToEndAsync(
        AwkUtf8Builder buffer,
        AwkFileDescriptor descriptor)
    {
        var buffered = buffer.CopyToAwkText(Context.State.Counters);
        var tail = await Context.DescriptorReader
            .ReadToEndTextAsync(descriptor, Context.State.Limits, Context.State.MemoryGovernor)
            .ConfigureAwait(false);
        return buffered.Concat(tail, Context.State.Counters, Context.State.MemoryGovernor);
    }
}

internal sealed class AwkByteSetRecordSeparatorStrategy(
    string separatorText,
    bool[] separatorBytes,
    AwkRecordSeparatorContext context) : AwkRecordSeparatorStrategy(separatorText, context)
{
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
                var content = await ReadBufferedTextToEndAsync(buffer, descriptor).ConfigureAwait(false);
                return await Context.ProcessCustomTextContentAsync(content, processRecordAsync)
                    .ConfigureAwait(false);
            }

            var batch = await Context.DescriptorReader.ReadNextBatchAsync(descriptor).ConfigureAwait(false);
            var batchContent = batch.Content;
            var segmentStart = 0;
            for (var index = 0; index < batchContent.Length; index++)
            {
                Context.State.ThrowIfCancellationRequested();
                var separatorByte = batchContent.Span[index];
                if (!separatorBytes[separatorByte])
                    continue;

                buffer.Append(batchContent.Slice(segmentStart, index - segmentStart));
                var record = buffer.CopyToAwkText(Context.State.Counters);
                buffer.Clear();
                segmentStart = index + 1;
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
                    return await Context.ProcessCustomTextContentAsync(remaining, processRecordAsync)
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

    public override async ValueTask<AwkStatementFlow> ProcessTextContentAsync(
        AwkText content,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        var byteLength = content.ByteLength;
        var start = 0;
        for (var index = 0; index < byteLength; index++)
        {
            Context.State.ThrowIfCancellationRequested();
            if (!IsCurrentSeparatorVersion)
            {
                var tail = content.CopyByteSlice(
                    start,
                    byteLength - start,
                    Context.State.Counters,
                    Context.State.MemoryGovernor);
                return await Context.ProcessCustomTextContentAsync(tail, processRecordAsync).ConfigureAwait(false);
            }

            var separatorByte = content.AsSpan()[index];
            if (!separatorBytes[separatorByte])
                continue;

            var record = content.CopyByteSlice(
                start,
                index - start,
                Context.State.Counters,
                Context.State.MemoryGovernor);
            var flow = await Context.RecordProcessor.ProcessAsync(
                    record,
                    CreateTerminator(separatorByte),
                    processRecordAsync)
                .ConfigureAwait(false);
            if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                return flow;

            start = index + 1;
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

    private static AwkRecordTerminator CreateTerminator(byte separatorByte) =>
        AwkRecordTerminator.FromText(((char)separatorByte).ToString());

    private async Task<AwkText> ReadBufferedTextToEndAsync(
        AwkUtf8Builder buffer,
        AwkFileDescriptor descriptor)
    {
        var buffered = buffer.CopyToAwkText(Context.State.Counters);
        var tail = await Context.DescriptorReader
            .ReadToEndTextAsync(descriptor, Context.State.Limits, Context.State.MemoryGovernor)
            .ConfigureAwait(false);
        return buffered.Concat(tail, Context.State.Counters, Context.State.MemoryGovernor);
    }
}

internal sealed class AwkSingleCharacterRecordSeparatorStrategy(
    string separatorText,
    AwkRecordSeparatorContext context) : AwkRecordSeparatorStrategy(separatorText, context)
{
    private readonly byte[] _separatorBytes = AwkUtf8Codec.Encode(separatorText, context.State.Counters).ToArray();

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
                return await Context.ProcessCustomTextContentAsync(remaining, processRecordAsync)
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
                buffer.RemovePrefix(separatorIndex + _separatorBytes.Length);
                var flow = await Context.RecordProcessor.ProcessAsync(
                        record,
                        AwkRecordTerminator.FromText(SeparatorText),
                        processRecordAsync)
                    .ConfigureAwait(false);
                if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                    return flow;

                if (!IsCurrentSeparatorVersion)
                {
                    var remaining = await ReadBufferedTextToEndAsync(buffer, descriptor).ConfigureAwait(false);
                    return await Context.ProcessCustomTextContentAsync(remaining, processRecordAsync)
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
                return await Context.ProcessCustomTextContentAsync(tail, processRecordAsync).ConfigureAwait(false);
            }

            var remaining = content.AsSpan()[start..];
            var next = remaining.IndexOf(_separatorBytes);
            var recordLength = next < 0 ? remaining.Length : next;
            var record = content.CopyByteSlice(
                start,
                recordLength,
                Context.State.Counters,
                Context.State.MemoryGovernor);
            start = next < 0 ? byteLength : start + next + _separatorBytes.Length;

            var terminator = next < 0
                ? AwkRecordTerminator.Empty
                : AwkRecordTerminator.FromText(SeparatorText);
            var flow = await Context.RecordProcessor.ProcessAsync(record, terminator, processRecordAsync)
                .ConfigureAwait(false);
            if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                return flow;
        }

        return AwkStatementFlow.Continue;
    }

    private bool TryFindSeparator(ReadOnlySpan<byte> content, out int index)
    {
        index = content.IndexOf(_separatorBytes);
        return index >= 0;
    }

    private async Task<AwkText> ReadBufferedTextToEndAsync(
        AwkUtf8Builder buffer,
        AwkFileDescriptor descriptor)
    {
        var buffered = buffer.CopyToAwkText(Context.State.Counters);
        var tail = await Context.DescriptorReader
            .ReadToEndTextAsync(descriptor, Context.State.Limits, Context.State.MemoryGovernor)
            .ConfigureAwait(false);
        return buffered.Concat(tail, Context.State.Counters, Context.State.MemoryGovernor);
    }
}

internal sealed class AwkRegexRecordSeparatorStrategy(
    string separatorText,
    AwkRecordSeparatorContext context) : AwkRecordSeparatorStrategy(separatorText, context)
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
                return await Context.ProcessCustomTextContentAsync(remainingContent, processRecordAsync)
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
                return await Context.ProcessCustomTextContentAsync(remaining, processRecordAsync)
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

        var text = AwkUtf8Codec.DecodeRuntimeText(
            content.AsSpan().Slice(match.ByteIndex, match.ByteLength),
            Context.State.Counters);
        return AwkRecordTerminator.FromText(text);
    }

    private static int AdvanceAfterRegexSeparator(AwkText content, int byteIndex, int byteLength)
    {
        if (byteLength > 0 || byteIndex >= content.ByteLength)
            return byteIndex + byteLength;

        var status = Rune.DecodeFromUtf8(content.AsSpan()[byteIndex..], out _, out var bytesConsumed);
        return byteIndex + (status == OperationStatus.Done && bytesConsumed > 0 ? bytesConsumed : 1);
    }

    private async Task<AwkText> ReadBufferedTextToEndAsync(
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

    private async Task<bool> AppendNextByteBatchAsync(
        AwkFileDescriptor descriptor,
        AwkUtf8Builder buffer)
    {
        var batch = await Context.DescriptorReader.ReadNextBatchAsync(descriptor).ConfigureAwait(false);
        buffer.Append(batch.Content);

        return batch.ReachedEof;
    }
}

internal sealed class AwkParagraphRecordSeparatorStrategy(
    string separatorText,
    AwkRecordSeparatorContext context) : AwkRecordSeparatorStrategy(separatorText, context)
{
    private static readonly Regex Separator =
        new(@"\r?\n[ \t]*\r?\n+", RegexOptions.None, AwkRegex.MatchTimeout);

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
                return await Context.ProcessCustomTextContentAsync(content, processRecordAsync)
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
                var terminator = CreateParagraphTerminator(buffer, separatorIndex, separatorLength, Context.State.Counters);
                buffer.RemovePrefix(separatorIndex + separatorLength);
                var flow = await Context.RecordProcessor.ProcessAsync(
                        record,
                        terminator,
                        processRecordAsync)
                    .ConfigureAwait(false);
                if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                    return flow;

                if (!IsCurrentSeparatorVersion)
                {
                    var content = await ReadBufferedTextToEndAsync(buffer, descriptor, reachedEof).ConfigureAwait(false);
                    return await Context.ProcessCustomTextContentAsync(content, processRecordAsync)
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
                return await Context.RecordProcessor.ProcessAsync(
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
                return await Context.ProcessCustomTextContentAsync(
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
                var terminator = CreateParagraphTerminator(buffer, separatorIndex, separatorLength, Context.State.Counters);
                buffer.RemovePrefix(separatorIndex + separatorLength);
                var flow = await Context.RecordProcessor.ProcessAsync(record, terminator, processRecordAsync)
                    .ConfigureAwait(false);
                if (AwkRecordSeparatorContext.IsTerminalFlow(flow))
                    return flow;

                continue;
            }

            var final = CreateFinalParagraphRecord(buffer, Context.State.Counters, Context.State.MemoryGovernor);
            buffer.Clear();
            return await Context.RecordProcessor.ProcessAsync(
                    final,
                    AwkRecordTerminator.Empty,
                    processRecordAsync)
                .ConfigureAwait(false);
        }
    }

    private async Task<AwkText> ReadBufferedTextToEndAsync(
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

    private static void RemoveLeadingParagraphSeparators(AwkUtf8Builder buffer)
    {
        var span = buffer.AsSpan();
        var removeLength = 0;
        while (removeLength < span.Length && span[removeLength] is (byte)'\r' or (byte)'\n')
        {
            removeLength++;
        }

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
            while (position < content.Length && content[position] is (byte)' ' or (byte)'\t')
                position++;

            if (!TryReadNewline(content, position, out var secondNewlineLength))
                continue;

            position += secondNewlineLength;
            while (position < content.Length && content[position] == (byte)'\n')
                position++;

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
        while (end > 0 && content[end - 1] is (byte)'\r' or (byte)'\n')
            end--;

        return end;
    }

    private static AwkRecordTerminator CreateParagraphTerminator(
        AwkUtf8Builder buffer,
        int separatorIndex,
        int separatorLength,
        AwkRuntimeCounters counters)
    {
        var text = AwkUtf8Codec.DecodeRuntimeText(
            buffer.AsSpan().Slice(separatorIndex, separatorLength).ToArray(),
            counters);
        return AwkRecordTerminator.FromText(text);
    }

    private async Task<bool> AppendNextByteBatchAsync(
        AwkFileDescriptor descriptor,
        AwkUtf8Builder buffer)
    {
        var batch = await Context.DescriptorReader.ReadNextBatchAsync(descriptor).ConfigureAwait(false);
        buffer.Append(batch.Content);

        return batch.ReachedEof;
    }
}
