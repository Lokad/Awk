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
    /// <summary>Gets the stateless shared engine. Compiled handles remain execution-owned.</summary>
    public static AwkUtf8RegexEngine Instance { get; } = new();

    /// <inheritdoc />
    public bool TryCompile(string pattern, bool ignoreCase, IMemoryBudget? memoryBudget,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out IAwkCompiledRegex? compiledPattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        cancellationToken.ThrowIfCancellationRequested();
        var options = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        // Pass cancellation through construction and matching without an independent clock.
        IMemoryReservation? storage = null;
        if (memoryBudget is not null && !memoryBudget.TryReserve(1_024L + pattern.Length * 64L, out storage))
            throw new AwkRegexAdmissionException();
        try
        {
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
        private readonly int[] _groupNumbers;
        private IMemoryReservation? _storage;

        public CompiledPattern(ProducerRegex regex, IMemoryBudget? budget, IMemoryReservation? storage)
        {
            _regex = regex;
            _budget = budget;
            _storage = storage;
            _groupNumbers = regex.GetGroupNumbers();
        }

        public int GroupCount => _groupNumbers.Length;

        private ProducerRegex Regex => _regex ?? throw new ObjectDisposedException(nameof(CompiledPattern));

        public bool IsMatch(ReadOnlySpan<byte> input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var regex = Regex;
            using var scratch = ReserveMatch(input.Length, 0);
            var result = regex.IsMatch(input, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        public AwkRegexCaptureRange Match(ReadOnlySpan<byte> input, int startInUtf16,
            Span<AwkRegexCaptureRange> captures, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(startInUtf16);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(captures.Length, GroupCount);
            cancellationToken.ThrowIfCancellationRequested();
            var regex = Regex;
            using var scratch = ReserveMatch(input.Length, captures.Length);
            if (captures.IsEmpty)
            {
                var range = regex.MatchFromUtf16Offset(input, startInUtf16, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!range.Success) return default;
                return new(range.Success, range.IsByteAligned ? range.IndexInBytes : 0,
                    range.IsByteAligned ? range.LengthInBytes : 0, range.IndexInUtf16,
                    range.LengthInUtf16, range.IsByteAligned);
            }
            var match = regex.MatchDetailedFromUtf16Offset(input, startInUtf16, cancellationToken);
            if (!match.Success)
            {
                captures.Clear();
                cancellationToken.ThrowIfCancellationRequested();
                return default;
            }
            for (var index = 0; index < captures.Length; index++)
            {
                if ((index & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                // AWK slots are dense; provider group numbers may be sparse.
                var group = match.GetGroup(_groupNumbers[index]);
                var aligned = group.IsByteAligned;
                captures[index] = new(group.Success, aligned ? group.IndexInBytes : 0,
                    aligned ? group.LengthInBytes : 0, group.IndexInUtf16, group.LengthInUtf16, aligned);
            }
            cancellationToken.ThrowIfCancellationRequested();
            var matchAligned = match.IsByteAligned;
            return new(match.Success, matchAligned ? match.IndexInBytes : 0,
                matchAligned ? match.LengthInBytes : 0, match.IndexInUtf16, match.LengthInUtf16, matchAligned);
        }

        private IMemoryReservation? ReserveMatch(int inputBytes, int captures)
        {
            if (_budget is null) return null;
            if (!_budget.TryReserve(256L + inputBytes * 2L + captures * 32L, out var scratch))
                throw new AwkRegexAdmissionException();
            return scratch;
        }

        public void Dispose()
        {
            _regex = null;
            _storage?.Dispose();
            _storage = null;
        }
    }
}
