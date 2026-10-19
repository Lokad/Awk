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

`AwkExecutionPolicy.CreateRestricted()` disables script-created file redirections and command
pipes, `@include`, and changed `ARGV` input paths; hides the invocation environment and host-error
details; pins time and local timezone to the Unix epoch and UTC; and applies finite frontend,
runtime, regex and host-operation bounds. Storage admission requires a supplied shared budget. `CreateDefault()` preserves the broader
compatibility surface. The `--sandbox` command-line option independently disables file and command
redirections; it does not replace host-side path and descriptor policy.

Pass a caller cancellation token and use `WithMemoryBudget` for shared host-governed admission.
This branch removes legacy execution-memory, record/value-size and cumulative I/O quotas,
and the four separate Awk deadline settings. Hosts supply shared admission and caller-owned
deadline cancellation explicitly. Some long builtin/backend calls still have coarse cancellation
boundaries; see [resource control](docs/RUNTIME_LIMITS.md).

See [docs/SECURITY.md](docs/SECURITY.md) before executing awk supplied by an untrusted party.

`WithMemoryBudget(IMemoryBudget)` admits major frontend/runtime storage against an application-owned
shared allowance. Implement the local budget interfaces as an adapter over your host's governor;
Awk disposes only its own child scope. Nested command descriptors carry the active budget,
which takes precedence over the policy budget. Governed cleanup waits for actual host users
before refunding storage, including after cancellation. Source copies and parser
buffers use coarse estimates; admission can conservatively refuse before execution.
See [runtime limits](docs/RUNTIME_LIMITS.md) for ownership and accounting scope.

Release notes and removed work-quota APIs are listed in [CHANGELOG.md](CHANGELOG.md#030---2026-10-19).
The Lokad.Awk package uses version 0.3.0, assembly/file versions 0.3.0.0 and informational version
`0.3.0+<source commit>`. Package dependencies and SourceLink identify the qualified source;
the release tag `v0.3.0` must identify that same commit. Before 1.0, minor releases may remove
public APIs as explicitly described in their release notes.

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
[`Lokad.Awk.Utf8Regex` adapter](docs/REGEX.md) is for internal qualification and is not distributed as a
NuGet package. Newer producer adoption remains postponed.

Current representative throughput, GNU comparison and managed allocations are maintained in
[benchmarks](docs/BENCHMARKS.md). CI builds and tests on Windows and Linux.

Compatibility note: `--characters-as-bytes` keeps awk string units in UTF-8 bytes for valid text,
but `FS=""` in byte mode is intentionally limited to ASCII input. Non-ASCII valid UTF-8 would
produce invalid one-byte fields, so Lokad.Awk reports an unsupported-policy diagnostic unless a
future raw-byte field representation is added. Invalid raw input is rejected even under
`LC_ALL=C`; locale values do not select a different ingress codec. See
[docs/UTF8_POLICY.md](docs/UTF8_POLICY.md).

## Design

The library is intended to stay host-agnostic. The public surface should stay small:

- parse command-line-style awk invocations
- execute programs against a host-provided IO boundary
- report structured diagnostics and exit codes
- keep unsupported host capabilities explicit

Detailed runtime contracts live in:

- [docs/UTF8_POLICY.md](docs/UTF8_POLICY.md)
- [docs/RUNTIME_LIMITS.md](docs/RUNTIME_LIMITS.md)
- [docs/SECURITY.md](docs/SECURITY.md)
- [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md)
- [docs/RECORD_SEMANTICS.md](docs/RECORD_SEMANTICS.md)
- [docs/REGEX.md](docs/REGEX.md)
- [docs/BENCHMARKS.md](docs/BENCHMARKS.md)

## Dependencies

- `Lokad.Parsing` for tokenization and parser infrastructure
- Regex uses .NET by default; `Lokad.Utf8Regex` is only a dependency of the internal adapter.
