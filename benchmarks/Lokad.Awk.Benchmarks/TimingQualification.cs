using System.Diagnostics;

namespace Lokad.Awk.Benchmarking;

internal sealed record QualificationResult(TimingSummary Summary, PairedSample[] Samples,
    QuietCheck[] QuietChecks, int LibraryWarmupCalls, int ReferenceWarmupCalls)
{
    public static QualificationResult NotMeasured { get; } = new(
        new("NotMeasured", "Correctness verification only.", 0, 0, 0, 0, 0, "No performance claim"), [], [], 0, 0);
}

internal static class TimingQualification
{
    public static async Task<QualificationResult> RunAsync(string executable, PreparedReferenceWorkload prepared,
        int pairs, CancellationToken cancellationToken)
    {
        var samples = new List<PairedSample>();
        var quietChecks = new List<QuietCheck>();
        var libraryWarmup = 0;
        var referenceWarmup = 0;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(120));
        try
        {
            libraryWarmup = await WarmAsync(true).ConfigureAwait(false);
            referenceWarmup = await WarmAsync(false).ConfigureAwait(false);
            var libraryIterations = await CalibrateAsync(true).ConfigureAwait(false);
            var referenceIterations = await CalibrateAsync(false).ConfigureAwait(false);
            for (var i = 0; i < pairs; i++)
            {
                await Task.Delay(500, deadline.Token).ConfigureAwait(false);
                var quiet = await MachineQuietProbe.CheckAsync(1, TimeSpan.FromSeconds(1), deadline.Token).ConfigureAwait(false);
                quietChecks.Add(quiet);
                if (!quiet.IsQuiet)
                    return Finish("Paused", quiet.Reason);
                var libraryFirst = i % 2 == 0;
                double libraryMs, referenceMs;
                if (libraryFirst)
                {
                    libraryMs = await TimeAsync(true, libraryIterations).ConfigureAwait(false);
                    referenceMs = await TimeAsync(false, referenceIterations).ConfigureAwait(false);
                }
                else
                {
                    referenceMs = await TimeAsync(false, referenceIterations).ConfigureAwait(false);
                    libraryMs = await TimeAsync(true, libraryIterations).ConfigureAwait(false);
                }
                samples.Add(new(libraryFirst, libraryIterations, referenceIterations, libraryMs, referenceMs));
            }
            return new(PairedStatistics.Summarize(samples), samples.ToArray(), quietChecks.ToArray(), libraryWarmup, referenceWarmup);
        }
        catch (OperationCanceledException)
        {
            return Finish(cancellationToken.IsCancellationRequested ? "Interrupted" : "Timeout",
                "Qualification interrupted or exceeded its 120-second case deadline.");
        }
        catch (TimeoutException) { return Finish("Timeout", "An invocation exceeded its deadline."); }
        catch (IOException exception) { return Finish("Failure", exception.Message); }
        catch (InvalidOperationException exception) { return Finish("Failure", exception.Message); }

        QualificationResult Finish(string status, string reason) => new(
            new(status, reason, 0, 0, 0, 0, 0, "No performance claim"),
            samples.ToArray(), quietChecks.ToArray(), libraryWarmup, referenceWarmup);

        async Task ExecuteAsync(bool library)
        {
            var result = library
                ? await ReferenceExecution.RunLibraryAsync(prepared.Workload, false, deadline.Token).ConfigureAwait(false)
                : await ReferenceExecution.RunReferenceAsync(executable, prepared, false, deadline.Token).ConfigureAwait(false);
            ReferenceExecution.Verify(result, prepared.Workload.ExpectedDigest);
        }

        async Task<int> WarmAsync(bool library)
        {
            var watch = Stopwatch.StartNew();
            var calls = 0;
            while (calls < 32 || watch.ElapsedMilliseconds < 1_000)
            {
                await ExecuteAsync(library).ConfigureAwait(false);
                calls++;
                if (library)
                    libraryWarmup = calls;
                else
                    referenceWarmup = calls;
            }
            return calls;
        }

        async Task<int> CalibrateAsync(bool library)
        {
            var iterations = 1;
            while (true)
            {
                var first = await TimeAsync(library, iterations).ConfigureAwait(false);
                var second = await TimeAsync(library, iterations).ConfigureAwait(false);
                if (Math.Min(first, second) >= 40 || iterations >= 4_096)
                    return iterations;
                iterations = Math.Min(iterations * 2, 4_096);
            }
        }

        async Task<double> TimeAsync(bool library, int iterations)
        {
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
                await ExecuteAsync(library).ConfigureAwait(false);
            return watch.Elapsed.TotalMilliseconds;
        }
    }
}
