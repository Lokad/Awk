using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRecordReader
{
    private readonly AwkRuntimeState _state;
    private readonly AwkIo _io;
    private readonly AwkRecordProcessor _recordProcessor;
    private readonly AwkRecordDescriptorReader _descriptorReader;

    public AwkRecordReader(
        IAwkHost host,
        AwkRuntimeState state,
        AwkIo io,
        CancellationToken cancellationToken)
    {
        _state = state;
        _io = io;
        _recordProcessor = new AwkRecordProcessor(state);
        _descriptorReader = new AwkRecordDescriptorReader(host, cancellationToken, state.Counters, state.Resources);
    }

    public async ValueTask<AwkStatementFlow> ProcessDescriptorAsync(
        AwkFileDescriptor descriptor,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        using var cursor = new AwkRecordCursor(
            _state,
            _descriptorReader,
            _descriptorReader.CreateLineReader(descriptor));
        return await ProcessCursorAsync(cursor, processRecordAsync).ConfigureAwait(false);
    }

    public async ValueTask<AwkStatementFlow> ProcessCursorAsync(
        AwkRecordCursor cursor,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        _io.BindMainInputReader(cursor);
        while (true)
        {
            _state.ThrowIfCancellationRequested();
            var input = await cursor.ReadAsync().ConfigureAwait(false);
            if (!input.HasRecord)
            {
                _io.ClearMainInputReader();
                return AwkStatementFlow.Continue;
            }

            var flow = input.FieldSplittingMode == AwkRecordFieldSplittingMode.Paragraph
                ? await _recordProcessor.ProcessParagraphAsync(
                        input.RecordText,
                        input.Terminator,
                        processRecordAsync)
                    .ConfigureAwait(false)
                : await _recordProcessor.ProcessAsync(
                        input.RecordText,
                        input.Terminator,
                        processRecordAsync)
                    .ConfigureAwait(false);
            if (flow is AwkStatementFlow.ExitProgram or AwkStatementFlow.NextFile)
            {
                _io.ClearMainInputReader();
                return flow;
            }
        }
    }
}
