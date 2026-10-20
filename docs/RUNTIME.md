# Runtime and host contract

Lokad.Awk runs managed code with all I/O supplied by `IAwkHost`. Awk programs have no
direct filesystem, process, console, networking, reflection or native-loading access.
Core disables unsafe C#; the host, parser and selected regex engine remain trusted code.
The embedding boundary does not provide OS isolation.

This guide describes current source. Published 0.3.0 retains the earlier quota/deadline
APIs; [migration notes](../CHANGELOG.md#unreleased) describe their removal.

## Host authority

`AwkExecutionPolicy.CreateRestricted()` disables script-created redirections, command pipes,
`@include` and changed `ARGV` input paths; hides invocation environment and host-error details;
fixes time to Unix epoch/UTC; and tightens stack, regex-construction and open-resource bounds.
Explicit invocation inputs remain available. Supply shared admission and caller cancellation
for resource control; the preset supplies neither an aggregate memory allowance nor a deadline.

`--sandbox` disables file and command redirections. `AccessiblePathRoot` checks canonical
virtual paths lexically; a filesystem host enforces its root against symlinks, reparse points
and races. Supported `/dev/*` aliases name supplied descriptors.

Pipes require policy and host permission. Awk passes opaque command text to the host without
invoking a shell. Hosts control command allowlists, parsing, environment and process limits.
Unsupported effects remain explicit; see [compatibility](COMPATIBILITY.md).

Hosts return structured outcomes and honor the [buffer lifetime contract](../README.md#embedding).
Unexpected host exceptions become generic awk diagnostics. Policy/admission failures produce
awk diagnostics and a nonzero status; caller cancellation propagates as cancellation after cleanup.
Mutation preflights preserve earlier state/output without rolling back completed effects.

## Shared memory admission

`WithMemoryBudget(IMemoryBudget)` borrows an application-owned allowance. An invocation budget
takes precedence and follows nested command descriptors. Awk owns only its child scopes and
reservations; scopes share the root allowance. Without a supplied budget, there is no private
aggregate memory cap.

Invocation input snapshots belong to the caller. `TryParse` only recognizes and wraps the
command; execution admits Awk-owned argument/environment preparation before copies.
Major frontend, prepared-program and runtime owners admit retained capacity and growth peaks
before allocation or mutation. Estimates are coarse, not a process-heap cap or per-object tracking.
Replacement may need old/new overlap; streaming charges live storage, not cumulative I/O.

**Any refused admission fails the command**, including optional cache storage. No retry,
waiting, spilling, eviction or algorithm change recovers from refusal. Unused admission is
released on failed allocation. Routine bounded-cache eviction remains separate from refusal.

Refund follows actual ownership. Pending host reads/writes/commands keep borrowed storage
funded until completion; cancellation alone does not end that lifetime. Reused prepared storage
stays funded through active executions, including when disposal prevents further use.

```csharp
// memoryBudget and cancellationToken belong to the embedding application.
var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(memoryBudget);
var exitCode = await awk.ExecuteAsync(host, policy, cancellationToken);
```

## Cancellation and cleanup

CPU control uses the caller's `CancellationToken`, including caller-owned `CancelAfter`
deadlines. Checkpoints remain active with observational counters disabled. Cancellation
requests that runtime and host work stop; it cannot forcibly terminate a non-cooperating host.

Awk awaits actual completion. Acquired descriptors and delivered buffers remain owned while
cancellation unwinds. Essential cleanup uses an uncancelled token, attempts every acquired
descriptor and awaits outstanding users before refunds. A host that never completes can
delay completion indefinitely; hard process/container isolation belongs to the embedding host.

Parent failure cancels owned pipe commands before cleanup waits. Cleanup still awaits actual
command completion; successful `close()` preserves command exit statuses.

The regex contract carries the current token with no adapter-owned timeout. The internal adapter
checks call boundaries; its pinned Utf8Regex 0.2.0 producer cannot poll the token. Awk-owned POSIX
matching polls cancellation; default .NET matching retains a fixed timeout because it cannot poll internally.
See [regex integration](REGEX.md) for backend guarantees and postponed producer adoption.

## Remaining bounds

Bounds cover function/syntax/include depth, open input/output files and pipes, regex-cache retention,
disclosed host-error length and checked allocation representability. Pattern-byte/group guards
remain pending default-backend construction admission and stack qualification; .NET construction
is indivisible and its internal footprint is only coarsely covered. These are not work quotas.
There are no fuel, host-call, record-count, regex-work or sorting-work quotas.

[Strict UTF-8](UTF8_POLICY.md) and [compatibility](COMPATIBILITY.md) define the independent
text-processing guarantees.
