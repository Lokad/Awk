# Lokad.Awk

An embeddable awk interpreter for .NET 10. Run ordinary text-processing programs while your
application controls files, streams, external commands and resource admission.

```powershell
dotnet add package Lokad.Awk
```

## Embedding

Supply command-line-style arguments and an `IAwkHost` implementation. All IO passes through
the host; the package does not provide a filesystem or process host.

```csharp
using Lokad.Awk;

var invocation = AwkCommandInvocation.CreateWithStandardDescriptors(
    "awk",
    ["{ total += $2 } END { print total }", "/data/sales.tsv"],
    [new AwkEnvironmentVariable("PWD", "/")]);

if (!Awk.TryParse(invocation, out var awk))
    throw new InvalidOperationException("Not an awk command.");

IAwkHost host = new ApplicationAwkHost(); // Implement the application's IO boundary.
var policy = AwkExecutionPolicy.CreateRestricted()
    .WithAccessiblePathRoot(new AwkPath("/data"));
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var exitCode = await awk.ExecuteAsync(host, policy, cancellation.Token);
```

`TryParse` recognizes the invocation; execution loads, parses and runs the program.
`ApplicationAwkHost` above is application code; the test suite's
[MockFileSystem](tests/Lokad.Awk.Tests/MockFileSystem.cs) illustrates a host implementation.

Use caller cancellation for deadlines and `WithMemoryBudget(IMemoryBudget)` for shared
memory admission. The restricted preset limits capabilities, stack depth and open resources;
aggregate memory admission requires a supplied budget. Text must be valid UTF-8. See the
[runtime contract](docs/RUNTIME.md) for host ownership, cleanup and protection guarantees.

## Regex and compatibility

Regex operations require a host-supplied `IAwkRegexEngine`, configured with
`policy.WithRegexEngine(engine)`. Core has no regex backend or Utf8Regex dependency.
A [copyable adapter](examples/README.md) uses published Lokad.Utf8Regex 0.4.0 with cooperative
cancellation. Its POSIX longest-match and capture-selection gaps remain documented limitations.
Non-regex processing needs no provider.

Supported core behavior includes records/fields, expressions, loops, functions, flat arrays,
formatting and `getline`, with selected GNU conveniences. This is not full POSIX/GNU
conformance: `system()` and namespaces remain unsupported.
Version 0.5.0 adds CSV input (`--csv`/`-k`) and arrays of arrays (`a[i][j]`);
published 0.4.0 supports neither.

- [Compatibility](docs/COMPATIBILITY.md): supported boundaries and known differences.
- [Benchmarks](docs/BENCHMARKS.md): representative elapsed-time ratios and managed allocations.
- [0.4.0 migration notes](CHANGELOG.md#040): breaking host, regex and policy API changes.

Before 1.0, minor releases may remove public APIs as described in the release notes.

## Building

Use the stable SDK pinned in `global.json`, then run `dotnet test Lokad.Awk.slnx -c Release`.
CI builds, tests and packs on Windows and Linux. Only `src/Lokad.Awk` produces a NuGet package;
the adapter example, tests and benchmarks are not packaged.
Development packages use `0.0.0-dev`, with output kept separate from published artifacts.
Release packaging supplies explicit package and assembly versions.
