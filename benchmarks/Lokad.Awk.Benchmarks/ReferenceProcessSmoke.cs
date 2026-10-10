using System.Diagnostics;
using System.Globalization;

namespace Lokad.Awk.Benchmarking;

// Opt-in integration checks for the native driver; these are not awk language expectations.
internal static class ReferenceProcessSmoke
{
    public static async Task RunAsync(string executable, CancellationToken token)
    {
        var result = await InvokeAsync("BEGIN{for(i=0;i<128;i++)printf \"%8192s\",\"x\";for(i=0;i<4;i++)printf \"%8192s\",\"e\" > \"/dev/stderr\"}").ConfigureAwait(false);
        Require(result.ExitCode == 0 && result.OutputDigest.Bytes == 1 << 20 &&
            result.ErrorDigest.Bytes == 32 << 10, "Concurrent stdout/stderr drain lost bytes.");
        Console.WriteLine("Concurrent output drains: passed.");

        result = await InvokeAsync("BEGIN{print \"failure\" > \"/dev/stderr\";exit 7}", new byte[4 << 20]).ConfigureAwait(false);
        Require(result.ExitCode == 7 && result.OutputDigest.Bytes == 0 &&
            result.Error.AsSpan().SequenceEqual("failure\n"u8), "Early nonzero exit was not preserved.");
        Console.WriteLine("Early exit and blocked stdin: passed.");

        await RejectOutputAsync("BEGIN{for(i=0;i<8193;i++)printf \"%8192s\",\"x\"}").ConfigureAwait(false);
        await RejectOutputAsync("BEGIN{for(i=0;i<9;i++)printf \"%8192s\",\"e\" > \"/dev/stderr\"}").ConfigureAwait(false);
        Console.WriteLine("Stdout/stderr bounds: passed.");

        var workload = ReferenceWorkloads.Create("projection", "small");
        using var prepared = new PreparedReferenceWorkload(workload);
        var pidFile = Path.Combine(prepared.DirectoryPath, "child.pid");
        string[] arguments = ["-v", "pidfile=" + pidFile,
            "BEGIN{print PROCINFO[\"pid\"] > pidfile;close(pidfile);while(1){}}"];
        try
        {
            await ReferenceProcess.RunAsync(executable, arguments, new byte[4 << 20], false,
                TimeSpan.FromMilliseconds(500), token).ConfigureAwait(false);
            throw new InvalidOperationException("An infinite process exceeded its deadline without rejection.");
        }
        catch (TimeoutException) { }
        CheckReaped(pidFile);
        Console.WriteLine("Deadline kills and reaps a child with blocked stdin: passed.");

        File.Delete(pidFile);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(500));
        try
        {
            await ReferenceProcess.RunAsync(executable, arguments, new byte[4 << 20], false,
                TimeSpan.FromSeconds(5), cancellation.Token).ConfigureAwait(false);
            throw new InvalidOperationException("An infinite process ignored cancellation.");
        }
        catch (OperationCanceledException) { token.ThrowIfCancellationRequested(); }
        CheckReaped(pidFile);
        Console.WriteLine("Cancellation kills and reaps a child with blocked stdin: passed.");

        Task<ReferenceExecutionResult> InvokeAsync(string program, byte[]? input = null) =>
            ReferenceProcess.RunAsync(executable, [program], input ?? [], true, TimeSpan.FromSeconds(5), token);

        async Task RejectOutputAsync(string program)
        {
            try
            {
                await InvokeAsync(program).ConfigureAwait(false);
                throw new InvalidOperationException("An oversized output stream was accepted.");
            }
            catch (IOException) { }
        }
    }

    private static void CheckReaped(string path)
    {
        var pid = int.Parse(File.ReadAllText(path).Trim(), CultureInfo.InvariantCulture);
        try
        {
            using var child = Process.GetProcessById(pid);
            Require(child.HasExited, "An interrupted reference child remains alive.");
        }
        catch (ArgumentException) { /* A reaped process no longer has a process-table entry. */ }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
