using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRecordReader
{
    private readonly AwkRuntimeState _state;
    private readonly AwkIo _io;
    private readonly CancellationToken _cancellationToken;
    private readonly AwkRecordProcessor _recordProcessor;
    private readonly AwkRecordDescriptorReader _descriptorReader;
    private readonly AwkRecordSeparatorEngine _separatorEngine;

    public AwkRecordReader(
        IAwkHost host,
        AwkRuntimeState state,
        AwkIo io,
        CancellationToken cancellationToken)
    {
        _state = state;
        _io = io;
        _cancellationToken = cancellationToken;
        _recordProcessor = new AwkRecordProcessor(state);
        _descriptorReader = new AwkRecordDescriptorReader(host, cancellationToken, state.Counters, state.Resources);
        _separatorEngine = new AwkRecordSeparatorEngine(
            state,
            _descriptorReader,
            _recordProcessor,
            cancellationToken);
    }

    public async ValueTask<AwkStatementFlow> ProcessDescriptorAsync(
        AwkFileDescriptor descriptor,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        if (_state.ResolveRecordSeparator() is { } separator && separator != "\n")
            return await _separatorEngine.ProcessDescriptorAsync(
                    descriptor,
                    processRecordAsync)
                .ConfigureAwait(false);

        // Default newline records stay on AsyncLineReader and stream one host batch at a time.
        var reader = _descriptorReader.CreateLineReader(descriptor);
        _io.BindMainInputReader(reader);
        return await ProcessReaderAsync(reader, processRecordAsync).ConfigureAwait(false);
    }

    public async ValueTask<AwkStatementFlow> ProcessReaderAsync(
        AsyncLineReader reader,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        _io.BindMainInputReader(reader);
        while (!await reader.ReachedEof().ConfigureAwait(false))
        {
            _state.ThrowIfCancellationRequested();

            var line = AwkRecordLine.ReadUtf8DefaultNewline(reader.ReadLine(), _state.Counters);
            var flow = await _recordProcessor.ProcessAsync(line.Record, line.Terminator, processRecordAsync)
                .ConfigureAwait(false);
            if (flow is AwkStatementFlow.ExitProgram or AwkStatementFlow.NextFile)
            {
                _io.ClearMainInputReader();
                return flow;
            }
        }

        _io.ClearMainInputReader();
        return AwkStatementFlow.Continue;
    }
}
