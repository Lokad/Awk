using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lokad.Awk.Benchmarking;

// Fresh-process evidence is required: ordinary warmups hide static metadata construction.
internal static class RegexCategoryProbe
{
    private const string Pattern = @"[\p{L}\p{N}]";

    public static async Task RunAsync(string mode)
    {
        if (IsPublished()) throw new InvalidOperationException("Category probe must start cold.");
        if (mode is "limit" or "cancel")
        {
            var governor = CreateGovernor(mode == "limit" ? 44 : 16L << 20);
            using var borrowed = governor.CreateTransientLease();
            borrowed.ReserveAdditional(43);
            using var cancellation = new CancellationTokenSource();
            var checks = 0;
            try
            {
                _ = AwkRegexScalarPattern.Translate(Pattern, () =>
                {
                    if (++checks == 16 && mode == "cancel") cancellation.Cancel();
                    cancellation.Token.ThrowIfCancellationRequested();
                }, governor, borrowed);
                throw new InvalidOperationException("Expected cold preparation failure.");
            }
            catch (OperationCanceledException error) when (mode == "cancel" && error.CancellationToken == cancellation.Token) { }
            catch (AwkRuntimeException error) when (mode == "limit" &&
                AwkUtf8Codec.Decode(error.MessageBytes) == "awk: memory limit exceeded\n") { }
            if (IsPublished() || governor.CurrentCommittedBytes != 43 || governor.CurrentReservedBytes != 0)
                throw new InvalidOperationException("Failed category preparation published metadata or changed borrowed admission.");
            var retry = Measure();
            Console.WriteLine(JsonSerializer.Serialize(new { Mode = mode, FailurePreservedState = true, Checks = checks, Retry = retry }));
            return;
        }
        if (mode == "concurrent")
        {
            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(Measure))).ConfigureAwait(false);
            if (results.Any(result => result.Hash != results[0].Hash || result.PeakBytes != results[0].PeakBytes))
                throw new InvalidOperationException("Concurrent category callers disagree on translation or admission.");
            Console.WriteLine(JsonSerializer.Serialize(new { Mode = mode, Results = results }));
            return;
        }
        if (mode != "timing") throw new ArgumentException("Usage: --regex-category-probe timing|limit|cancel|concurrent");
        // Exercise translation/JIT paths without constructing category metadata.
        _ = AwkRegexScalarPattern.Translate(".[A]$()", () => { }, CreateGovernor(16L << 20));
        if (IsPublished()) throw new InvalidOperationException("Probe warmup populated category metadata.");
        var cold = Measure();
        var warm = Measure();
        if (!IsPublished() || cold.Hash != warm.Hash || cold.PeakBytes != warm.PeakBytes)
            throw new InvalidOperationException("Cold/warm category contracts differ.");
        Console.WriteLine(JsonSerializer.Serialize(new { Mode = mode, Cold = cold, Warm = warm }));
    }

    private static Measurement Measure()
    {
        var governor = CreateGovernor(16L << 20);
        var watch = new Stopwatch();
        var before = GC.GetAllocatedBytesForCurrentThread();
        watch.Start();
        var translated = AwkRegexScalarPattern.Translate(Pattern, () => { }, governor);
        watch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (governor.CurrentCommittedBytes != 0 || governor.CurrentReservedBytes != 0)
            throw new InvalidOperationException("Category probe left admission live.");
        return new(watch.Elapsed.TotalMilliseconds, allocated, governor.PeakLiveBytes,
            governor.ReserveCount, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(translated))));
    }

    private static AwkMemoryGovernor CreateGovernor(long bytes) =>
        AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(bytes));

    private static bool IsPublished()
    {
        // Observe publication in tooling only, without adding a runtime test hook.
        var field = typeof(AwkRegexScalarPattern).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(field => field.Name == "SupplementaryCategories");
        return field.GetValue(null) switch
        {
            null => false,
            Array => true,
            object value => value.GetType().GetProperty("IsValueCreated")?.GetValue(value) is true
        };
    }

    private sealed record Measurement(double Milliseconds, long AllocatedBytes, long PeakBytes, long Reservations, string Hash);
}
