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

For an in-memory host implementation pattern, see
[`MockFileSystem`](tests/Lokad.Awk.Tests/MockFileSystem.cs) in the test suite.
Host reads report EOF and failure as separate outcomes: successful batches use
`AwkReadResult.FromContent` (or `EndOfFile`), while failed reads use `AwkReadResult.Failure` with a
structured `AwkOperationError`. `AwkReadCompletion.MoreData` requires a nonempty batch, preventing a
host from causing a no-progress read loop. The runtime maps failures to awk diagnostics and `ERRNO`.
The result snapshots line offsets. Hosts keep successful read content unchanged and alive until the
next read starts on the same descriptor or that descriptor is closed; descriptors have independent
buffer lifetimes.
`IAwkHost` contains only the effects used by the runtime: command execution, append, open, close,
read, and pipe creation. Open, read, and pipe creation report unsupported capabilities and failures
as structured results so the runtime can produce awk diagnostics without leaking host exceptions.
Append and command outcomes use `AwkExitCode`; a batch command response must contain exactly one
result per submitted command.

`AwkExecutionPolicy.CreateRestricted()` disables script-created file redirections and command
pipes, `@include`, and changed `ARGV` input paths; hides the invocation environment and host-error
details; pins time and local timezone to the Unix epoch and UTC; and applies finite frontend,
runtime, IO, regex, memory, and host-operation budgets. `CreateDefault()` preserves the broader
compatibility surface. The `--sandbox` command-line option independently disables file and command
redirections; it does not replace host-side path and descriptor policy.

Both presets have no implicit execution-fuel cutoff. Pass a caller cancellation token or configure
WithExecutionTimeout. Ordinary finite-memory execution can use
`AwkExecutionPolicy.CreateDefault().WithMaxExecutionMemoryBytes(64L << 20)`; restricted execution
already has finite memory and a 30-second cooperative deadline. Positive explicit
WithMaxExecutionSteps quotas remain enforced. Some long builtin/backend calls still have coarse
cancellation boundaries; see the runtime-limit contract for their current limits.

See [docs/SECURITY.md](docs/SECURITY.md) before executing awk supplied by an untrusted party.

Release notes are tracked in [CHANGELOG.md](CHANGELOG.md).

## Status

This repository preserves command-line awk behavior for common valid-UTF-8 workflows while the
runtime is being migrated toward byte-native text execution, explicit resource limits, and an
internal executable representation. The interpreter remains the default engine. The opt-in
executable engine runs its supported expression and statement shapes as opcodes, represents every
unsupported shape as an explicit interpreter fallback, and reports both paths through runtime
counters. Its default-engine gate is defined in [docs/VM_ROLLOUT.md](docs/VM_ROLLOUT.md).

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
- [docs/COMPATIBILITY_CONTRACT.md](docs/COMPATIBILITY_CONTRACT.md)
- [docs/REGEX_COMPATIBILITY.md](docs/REGEX_COMPATIBILITY.md)
- [docs/VM_ROLLOUT.md](docs/VM_ROLLOUT.md)
- [docs/BENCHMARKS.md](docs/BENCHMARKS.md)
- [docs/RELEASE_READINESS.md](docs/RELEASE_READINESS.md)

## Dependencies

- `Lokad.Parsing` for tokenization and parser infrastructure
- `Lokad.Utf8Regex` for strict UTF-8 regex matching and byte-coordinate adapter paths
