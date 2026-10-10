using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkIoDescriptorTable
{
    private readonly Dictionary<AwkPath, AwkOutputFile> _outputFiles = [];
    private readonly Dictionary<AwkPath, AwkInputFile> _inputFiles = [];

    public int OutputFileCount => _outputFiles.Count;

    public int InputFileCount => _inputFiles.Count;

    public bool HasOutput(AwkPath path) => _outputFiles.ContainsKey(path);

    public bool TryGetOutput(AwkPath path, out AwkFileDescriptor descriptor)
    {
        if (_outputFiles.TryGetValue(path, out var output) && output.HasDescriptor)
        {
            descriptor = output.Descriptor;
            return true;
        }
        descriptor = default;
        return false;
    }

    public void AddOutput(AwkPath path, AwkFileDescriptor descriptor, long openedOrder)
    {
        var order = _outputFiles.TryGetValue(path, out var previous) ? previous.Order : openedOrder;
        _outputFiles[path] = new AwkOutputFile(descriptor, true, order);
    }

    public AwkOpenRedirection GetRedirection(AwkPath path)
    {
        var output = _outputFiles.TryGetValue(path, out var written)
            ? new AwkOpenRedirection(AwkOpenRedirectionKind.OutputFile, written.Order) : default;
        var input = _inputFiles.TryGetValue(path, out var read)
            ? new AwkOpenRedirection(AwkOpenRedirectionKind.InputFile, read.Order) : default;
        return output.Newer(input);
    }

    public AwkFileOpenMode ResolveOutputOpenMode(AwkPath path, AwkOutputRedirectionKind kind) =>
        kind == AwkOutputRedirectionKind.Append || HasOutput(path)
            ? AwkFileOpenMode.WriteAppend
            : AwkFileOpenMode.WriteTruncate;

    public bool TryGetInput(AwkPath path, [NotNullWhen(true)] out AwkRecordCursor? reader)
    {
        if (_inputFiles.TryGetValue(path, out var input))
        {
            reader = input.Reader;
            return true;
        }

        reader = null;
        return false;
    }

    public void AddInput(AwkPath path, AwkFileDescriptor descriptor, AwkRecordCursor reader, long openedOrder)
    {
        _inputFiles[path] = new AwkInputFile(descriptor, reader, openedOrder);
    }

    public AwkPath[] SnapshotOutputPaths() =>
        _outputFiles.Keys.ToArray();

    public async Task<bool> FlushOutputAsync(
        AwkIoHostAdapter host,
        AwkPath path)
    {
        if (!_outputFiles.TryGetValue(path, out var output))
            return false;

        if (output.HasDescriptor)
        {
            await host.CloseDescriptorAsync(output.Descriptor).ConfigureAwait(false);
            _outputFiles[path] = output with { Descriptor = default, HasDescriptor = false };
        }
        return true;
    }

    public async Task CloseAllAsync(AwkIoHostAdapter host)
    {
        foreach (var output in _outputFiles.Values)
        {
            if (output.HasDescriptor)
                await host.CloseDescriptorAsync(output.Descriptor).ConfigureAwait(false);
        }

        _outputFiles.Clear();

        foreach (var input in _inputFiles.Values)
        {
            input.Reader.Dispose();
            await host.CloseDescriptorAsync(input.Descriptor).ConfigureAwait(false);
        }

        _inputFiles.Clear();
    }

    public async Task CloseAllForCleanupAsync(AwkIoHostAdapter host)
    {
        var outputDescriptors = _outputFiles.Values.Where(output => output.HasDescriptor)
            .Select(output => output.Descriptor).ToArray();
        var inputFiles = _inputFiles.Values.ToArray();
        _outputFiles.Clear();
        _inputFiles.Clear();

        for (var index = 0; index < outputDescriptors.Length; index++)
        {
            try
            {
                await host.CloseDescriptorForCleanupAsync(outputDescriptors[index]).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Continue closing the remaining descriptors while preserving the primary failure.
            }
        }

        for (var index = 0; index < inputFiles.Length; index++)
        {
            inputFiles[index].Reader.Dispose();
            try
            {
                await host.CloseDescriptorForCleanupAsync(inputFiles[index].Descriptor).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Continue closing the remaining descriptors while preserving the primary failure.
            }
        }
    }

    public async Task<bool> CloseOutputAsync(
        AwkIoHostAdapter host,
        AwkPath path)
    {
        if (!_outputFiles.TryGetValue(path, out var output))
            return false;

        if (output.HasDescriptor)
            await host.CloseDescriptorAsync(output.Descriptor).ConfigureAwait(false);
        _outputFiles.Remove(path);
        return true;
    }

    public async Task<bool> CloseInputAsync(
        AwkIoHostAdapter host,
        AwkPath path)
    {
        if (!_inputFiles.TryGetValue(path, out var input))
            return false;

        input.Reader.Dispose();
        await host.CloseDescriptorAsync(input.Descriptor).ConfigureAwait(false);
        _inputFiles.Remove(path);
        return true;
    }

    private readonly record struct AwkOutputFile(AwkFileDescriptor Descriptor, bool HasDescriptor, long Order);

    private sealed class AwkInputFile(AwkFileDescriptor descriptor, AwkRecordCursor reader, long order)
    {
        public long Order { get; } = order;

        public AwkFileDescriptor Descriptor { get; } = descriptor;

        public AwkRecordCursor Reader { get; } = reader;
    }
}
