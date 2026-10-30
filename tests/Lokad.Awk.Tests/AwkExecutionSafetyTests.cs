using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk.Tests;

public sealed class AwkExecutionSafetyTests
{

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefaultDepthAdmitsOneThousandCallsAndRejectsTheNext(
bool asynchronousHost)
    {
        var read = asynchronousHost ? "getline value;" : "";
        // Include multiple expression and statement frames between recursive calls.
        var program = "function f(n, value){" + read +
            "if(n>1) return 1+(2+(3+(f(n-1)-5))); return 1}" +
            "BEGIN{print f(1000); print f(1001)}";
        var compiled = await CompileAsync(program);
        var host = new MockFileSystem();
        var reads = 0;
        if (asynchronousHost)
        {
            host.ReadAsyncOverride = async (_, _, token) =>
            {
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
                token.ThrowIfCancellationRequested();
                reads++;
                return AwkReadBuffer.FromContent("x\n"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { });
            };
        }

        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = Options(AwkRuntimeLimits.CreateDefault(), counters);
        var exitCode = await compiled.ExecuteAsync(host, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("1000\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: function recursion limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        if (asynchronousHost)
            Assert.True(reads >= 2_000);
    }

    private static Task<AwkCompiledProgram> CompileAsync(string program) =>
        AwkExecutor.CompileAsync(new MockFileSystem(), AwkTestHarness.BuildInvocation("awk", program), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);

    [Fact]
    internal async Task NestedArgumentsKeepTheClrStackBounded()
    {
        var argument = "1";
        for (var index = 0; index < 120; index++)
            argument = "f(" + argument + ")";
        var compiled = await CompileAsync("function f(x){return x} BEGIN{print " + argument + "}");
        var result = await RunAsync(compiled, "", AwkRuntimeLimits.CreateDefault().WithMaxFunctionCallDepth(1), true);
        Assert.Equal(AwkExitCode.Success.Value, result.ExitCode);
        Assert.Equal("1\n", result.StdOut);
        Assert.Equal("", result.StdErr);
    }

    [Fact]
    public void HostCancellationPrecedesCallsAndCleanupCanStillRun()
    {

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var cancelled = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters, cancellation.Token);
        Assert.Throws<OperationCanceledException>(cancelled.RecordHostCall);
        Assert.Equal(0, counters.HostCalls);
        cancelled.RecordCleanupHostCall();
        Assert.Equal(1, counters.HostCalls);
    }

    [Fact]
    internal async Task CallerCancellationUnwindsDeepAsynchronousCalls()
    {
        var compiled = await CompileAsync("function f(n){getline x; if(n>1) return f(n-1); return 1} BEGIN{print f(1000)}");
        using var cancellation = new CancellationTokenSource();
        var host = new MockFileSystem();
        var reads = 0;
        host.ReadAsyncOverride = async (_, _, token) =>
        {
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            if (++reads == 500)
                cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return AwkReadBuffer.FromContent("x\n"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { });
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => compiled.ExecuteAsync(host, cancellation.Token, Options(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled)));
        Assert.Equal(500, reads);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    private static AwkRuntimeOptions Options(
AwkRuntimeLimits limits, AwkRuntimeCounters counters) =>
        new(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);

    private static async Task<Result> RunAsync(AwkCompiledProgram compiled, string input, AwkRuntimeLimits limits, bool observed)
    {
        var host = new MockFileSystem();
        host.SetStandardInput(input);
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var exitCode = await compiled.ExecuteAsync(host, CancellationToken.None, Options(limits, counters));
        return new Result(exitCode.Value, host.GetOutput(AwkFileDescriptor.StdOut), host.GetOutput(AwkFileDescriptor.StdErr));
    }

    private readonly record struct Result(int ExitCode, string StdOut, string StdErr);
}
