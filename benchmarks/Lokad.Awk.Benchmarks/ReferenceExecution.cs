using System.Text;
using Lokad.Awk;

namespace Lokad.Awk.Benchmarking;

internal static class ReferenceExecution
{
    public static readonly TimeSpan InvocationTimeout = TimeSpan.FromSeconds(15);

    public static async Task<ReferenceExecutionResult> RunLibraryAsync(ReferenceWorkload workload,
        bool capture, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(InvocationTimeout);
        using var host = new ReferenceBenchmarkHost(workload, capture);
        var invocation = Invocation(workload);
        if (!Awk.TryParse(invocation, out var awk))
            throw new InvalidOperationException("Benchmark invocation was not parsed.");
        try
        {
            var exit = await awk.ExecuteAsync(host,
                AwkExecutionPolicy.CreateDefault().WithTimeZone(TimeZoneInfo.Utc), deadline.Token).ConfigureAwait(false);
            return host.Finish(exit.Value);
        }
        catch (OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException("Library invocation exceeded its deadline.");
        }
    }

    public static Task<ReferenceExecutionResult> RunReferenceAsync(string executable,
        PreparedReferenceWorkload prepared, bool capture, CancellationToken cancellationToken) =>
        ReferenceProcess.RunAsync(executable, prepared.Workload.Arguments,
            prepared.Workload.StandardInput, capture, InvocationTimeout, cancellationToken,
            prepared.Workload.Locale, prepared.DirectoryPath);

    public static async Task<ReferenceCounters> CountersAsync(ReferenceWorkload workload,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(InvocationTimeout);
        using var host = new ReferenceBenchmarkHost(workload, capture: false);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(AwkUnixTimestampSeconds.FromSystemClock, TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault());
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(Invocation(workload)),
            deadline.Token, options).ConfigureAwait(false);
        Verify(host.Finish(exit.Value), workload.ExpectedDigest);
        return new(counters.HostCalls, counters.RecordsRead, counters.FieldSplits,
            counters.StringDecodes, counters.Utf8Encodes, counters.RegexMatches, counters.RegexCompiles,
            counters.FieldsMaterialized);
    }

    public static void Verify(ReferenceExecutionResult result, OutputDigest expected)
    {
        if (!result.IsSuccess || result.OutputDigest != expected)
            throw new InvalidOperationException("Benchmark execution failed validation: exit " + result.ExitCode +
                "; stdout " + result.OutputDigest.Bytes + " bytes; stderr " + Encoding.UTF8.GetString(result.Error));
    }

    private static AwkCommandInvocation Invocation(ReferenceWorkload workload) =>
        AwkCommandInvocation.CreateWithStandardDescriptors("awk", workload.Arguments,
            [new("LC_ALL", workload.Locale), new("TZ", "UTC"), new("PWD", "/")]);
}

internal sealed record ReferenceCounters(long HostCalls, long Records,
    long FieldSplits, long StringDecodes, long Utf8Encodes, long RegexMatches, long RegexCompiles,
    long FieldsMaterialized);
