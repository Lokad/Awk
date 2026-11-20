using System.Text;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;

namespace Lokad.Awk.Benchmarking;

internal static class ReferenceComparison
{
    internal static int RunBenchmarks(Type[] types, string[] arguments)
    {
        var steady = arguments.Contains("--steady", StringComparer.Ordinal);
        var snapshot = arguments.Contains("--snapshot", StringComparer.Ordinal);
        if (steady || snapshot)
            arguments = arguments.Where(argument => argument is not ("--steady" or "--snapshot")).ToArray();
        if (snapshot)
            arguments = [.. arguments, "--launchCount", "1", "--warmupCount", "4", "--iterationCount", "5",
                "--iterationTime", "500", "--exporters", "json"];

        var config = ManualConfig.Create(DefaultConfig.Instance).AddFilter(new SimpleFilter(Include));
        var summaries = BenchmarkSwitcher.FromTypes(types).Run(arguments, config).ToArray();
        if (summaries.Length > 0)
            return summaries.All(summary => !summary.HasCriticalValidationErrors && summary.Reports.Length > 0 &&
                summary.Reports.All(report => report.Success)) ? 0 : 1;

        // These CLI commands intentionally produce no measurement summaries.
        var list = Array.IndexOf(arguments, "--list");
        var isListing = list >= 0 && list + 1 < arguments.Length &&
            arguments[list + 1].ToLowerInvariant() is "flat" or "tree";
        return isListing || arguments.Any(argument => argument is "--help" or "--version" or "--info" ||
            argument.ToLowerInvariant() is "--list=flat" or "--list=tree") ? 0 : 1;

        bool Include(BenchmarkCase benchmark)
        {
            if (benchmark.Descriptor.Type != typeof(ReferenceAwkBenchmarks) &&
                benchmark.Descriptor.Type != typeof(WorkloadAllocationBenchmarks)) return !steady;
            var scale = benchmark.Parameters[nameof(ReferenceAwkBenchmarks.Scale)];
            if (!Equals(scale, ReferenceWorkloads.SteadyScale)) return !steady;
            return steady && benchmark.Parameters[nameof(ReferenceAwkBenchmarks.Workload)] is string name &&
                ReferenceWorkloads.IsSteadyFamily(name);
        }
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
                library.Output.AsSpan().SequenceEqual(workload.ExpectedOutput.Span) &&
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
        if (mode != "verify")
            throw new ArgumentException("Use --mode verify; timings belong to BenchmarkDotNet.");
        var output = ArtifactPath(options.GetValueOrDefault("--output",
            smoke ? "BenchmarkDotNet.Artifacts/reference/smoke.json" : "BenchmarkDotNet.Artifacts/reference/verification.json"));
        var name = options.GetValueOrDefault("--case", "*");
        var scale = options.GetValueOrDefault("--scale", smoke ? "small" : "all");
        var cases = ReferenceWorkloads.Select(name, scale, includeControls: smoke).ToArray();
        if (cases.Length == 0)
            throw new ArgumentException("No workloads match --case/--scale.");
        var host = await ReferenceReport.IdentifyHostAsync(token).ConfigureAwait(false);
        string? executable = smoke && !options.ContainsKey("--awk") &&
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LOKAD_AWK_ORACLE_EXE"))
            ? null : ResolveExecutable(options.GetValueOrDefault("--awk"));
        var reference = executable is null ? null : await IdentifyReferenceAsync(executable, token).ConfigureAwait(false);
        var rows = new List<ReferenceComparisonRow>();
        var complete = false;
        await SaveAsync().ConfigureAwait(false);
        try
        {
            foreach (var (family, size) in cases)
            {
                token.ThrowIfCancellationRequested();
                var workload = ReferenceWorkloads.Create(family, size);
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
                rows.Add(new(ReferenceReport.Describe(workload), verified));
                Console.WriteLine(workload.Name + "/" + workload.Scale + ": " + verified.Status);
                await SaveAsync().ConfigureAwait(false);
            }
            complete = true;
            await SaveAsync().ConfigureAwait(false);
            Console.WriteLine("Correctness only; no timings collected.");
            return rows.All(row => row.Verification.Status == "Equivalent") ? 0 : 1;
        }
        catch
        {
            mode = token.IsCancellationRequested ? "interrupted" : "failed";
            await SaveAsync().ConfigureAwait(false);
            throw;
        }

        Task SaveAsync() => ReferenceReport.SaveAsync(output,
            ReferenceReport.Create(mode, host, reference, cases.Length, complete, rows), CancellationToken.None);
    }

    internal static Dictionary<string, string> Options(string[] arguments)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < arguments.Length; i += 2)
        {
            if (i + 1 >= arguments.Length || arguments[i] is not ("--awk" or "--mode" or "--case" or "--scale" or "--output") ||
                !options.TryAdd(arguments[i], arguments[i + 1]))
                throw new ArgumentException("Usage: --compare|--smoke [--awk PATH] [--mode verify] " +
                    "[--case NAME] [--scale small|medium|large|steady|control|all] [--output BenchmarkDotNet.Artifacts/PATH]");
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
