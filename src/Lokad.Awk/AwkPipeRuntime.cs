using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkPipeRuntime(
    AwkIoHostAdapter host,
    int maxOutputRedirections,
    Func<int> outputFileCount)
{
    private readonly Dictionary<string, AwkOutputPipe> _outputPipes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AwkInputPipe> _inputPipes = new(StringComparer.Ordinal);

    public bool HasOutputPipe(string command) => _outputPipes.ContainsKey(command);

    public bool HasInputPipe(string command) => _inputPipes.ContainsKey(command);

    public async Task WriteOutputAsync(AwkRedirectionPath command, ReadOnlyMemory<byte> content)
    {
        var pipe = await GetOrOpenOutputPipeAsync(command.Text).ConfigureAwait(false);
        await host.AppendAsync(pipe.WriteDescriptor, content).ConfigureAwait(false);
    }

    public async Task<AsyncLineReader> GetInputReaderAsync(string command, int maxLineCount)
    {
        var pipe = await GetOrOpenInputPipeAsync(command, maxLineCount).ConfigureAwait(false);
        return pipe.Reader;
    }

    public async Task<AwkPipeCloseResult> CloseAnyAsync(string command)
    {
        var outputClosed = await CloseOutputAsync(command).ConfigureAwait(false);
        var inputClosed = await CloseInputAsync(command).ConfigureAwait(false);
        if (inputClosed.ClosedPipe)
            return inputClosed;

        return outputClosed;
    }

    public async Task<AwkPipeCloseResult> CloseOutputAsync(string command)
    {
        if (!_outputPipes.TryGetValue(command, out var pipe))
            return AwkPipeCloseResult.NotOpened;

        var exitCode = await pipe.CloseAsync(host).ConfigureAwait(false);
        _outputPipes.Remove(command);
        return AwkPipeCloseResult.Closed(exitCode);
    }

    public async Task<AwkPipeCloseResult> CloseInputAsync(string command)
    {
        if (!_inputPipes.TryGetValue(command, out var pipe))
            return AwkPipeCloseResult.NotOpened;

        var exitCode = await pipe.CloseAsync(host).ConfigureAwait(false);
        _inputPipes.Remove(command);
        return AwkPipeCloseResult.Closed(exitCode);
    }

    public async Task CloseAllAsync()
    {
        if (_outputPipes.Count != 0)
        {
            var outputCommands = new string[_outputPipes.Count];
            _outputPipes.Keys.CopyTo(outputCommands, 0);
            for (var index = 0; index < outputCommands.Length; index++)
                await CloseOutputAsync(outputCommands[index]).ConfigureAwait(false);
        }

        if (_inputPipes.Count != 0)
        {
            var inputCommands = new string[_inputPipes.Count];
            _inputPipes.Keys.CopyTo(inputCommands, 0);
            for (var index = 0; index < inputCommands.Length; index++)
                await CloseInputAsync(inputCommands[index]).ConfigureAwait(false);
        }
    }

    public async Task CloseAllForCleanupAsync()
    {
        var outputPipes = new AwkOutputPipe[_outputPipes.Count];
        _outputPipes.Values.CopyTo(outputPipes, 0);
        var inputPipes = new AwkInputPipe[_inputPipes.Count];
        _inputPipes.Values.CopyTo(inputPipes, 0);
        _outputPipes.Clear();
        _inputPipes.Clear();

        for (var index = 0; index < outputPipes.Length; index++)
            await outputPipes[index].CloseForCleanupAsync(host).ConfigureAwait(false);

        for (var index = 0; index < inputPipes.Length; index++)
            await inputPipes[index].CloseForCleanupAsync(host).ConfigureAwait(false);
    }

    private async Task<AwkOutputPipe> GetOrOpenOutputPipeAsync(string command)
    {
        if (_outputPipes.TryGetValue(command, out var existing))
            return existing;

        if (outputFileCount() + _outputPipes.Count >= maxOutputRedirections)
            throw new AwkRuntimeException(AwkMessages.ErrorOutputFileLimit, AwkDiagnosticId.HostIoFailure);

        var descriptors = await host.CreatePipeAsync(AwkPipeMode.Pipeline).ConfigureAwait(false);
        try
        {
            var invocation = host.CreateOutputPipeCommand(command, descriptors.Read);
            var completion = host.RunCommandAsync(invocation);
            var pipe = new AwkOutputPipe(descriptors.Write, completion);
            _outputPipes.Add(command, pipe);
            return pipe;
        }
        catch
        {
            await CloseCreatedDescriptorsForCleanupAsync(descriptors).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<AwkInputPipe> GetOrOpenInputPipeAsync(string command, int maxLineCount)
    {
        if (_inputPipes.TryGetValue(command, out var existing))
            return existing;

        var descriptors = await host.CreatePipeAsync(AwkPipeMode.Pipeline).ConfigureAwait(false);
        try
        {
            var invocation = host.CreateInputPipeCommand(command, descriptors.Write);
            var completion = host.RunCommandAsync(invocation);
            var reader = host.CreateLineReader(descriptors.Read, maxLineCount);
            var pipe = new AwkInputPipe(descriptors.Read, descriptors.Write, reader, completion);
            _inputPipes.Add(command, pipe);
            return pipe;
        }
        catch
        {
            await CloseCreatedDescriptorsForCleanupAsync(descriptors).ConfigureAwait(false);
            throw;
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

    private sealed class AwkOutputPipe(
        AwkFileDescriptor writeDescriptor,
        Task<AwkExitCode> commandCompletion)
    {
        public AwkFileDescriptor WriteDescriptor { get; } = writeDescriptor;

        public async Task<AwkExitCode> CloseAsync(AwkIoHostAdapter host)
        {
            await host.CloseDescriptorAsync(WriteDescriptor).ConfigureAwait(false);
            return await commandCompletion.ConfigureAwait(false);
        }

        public async Task CloseForCleanupAsync(AwkIoHostAdapter host)
        {
            try
            {
                await host.CloseDescriptorForCleanupAsync(WriteDescriptor).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Keep cleanup best-effort so the primary execution failure is preserved.
            }

            try
            {
                _ = await commandCompletion.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Command completion cannot replace the primary execution failure.
            }
        }
    }

    private sealed class AwkInputPipe(
        AwkFileDescriptor readDescriptor,
        AwkFileDescriptor writeDescriptor,
        AsyncLineReader reader,
        Task<AwkExitCode> commandCompletion)
    {
        public AsyncLineReader Reader { get; } = reader;

        public async Task<AwkExitCode> CloseAsync(AwkIoHostAdapter host)
        {
            await host.CloseDescriptorAsync(readDescriptor).ConfigureAwait(false);
            await host.CloseDescriptorAsync(writeDescriptor).ConfigureAwait(false);
            return await commandCompletion.ConfigureAwait(false);
        }

        public async Task CloseForCleanupAsync(AwkIoHostAdapter host)
        {
            try
            {
                await host.CloseDescriptorForCleanupAsync(readDescriptor).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Keep closing the remaining pipe endpoint.
            }

            try
            {
                await host.CloseDescriptorForCleanupAsync(writeDescriptor).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Keep cleanup best-effort so the primary execution failure is preserved.
            }

            try
            {
                _ = await commandCompletion.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Command completion cannot replace the primary execution failure.
            }
        }
    }
}

internal readonly struct AwkPipeCloseResult
{
    private AwkPipeCloseResult(bool closed, AwkExitCode exitCode)
    {
        ClosedPipe = closed;
        ExitCode = exitCode;
    }

    public static AwkPipeCloseResult NotOpened => default;

    public static AwkPipeCloseResult Closed(AwkExitCode exitCode) => new(true, exitCode);

    public bool ClosedPipe { get; }

    public AwkExitCode ExitCode { get; }
}
