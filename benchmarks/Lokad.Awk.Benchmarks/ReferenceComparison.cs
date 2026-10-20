using System.Text;
using BenchmarkDotNet.Running;

namespace Lokad.Awk.Benchmarking;

internal static class ReferenceComparison
{
    internal static int RunBenchmarks(Type[] types, string[] arguments)
    {
        var summaries = BenchmarkSwitcher.FromTypes(types).Run(arguments).ToArray();
        if (summaries.Length > 0)
            return summaries.All(summary => !summary.HasCriticalValidationErrors && summary.Reports.Length > 0 &&
                summary.Reports.All(report => report.Success)) ? 0 : 1;

        // These CLI commands intentionally produce no measurement summaries.
        var list = Array.IndexOf(arguments, "--list");
        var isListing = list >= 0 && list + 1 < arguments.Length &&
            arguments[list + 1].ToLowerInvariant() is "flat" or "tree";
        return isListing || arguments.Any(argument => argument is "--help" or "--version" or "--info" ||
            argument.ToLowerInvariant() is "--list=flat" or "--list=tree") ? 0 : 1;
    }

    public static async Task<int> RunCommandAsync(string[] arguments, CancellationToken token)
    {
        if (arguments[0] == "--reference-benchmarks")
        {
            var forwarded = arguments.Skip(1).ToList();
            var index = forwarded.IndexOf("--awk");
            string? option = null;
            if (index >= 0)
            {
                if (index + 1 >= forwarded.Count)
                    throw new ArgumentException("--awk requires an executable path.");
                option = forwarded[index + 1];
                forwarded.RemoveRange(index, 2);
            }
            var executable = ResolveExecutable(option);
            _ = await IdentifyReferenceAsync(executable, token).ConfigureAwait(false);
            Environment.SetEnvironmentVariable("LOKAD_AWK_ORACLE_EXE", executable);
            return RunBenchmarks([typeof(ReferenceAwkBenchmarks)], forwarded.ToArray());
        }
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            if (arguments[0] == "--reference-contracts")
                return await ReferenceContractRunner.RunAsync(arguments.Skip(1).ToArray(), cancellation.Token).ConfigureAwait(false);
            if (arguments[0] == "--driver-smoke")
            {
                var options = Options(arguments.Skip(1).ToArray());
                if (options.Keys.Any(key => key != "--awk"))
                    throw new ArgumentException("Usage: --driver-smoke [--awk PATH]");
                var executable = ResolveExecutable(options.GetValueOrDefault("--awk"));
                _ = await IdentifyReferenceAsync(executable, cancellation.Token).ConfigureAwait(false);
                await ReferenceProcessSmoke.RunAsync(executable, cancellation.Token).ConfigureAwait(false);
                return 0;
            }
            if (arguments[0] == "--check-machine")
            {
                var quiet = await MachineQuietProbe.CheckAsync(5, TimeSpan.FromSeconds(1), cancellation.Token).ConfigureAwait(false);
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(quiet, ReferenceReport.JsonOptions));
                return quiet.IsQuiet ? 0 : 3;
            }
            if (arguments[0] == "--render-report" && arguments.Length == 2)
            {
                await ReferenceReport.RenderFileAsync(arguments[1], cancellation.Token).ConfigureAwait(false);
                return 0;
            }
            return await RunAsync(arguments, cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Comparison interrupted."); return 130; }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
        finally { Console.CancelKeyPress -= handler; }
    }

