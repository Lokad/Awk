using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Lokad.Awk.Benchmarking;

internal readonly record struct OutputDigest(long Bytes, string Sha256)
{
    public static OutputDigest FromBytes(ReadOnlySpan<byte> bytes) =>
        new(bytes.Length, Convert.ToHexStringLower(SHA256.HashData(bytes)));
}

internal sealed record ReferenceExecutionResult(int ExitCode, OutputDigest OutputDigest,
    OutputDigest ErrorDigest, byte[] Output, byte[] Error)
{
    public bool IsSuccess => ExitCode == 0 && ErrorDigest.Bytes == 0;
}

internal sealed class ReferenceOutput(bool capture, long maximumBytes) : IDisposable
{
    [InlineArray(256)]
    private struct HashBuffer
    {
        private byte _element0;
    }

    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private readonly MemoryStream _captured = new();
    private HashBuffer _pendingHash;
    private int _pendingHashBytes;
    private long _bytes;
    private bool _finished;
    private bool _disposed;

    public void Append(ReadOnlySpan<byte> content)
    {
        if (_finished)
            throw new InvalidOperationException("Output was already finalized.");
        if (content.Length > maximumBytes - _bytes)
            throw new IOException("Benchmark output exceeded its driver bound.");
        ObjectDisposedException.ThrowIf(_disposed, this);
        AppendHash(content);
        if (capture)
            _captured.Write(content);
        _bytes += content.Length;
    }

    // Verification must consume each append, without a native hash transition per record.
    // Copy small borrows now; the host may reuse their backing as soon as Append returns.
    private void AppendHash(ReadOnlySpan<byte> content)
    {
        Span<byte> buffer = _pendingHash;
        if (_pendingHashBytes != 0)
        {
            var copied = Math.Min(buffer.Length - _pendingHashBytes, content.Length);
            content[..copied].CopyTo(buffer[_pendingHashBytes..]);
            _pendingHashBytes += copied;
            content = content[copied..];
            if (_pendingHashBytes < buffer.Length)
                return;
            _hash.AppendData(buffer);
            _pendingHashBytes = 0;
        }
        if (content.Length >= buffer.Length)
        {
            _hash.AppendData(content);
            return;
        }
        content.CopyTo(buffer);
        _pendingHashBytes = content.Length;
    }

    public (OutputDigest Digest, byte[] Bytes) Finish()
    {
        if (_finished)
            throw new InvalidOperationException("Output was already finalized.");
        _finished = true;
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_pendingHashBytes != 0)
            _hash.AppendData(((ReadOnlySpan<byte>)_pendingHash)[.._pendingHashBytes]);
        return (new(_bytes, Convert.ToHexStringLower(_hash.GetHashAndReset())),
            capture ? _captured.ToArray() : []);
    }

    public void Dispose()
    {
        _disposed = true;
        _hash.Dispose();
        _captured.Dispose();
    }
}
