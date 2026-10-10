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
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private readonly MemoryStream _captured = new();
    private long _bytes;
    private bool _finished;

    public void Append(ReadOnlySpan<byte> content)
    {
        if (_finished)
            throw new InvalidOperationException("Output was already finalized.");
        if (content.Length > maximumBytes - _bytes)
            throw new IOException("Benchmark output exceeded its driver bound.");
        _hash.AppendData(content);
        if (capture)
            _captured.Write(content);
        _bytes += content.Length;
    }

    public (OutputDigest Digest, byte[] Bytes) Finish()
    {
        if (_finished)
            throw new InvalidOperationException("Output was already finalized.");
        _finished = true;
        return (new(_bytes, Convert.ToHexStringLower(_hash.GetHashAndReset())),
            capture ? _captured.ToArray() : []);
    }

    public void Dispose()
    {
        _hash.Dispose();
        _captured.Dispose();
    }
}
