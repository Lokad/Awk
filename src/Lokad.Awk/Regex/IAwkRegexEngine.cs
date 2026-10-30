using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

/// <summary>Compiles Awk-translated patterns for synchronous matching of valid UTF-8.</summary>
/// <remarks>The application owns the engine. Implementations must permit concurrent compilation.
/// Awk owns each successful compiled pattern. Return false only for unsupported syntax;
/// refused memory admission and cancellation must fail. Unsupported patterns fail the command;
/// Awk has no fallback engine.
/// The provider owns admission for its construction, retained handles and matching workspace;
/// engine-specific bounds belong to the provider, not Awk's execution policy.</remarks>
public interface IAwkRegexEngine
{
    /// <summary>Compiles an already translated pattern, or reports unsupported syntax.</summary>
    /// <param name="patternUtf8">Borrowed valid UTF-8 bytes of Awk's translated ERE pattern using .NET-compatible assertion/category spelling;
    /// match Unicode scalars rather than UTF-16 halves. Do not translate AWK syntax again. Do not retain the span; any retained source must be
    /// provider-owned and admitted before allocation.</param>
    /// <param name="ignoreCase">Whether invariant case-insensitive matching is requested.</param>
    /// <param name="memoryBudget">The borrowed active budget, or null without host accounting.</param>
    /// <param name="cancellationToken">Current operation cancellation; do not retain it.</param>
    /// <param name="compiledPattern">An owned handle on success, otherwise null.</param>
    /// <returns>True on success, false when this engine cannot implement the pattern.</returns>
    bool TryCompile(ReadOnlySpan<byte> patternUtf8, bool ignoreCase, IMemoryBudget? memoryBudget,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out IAwkCompiledRegex? compiledPattern);
}

/// <summary>An Awk-owned compiled pattern used synchronously within one execution.</summary>
/// <remarks>Do not retain operation tokens. Poll current cancellation during work and throw
/// AwkRegexAdmissionException for refused admission. Awk keeps the compiled pattern alive
/// until its active calls and scoped searches complete.
/// Disposal must be idempotent and non-throwing. Awk never disposes the supplied engine.</remarks>
public interface IAwkCompiledRegex : IDisposable
{
    /// <summary>Gets the fixed number of groups, including group0; always at least one.</summary>
    int GroupCount { get; }

    /// <summary>Tests the input without materializing capture values.</summary>
    bool IsMatch(ReadOnlySpan<byte> input, CancellationToken cancellationToken);

    /// <summary>Starts a synchronous search scope borrowing the complete immutable UTF-8 subject.</summary>
    /// <remarks>The caller keeps input valid and unchanged until the search is disposed. A search
    /// may retain this memory for that lifetime, but must not copy the subject unnecessarily or
    /// retain its construction token. Dispose each search before disposing the compiled pattern.</remarks>
    IAwkRegexSearch CreateSearch(ReadOnlyMemory<byte> input, CancellationToken cancellationToken);
}

/// <summary>A scoped search over one complete immutable UTF-8 subject.</summary>
/// <remarks>Dispose before releasing or changing the subject. Disposal is idempotent and
/// non-throwing. Do not retain capture spans or operation tokens after a call.</remarks>
public interface IAwkRegexSearch : IDisposable
{
    /// <summary>Finds a match from an absolute, scalar-aligned byte offset in the bound subject.</summary>
    /// <remarks>The caller supplies zero or a prefix of GroupCount capture slots, including group0.
    /// Fill every supplied slot, including unmatched groups. Offsets are absolute in the bound
    /// subject. The result describes the whole match independently of requested captures. Preserve
    /// complete-subject anchor/lookbehind context; do not search an isolated suffix.
    /// Choose the earliest viable start, then the longest whole match. Resolve ambiguous
    /// subexpressions using POSIX capture priority, independently of how many slots were requested.
    /// Every successful range must end on Unicode scalar boundaries. A provider must implement
    /// these rules itself; Awk validates ranges and publishes captures but does not select matches.</remarks>
    AwkRegexCaptureRange Match(int startOffsetInBytes,
        Span<AwkRegexCaptureRange> captures, CancellationToken cancellationToken);
}

/// <summary>A participating or absent match/capture range in UTF-8 bytes.</summary>
/// <param name="Success">Whether the group participated.</param>
/// <param name="ByteIndex">Its absolute UTF-8 byte offset.</param>
/// <param name="ByteLength">Its UTF-8 byte length.</param>
public readonly record struct AwkRegexCaptureRange(bool Success, int ByteIndex, int ByteLength);

/// <summary>Signals refused host memory admission inside a supplied regex engine.</summary>
/// <remarks>Awk treats this as a fatal memory-limit failure, never an unsupported-pattern fallback.</remarks>
public sealed class AwkRegexAdmissionException : Exception
{
    /// <summary>Creates a refused-admission signal without host or engine error details.</summary>
    public AwkRegexAdmissionException() : base("Regex memory admission was refused.") { }
}
