using System.Text.RegularExpressions;

namespace Lokad.Awk;

// The default provider owns its construction allowance and non-cooperative match protection.
internal sealed class AwkDotNetRegexPattern : Regex, IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
    private IMemoryReservation? _storage;

    private AwkDotNetRegexPattern(string pattern, RegexOptions options, IMemoryReservation? storage)
        : base(pattern, options, Timeout) => _storage = storage;

    public static AwkDotNetRegexPattern Compile(string pattern, RegexOptions options,
        IMemoryBudget? budget = null, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        IMemoryReservation? storage = null;
        // Coarse provider storage, including construction overlap; no pattern/group traversal.
        if (budget is not null && !budget.TryReserve(1_024L + pattern.Length * 64L, out storage))
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        try
        {
            var result = new AwkDotNetRegexPattern(pattern, options, storage);
            checkpoint?.Invoke();
            return result;
        }
        catch
        {
            storage?.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _storage?.Dispose();
        _storage = null;
    }

    public static void Release(Regex pattern)
    {
        if (pattern is IDisposable owned) owned.Dispose();
    }
}
