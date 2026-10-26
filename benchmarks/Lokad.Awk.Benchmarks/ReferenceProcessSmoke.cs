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

        var earlyInput = new byte[4 << 20];
        Array.Fill(earlyInput, (byte)'\n');
        foreach (var phase in new[] { "BEGIN", "" })
        foreach (var exitCode in new[] { 0, 7 })
        {
            var exitStatement = phase == "BEGIN" && exitCode == 0 ? "" : "exit " + exitCode;
            result = await InvokeAsync(phase + "{print \"done\";print \"note\" > \"/dev/stderr\";" +
                exitStatement + "}", earlyInput).ConfigureAwait(false);
            Require(result.ExitCode == exitCode && result.Output.AsSpan().SequenceEqual("done\n"u8) &&
                result.Error.AsSpan().SequenceEqual("note\n"u8), "Early exit lost its status or output.");
        }
        Console.WriteLine("Successful/nonzero BEGIN and first-record exits with unread stdin: passed.");

        result = await InvokeAsync("{print}", "a\nb\n"u8.ToArray()).ConfigureAwait(false);
        Require(result.ExitCode == 0 && result.Output.AsSpan().SequenceEqual("a\nb\n"u8) &&
            result.ErrorDigest.Bytes == 0, "Fully consumed stdin was not preserved.");
        Console.WriteLine("Fully consumed stdin: passed.");

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
