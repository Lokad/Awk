using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lokad.Awk;

namespace Lokad.Awk.Benchmarking;

internal sealed record ReferenceIdentity(string Path, string Version, string VersionOutput, string Sha256);
internal sealed record ReferenceSourceSnapshot(string OriginCommit, bool OriginCleanCheckout,
    string SourceSha256, string ArchiveSha256, string SnapshotCommit);
internal sealed record ReferenceHostProvenance(string Commit, bool CleanCheckout, string SourceSha256,
    string Sdk, string Runtime, string OperatingSystem, string Architecture, string Cpu, int LogicalProcessors,
    string MemoryInfo, string BenchmarkBuild, string LibraryBuild, string BenchmarkAssemblySha256, string LibraryAssemblySha256,
    ReferenceSourceSnapshot? Snapshot);
internal sealed record ReferenceInputInfo(string Name, OutputDigest Digest);
internal sealed record ReferenceWorkloadInfo(string Name, string Scale, int Records, int Fields, long InputBytes,
    string[] Arguments, string Locale, string TimeZone, string ProgramSha256, ReferenceInputInfo[] Inputs, OutputDigest ExpectedOutput);
internal sealed record ReferenceVerification(string Status, string Reason, OutputDigest LibraryOutput,
    OutputDigest NativeOutput, ReferenceCounters? Counters);
internal sealed record ReferenceComparisonRow(ReferenceWorkloadInfo Workload, ReferenceVerification Verification,
    QualificationResult Timing);
internal sealed record ReferenceComparisonReport(int SchemaVersion, string Mode, DateTimeOffset UpdatedUtc,
    string Contract, ReferenceHostProvenance Host, ReferenceIdentity? Reference, object Policy,
    QuietCheck MachineGate, int RequestedCases, int RequestedPairs, bool Complete, ReferenceComparisonRow[] Cases);

