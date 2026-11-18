using System.Diagnostics;

namespace Lokad.Awk.Benchmarking;

internal static class ReferenceProcess
{
    public static async Task<ReferenceExecutionResult> RunAsync(string executable,
        IReadOnlyList<string> arguments, byte[] input, bool capture,
        TimeSpan timeout, CancellationToken cancellationToken, string locale = "C",
        string? workingDirectory = null, bool legacyGnuMatchers = false)
    {
        using var deadline = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        if (workingDirectory is not null)
            start.WorkingDirectory = workingDirectory;
        start.Environment["LC_ALL"] = locale;
        start.Environment["LANG"] = locale;
        start.Environment["TZ"] = "UTC";
        foreach (var name in start.Environment.Keys.Where(name =>
            name.StartsWith("GAWK_", StringComparison.Ordinal) || name is "POSIXLY_CORRECT" or "AWKPATH" or "AWKLIBPATH").ToArray())
            start.Environment.Remove(name);
        if (legacyGnuMatchers)
            start.Environment["GAWK_GNU_MATCHERS"] = "1";
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = start };
        using var stdout = new ReferenceOutput(capture, 64L << 20);
        using var stderr = new ReferenceOutput(capture: true, 64 << 10);
        cancellationToken.ThrowIfCancellationRequested();
        if (!process.Start())
            throw new IOException("Reference process did not start.");
        var exit = process.WaitForExitAsync(linked.Token);
        var write = WriteInputAsync();
        var output = DrainAsync(process.StandardOutput.BaseStream, stdout);
        var error = DrainAsync(process.StandardError.BaseStream, stderr);
        Task[] tasks = [exit, write, output, error];
        foreach (var task in tasks)
        {
            _ = task.ContinueWith(_ => linked.Cancel(), CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
            var completedOutput = stdout.Finish();
            var completedError = stderr.Finish();
            return new(process.ExitCode, completedOutput.Digest, completedError.Digest,
                completedOutput.Bytes, completedError.Bytes);
        }
        catch
        {
            linked.Cancel();
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) when (process.HasExited) { }
            await process.WaitForExitAsync(CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            try { await Task.WhenAll(tasks).ConfigureAwait(false); }
            catch (Exception) { /* Preserve the original failure after observing every pump. */ }
            cancellationToken.ThrowIfCancellationRequested();
            if (deadline.IsCancellationRequested)
                throw new TimeoutException("Reference invocation exceeded its deadline.");
            var failure = tasks.Where(task => task.IsFaulted)
                .SelectMany(task => task.Exception?.Flatten().InnerExceptions.AsEnumerable() ?? [])
                .FirstOrDefault(exception => exception is not OperationCanceledException);
            if (failure is not null)
                throw new IOException("Reference process IO failed: " + failure.Message, failure);
            throw;
        }

        async Task WriteInputAsync()
        {
            try { await process.StandardInput.BaseStream.WriteAsync(input, linked.Token).ConfigureAwait(false); }
            catch (IOException)
            {
                // Early exit can close stdin before it is consumed, even on success.
                // Keep its actual status; independent output-drain failures still fail.
                await exit.ConfigureAwait(false);
            }
            finally
            {
                try { process.StandardInput.Close(); }
                catch (IOException)
                {
                    await exit.ConfigureAwait(false);
                }
            }
        }

        async Task DrainAsync(Stream stream, ReferenceOutput collector)
        {
            var buffer = new byte[16 << 10];
            while (true)
            {
                var count = await stream.ReadAsync(buffer, linked.Token).ConfigureAwait(false);
                if (count == 0)
                    return;
                collector.Append(buffer.AsSpan(0, count));
            }
        }
    }
}
