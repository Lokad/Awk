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

    public MockDescriptorReadResult ReadLines(AwkFileDescriptor descriptor, int maxLineCount)
    {
        if (descriptor.IsDevNull)
        {
            return new MockDescriptorReadResult(
                AwkReadResult.EndOfFile,
                false);
        }

        var state = GetStateForDescriptor(descriptor);
        if (state == null)
        {
            return new MockDescriptorReadResult(
                AwkReadResult.EndOfFile,
                false);
        }

        return new MockDescriptorReadResult(state.ReadLines(maxLineCount), true);
    }

    private MockOpenFileState? GetStateForDescriptor(AwkFileDescriptor descriptor)
    {
        if (descriptor == AwkFileDescriptor.StdIn)
            return _stdinState ??= MockOpenFileState.ForRead([]);

        _openFiles.TryGetValue(descriptor, out var state);
        return state;
    }
}

internal readonly record struct MockDescriptorReadResult(AwkReadResult Result, bool NotifyAfterRead);

internal sealed class MockOpenFileState
{
    private readonly byte[] _content;
    private readonly MockHostByteBuffer? _writeBuffer;
    private readonly Action<byte[]>? _onClose;
    private int _offset;

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
        if (_writeBuffer == null || _onClose == null)
            return;

        _onClose(_writeBuffer.ToMemory().ToArray());
    }

    public AwkReadResult ReadLines(int maxLineCount)
    {
        if (_offset >= _content.Length)
            return AwkReadResult.EndOfFile;

        var start = _offset;
        var span = _content.AsSpan(start);
        var offsets = maxLineCount == 1 ? Array.Empty<int>() : new int[Math.Min(maxLineCount - 1, span.Length)];
        var lineCount = 0;
        var consumed = 0;
        while (lineCount < maxLineCount)
        {
            var newline = span[consumed..].IndexOf((byte)'\n');
            if (newline < 0)
                break;

            lineCount++;
            consumed += newline + 1;
            if (lineCount >= maxLineCount)
                break;

            offsets[lineCount - 1] = consumed;
        }

        if (lineCount > 0)
        {
            _offset += consumed;
            var completeBuffer = new byte[consumed];
            Array.Copy(_content, start, completeBuffer, 0, consumed);
            return AwkReadResult.FromContent(
                completeBuffer,
                lineCount > 1 ? offsets[..(lineCount - 1)] : Array.Empty<int>(),
                _offset >= _content.Length);
        }

        var length = _content.Length - start;
        var buffer = new byte[length];
        Array.Copy(_content, start, buffer, 0, length);
        _offset = _content.Length;
        return AwkReadResult.FromContent(buffer, Array.Empty<int>(), reachedEof: true);
    }
}
