using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lokad.Awk.Benchmarking;
using Xunit.Abstractions;

namespace Lokad.Awk.Tests;

public sealed class AwkDifferentialTests(ITestOutputHelper output)
{
    internal const string ExecutableEnvironment = "LOKAD_AWK_ORACLE_EXE";
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    private static readonly Lazy<Task<NativeIdentity>> Identity = new(IdentifyAsync);

    public static IEnumerable<object[]> Cases()
    {
        foreach (var test in AwkGoldenCorpus.DirectCases.Where(test => test.Tags.Contains(AwkGoldenTag.Idiom))
            .Concat(AwkFusionBoundaryTests.NativeCases()).DistinctBy(test => test.Name))
            yield return [new AwkDifferentialCase(test)];

        var diagnosticNames = new[]
        {
            "computed-field-validates-builtin-arity-before-effects",
            "getline-nested-source-invalid-function-before-effects",
            "getline-expression-nested-source-invalid-function-before-effects"
        };
        var diagnostics = diagnosticNames.Select(name => AwkGoldenCorpus.DirectCases.Single(test => test.Name == name))
            .Concat(AwkGoldenCorpus.DirectCases.Where(test => test.Name.StartsWith("for-invalid-", StringComparison.Ordinal)));
        foreach (var diagnostic in diagnostics)
            yield return [new AwkDifferentialCase(diagnostic, 1,
                "GNU reports this compile error as status 1; Lokad.Awk uses fatal status 2.")];
    }

