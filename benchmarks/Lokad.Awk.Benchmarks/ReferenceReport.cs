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
internal sealed record ReferenceComparisonRow(ReferenceWorkloadInfo Workload, ReferenceVerification Verification);
internal sealed record ReferenceComparisonReport(int SchemaVersion, string Mode, DateTimeOffset UpdatedUtc,
    string Contract, ReferenceHostProvenance Host, ReferenceIdentity? Reference, object Policy,
    int RequestedCases, bool Complete, ReferenceComparisonRow[] Cases);

internal static class ReferenceReport
{
    public const string Contract =
        "Public Lokad.Awk command execution versus GNU awk CLI, checking exact expected stdout and successful completion. " +
        "GNU uses processes and native fixture files; Lokad uses an embedded in-memory host. No timings collected.";
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
        ReferenceIdentity? reference, int cases, bool complete,
        IReadOnlyList<ReferenceComparisonRow> rows)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var policy = new
        {
            Preset = "AwkExecutionPolicy.CreateDefault; interpreter; host Utf8RegexAdapter; TimeZone=UTC",
            limits.MaxFunctionCallDepth,
            InvocationTimeoutSeconds = ReferenceExecution.InvocationTimeout.TotalSeconds,
            DriverOutputBytes = 64L << 20,
            DriverErrorBytes = 64 << 10
        };
        return new(2, mode, DateTimeOffset.UtcNow, Contract, host, reference, policy, cases, complete, rows.ToArray());
    }

    public static async Task SaveAsync(string path, ReferenceComparisonReport report, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Missing report directory."));
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(report, JsonOptions), token).ConfigureAwait(false);
        File.Move(temporary, path, overwrite: true);
    }
}
