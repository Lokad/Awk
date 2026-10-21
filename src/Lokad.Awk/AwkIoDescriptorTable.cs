using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkIoDescriptorTable(AwkMemoryAccounting accounting)
{
    private Dictionary<AwkPath, AwkOutputFile> _outputFiles = [];
    private Dictionary<AwkPath, AwkInputFile> _inputFiles = [];
    private readonly AwkMemoryLease _memory = accounting.CreateTransientLease();

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

    public void PrepareOutput(AwkPath path, AwkMemoryLease pending)
    {
        if (_outputFiles.ContainsKey(path)) return;
        pending.ReserveAdditional(EstimatePathBytes(path));
        PrepareCapacity(_outputFiles);
    }

    public void AddOutput(AwkPath path, AwkFileDescriptor descriptor, long openedOrder, AwkMemoryLease pending)
    {
        var exists = _outputFiles.TryGetValue(path, out var previous);
        var order = exists ? previous.Order : openedOrder;
        _outputFiles[path] = new AwkOutputFile(descriptor, true, order);
        if (!exists) pending.MoveTo(_memory, EstimatePathBytes(path));
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

    public void PrepareInput(AwkPath path, AwkMemoryLease pending)
    {
        pending.ReserveAdditional(EstimatePathBytes(path));
        PrepareCapacity(_inputFiles);
    }

    public void AddInput(AwkPath path, AwkFileDescriptor descriptor, AwkRecordCursor reader, long openedOrder,
        AwkMemoryLease pending)
    {
        _inputFiles[path] = new AwkInputFile(descriptor, reader, openedOrder);
        pending.MoveTo(_memory, EstimatePathBytes(path));
    }

    // One coarse owner funds retained keys, cursor metadata and backing capacity.
    // Deleted entries leave reusable backing, so only their payload is refunded.
    private static long EstimatePathBytes(AwkPath path) =>
        AwkRuntimeMemory.CheckedAdd(512, AwkRuntimeMemory.EstimateUtf16StringBytes(path.Path.Length));

    private static long EstimateBackingBytes(int capacity) =>
        capacity == 0 ? 0 : AwkRuntimeMemory.CheckedAdd(128,
            AwkRuntimeMemory.CheckedMultiply(capacity, 96));

    private void PrepareCapacity<T>(Dictionary<AwkPath, T> files)
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
        _outputFiles = [];
        _inputFiles = [];
        _memory.Dispose();
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
        // Retire each successful close immediately. If a later close fails, cleanup
        // owns only the endpoints that still need closing.
        foreach (var path in _outputFiles.Keys.ToArray())
            await CloseOutputAsync(host, path).ConfigureAwait(false);

        foreach (var path in _inputFiles.Keys.ToArray())
            await CloseInputAsync(host, path).ConfigureAwait(false);
        ReleaseStorage();
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
        ReleaseStorage();
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
        _memory.Release(EstimatePathBytes(path));
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
        _memory.Release(EstimatePathBytes(path));
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
