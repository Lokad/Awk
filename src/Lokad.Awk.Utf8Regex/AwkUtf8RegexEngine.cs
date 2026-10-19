using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using ProducerRegex = Lokad.Utf8Regex.Utf8Regex;

namespace Lokad.Awk.Utf8Regex;

/// <summary>Adapts Lokad.Utf8Regex to Awk's UTF-8 matching contract.</summary>
/// <remarks>This adapter adds no input copies or capture-text allocations. Producer internals
/// retain the same approximate accounting as the existing integration; no independent allowance
/// or governor is created.</remarks>
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
        // Pinned0.2.0 has no token-aware APIs. Keep boundary checks until a producer upgrade
        // is qualified; do not inherit its default timeout or create an independent clock.
        var regex = new ProducerRegex(pattern, options, System.Text.RegularExpressions.Regex.InfiniteMatchTimeout);
        cancellationToken.ThrowIfCancellationRequested();
        compiledPattern = new CompiledPattern(regex);
        return true;
    }

    private sealed class CompiledPattern : IAwkCompiledRegex
    {
        private ProducerRegex? _regex;

        public CompiledPattern(ProducerRegex regex)
        {
            _regex = regex;
            GroupNames = regex.GetGroupNames();
        }

        public int GroupCount => GroupNames.Count;

        public IReadOnlyList<string> GroupNames { get; }

        private ProducerRegex Regex => _regex ?? throw new ObjectDisposedException(nameof(CompiledPattern));

        public bool IsMatch(ReadOnlySpan<byte> input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = Regex.Match(input).Success;
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        public AwkRegexCaptureRange Match(ReadOnlySpan<byte> input, int startInUtf16,
            Span<AwkRegexCaptureRange> captures, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(startInUtf16);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(captures.Length, GroupCount);
            cancellationToken.ThrowIfCancellationRequested();
            var match = Regex.MatchDetailedFromUtf16Offset(input, startInUtf16);
            if (!match.Success)
            {
                captures.Clear();
                cancellationToken.ThrowIfCancellationRequested();
                return default;
            }
            for (var index = 0; index < captures.Length; index++)
            {
                if ((index & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                var group = match.GetGroup(index);
                var aligned = group.IsByteAligned;
                captures[index] = new(group.Success, aligned ? group.IndexInBytes : 0,
                    aligned ? group.LengthInBytes : 0, group.IndexInUtf16, group.LengthInUtf16, aligned);
            }
            cancellationToken.ThrowIfCancellationRequested();
            var matchAligned = match.IsByteAligned;
            return new(match.Success, matchAligned ? match.IndexInBytes : 0,
                matchAligned ? match.LengthInBytes : 0, match.IndexInUtf16, match.LengthInUtf16, matchAligned);
        }

        public void Dispose() => _regex = null;
    }
}
