# Lokad.Awk

Lokad.Awk is a standalone, embeddable awk runtime for .NET, with host-mediated IO so embedding applications keep control over files, streams, environment, and process policy.

```powershell
dotnet add package Lokad.Awk
```

## Embedding

Embedding code passes command-line-style awk arguments and an application-owned host. The host is
the policy boundary for files, descriptors, pipes, external commands, and captured output.

```csharp
using System;
using System.Threading;
using Lokad.Awk;

var invocation = AwkCommandInvocation.CreateWithStandardDescriptors(
    "awk",
    ["-F,", "NR > 1 { total += $3 } END { print total }", "/data/sales.csv"],
    [new AwkEnvironmentVariable("PWD", "/")]);

if (!Awk.TryParse(invocation, out var awk))
    throw new InvalidOperationException("The invocation is not an awk command.");

IAwkHost host = new ApplicationAwkHost(); // Owns files, streams, pipes, and process policy.
var policy = AwkExecutionPolicy.CreateRestricted()
    .WithAccessiblePathRoot(new AwkPath("/data"));
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var exitCode = await awk.ExecuteAsync(host, policy, cancellation.Token);
```

`ExecuteAsync` returns an `AwkExitCode`; embedding code can keep the result typed and cross to its
integer `Value` only at a process boundary.

The example reads unquoted comma-separated values. Quoted or multiline CSV requires host
preprocessing; GNU CSV mode is not implemented.

For an in-memory host implementation pattern, see
[`MockFileSystem`](tests/Lokad.Awk.Tests/MockFileSystem.cs) in the test suite.
Host reads report EOF and failure as separate outcomes: successful batches use
`AwkReadResult.FromContent` (or `EndOfFile`), while failed reads use `AwkReadResult.Failure` with a
structured `AwkOperationError`. `AwkReadCompletion.MoreData` requires a nonempty batch, preventing a
host from causing a no-progress read loop. Recoverable redirected `getline` open/read failures
return -1, set enabled `ERRNO`, and emit no automatic runtime stderr diagnostic. Success/EOF
preserve `ERRNO`. Unhandled main-input failures, policy/capability denial, invalid UTF-8 and
resource refusal remain fatal; stderr written by a host command is unaffected.
The result snapshots line offsets. Hosts keep successful read content unchanged and alive until the
next read starts on the same descriptor or that descriptor is closed; descriptors have independent
buffer lifetimes.
`IAwkHost` contains only the effects used by the runtime: command execution, append, open, close,
read, and pipe creation. Open, read, and pipe creation report unsupported capabilities and failures
as structured results so the runtime can apply those semantics without leaking host exceptions.
Append and command outcomes use `AwkExitCode`; a batch command response must contain exactly one
result per submitted command.
Descriptor close returns `AwkDescriptorCloseResult`: expected failures carry an
`AwkOperationError` and explicitly state whether the descriptor was retired. Retired failures
are reported without a duplicate close; still-owned failures can receive cleanup. This is an
unreleased host API change; published 0.3.0 uses the earlier status-only result.

`AwkExecutionPolicy.CreateRestricted()` disables script-created file redirections and command
pipes, `@include`, and changed `ARGV` input paths; hides the invocation environment and host-error
details; pins time and local timezone to the Unix epoch and UTC; and applies tighter stack
and open-resource bounds. Storage admission requires a supplied shared budget.
`CreateDefault()` preserves the broader compatibility surface. The `--sandbox` command-line option
independently disables file and command redirections; it does not replace host-side path and
descriptor policy.

Pass a caller cancellation token and use `WithMemoryBudget` for shared host-governed admission.
An invocation budget takes precedence and follows nested command descriptors. Awk owns only
its child scopes; governed cleanup waits for actual host users before refunding storage.
Admission uses coarse estimates and can refuse before execution. See the
[runtime and host contract](docs/RUNTIME.md) for authority, memory ownership, cancellation
and remaining bounds, including responsibilities when executing untrusted scripts.

Release notes and removed work-quota APIs are listed in [CHANGELOG.md](CHANGELOG.md#unreleased).
Published 0.3.0 corresponds to tag `v0.3.0`; its packages and source are immutable.
Current source builds development packages as `0.0.0-dev` under `artifacts/dev/nuget/`,
with assembly/file versions `0.0.0.0` and informational version `0.0.0-dev+<source commit>`.
This placeholder does not select the next release version. A release must align its package,
assembly metadata, SourceLink and tag, then qualify the final packages. Before 1.0, minor
releases may remove public APIs as explicitly described in their release notes.

## Status

Lokad.Awk qualifies ordinary valid-UTF-8 text processing: patterns, expressions, loops, user
functions, flat arrays, records/fields, getline and formatting. This is a supported core with
selected GNU conveniences, not full POSIX/GNU conformance. CSV, nested arrays, system(), namespaces,
typed regex values and indirect calls remain unsupported; see the
[compatibility guide](docs/COMPATIBILITY.md).

The interpreter is the only execution engine, including its synchronous/prepared paths.
Regex defaults to .NET compatibility, with
documented [regex semantics](docs/COMPATIBILITY.md#regex-semantics). Applications can supply an
`IAwkRegexEngine` through `policy.WithRegexEngine(engine)`. The repository's
[`Lokad.Awk.Utf8Regex` adapter](docs/RUNTIME.md#regex-engines) is for internal qualification and is not distributed as a
NuGet package. Newer producer adoption remains postponed.

Current representative throughput, GNU comparison and managed allocations are maintained in
[benchmarks](docs/BENCHMARKS.md). CI builds and tests on Windows and Linux.

Text remains strict UTF-8 in byte mode and under `LC_ALL=C`. The
[runtime text boundary](docs/RUNTIME.md#text-boundary) defines units and byte-mode restrictions.

## Design

Source builds require the stable .NET SDK selected by `global.json` (10.0.401 with patch
roll-forward, excluding previews). Run `dotnet test Lokad.Awk.slnx` for build and correctness.

The library is intended to stay host-agnostic. The public surface should stay small:

- parse command-line-style awk invocations
- execute programs against a host-provided IO boundary
- report structured diagnostics and exit codes
- keep unsupported host capabilities explicit

Detailed runtime contracts live in:

- [docs/RUNTIME.md](docs/RUNTIME.md)
- [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md)
- [docs/BENCHMARKS.md](docs/BENCHMARKS.md)

## Dependencies

- `Lokad.Parsing` for tokenization and parser infrastructure
- Regex uses .NET by default; `Lokad.Utf8Regex` is only a dependency of the internal adapter.
