using System.Text.Json;

namespace Lokad.Awk.Benchmarking;

internal static class ReferenceContractRunner
{
    public static async Task<int> RunAsync(string[] arguments, CancellationToken token)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < arguments.Length; i += 2)
        {
            if (i + 1 >= arguments.Length || arguments[i] is not ("--awk" or "--matcher" or "--output") ||
                !options.TryAdd(arguments[i], arguments[i + 1]))
                throw new ArgumentException("Usage: --reference-contracts [--awk PATH] [--matcher default|legacy] " +
                    "[--output BenchmarkDotNet.Artifacts/PATH]");
        }
        var matcher = options.GetValueOrDefault("--matcher", "default");
        if (matcher is not ("default" or "legacy"))
            throw new ArgumentException("--matcher must be default or legacy.");
        var path = ReferenceComparison.ArtifactPath(options.GetValueOrDefault("--output",
            "BenchmarkDotNet.Artifacts/reference/contracts-" + matcher + ".json"));
        var supplied = options.GetValueOrDefault("--awk") ?? Environment.GetEnvironmentVariable("LOKAD_AWK_ORACLE_EXE");
        var executable = string.IsNullOrWhiteSpace(supplied) ? null : ReferenceComparison.ResolveExecutable(supplied);
        var identity = executable is null ? null : await ReferenceComparison.IdentifyReferenceAsync(executable, token).ConfigureAwait(false);
        var host = await ReferenceReport.IdentifyHostAsync(token).ConfigureAwait(false);
        var rows = new List<object>();
        var success = true;
        foreach (var contract in ReferenceContracts.Cases)
        {
            var workload = new ReferenceWorkload(contract.Name, "control", 0, 0, contract.Program, [],
                contract.InputFile.Length == 0 ? [] : [new("/" + contract.InputFile, contract.Input)],
                contract.ExpectedBytes, "C.UTF-8");
            var library = await ReferenceExecution.RunLibraryAsync(workload, true, token).ConfigureAwait(false);
            var libraryMatches = library.IsSuccess && library.Output.AsSpan().SequenceEqual(workload.ExpectedOutput);
            ReferenceExecutionResult? native = null;
            var classification = new ReferenceContractClassification("NotRun", "No GNU executable selected.");
            if (executable is not null && identity is not null)
            {
                using var prepared = new PreparedReferenceWorkload(workload);
                native = await ReferenceProcess.RunAsync(executable, workload.Arguments, [], true,
                    ReferenceExecution.InvocationTimeout, token, workload.Locale, prepared.DirectoryPath,
                    legacyGnuMatchers: matcher == "legacy").ConfigureAwait(false);
                classification = ReferenceContracts.Classify(contract, identity.Version, native.ExitCode, native.Output, native.Error);
            }
            success &= libraryMatches && classification.Status != "UnexpectedDifference";
            rows.Add(new
            {
                Workload = ReferenceReport.Describe(workload), LibraryMatches = libraryMatches,
                Library = Describe(library), Reference = native is null ? null : Describe(native),
                Classification = classification, EligibleForTiming = libraryMatches && classification.Equivalent
            });
            Console.WriteLine(contract.Name + ": library " + (libraryMatches ? "Equivalent" : "Mismatch") +
                "; GNU " + classification.Status);
        }
        var report = new
        {
            SchemaVersion = 1, Mode = "reference-contracts", UpdatedUtc = DateTimeOffset.UtcNow,
            Host = host, Reference = identity, Locale = "C.UTF-8", TimeZone = "UTC", Matcher = matcher,
            GawkGnuMatchers = matcher == "legacy" ? "1" : "unset",
            Complete = true, NativeChecked = identity is not null, TimingsCollected = false,
            Contract = "Known reference differences retain Awk expectations and never qualify equivalent-work timings.",
            Cases = rows
        };
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Missing artifact directory."));
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(report, ReferenceReport.JsonOptions), token).ConfigureAwait(false);
        File.Move(temporary, path, overwrite: true);
        return success ? 0 : 1;

        static object Describe(ReferenceExecutionResult result) => new
        {
            result.ExitCode, result.OutputDigest, result.ErrorDigest,
            StdOutHex = Convert.ToHexStringLower(result.Output), StdErrHex = Convert.ToHexStringLower(result.Error)
        };
    }
}
