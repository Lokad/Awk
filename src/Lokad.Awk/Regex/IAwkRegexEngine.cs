using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

/// <summary>Compiles Awk-translated patterns for synchronous matching of valid UTF-8.</summary>
/// <remarks>The application owns the engine. Implementations must permit concurrent compilation.
/// Awk owns each successful compiled pattern. Return false only for unsupported syntax;
/// refused memory admission and cancellation must fail rather than request fallback.</remarks>
public interface IAwkRegexEngine
{
    /// <summary>Compiles an already translated pattern, or reports unsupported syntax.</summary>
    /// <param name="pattern">Awk's translated .NET-compatible pattern; do not translate it again.</param>
    /// <param name="ignoreCase">Whether invariant case-insensitive matching is requested.</param>
    /// <param name="memoryBudget">The borrowed active budget, or null without host accounting.</param>
    /// <param name="cancellationToken">Current operation cancellation; do not retain it.</param>
    /// <param name="compiledPattern">An owned handle on success, otherwise null.</param>
    /// <returns>True on success, false when this engine cannot implement the pattern.</returns>
    bool TryCompile(string pattern, bool ignoreCase, IMemoryBudget? memoryBudget,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out IAwkCompiledRegex? compiledPattern);
}

/// <summary>An Awk-owned compiled pattern used synchronously within one execution.</summary>
/// <remarks>Do not retain input, capture spans or operation tokens after a call. Poll current
/// cancellation during work and throw AwkRegexAdmissionException for refused admission.
/// Disposal must be idempotent and non-throwing. Awk never disposes the supplied engine.</remarks>
public interface IAwkCompiledRegex : IDisposable
{
    /// <summary>Gets the whole-match group plus all numbered capture groups.</summary>
    int GroupCount { get; }

    /// <summary>Gets immutable group names in numbered-group order, including group0.</summary>
    IReadOnlyList<string> GroupNames { get; }

    /// <summary>Tests the input without materializing capture values.</summary>
    bool IsMatch(ReadOnlySpan<byte> input, CancellationToken cancellationToken);

    /// <summary>Finds a match in the complete input from a UTF-16 search offset.</summary>
    /// <remarks>The caller supplies zero or a prefix of GroupCount capture slots, including group0.
    /// Fill every supplied slot, including unmatched groups. Offsets are absolute in the complete
    /// input. The result describes the whole match independently of requested captures. Preserve
    /// full-input anchor/lookbehind context; do not search an isolated suffix.</remarks>
    AwkRegexCaptureRange Match(ReadOnlySpan<byte> input, int startInUtf16,
        Span<AwkRegexCaptureRange> captures, CancellationToken cancellationToken);
}

/// <summary>A match/capture range in UTF-8 bytes and UTF-16 code units.</summary>
/// <param name="Success">Whether the group participated.</param>
/// <param name="ByteIndex">Its absolute UTF-8 byte offset.</param>
/// <param name="ByteLength">Its UTF-8 byte length.</param>
/// <param name="Utf16Index">Its absolute UTF-16 code-unit offset.</param>
/// <param name="Utf16Length">Its UTF-16 code-unit length.</param>
/// <param name="IsByteAligned">Whether both ends lie on Unicode scalar boundaries.</param>
public readonly record struct AwkRegexCaptureRange(bool Success, int ByteIndex, int ByteLength,
    int Utf16Index, int Utf16Length, bool IsByteAligned);

/// <summary>Signals refused host memory admission inside a supplied regex engine.</summary>
/// <remarks>Awk treats this as a fatal memory-limit failure, never an unsupported-pattern fallback.</remarks>
public sealed class AwkRegexAdmissionException : Exception
{
    /// <summary>Creates a refused-admission signal without host or engine error details.</summary>
    public AwkRegexAdmissionException() : base("Regex memory admission was refused.") { }
}
