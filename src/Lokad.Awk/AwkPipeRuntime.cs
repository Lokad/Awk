using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkPipeRuntime(
    AwkIoHostAdapter host,
    AwkMemoryAccounting memoryAccounting,
    int maxOutputRedirections,
    int maxPipes,
    Func<int> outputFileCount,
    Func<AwkFileDescriptor, AwkRecordCursor> createRecordCursor,
    Func<long> nextRedirectionOrder)
{
    private readonly Dictionary<string, AwkOutputPipe> _outputPipes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AwkInputPipe> _inputPipes = new(StringComparer.Ordinal);

    public int OutputPipeCount => _outputPipes.Count;

    public AwkOpenRedirection GetRedirection(string command)
    {
        var output = _outputPipes.TryGetValue(command, out var written)
            ? new AwkOpenRedirection(AwkOpenRedirectionKind.OutputPipe, written.Order) : default;
        var input = _inputPipes.TryGetValue(command, out var read)
            ? new AwkOpenRedirection(AwkOpenRedirectionKind.InputPipe, read.Order) : default;
        return output.Newer(input);
    }

    public async Task WriteOutputAsync(AwkRedirectionPath command, ReadOnlyMemory<byte> content)
    {
        var pipe = await GetOrOpenOutputPipeAsync(command.Text).ConfigureAwait(false);
        await host.AppendAsync(pipe.WriteDescriptor, content).ConfigureAwait(false);

        async Task<AwkOutputPipe> GetOrOpenOutputPipeAsync(string command)
        {
            if (_outputPipes.TryGetValue(command, out var existing))
                return existing;

            ThrowIfPipeLimitReached();

            if (outputFileCount() + _outputPipes.Count >= maxOutputRedirections)
                throw new AwkRuntimeException(AwkMessages.ErrorOutputFileLimit, AwkDiagnosticId.HostIoFailure);

            var descriptors = await host.CreatePipeAsync(AwkPipeMode.Pipeline).ConfigureAwait(false);
            var memory = memoryAccounting.CreateTransientLease();
            Task<AwkExitCode>? completion = null;
            try
            {
                var invocation = host.CreateOutputPipeCommand(command, descriptors.Read, memory);
                completion = host.RunCommandAsync(invocation);
                var pipe = new AwkOutputPipe(descriptors.Write, completion, memory, nextRedirectionOrder());
                _outputPipes.Add(command, pipe);
                return pipe;
            }
            catch
            {
                await CloseCreatedDescriptorsForCleanupAsync(descriptors).ConfigureAwait(false);
                await AwaitCommandForCleanupAsync(completion, memory).ConfigureAwait(false);
                throw;
            }
        }
    }

    public async Task<AwkRecordCursor> GetInputReaderAsync(string command)
    {
        var pipe = await GetOrOpenInputPipeAsync(command).ConfigureAwait(false);
        return pipe.Reader;

        async Task<AwkInputPipe> GetOrOpenInputPipeAsync(string command)
        {
            if (_inputPipes.TryGetValue(command, out var existing))
                return existing;

            ThrowIfPipeLimitReached();

            var descriptors = await host.CreatePipeAsync(AwkPipeMode.Pipeline).ConfigureAwait(false);
            var memory = memoryAccounting.CreateTransientLease();
            Task<AwkExitCode>? completion = null;
            try
            {
                var invocation = host.CreateInputPipeCommand(command, descriptors.Write, memory);
                completion = host.RunCommandAsync(invocation);
                var reader = createRecordCursor(descriptors.Read);
                var pipe = new AwkInputPipe(descriptors.Read, descriptors.Write, reader, completion, memory, nextRedirectionOrder());
                _inputPipes.Add(command, pipe);
                return pipe;
            }
            catch
            {
                await CloseCreatedDescriptorsForCleanupAsync(descriptors).ConfigureAwait(false);
                await AwaitCommandForCleanupAsync(completion, memory).ConfigureAwait(false);
                throw;
            }
        }
    }

    public async Task<AwkExitCode> CloseOutputAsync(string command)
    {
        var exitCode = await _outputPipes[command].CloseAsync(host, command).ConfigureAwait(false);
        _outputPipes.Remove(command);
        return exitCode;
    }

    private void ThrowIfPipeLimitReached()
    {
        if (_outputPipes.Count + _inputPipes.Count >= maxPipes)
            throw new AwkRuntimeException(AwkMessages.ErrorPipeLimit, AwkDiagnosticId.RuntimeResourceLimit);
    }

    public async Task<AwkExitCode> CloseInputAsync(string command)
    {
        var exitCode = await _inputPipes[command].CloseAsync(host, command).ConfigureAwait(false);
        _inputPipes.Remove(command);
        return exitCode;
    }

    public async Task CloseAllAsync()
    {
        foreach (var command in _outputPipes.Keys)
            await CloseOutputAsync(command).ConfigureAwait(false);

        foreach (var command in _inputPipes.Keys)
            await CloseInputAsync(command).ConfigureAwait(false);
    }

    public async Task CloseAllForCleanupAsync()
    {
        foreach (var pipe in _outputPipes)
        {
            await pipe.Value.CloseForCleanupAsync(host).ConfigureAwait(false);
            _outputPipes.Remove(pipe.Key);
        }

        foreach (var pipe in _inputPipes)
        {
            await pipe.Value.CloseForCleanupAsync(host).ConfigureAwait(false);
            _inputPipes.Remove(pipe.Key);
        }
    }

    private async Task CloseCreatedDescriptorsForCleanupAsync(AwkPipeDescriptors descriptors)
    {
        try
        {
            await host.CloseDescriptorForCleanupAsync(descriptors.Read).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Preserve the failure that interrupted pipe creation.
        }

        try
        {
            await host.CloseDescriptorForCleanupAsync(descriptors.Write).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Preserve the failure that interrupted pipe creation.
        }
    }

    private static async Task AwaitCommandForCleanupAsync(Task<AwkExitCode>? completion, AwkMemoryLease memory)
    {
        try
        {
            if (completion is not null)
                _ = await completion.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Command completion cannot replace the primary execution failure.
        }
        finally
        {
            memory.Dispose();
        }
    }

    private sealed class AwkOutputPipe(
        AwkFileDescriptor writeDescriptor,
        Task<AwkExitCode> commandCompletion, AwkMemoryLease memory, long order)
    {
        private bool _closed;

        public long Order { get; } = order;

        public AwkFileDescriptor WriteDescriptor { get; } = writeDescriptor;

        public async Task<AwkExitCode> CloseAsync(AwkIoHostAdapter host, string command)
        {
            if (!_closed)
            {
                var closed = await host.CloseDescriptorAsync(WriteDescriptor).ConfigureAwait(false);
                _closed = closed.DescriptorRetired;
                host.ThrowIfCloseFailed(WriteDescriptor, closed, command);
            }
            try { return await commandCompletion.ConfigureAwait(false); }
            finally { memory.Dispose(); }
        }

        public async Task CloseForCleanupAsync(AwkIoHostAdapter host)
        {
            try
            {
                if (!_closed)
                    await host.CloseDescriptorForCleanupAsync(WriteDescriptor).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Keep cleanup best-effort so the primary execution failure is preserved.
            }

            await AwaitCommandForCleanupAsync(commandCompletion, memory).ConfigureAwait(false);
        }
    }

    private sealed class AwkInputPipe(
        AwkFileDescriptor readDescriptor,
        AwkFileDescriptor writeDescriptor,
        AwkRecordCursor reader,
        Task<AwkExitCode> commandCompletion, AwkMemoryLease memory, long order)
    {
        private bool _readClosed;
        private bool _writeClosed;

        public long Order { get; } = order;

        public AwkRecordCursor Reader { get; } = reader;

        public async Task<AwkExitCode> CloseAsync(AwkIoHostAdapter host, string command)
        {
            Reader.Dispose();
            if (!_readClosed)
            {
                var closed = await host.CloseDescriptorAsync(readDescriptor).ConfigureAwait(false);
                _readClosed = closed.DescriptorRetired;
                host.ThrowIfCloseFailed(readDescriptor, closed, command);
            }
            if (!_writeClosed)
            {
                var closed = await host.CloseDescriptorAsync(writeDescriptor).ConfigureAwait(false);
                _writeClosed = closed.DescriptorRetired;
                host.ThrowIfCloseFailed(writeDescriptor, closed, command);
            }
            try { return await commandCompletion.ConfigureAwait(false); }
            finally { memory.Dispose(); }
        }

        public async Task CloseForCleanupAsync(AwkIoHostAdapter host)
        {
            Reader.Dispose();
            try
            {
                if (!_readClosed)
                    await host.CloseDescriptorForCleanupAsync(readDescriptor).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Keep closing the remaining pipe endpoint.
            }

            try
            {
                if (!_writeClosed)
                    await host.CloseDescriptorForCleanupAsync(writeDescriptor).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Keep cleanup best-effort so the primary execution failure is preserved.
            }

            await AwaitCommandForCleanupAsync(commandCompletion, memory).ConfigureAwait(false);
        }
    }
}
