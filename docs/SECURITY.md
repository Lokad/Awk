# Security and host containment

Lokad.Awk executes managed code with all I/O supplied by `IAwkHost`. Awk programs have
no direct filesystem, process, console, networking, reflection or native-loading access.
The host, parser and selected regex engine are trusted implementation code; this is
an embedding boundary, not an OS sandbox.

## Authority

Start untrusted scripts with `AwkExecutionPolicy.CreateRestricted()`. It disables
script-created redirections, command pipes, `@include` and changed `ARGV` input paths;
hides invocation environment and host-error details; fixes time to Unix epoch/UTC;
and, in released 0.3.0, supplies finite data/memory budgets and a cooperative 30-second execution deadline.
Explicit invocation inputs remain available through the host.
These duplicate quota/deadline controls are planned for removal; hosts should supply a shared
memory budget and caller cancellation. See [resource control](RUNTIME_LIMITS.md).

`--sandbox` only disables file and command redirections. `AccessiblePathRoot` checks
canonical virtual paths lexically; a filesystem host must enforce its own root against
symlinks, reparse points and races. Supported `/dev/*` aliases name supplied descriptors.

One-way pipes require policy and host permission. The runtime passes opaque command
text to the host; it does not invoke a shell. Hosts own command allowlists, argument
parsing, environment and process limits. `system()`, coprocesses, network special files
and native extensions are unsupported.

## Resources and host responsibilities

Use structured host outcomes and honor the [buffer lifetime contract](../README.md#embedding).
Unexpected host exceptions become generic awk diagnostics; caller cancellation propagates
as cancellation. Policy/resource failures return awk diagnostics and a nonzero exit status.
Mutation preflights preserve earlier committed state/output, rather than rolling back a command.

CPU execution uses caller cancellation or a cooperative whole-execution deadline.
Shared memory admission is immediate: any refusal fails the command;
Awk does not retry, wait, spill or change algorithms to recover. Accounting is conservative
and approximate, not a process-heap cap. See [runtime limits](RUNTIME_LIMITS.md).

Cancellation is cooperative. Host/cleanup deadlines bound waiting, not forced termination
or the effects of an uncooperative host. Governed storage remains charged until actual
pending host users finish. Use process/container limits when hard isolation is required.

Ingress remains [strict UTF-8](UTF8_POLICY.md). Core Awk disables unsafe C# and has no
Utf8Regex runtime dependency. Applications selecting an optional regex engine also own
its trust and dependency qualification; core releases do not require that engine's release.
