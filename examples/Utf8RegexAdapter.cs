using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using ProducerRegex = Lokad.Utf8Regex.Utf8Regex;

namespace Lokad.Awk.Examples;

/// <summary>Application-side integration example for the published Lokad.Utf8Regex API.</summary>
/// <remarks>The pinned package has known POSIX selection gaps; this is not a qualified
/// general AWK provider. This adapter adds no input copies or capture-text allocations. Producer internals
/// are not connected to the host budget by the published API. This adapter reserves coarse estimates
/// for compiled handles and active searches; these are not full engine-allocation coverage.
/// No independent allowance or governor is created.</remarks>
public sealed class AwkUtf8RegexEngine : IAwkRegexEngine
{
    private static readonly System.Text.UTF8Encoding PatternEncoding = new(false, true);
    /// <summary>Gets the stateless shared engine. Compiled handles remain execution-owned.</summary>
    public static AwkUtf8RegexEngine Instance { get; } = new();

    /// <inheritdoc />
    public bool TryCompile(ReadOnlySpan<byte> patternUtf8, bool ignoreCase, IMemoryBudget? memoryBudget,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out IAwkCompiledRegex? compiledPattern)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        // Pass cancellation through construction and matching without an independent clock.
        IMemoryReservation? storage = null;
        if (memoryBudget is not null && !memoryBudget.TryReserve(1_024L + patternUtf8.Length * 64L, out storage))
            throw new AwkRegexAdmissionException();
        try
        {
            // Published 0.4.0 only compiles strings. This temporary adapter-owned conversion
            // is included in the coarse construction admission; core preparation stays UTF-8.
            var pattern = PatternEncoding.GetString(patternUtf8);
            cancellationToken.ThrowIfCancellationRequested();
            var regex = ProducerRegex.Compile(pattern, options,
                System.Text.RegularExpressions.Regex.InfiniteMatchTimeout,
                resourcePolicy: default, constructionPolicy: default,
                constructionTimeout: System.Text.RegularExpressions.Regex.InfiniteMatchTimeout,
                cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            compiledPattern = new CompiledPattern(regex, memoryBudget, storage);
            return true;
        }
        catch
        {
            storage?.Dispose();
            throw;
        }
    }

    private sealed class CompiledPattern : IAwkCompiledRegex
    {
        private ProducerRegex? _regex;
        private readonly IMemoryBudget? _budget;
        private int[] _groupNumbers;
        private IMemoryReservation? _storage;

        public CompiledPattern(ProducerRegex regex, IMemoryBudget? budget, IMemoryReservation? storage)
        {
            _regex = regex;
            _budget = budget;
            _storage = storage;
            _groupNumbers = regex.GetGroupNumbers();
            GroupCount = _groupNumbers.Length;
        }

        public int GroupCount { get; }

        private ProducerRegex Regex => _regex ?? throw new ObjectDisposedException(nameof(CompiledPattern));

        public bool IsMatch(ReadOnlySpan<byte> input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var regex = Regex;
            using var scratch = Reserve(256L + input.Length * 2L);
            var result = regex.IsMatch(input, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        public IAwkRegexSearch CreateSearch(ReadOnlyMemory<byte> input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = Regex;
            // Fund the scoped metadata and one serial matching workspace together. Keep
            // that admission until disposal rather than reserving again for each match.
            var storage = Reserve(384L + input.Length * 2L + GroupCount * 32L);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new Search(this, input, storage);
            }
            catch { storage?.Dispose(); throw; }
        }

        private sealed class Search(CompiledPattern pattern, ReadOnlyMemory<byte> subject,
            IMemoryReservation? storage) : IAwkRegexSearch
        {
            private ReadOnlyMemory<byte> _subject = subject;
            private IMemoryReservation? _storage = storage;
            private int _byteOffset;
            private int _utf16Offset;
            private bool _disposed;

            public AwkRegexCaptureRange Match(int startOffsetInBytes,
                Span<AwkRegexCaptureRange> captures, CancellationToken cancellationToken)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(captures.Length, pattern.GroupCount);
                cancellationToken.ThrowIfCancellationRequested();
                MoveTo(startOffsetInBytes, cancellationToken);
                var input = _subject.Span;
                var regex = pattern.Regex;
                if (captures.IsEmpty)
                {
                    var range = regex.MatchFromUtf16Offset(input, _utf16Offset, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    return range.Success ? new(true, range.IsByteAligned ? range.IndexInBytes : -1,
                        range.IsByteAligned ? range.LengthInBytes : 0) : default;
                }
                var match = regex.MatchDetailedFromUtf16Offset(input, _utf16Offset, cancellationToken);
                if (!match.Success)
                {
                    captures.Clear();
                    cancellationToken.ThrowIfCancellationRequested();
                    return default;
                }
                for (var index = 0; index < captures.Length; index++)
                {
                    if ((index & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                    var group = match.GetGroup(pattern._groupNumbers[index]);
                    captures[index] = group.Success ? new(true, group.IsByteAligned ? group.IndexInBytes : -1,
                        group.IsByteAligned ? group.LengthInBytes : 0) : default;
                }
                cancellationToken.ThrowIfCancellationRequested();
                return new(true, match.IsByteAligned ? match.IndexInBytes : -1,
                    match.IsByteAligned ? match.LengthInBytes : 0);
            }

            private void MoveTo(int target, CancellationToken token)
            {
                var bytes = _subject.Span;
                ArgumentOutOfRangeException.ThrowIfNegative(target);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(target, bytes.Length);
                if (target < bytes.Length && (bytes[target] & 0xC0) == 0x80)
                    throw new ArgumentException("Search offset splits a Unicode scalar.", nameof(target));
                // Published 0.4.0 only accepts UTF-16 restarts. Reuse scalar progress rather
                // than decoding a prefix for every match. This scope makes subject identity
                // and lifetime explicit; no borrowed-span identity cache is needed.
                var nextCheck = _byteOffset;
                while (_byteOffset < target)
                {
                    if (_byteOffset >= nextCheck)
                    {
                        token.ThrowIfCancellationRequested();
                        nextCheck = (int)Math.Min(int.MaxValue, _byteOffset + 16_384L);
                    }
                    if (System.Text.Rune.DecodeFromUtf8(bytes[_byteOffset..], out var rune, out var consumed) !=
                        System.Buffers.OperationStatus.Done) throw new ArgumentException("Invalid UTF-8 subject.");
                    _byteOffset += consumed;
                    _utf16Offset += rune.Utf16SequenceLength;
                }
                nextCheck = _byteOffset;
                while (_byteOffset > target)
                {
                    if (_byteOffset <= nextCheck)
                    {
                        token.ThrowIfCancellationRequested();
                        nextCheck = _byteOffset - 16_384;
                    }
                    if (System.Text.Rune.DecodeLastFromUtf8(bytes[.._byteOffset], out var rune, out var consumed) !=
                        System.Buffers.OperationStatus.Done) throw new ArgumentException("Invalid UTF-8 subject.");
                    _byteOffset -= consumed;
                    _utf16Offset -= rune.Utf16SequenceLength;
                }
                token.ThrowIfCancellationRequested();
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _subject = default;
                _storage?.Dispose();
                _storage = null;
            }
        }

        private IMemoryReservation? Reserve(long bytes)
        {
            if (_budget is null) return null;
            if (!_budget.TryReserve(bytes, out var scratch))
                throw new AwkRegexAdmissionException();
            return scratch;
        }

        public void Dispose()
        {
            _regex = null;
            _groupNumbers = [];
            _storage?.Dispose();
            _storage = null;
        }
    }
}
