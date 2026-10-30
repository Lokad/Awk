using System;
using System.Collections.Generic;
using System.Text;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

internal sealed class MockFileDescriptorTable
{
    private readonly Dictionary<AwkFileDescriptor, MockOpenFileState> _openFiles = new();
    private readonly Dictionary<AwkFileDescriptor, MockHostByteBuffer> _outputs = new();
    private MockOpenFileState? _stdinState;
    private int _nextDescriptor = 3;

    public int OpenFileCount => _openFiles.Count;

    public void SetStandardInput(byte[] content)
    {
        _stdinState = MockOpenFileState.ForRead(content);
    }

    public byte[] GetOutputBytes(AwkFileDescriptor descriptor)
    {
        if (!_outputs.TryGetValue(descriptor, out var bytes))
            return [];

        return bytes.ToMemory().ToArray();
    }

    public string GetOutput(AwkFileDescriptor descriptor)
    {
        if (!_outputs.TryGetValue(descriptor, out var bytes))
            return string.Empty;

        return Encoding.UTF8.GetString(bytes.ToMemory().Span);
    }

    public AwkFileDescriptor OpenRead(byte[] content)
    {
        var descriptor = new AwkFileDescriptor(_nextDescriptor++);
        _openFiles[descriptor] = MockOpenFileState.ForRead(content);
        return descriptor;
    }

    public AwkFileDescriptor OpenWrite(ReadOnlyMemory<byte> initialContent, Action<byte[]> onClose)
    {
        var descriptor = new AwkFileDescriptor(_nextDescriptor++);
        _openFiles[descriptor] = MockOpenFileState.ForWrite(initialContent, onClose);
        return descriptor;
    }

    public int Append(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content)
    {
        if (_openFiles.TryGetValue(descriptor, out var openFile))
            return openFile.TryAppend(content) ? 0 : 1;

        if (!_outputs.TryGetValue(descriptor, out var bytes))
        {
            bytes = new MockHostByteBuffer();
            _outputs[descriptor] = bytes;
        }

        bytes.Append(content);
        return 0;
    }

    public bool Close(AwkFileDescriptor descriptor)
    {
        if (!_openFiles.Remove(descriptor, out var state))
            return false;

        state.Close();
        return true;
    }

    public MockDescriptorReadResult ReadBytes(AwkFileDescriptor descriptor, int maxBytes)
    {
        if (descriptor.IsDevNull)
        {
            return new MockDescriptorReadResult(
                AwkReadBuffer.EndOfFile,
                false);
        }

        var state = GetStateForDescriptor(descriptor);
        if (state == null)
        {
            return new MockDescriptorReadResult(
                AwkReadBuffer.EndOfFile,
                false);
        }

        return new MockDescriptorReadResult(state.ReadBytes(maxBytes), true);

        MockOpenFileState? GetStateForDescriptor(AwkFileDescriptor descriptor)
        {
            if (descriptor == AwkFileDescriptor.StdIn)
                return _stdinState ??= MockOpenFileState.ForRead([]);

            _openFiles.TryGetValue(descriptor, out var state);
            return state;
        }
    }
}

internal readonly record struct MockDescriptorReadResult(AwkReadBuffer Result, bool NotifyAfterRead);

internal sealed class MockOpenFileState
{
    private readonly byte[] _content;
    private readonly MockHostByteBuffer? _writeBuffer;
    private readonly Action<byte[]>? _onClose;
    private int _offset;
    private bool _borrowed;

    private MockOpenFileState(byte[] content, MockHostByteBuffer? writeBuffer, Action<byte[]>? onClose)
    {
        _content = content ?? [];
        _writeBuffer = writeBuffer;
        _onClose = onClose;
    }

    public static MockOpenFileState ForRead(byte[] content) => new(content, null, null);

    public static MockOpenFileState ForWrite(ReadOnlyMemory<byte> initialContent, Action<byte[]> onClose)
    {
        var builder = new MockHostByteBuffer(initialContent.Length);
        builder.Append(initialContent);
        return new MockOpenFileState([], builder, onClose);
    }

    public bool TryAppend(ReadOnlyMemory<byte> content)
    {
        if (_writeBuffer == null)
            return false;

        _writeBuffer.Append(content);
        return true;
    }

    public void Close()
    {
        if (_borrowed) throw new InvalidOperationException("Close while a read borrow is active.");
        if (_writeBuffer == null || _onClose == null)
            return;

        _onClose(_writeBuffer.ToMemory().ToArray());
    }

    public AwkReadBuffer ReadBytes(int maxBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        if (_borrowed) return AwkReadBuffer.Failure(new(AwkOperationErrorKind.IoFailure, "A read borrow is already active."));
        if (_offset >= _content.Length) return AwkReadBuffer.EndOfFile;
        var length = Math.Min(maxBytes, _content.Length - _offset);
        var content = _content.AsMemory(_offset, length);
        _borrowed = true;
        return AwkReadBuffer.FromContent(content, _offset + length == _content.Length
            ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData, consumed =>
            {
                _offset += consumed;
                _borrowed = false;
            });
    }
}
