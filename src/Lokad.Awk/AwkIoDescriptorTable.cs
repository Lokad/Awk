using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkIoDescriptorTable(AwkMemoryAccounting accounting)
{
    private Dictionary<string, AwkOutputFile> _outputFiles = new(StringComparer.Ordinal);
    private Dictionary<string, AwkInputFile> _inputFiles = new(StringComparer.Ordinal);
    private readonly AwkMemoryLease _memory = accounting.CreateTransientLease();

    public int OutputFileCount => _outputFiles.Count;

    public int InputFileCount => _inputFiles.Count;

    public bool HasOutput(string path) => _outputFiles.ContainsKey(path);

    public bool TryGetOutput(string path, out AwkFileDescriptor descriptor)
    {
        if (_outputFiles.TryGetValue(path, out var output) && output.HasDescriptor)
        {
            descriptor = output.Descriptor;
            return true;
        }
        descriptor = default;
        return false;
    }

    public void PrepareOutput(string path, AwkMemoryLease pending)
    {
        if (_outputFiles.ContainsKey(path)) return;
        pending.ReserveAdditional(EstimatePathBytes(path));
        PrepareCapacity(_outputFiles);
    }

    public void AddOutput(string path, AwkFileDescriptor descriptor, long openedOrder, AwkMemoryLease pending)
    {
        var exists = _outputFiles.TryGetValue(path, out var previous);
        var order = exists ? previous.Order : openedOrder;
        _outputFiles[path] = new AwkOutputFile(descriptor, true, order);
        if (!exists) pending.MoveTo(_memory, EstimatePathBytes(path));
    }

    public AwkOpenRedirection GetRedirection(string path)
    {
        var output = _outputFiles.TryGetValue(path, out var written)
            ? new AwkOpenRedirection(AwkOpenRedirectionKind.OutputFile, written.Order) : default;
        var input = _inputFiles.TryGetValue(path, out var read)
            ? new AwkOpenRedirection(AwkOpenRedirectionKind.InputFile, read.Order) : default;
        return output.Newer(input);
    }

    public AwkFileOpenMode ResolveOutputOpenMode(string path, AwkOutputRedirectionKind kind) =>
        kind == AwkOutputRedirectionKind.Append || HasOutput(path)
            ? AwkFileOpenMode.WriteAppend
            : AwkFileOpenMode.WriteTruncate;

    public bool TryGetInput(string path, [NotNullWhen(true)] out AwkRecordCursor? reader)
    {
        if (_inputFiles.TryGetValue(path, out var input))
        {
            reader = input.Reader;
            return true;
        }

        reader = null;
        return false;
    }

    public void PrepareInput(string path, AwkMemoryLease pending)
    {
        pending.ReserveAdditional(EstimatePathBytes(path));
        PrepareCapacity(_inputFiles);
    }

    public void AddInput(string path, AwkFileDescriptor descriptor, AwkRecordCursor reader, long openedOrder,
        AwkMemoryLease pending)
    {
        _inputFiles[path] = new AwkInputFile(descriptor, reader, openedOrder);
        pending.MoveTo(_memory, EstimatePathBytes(path));
    }

    // One coarse owner funds retained keys, cursor metadata and backing capacity.
    // Deleted entries leave reusable backing, so only their payload is refunded.
    private static long EstimatePathBytes(string path) =>
        AwkRuntimeMemory.CheckedAdd(512, AwkRuntimeMemory.EstimateUtf16StringBytes(path.Length));

    private static long EstimateBackingBytes(int capacity) =>
        capacity == 0 ? 0 : AwkRuntimeMemory.CheckedAdd(128,
            AwkRuntimeMemory.CheckedMultiply(capacity, 96));

    private void PrepareCapacity<T>(Dictionary<string, T> files)
    {
        if (!accounting.IsEnabled) return;
        var previous = files.EnsureCapacity(0);
        if (files.Count < previous) return;
        var target = (int)Math.Min(Array.MaxLength, Math.Max(3L, 2L * files.Count));
        var bound = (int)Math.Min(Array.MaxLength, 2L * target + 4);
        // Admit new backing while old backing is live, including prime rounding.
        using var growth = accounting.AdmitMutation(EstimateBackingBytes(bound));
        var capacity = files.EnsureCapacity(target);
        _memory.ReserveAdditional(EstimateBackingBytes(capacity) - EstimateBackingBytes(previous));
    }

    private void ReleaseStorage()
    {
        _outputFiles = new(StringComparer.Ordinal);
        _inputFiles = new(StringComparer.Ordinal);
        _memory.Dispose();
    }

    public async Task FlushAllAsync(AwkIoHostAdapter host)
    {
        foreach (var path in _outputFiles.Keys)
            await FlushOutputAsync(host, path).ConfigureAwait(false);
    }

    public async Task<bool> FlushOutputAsync(
        AwkIoHostAdapter host,
        string path)
    {
        if (!_outputFiles.TryGetValue(path, out var output))
            return false;

        if (output.HasDescriptor)
        {
            var closed = await host.CloseDescriptorAsync(output.Descriptor).ConfigureAwait(false);
            if (closed.DescriptorRetired)
                _outputFiles[path] = output with { Descriptor = default, HasDescriptor = false };
            host.ThrowIfCloseFailed(output.Descriptor, closed, path);
        }
        return true;
    }

    public async Task CloseAllAsync(AwkIoHostAdapter host)
    {
        // Retire each successful close immediately. If a later close fails, cleanup
        // owns only the endpoints that still need closing.
        foreach (var path in _outputFiles.Keys)
            await CloseOutputAsync(host, path).ConfigureAwait(false);

        foreach (var path in _inputFiles.Keys)
            await CloseInputAsync(host, path).ConfigureAwait(false);
        ReleaseStorage();
    }

    public async Task CloseAllForCleanupAsync(AwkIoHostAdapter host)
    {
        foreach (var output in _outputFiles.Values)
        {
            if (!output.HasDescriptor) continue;
            try
            {
                await host.CloseDescriptorForCleanupAsync(output.Descriptor).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Continue closing the remaining descriptors while preserving the primary failure.
            }
        }

        foreach (var input in _inputFiles.Values)
        {
            input.Reader.Dispose();
            try
            {
                await host.CloseDescriptorForCleanupAsync(input.Descriptor).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Continue closing the remaining descriptors while preserving the primary failure.
            }
        }
        ReleaseStorage();
    }

    public async Task<bool> CloseOutputAsync(
        AwkIoHostAdapter host,
        string path)
    {
        if (!_outputFiles.TryGetValue(path, out var output))
            return false;

        var closed = output.HasDescriptor
            ? await host.CloseDescriptorAsync(output.Descriptor).ConfigureAwait(false)
            : AwkDescriptorCloseResult.Closed();
        if (closed.DescriptorRetired)
            _outputFiles.Remove(path);
        try
        {
            host.ThrowIfCloseFailed(output.Descriptor, closed, path);
            return true;
        }
        finally
        {
            if (closed.DescriptorRetired) _memory.Release(EstimatePathBytes(path));
        }
    }

    public async Task<bool> CloseInputAsync(
        AwkIoHostAdapter host,
        string path)
    {
        if (!_inputFiles.TryGetValue(path, out var input))
            return false;

        input.Reader.Dispose();
        var closed = await host.CloseDescriptorAsync(input.Descriptor).ConfigureAwait(false);
        if (closed.DescriptorRetired)
            _inputFiles.Remove(path);
        try
        {
            host.ThrowIfCloseFailed(input.Descriptor, closed, path);
            return true;
        }
        finally
        {
            if (closed.DescriptorRetired) _memory.Release(EstimatePathBytes(path));
        }
    }

    private readonly record struct AwkOutputFile(AwkFileDescriptor Descriptor, bool HasDescriptor, long Order);

    private sealed class AwkInputFile(AwkFileDescriptor descriptor, AwkRecordCursor reader, long order)
    {
        public long Order { get; } = order;

        public AwkFileDescriptor Descriptor { get; } = descriptor;

        public AwkRecordCursor Reader { get; } = reader;
    }
}
