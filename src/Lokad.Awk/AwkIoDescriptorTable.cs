using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkIoDescriptorTable
{
    private readonly Dictionary<AwkPath, AwkFileDescriptor> _outputFiles = [];
    private readonly Dictionary<AwkPath, AwkInputFile> _inputFiles = [];
    private readonly HashSet<AwkPath> _flushedOutputFiles = [];

    public int OutputFileCount => _outputFiles.Count;

    public bool TryGetOutput(AwkPath path, out AwkFileDescriptor descriptor) =>
        _outputFiles.TryGetValue(path, out descriptor);

    public void AddOutput(AwkPath path, AwkFileDescriptor descriptor)
    {
        _outputFiles[path] = descriptor;
    }

    public AwkFileOpenMode ResolveOutputOpenMode(AwkPath path, AwkOutputRedirectionKind kind) =>
        kind == AwkOutputRedirectionKind.Append || _flushedOutputFiles.Remove(path)
            ? AwkFileOpenMode.WriteAppend
            : AwkFileOpenMode.WriteTruncate;

    public bool TryGetInput(AwkPath path, [NotNullWhen(true)] out AsyncLineReader? reader)
    {
        if (_inputFiles.TryGetValue(path, out var input))
        {
            reader = input.Reader;
            return true;
        }

        reader = null;
        return false;
    }

    public void AddInput(AwkPath path, AwkFileDescriptor descriptor, AsyncLineReader reader)
    {
        _inputFiles[path] = new AwkInputFile(descriptor, reader);
    }

    public AwkPath[] SnapshotOutputPaths() =>
        _outputFiles.Keys.ToArray();

    public async Task<bool> FlushOutputAsync(
        AwkIoHostAdapter host,
        AwkPath path)
    {
        if (!_outputFiles.TryGetValue(path, out var descriptor))
            return false;

        await host.CloseDescriptorAsync(descriptor).ConfigureAwait(false);
        _outputFiles.Remove(path);
        _flushedOutputFiles.Add(path);
        return true;
    }

    public async Task CloseAllAsync(AwkIoHostAdapter host)
    {
        foreach (var descriptor in _outputFiles.Values)
        {
            await host.CloseDescriptorAsync(descriptor).ConfigureAwait(false);
        }

        _outputFiles.Clear();
        _flushedOutputFiles.Clear();

        foreach (var input in _inputFiles.Values)
        {
            await host.CloseDescriptorAsync(input.Descriptor).ConfigureAwait(false);
        }

        _inputFiles.Clear();
    }

    public async Task CloseAllForCleanupAsync(AwkIoHostAdapter host)
    {
        var outputDescriptors = _outputFiles.Values.ToArray();
        var inputDescriptors = _inputFiles.Values.Select(static input => input.Descriptor).ToArray();
        _outputFiles.Clear();
        _flushedOutputFiles.Clear();
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

        for (var index = 0; index < inputDescriptors.Length; index++)
        {
            try
            {
                await host.CloseDescriptorForCleanupAsync(inputDescriptors[index]).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Continue closing the remaining descriptors while preserving the primary failure.
            }
        }
    }

    public async Task<bool> CloseAnyAsync(
        AwkIoHostAdapter host,
        AwkPath path)
    {
        var closed = false;
        _flushedOutputFiles.Remove(path);
        if (_outputFiles.TryGetValue(path, out var descriptor))
        {
            await host.CloseDescriptorAsync(descriptor).ConfigureAwait(false);
            _outputFiles.Remove(path);
            closed = true;
        }

        if (_inputFiles.TryGetValue(path, out var input))
        {
            await host.CloseDescriptorAsync(input.Descriptor).ConfigureAwait(false);
            _inputFiles.Remove(path);
            closed = true;
        }

        return closed;
    }

    public async Task<bool> CloseOutputAsync(
        AwkIoHostAdapter host,
        AwkPath path)
    {
        _flushedOutputFiles.Remove(path);
        if (!_outputFiles.TryGetValue(path, out var descriptor))
            return false;

        await host.CloseDescriptorAsync(descriptor).ConfigureAwait(false);
        _outputFiles.Remove(path);
        return true;
    }

    public async Task<bool> CloseInputAsync(
        AwkIoHostAdapter host,
        AwkPath path)
    {
        if (!_inputFiles.TryGetValue(path, out var input))
            return false;

        await host.CloseDescriptorAsync(input.Descriptor).ConfigureAwait(false);
        _inputFiles.Remove(path);
        return true;
    }

    private sealed class AwkInputFile(AwkFileDescriptor descriptor, AsyncLineReader reader)
    {
        public AwkFileDescriptor Descriptor { get; } = descriptor;

        public AsyncLineReader Reader { get; } = reader;
    }
}
