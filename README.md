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

var invocation = new AwkCommandInvocation(
    "awk",
    ["-F,", "NR > 1 { total += $3 } END { print total }", "/sales.csv"],
    [new AwkEnvironmentVariable("PWD", "/")]);

if (Awk.TryParse(invocation) is not { } awk)
    throw new InvalidOperationException("The invocation is not an awk command.");

IAwkHost host = new ApplicationAwkHost(); // Owns files, streams, pipes, and process policy.
var exitCode = await awk.ExecuteAsync(host, CancellationToken.None);
```

For an in-memory host implementation pattern, see
[`MockFileSystem`](tests/Lokad.Awk.Tests/MockFileSystem.cs) in the test suite.
Host reads report EOF and failure as separate outcomes: successful batches use
`AwkReadResult.FromContent` (or `EndOfFile`), while failed reads use `AwkReadResult.Failure` with a
structured `AwkOperationError`. The runtime maps those failures to awk diagnostics and `ERRNO`.

Release notes are tracked in [CHANGELOG.md](CHANGELOG.md).

## Status

This repository preserves command-line awk behavior for common valid-UTF-8 workflows while the
runtime is being migrated toward byte-native text execution, explicit resource limits, and an
internal executable representation. The executable engine currently has a fallback dispatcher and
metrics; real opcode lowering remains an internal rollout item.

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
- [docs/COMPATIBILITY_CONTRACT.md](docs/COMPATIBILITY_CONTRACT.md)
- [docs/REGEX_COMPATIBILITY.md](docs/REGEX_COMPATIBILITY.md)
- [docs/VM_ROLLOUT.md](docs/VM_ROLLOUT.md)
- [docs/BENCHMARKS.md](docs/BENCHMARKS.md)
- [docs/RELEASE_READINESS.md](docs/RELEASE_READINESS.md)

## Dependencies

- `Lokad.Parsing` for tokenization and parser infrastructure
