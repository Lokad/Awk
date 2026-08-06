using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkIoDescriptorTable
{
    private readonly Dictionary<string, AwkFileDescriptor> _outputFiles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AwkInputFile> _inputFiles = new(StringComparer.Ordinal);
    private readonly HashSet<string> _flushedOutputFiles = new(StringComparer.Ordinal);

    public int OutputFileCount => _outputFiles.Count;

    public bool TryGetOutput(string key, out AwkFileDescriptor descriptor) =>
        _outputFiles.TryGetValue(key, out descriptor);

    public void AddOutput(string key, AwkFileDescriptor descriptor)
    {
        _outputFiles[key] = descriptor;
    }

    public AwkFileOpenMode ResolveOutputOpenMode(string key, AwkOutputRedirectionKind kind) =>
        kind == AwkOutputRedirectionKind.Append || _flushedOutputFiles.Remove(key)
            ? AwkFileOpenMode.WriteAppend
            : AwkFileOpenMode.WriteTruncate;

    public bool TryGetInput(string key, [NotNullWhen(true)] out AsyncLineReader? reader)
    {
        if (_inputFiles.TryGetValue(key, out var input))
        {
            reader = input.Reader;
            return true;
        }

        reader = null;
        return false;
    }

    public void AddInput(string key, AwkFileDescriptor descriptor, AsyncLineReader reader)
    {
        _inputFiles[key] = new AwkInputFile(descriptor, reader);
    }

    public string[] SnapshotOutputKeys() =>
        _outputFiles.Keys.ToArray();

    public async Task<bool> FlushOutputAsync(
        AwkIoHostAdapter host,
        string key)
    {
        if (!_outputFiles.TryGetValue(key, out var descriptor))
            return false;

        await host.CloseDescriptorAsync(descriptor).ConfigureAwait(false);
        _outputFiles.Remove(key);
        _flushedOutputFiles.Add(key);
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
        string key)
    {
        var closed = false;
        _flushedOutputFiles.Remove(key);
        if (_outputFiles.TryGetValue(key, out var descriptor))
        {
            await host.CloseDescriptorAsync(descriptor).ConfigureAwait(false);
            _outputFiles.Remove(key);
            closed = true;
        }

        if (_inputFiles.TryGetValue(key, out var input))
        {
            await host.CloseDescriptorAsync(input.Descriptor).ConfigureAwait(false);
            _inputFiles.Remove(key);
            closed = true;
        }

        return closed;
    }

    public async Task<bool> CloseOutputAsync(
        AwkIoHostAdapter host,
        string key)
    {
        _flushedOutputFiles.Remove(key);
        if (!_outputFiles.TryGetValue(key, out var descriptor))
            return false;

        await host.CloseDescriptorAsync(descriptor).ConfigureAwait(false);
        _outputFiles.Remove(key);
        return true;
    }

    public async Task<bool> CloseInputAsync(
        AwkIoHostAdapter host,
        string key)
    {
        if (!_inputFiles.TryGetValue(key, out var input))
            return false;

        await host.CloseDescriptorAsync(input.Descriptor).ConfigureAwait(false);
        _inputFiles.Remove(key);
        return true;
    }

    private sealed class AwkInputFile(AwkFileDescriptor descriptor, AsyncLineReader reader)
    {
        public AwkFileDescriptor Descriptor { get; } = descriptor;

        public AsyncLineReader Reader { get; } = reader;
    }
}
