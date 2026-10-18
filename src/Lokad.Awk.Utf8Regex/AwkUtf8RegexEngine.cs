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
    public bool TryCompile(string pattern, bool ignoreCase, TimeSpan matchTimeout,
        IMemoryBudget? memoryBudget, Action checkpoint,
        [NotNullWhen(true)] out IAwkCompiledRegex? compiledPattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(checkpoint);
        checkpoint();
        var options = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        var regex = new ProducerRegex(pattern, options, matchTimeout);
        checkpoint();
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

        public bool IsMatch(ReadOnlySpan<byte> input, Action checkpoint)
        {
            ArgumentNullException.ThrowIfNull(checkpoint);
            checkpoint();
            var result = Regex.Match(input).Success;
            checkpoint();
            return result;
        }

        public AwkRegexCaptureRange Match(ReadOnlySpan<byte> input, int startInUtf16,
            Span<AwkRegexCaptureRange> captures, Action checkpoint)
        {
            ArgumentNullException.ThrowIfNull(checkpoint);
            ArgumentOutOfRangeException.ThrowIfNegative(startInUtf16);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(captures.Length, GroupCount);
            checkpoint();
            var match = Regex.MatchDetailedFromUtf16Offset(input, startInUtf16);
            if (!match.Success)
            {
                captures.Clear();
                checkpoint();
                return default;
            }
            for (var index = 0; index < captures.Length; index++)
            {
                if ((index & 1023) == 0) checkpoint();
                var group = match.GetGroup(index);
                var aligned = group.IsByteAligned;
                captures[index] = new(group.Success, aligned ? group.IndexInBytes : 0,
                    aligned ? group.LengthInBytes : 0, group.IndexInUtf16, group.LengthInUtf16, aligned);
            }
            checkpoint();
            var matchAligned = match.IsByteAligned;
            return new(match.Success, matchAligned ? match.IndexInBytes : 0,
                matchAligned ? match.LengthInBytes : 0, match.IndexInUtf16, match.LengthInUtf16, matchAligned);
        }

        public void Dispose() => _regex = null;
    }
}
