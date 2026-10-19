using System.Text;
using Lokad.Awk;

namespace Lokad.Awk.Benchmarking;

internal sealed class ReferenceBenchmarkHost(ReferenceWorkload workload, bool capture,
    Func<AwkFileDescriptor, int, CancellationToken, Task<AwkReadResult>>? readOverride = null) : IAwkHost, IDisposable
{
    private readonly BenchmarkHost _input = BenchmarkHost.FromStandardInput(workload.StandardInput, workload.Files);
    private readonly ReferenceOutput _stdout = new(capture, 64L << 20);
    private readonly ReferenceOutput _stderr = new(capture: true, 64 << 10);

    public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (descriptor == AwkFileDescriptor.StdOut)
            _stdout.Append(content.Span);
        else if (descriptor == AwkFileDescriptor.StdErr)
            _stderr.Append(content.Span);
        else
            throw new InvalidOperationException("Comparison workloads cannot write another descriptor.");
        return Task.FromResult(AwkExitCode.Success);
    }

    public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) =>
        _input.OpenFileAsync(path, mode, token);
    public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int limit, CancellationToken token) =>
        readOverride is null ? _input.ReadLinesAsync(descriptor, limit, token) : readOverride(descriptor, limit, token);
    public Task<AwkOperationStatus> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) =>
        _input.CloseDescriptorAsync(descriptor, token);
    public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands,
        CancellationToken token) => _input.RunCommandsAsync(commands, token);
    public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken token) =>
        _input.CreatePipeAsync(mode, token);

    public ReferenceExecutionResult Finish(int exit)
    {
        var output = _stdout.Finish();
        var error = _stderr.Finish();
        return new(exit, output.Digest, error.Digest, output.Bytes, error.Bytes);
    }

    public void Dispose()
    {
        _stdout.Dispose();
        _stderr.Dispose();
    }
}

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
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility);
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(Invocation(workload)),
            deadline.Token, options).ConfigureAwait(false);
        Verify(host.Finish(exit.Value), workload.ExpectedDigest);
        return new(counters.HostCalls, counters.RecordsRead, counters.FieldSplits,
            counters.StringDecodes, counters.Utf8Encodes, counters.RegexMatches, counters.RegexCompiles,
            counters.MemoryPeakLiveBytes, counters.FieldsMaterialized);
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
    long MemoryPeakBytes, long FieldsMaterialized);