internal static class ReferenceReport
{
    public const string Contract =
        "Warm embedded Lokad.Awk public full invocation versus GNU awk CLI. Both include compilation, execution, " +
        "complete output consumption and SHA-256. GNU includes launch and pipes; multi-file cases use native fixture " +
        "files versus in-memory host files. Fixture generation and preflight are excluded. No startup subtraction or overall speedup.";
    public const string BeginMarker = "<!-- BEGIN GENERATED AWK COMPARISON -->";
    public const string EndMarker = "<!-- END GENERATED AWK COMPARISON -->";
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static ReferenceWorkloadInfo Describe(ReferenceWorkload workload) =>
        new(workload.Name, workload.Scale, workload.Records, workload.Fields, workload.InputBytes, workload.Arguments,
            workload.Locale, "UTC", Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(workload.Program))),
            [new("stdin", OutputDigest.FromBytes(workload.StandardInput)),
             .. workload.Files.Select(file => new ReferenceInputInfo(file.Path, OutputDigest.FromBytes(file.ContentBytes)))],
            workload.ExpectedDigest);

    public static async Task<ReferenceHostProvenance> IdentifyHostAsync(CancellationToken token)
    {
        var commit = await QueryAsync("git", ["rev-parse", "HEAD"]).ConfigureAwait(false);
        var status = await QueryAsync("git", ["status", "--porcelain", "--untracked-files=normal"]).ConfigureAwait(false);
        var files = await QueryAsync("git", ["ls-files", "--cached", "--others", "--exclude-standard"]).ConfigureAwait(false);
        var sourceHash = "Unavailable";
        if (files != "Unavailable")
        {
            using var manifest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var name in files.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(name => name.TrimEnd('\r')).Order(StringComparer.Ordinal))
            {
                if (!File.Exists(name))
                    continue;
                manifest.AppendData(Encoding.UTF8.GetBytes(name + "\0"));
                await using var file = File.OpenRead(name);
                manifest.AppendData(await SHA256.HashDataAsync(file, token).ConfigureAwait(false));
            }
            sourceHash = Convert.ToHexStringLower(manifest.GetHashAndReset());
        }
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        var dotnet = dotnetRoot is null ? "dotnet" : Path.Combine(dotnetRoot, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        var sdk = await QueryAsync(dotnet, ["--version"]).ConfigureAwait(false);
        var benchmarkAssembly = typeof(ReferenceReport).Assembly;
        var libraryAssembly = typeof(Awk).Assembly;
        var cpu = OperatingSystem.IsWindows() ? Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unavailable"
            : ReadLines("/proc/cpuinfo").FirstOrDefault(line => line.StartsWith("model name", StringComparison.Ordinal))?.Split(':', 2)[^1].Trim() ?? "Unavailable";
        var memory = string.Join("; ", ReadLines("/proc/meminfo").Where(line =>
            line.StartsWith("MemTotal:", StringComparison.Ordinal) || line.StartsWith("SwapTotal:", StringComparison.Ordinal)));
        ReferenceSourceSnapshot? snapshot = null;
        var snapshotPath = Environment.GetEnvironmentVariable("AWK_BENCH_SNAPSHOT_MANIFEST");
        if (!string.IsNullOrWhiteSpace(snapshotPath))
        {
            snapshot = JsonSerializer.Deserialize<ReferenceSourceSnapshot>(
                await File.ReadAllTextAsync(snapshotPath, token).ConfigureAwait(false), JsonOptions)
                ?? throw new InvalidOperationException("Missing source snapshot provenance.");
            if (snapshot.SourceSha256 != sourceHash || snapshot.SnapshotCommit != commit)
                throw new InvalidOperationException("Source snapshot provenance does not match this checkout.");
        }
        return new(commit, commit != "Unavailable" && status.Length == 0, sourceHash, sdk,
            RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture.ToString(),
            cpu, Environment.ProcessorCount, memory, Build(benchmarkAssembly), Build(libraryAssembly),
            await HashFileAsync(benchmarkAssembly.Location, token).ConfigureAwait(false),
            await HashFileAsync(libraryAssembly.Location, token).ConfigureAwait(false), snapshot);

        async Task<string> QueryAsync(string executable, string[] arguments)
        {
            try
            {
                var result = await ReferenceProcess.RunAsync(executable, arguments, [], true,
                    TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
                return result.IsSuccess ? Encoding.UTF8.GetString(result.Output).Trim() : "Unavailable";
            }
            catch (Exception exception) when (exception is IOException or System.ComponentModel.Win32Exception or TimeoutException)
            {
                return "Unavailable";
            }
        }

        static string Build(Assembly assembly) =>
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unavailable";
        static string[] ReadLines(string path)
        {
            if (!OperatingSystem.IsLinux())
                return [];
            try { return File.ReadAllLines(path); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return []; }
        }
    }

    public static async Task<string> HashFileAsync(string path, CancellationToken token)
    {
        await using var file = File.OpenRead(path);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(file, token).ConfigureAwait(false));
    }

    public static ReferenceComparisonReport Create(string mode, ReferenceHostProvenance host,
        ReferenceIdentity? reference, QuietCheck gate, int cases, int pairs, bool complete,
        IReadOnlyList<ReferenceComparisonRow> rows)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var policy = new
        {
            Preset = "AwkExecutionPolicy.CreateDefault; interpreter; DotNetCompatibility; TimeZone=UTC",
            limits.MaxExecutionSteps, limits.MaxHostCalls, limits.MaxFunctionCallDepth,
            limits.MaxExecutionMemoryBytes, limits.MaxArrayElements, limits.MaxFields,
            limits.MaxInputBytes, limits.MaxOutputBytes, limits.MaxRecordBytes,
            limits.MaxRegexOperations, limits.MaxRegexRecordSeparatorBytesScanned, limits.MaxSortComparisons,
            InvocationTimeoutSeconds = ReferenceExecution.InvocationTimeout.TotalSeconds,
            DriverOutputBytes = 64L << 20, DriverErrorBytes = 64 << 10
        };
        return new(1, mode, DateTimeOffset.UtcNow, Contract, host, reference, policy, gate, cases, pairs, complete, rows.ToArray());
    }

    public static async Task SaveAsync(string path, ReferenceComparisonReport report, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Missing report directory."));
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(report, JsonOptions), token).ConfigureAwait(false);
        File.Move(temporary, path, overwrite: true);
    }

    public static string Render(ReferenceComparisonReport report)
    {
        if (report.SchemaVersion != 1)
            throw new InvalidOperationException("Unsupported comparison artifact schema.");
        var claims = ReferenceClaimPolicy.CanPublish(report.Mode, report.Complete, report.Host.CleanCheckout,
            report.MachineGate.IsQuiet, report.Host.Commit != "Unavailable" &&
            report.Host.BenchmarkBuild.Contains(report.Host.Commit, StringComparison.Ordinal) &&
            report.Host.LibraryBuild.Contains(report.Host.Commit, StringComparison.Ordinal),
            report.Reference is not null, report.RequestedCases, report.Cases.Length,
            report.Cases.All(row => row.Verification.Status == "Equivalent"));
        var text = new StringBuilder().AppendLine(BeginMarker).AppendLine("## Recorded GNU awk comparison").AppendLine();
        text.AppendLine("Recorded UTC: " + report.UpdatedUtc.UtcDateTime.ToString("u",
            System.Globalization.CultureInfo.InvariantCulture) + "; mode " + Escape(report.Mode) + ".");
        text.AppendLine("Revision: " + Escape(report.Host.Commit) + "; SDK " + Escape(report.Host.Sdk) +
            "; " + Escape(report.Host.Runtime) + "; " + Escape(report.Host.OperatingSystem) + ".");
        text.AppendLine("Machine: " + Escape(report.Host.Cpu) + "; " + report.Host.LogicalProcessors +
            " logical processors; " + Escape(report.Host.MemoryInfo) + ".");
        text.AppendLine("Assembly SHA-256: benchmark " + Escape(report.Host.BenchmarkAssemblySha256) +
            "; library " + Escape(report.Host.LibraryAssemblySha256) + ".");
        if (report.Host.Snapshot is { } snapshot)
            text.AppendLine("Private source snapshot from " + Escape(snapshot.OriginCommit) +
                " (origin clean: " + snapshot.OriginCleanCheckout + "); source SHA-256 " +
                Escape(snapshot.SourceSha256) + "; archive SHA-256 " + Escape(snapshot.ArchiveSha256) + ".");
        text.AppendLine("Reference: " + Escape(report.Reference?.Version ?? "none") + "; SHA-256 " +
            Escape(report.Reference?.Sha256 ?? "none") + ".").AppendLine();
        text.AppendLine(Contract).AppendLine();
        if (!claims)
            text.AppendLine("No qualified performance claim: verification-only, paused, incomplete, or missing clean/quiet qualification.").AppendLine();
        text.AppendLine("| Case / scale | Records | Input bytes | Status | Lokad ms | GNU ms | GNU / Lokad, 95% CI | Interpretation |");
        text.AppendLine("| --- | ---: | ---: | --- | ---: | ---: | --- | --- |");
        foreach (var row in report.Cases)
        {
            var timing = row.Timing.Summary;
            var qualified = claims && row.Verification.Status == "Equivalent" && timing.Status == "Qualified";
            var numbers = qualified
                ? FormattableString.Invariant($"{timing.LibraryMedianMs:F3} | {timing.ReferenceMedianMs:F3} | {timing.Ratio:F3} [{timing.RatioLow:F3}, {timing.RatioHigh:F3}]")
                : "— | — | —";
            text.AppendLine($"| {Escape(row.Workload.Name)}/{Escape(row.Workload.Scale)} | {row.Workload.Records} | {row.Workload.InputBytes} | " +
                $"{Escape(row.Verification.Status == "Equivalent" ? timing.Status : row.Verification.Status)} | {numbers} | " +
                $"{Escape(qualified ? timing.Interpretation : row.Verification.Reason.Length > 0 ? row.Verification.Reason : timing.Reason)} |");
        }
        return text.AppendLine().AppendLine(EndMarker).ToString();

        static string Escape(string value) => System.Net.WebUtility.HtmlEncode(value)
            .Replace("|", "\\|", StringComparison.Ordinal).Replace('\r', ' ').Replace('\n', ' ');
    }

    public static async Task RenderFileAsync(string artifact, CancellationToken token)
    {
        var report = JsonSerializer.Deserialize<ReferenceComparisonReport>(
            await File.ReadAllTextAsync(artifact, token).ConfigureAwait(false), JsonOptions)
            ?? throw new InvalidOperationException("Missing comparison report.");
        const string path = "docs/BENCHMARKS.md";
        var content = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
        var begin = content.IndexOf(BeginMarker, StringComparison.Ordinal);
        if (begin >= 0)
        {
            var end = content.IndexOf(EndMarker, begin, StringComparison.Ordinal);
            if (end < 0)
                throw new InvalidOperationException("Missing comparison report end marker.");
            var after = content[(end + EndMarker.Length)..].TrimStart('\r', '\n');
            content = content[..begin] + Render(report) + (after.Length == 0 ? "" : "\n" + after);
        }
        else
            content = content.TrimEnd() + "\n\n" + Render(report);
        await File.WriteAllTextAsync(path, content, token).ConfigureAwait(false);
    }
}
