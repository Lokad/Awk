using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

// Only provider invocation is inside these catches. Core preparation/validation
// faults stay internal; existing provider syntax/admission/cancellation signals stay intact.
internal static class AwkRegexCallbacks
{
    public static bool TryCompile(IAwkRegexEngine engine, ReadOnlySpan<byte> pattern, bool ignoreCase,
        IMemoryBudget? budget, CancellationToken token, [NotNullWhen(true)] out IAwkCompiledRegex? compiled)
    {
        try { return engine.TryCompile(pattern, ignoreCase, budget, token, out compiled); }
        catch (Exception error) when (IsFault(error, token) && error is not ArgumentException)
        { throw new AwkHostException(error); }
    }

    public static int GroupCount(IAwkCompiledRegex pattern)
    {
        try { return pattern.GroupCount; }
        catch (Exception error) { throw new AwkHostException(error); }
    }

    public static bool IsMatch(IAwkCompiledRegex pattern, ReadOnlySpan<byte> input, CancellationToken token)
    {
        try { return pattern.IsMatch(input, token); }
        catch (Exception error) when (IsFault(error, token)) { throw new AwkHostException(error); }
    }

    public static IAwkRegexSearch CreateSearch(IAwkCompiledRegex pattern, ReadOnlyMemory<byte> input,
        CancellationToken token)
    {
        try { return pattern.CreateSearch(input, token); }
        catch (Exception error) when (IsFault(error, token)) { throw new AwkHostException(error); }
    }

    public static AwkRegexCaptureRange Match(IAwkRegexSearch search, int start,
        Span<AwkRegexCaptureRange> captures, CancellationToken token)
    {
        try { return search.Match(start, captures, token); }
        catch (Exception error) when (IsFault(error, token)) { throw new AwkHostException(error); }
    }

    public static void Dispose(IDisposable? owner)
    {
        try { owner?.Dispose(); }
        catch (Exception error) { throw new AwkHostException(error); }
    }

    private static bool IsFault(Exception error, CancellationToken token) =>
        error is not AwkRegexAdmissionException && error is not AwkHostException &&
        !(error is OperationCanceledException && token.IsCancellationRequested);
}