    [AwkOracleTheory]
    [MemberData(nameof(Cases))]
    public async Task PublicExecutionMatchesGnu(AwkDifferentialCase test)
    {
        var identity = await Identity.Value;
        var golden = test.Golden;
        Assert.Empty(golden.ReadOpenErrors);
        Assert.Empty(golden.Environment);
        Assert.False(AwkRegexProviderGaps.AffectsExecution(golden),
            "Provider selection deferrals need their own declared differential profile.");

        using var fixture = new AwkDifferentialFiles(golden);
        var native = await ReferenceProcess.RunAsync(identity.Executable, golden.Arguments,
            Encoding.UTF8.GetBytes(golden.StandardInput), capture: true, Deadline,
            CancellationToken.None, locale: "C.UTF-8", workingDirectory: fixture.WorkingDirectory);
        var reference = new AwkDifferentialObservation(native.ExitCode, native.Output, native.Error)
        { Files = fixture.Capture(golden) };
        foreach (var governed in new[] { false, true })
        {
            var files = AwkTestHarness.BuildFileSystem(golden);
            var budget = new CooperativeTestBudget(128 << 20);
            var policy = AwkTestHarness.CreatePolicy();
            if (governed) policy = policy.WithMemoryBudget(budget);
            using var cancellation = new CancellationTokenSource(Deadline);
            var invocation = AwkCommandInvocation.CreateWithStandardDescriptors("awk", golden.Arguments,
                [new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]);
            Assert.True(Awk.TryParse(invocation, out var awk));
            var exit = await awk.ExecuteAsync(files, policy, cancellation.Token);
            var actual = new AwkDifferentialObservation(exit.Value,
                files.GetOutputBytes(AwkFileDescriptor.StdOut), files.GetOutputBytes(AwkFileDescriptor.StdErr))
            {
                Files = golden.ExpectedFiles.ToDictionary(file => file.Path,
                    file => files.FileExists(file.Path) ? files.ReadFileBytes(file.Path) : null, StringComparer.Ordinal)
            };
            Assert.Equal(0, files.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
            var differences = Compare(test, actual, reference);
            output.WriteLine(JsonSerializer.Serialize(new
            {
                golden.Name, golden.Arguments, Input = golden.StandardInput,
                InputFiles = golden.InputFiles.Select(file => new { file.Path, file.Content }),
                InputDirectories = golden.InputDirectories.Select(directory => directory.Path),
                ExpectedFiles = golden.ExpectedFiles.Select(file => file.Path),
                Native = identity, Locale = "C.UTF-8", TimeZone = "UTC",
                Core = typeof(Awk).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                Governed = governed, Actual = actual, Reference = reference,
                test.NativeExitNote, Differences = differences
            }));
            Assert.True(differences.Count == 0, golden.Name + ": " + string.Join("; ", differences));
        }
    }

    internal static IReadOnlyList<string> Compare(AwkDifferentialCase test,
        AwkDifferentialObservation actual, AwkDifferentialObservation reference)
    {
        var differences = new List<string>();
        if (actual.ExitCode != test.Golden.ExpectedExitCode)
            differences.Add($"Awk status {actual.ExitCode}, expected {test.Golden.ExpectedExitCode}");
        if (reference.ExitCode != test.NativeExitCode)
            differences.Add($"GNU status {reference.ExitCode}, expected {test.NativeExitCode}");
        if (!actual.Output.AsSpan().SequenceEqual(reference.Output))
            differences.Add("stdout differs");
        if (actual.DiagnosticClass != reference.DiagnosticClass)
            differences.Add($"stderr class differs: Awk {actual.DiagnosticClass}, GNU {reference.DiagnosticClass}");
        foreach (var file in test.Golden.ExpectedFiles)
        {
            var haveActual = actual.Files.TryGetValue(file.Path, out var actualBytes) && actualBytes is not null;
            var haveReference = reference.Files.TryGetValue(file.Path, out var referenceBytes) && referenceBytes is not null;
            if (!haveActual) differences.Add("Awk missing expected file: " + file.Path);
            if (!haveReference) differences.Add("GNU missing expected file: " + file.Path);
            if (haveActual && haveReference && !actualBytes.AsSpan().SequenceEqual(referenceBytes))
                differences.Add("file bytes differ: " + file.Path);
        }
        return differences;
    }

    private static async Task<NativeIdentity> IdentifyAsync()
    {
        var executable = Environment.GetEnvironmentVariable(ExecutableEnvironment);
        if (string.IsNullOrWhiteSpace(executable) || !Path.IsPathFullyQualified(executable) || !File.Exists(executable))
            throw new InvalidOperationException(ExecutableEnvironment + " must name an existing absolute GNU awk executable.");
        var result = await ReferenceProcess.RunAsync(executable, ["--version"], [], capture: true,
            Deadline, CancellationToken.None, locale: "C.UTF-8");
        var version = Encoding.UTF8.GetString(result.Output).Split('\n', 2)[0].Trim();
        var parts = version.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (result.ExitCode != 0 || !version.StartsWith("GNU Awk ", StringComparison.Ordinal) || parts.Length < 3 ||
            !Version.TryParse(parts[2].TrimEnd(','), out var number) ||
            number < new Version(5, 3))
            throw new InvalidOperationException("Differential idioms require GNU awk 5.3 or newer (including CSV); observed " + version);
        using var bytes = File.OpenRead(executable);
        return new(executable, version, Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    private sealed record NativeIdentity(string Executable, string Version, string Sha256);
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class AwkOracleTheoryAttribute : TheoryAttribute
{
    public AwkOracleTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AwkDifferentialTests.ExecutableEnvironment)))
            Skip = "Opt in with LOKAD_AWK_ORACLE_EXE set to an absolute GNU awk 5.3+ executable.";
    }
}

public sealed class AwkDifferentialCase
{
    public AwkDifferentialCase(AwkGoldenCase golden, int? nativeExitCode = null, string? nativeExitNote = null)
    {
        Golden = golden;
        NativeExitCode = nativeExitCode ?? golden.ExpectedExitCode;
        if (NativeExitCode != golden.ExpectedExitCode && string.IsNullOrWhiteSpace(nativeExitNote))
            throw new ArgumentException("A native exit-status difference needs an explicit case-level explanation.");
        NativeExitNote = nativeExitNote;
    }

    public AwkGoldenCase Golden { get; }
    public int NativeExitCode { get; }
    public string? NativeExitNote { get; }
    public override string ToString() => Golden.Name;
}

internal sealed record AwkDifferentialObservation(int ExitCode, byte[] Output, byte[] Error)
{
    public IReadOnlyDictionary<string, byte[]?> Files { get; init; } = new Dictionary<string, byte[]?>(StringComparer.Ordinal);
    public string DiagnosticClass => Error.Length == 0 ? "empty" : ExitCode == 0 ? "warning" : "error";
}