    public static string ResolveExecutable(string? option)
    {
        var supplied = option ?? Environment.GetEnvironmentVariable("LOKAD_AWK_ORACLE_EXE");
        if (string.IsNullOrWhiteSpace(supplied))
            throw new ArgumentException("Supply --awk PATH or LOKAD_AWK_ORACLE_EXE for an explicitly requested reference run.");
        var path = Path.GetFullPath(supplied);
        if (!File.Exists(path))
            throw new ArgumentException("Reference executable does not exist: " + path);
        path = File.ResolveLinkTarget(path, returnFinalTarget: true)?.FullName ?? path;
        if (path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part.Equals("external", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Supply an independently installed reference outside external/.");
        return path;
    }

    public static async Task<ReferenceIdentity> IdentifyReferenceAsync(string executable, CancellationToken token)
    {
        var result = await ReferenceProcess.RunAsync(executable, ["--version"], [], true,
            TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
        var output = Encoding.UTF8.GetString(result.Output).Trim();
        var version = output.Split('\n')[0].Trim();
        if (!result.IsSuccess || !version.StartsWith("GNU Awk ", StringComparison.Ordinal))
            throw new InvalidOperationException("The primary benchmark reference must identify itself as GNU awk.");
        return new(executable, version, output, await ReferenceReport.HashFileAsync(executable, token).ConfigureAwait(false));
    }

    public static async Task<ReferenceVerification> VerifyAsync(string executable, PreparedReferenceWorkload prepared,
        CancellationToken token)
    {
        var empty = OutputDigest.FromBytes([]);
        try
        {
            var workload = prepared.Workload;
            var library = await ReferenceExecution.RunLibraryAsync(workload, true, token).ConfigureAwait(false);
            var reference = await ReferenceExecution.RunReferenceAsync(executable, prepared, true, token).ConfigureAwait(false);
            var equivalent = library.IsSuccess && reference.IsSuccess &&
                library.Output.AsSpan().SequenceEqual(reference.Output) &&
                library.Output.AsSpan().SequenceEqual(workload.ExpectedOutput) &&
                library.OutputDigest == workload.ExpectedDigest && reference.OutputDigest == workload.ExpectedDigest;
            if (!equivalent)
                return new("Mismatch", $"Exit {library.ExitCode}/{reference.ExitCode}; " +
                    $"stderr {Encoding.UTF8.GetString(library.Error)} / {Encoding.UTF8.GetString(reference.Error)}; " +
                    $"stdout digests {library.OutputDigest.Sha256} / {reference.OutputDigest.Sha256}; expected {workload.ExpectedDigest.Sha256}",
                    library.OutputDigest, reference.OutputDigest, null);
            return new("Equivalent", "", library.OutputDigest, reference.OutputDigest,
                await ReferenceExecution.CountersAsync(workload, token).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { token.ThrowIfCancellationRequested(); return new("Timeout", "Preflight deadline exceeded.", empty, empty, null); }
        catch (TimeoutException) { return new("Timeout", "Preflight deadline exceeded.", empty, empty, null); }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            return new("Failure", exception.Message, empty, empty, null);
        }
    }

    private static async Task<int> RunAsync(string[] arguments, CancellationToken token)
    {
        var options = Options(arguments.Skip(1).ToArray());
        var smoke = arguments[0] == "--smoke";
        var mode = options.GetValueOrDefault("--mode", "verify");
        if (mode is not ("verify" or "qualify") || smoke && mode != "verify")
            throw new ArgumentException("Use --mode verify|qualify; smoke is correctness only.");
        var output = ArtifactPath(options.GetValueOrDefault("--output",
            smoke ? "BenchmarkDotNet.Artifacts/reference/smoke.json" : "BenchmarkDotNet.Artifacts/reference/verification.json"));
        var name = options.GetValueOrDefault("--case", "*");
        var scale = options.GetValueOrDefault("--scale", smoke ? "small" : "all");
        if (!int.TryParse(options.GetValueOrDefault("--pairs", "11"), out var pairs) || pairs < 9 || pairs > 31)
            throw new ArgumentException("--pairs must be between 9 and 31.");
        var workloads = ReferenceWorkloads.CreateAll().Where(workload =>
            (name == "*" || workload.Name == name) &&
            (scale == "all" || workload.Scale == scale || smoke && workload.Scale == "control")).ToArray();
        if (workloads.Length == 0)
            throw new ArgumentException("No workloads match --case/--scale.");
        var host = await ReferenceReport.IdentifyHostAsync(token).ConfigureAwait(false);
        string? executable = smoke && !options.ContainsKey("--awk") &&
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LOKAD_AWK_ORACLE_EXE"))
            ? null : ResolveExecutable(options.GetValueOrDefault("--awk"));
        var reference = executable is null ? null : await IdentifyReferenceAsync(executable, token).ConfigureAwait(false);
        if (mode == "qualify")
        {
#if DEBUG
            throw new InvalidOperationException("Qualification requires a Release build.");
#else
            if (!host.CleanCheckout || !host.BenchmarkBuild.Contains(host.Commit, StringComparison.Ordinal) ||
                !host.LibraryBuild.Contains(host.Commit, StringComparison.Ordinal))
                throw new InvalidOperationException("Qualification requires a clean committed checkout and assemblies built from that revision.");
#endif
        }
        var gate = mode == "qualify"
            ? await MachineQuietProbe.CheckAsync(5, TimeSpan.FromSeconds(1), token).ConfigureAwait(false)
            : new QuietCheck(false, "Correctness verification does not qualify quietness.", []);
        var rows = new List<ReferenceComparisonRow>();
        var complete = false;
        if (mode == "qualify" && !gate.IsQuiet)
        {
            mode = "paused";
            await SaveAsync().ConfigureAwait(false);
            Console.WriteLine(gate.Reason);
            return 3;
        }
        await SaveAsync().ConfigureAwait(false);
        try
        {
            foreach (var workload in workloads)
            {
                token.ThrowIfCancellationRequested();
                using var prepared = new PreparedReferenceWorkload(workload);
                ReferenceVerification verified;
                if (executable is null)
                {
                    var result = await ReferenceExecution.RunLibraryAsync(workload, true, token).ConfigureAwait(false);
                    ReferenceExecution.Verify(result, workload.ExpectedDigest);
                    verified = new("Equivalent", "Library smoke; no native comparison.", result.OutputDigest,
                        OutputDigest.FromBytes([]), await ReferenceExecution.CountersAsync(workload, token).ConfigureAwait(false));
                }
                else verified = await VerifyAsync(executable, prepared, token).ConfigureAwait(false);
                var timing = mode == "qualify" && verified.Status == "Equivalent" && executable is not null
                    ? await TimingQualification.RunAsync(executable, prepared, pairs, token).ConfigureAwait(false)
                    : QualificationResult.NotMeasured;
                rows.Add(new(ReferenceReport.Describe(workload), verified, timing));
                Console.WriteLine(workload.Name + "/" + workload.Scale + ": " + verified.Status + "/" + timing.Summary.Status);
                if (timing.Summary.Status is "Paused" or "Interrupted")
                {
                    mode = timing.Summary.Status.ToLowerInvariant();
                    await SaveAsync().ConfigureAwait(false);
                    return mode == "paused" ? 3 : 130;
                }
                await SaveAsync().ConfigureAwait(false);
            }
            complete = true;
            await SaveAsync().ConfigureAwait(false);
            Console.WriteLine(mode == "verify" ? "Correctness only; no timings collected." : "Qualification checkpoint saved.");
            return rows.All(row => row.Verification.Status == "Equivalent" &&
                (mode == "verify" || row.Timing.Summary.Status == "Qualified")) ? 0 : 1;
        }
        catch
        {
            mode = token.IsCancellationRequested ? "interrupted" : "failed";
            await SaveAsync().ConfigureAwait(false);
            throw;
        }

        Task SaveAsync() => ReferenceReport.SaveAsync(output,
            ReferenceReport.Create(mode, host, reference, gate, workloads.Length, pairs, complete, rows), CancellationToken.None);
    }

    internal static Dictionary<string, string> Options(string[] arguments)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < arguments.Length; i += 2)
        {
            if (i + 1 >= arguments.Length || arguments[i] is not ("--awk" or "--mode" or "--case" or "--scale" or "--output" or "--pairs") ||
                !options.TryAdd(arguments[i], arguments[i + 1]))
                throw new ArgumentException("Usage: --compare|--smoke [--awk PATH] [--mode verify|qualify] " +
                    "[--case NAME] [--scale small|medium|large|control|all] [--pairs 11] [--output BenchmarkDotNet.Artifacts/PATH]");
        }
        return options;
    }

    internal static string ArtifactPath(string requested)
    {
        var path = Path.GetFullPath(requested);
        var root = Path.GetFullPath("BenchmarkDotNet.Artifacts") + Path.DirectorySeparatorChar;
        if (!path.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new ArgumentException("Raw comparison artifacts must stay under BenchmarkDotNet.Artifacts/.");
        return path;
    }
}
