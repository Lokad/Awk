# Runtime resource control

The host's shared governor admits memory, caller cancellation controls duration, and
`IAwkHost` controls effects. This branch removes duplicate execution-memory, record/value,
builder/capture and cumulative I/O ceilings, plus Awk-owned deadlines. Collection/frontend
caps remain pending cleanup. Released 0.3.0 retains its older API; see the [public policy](../src/Lokad.Awk/AwkExecutionPolicy.cs)
and [migration notes](../CHANGELOG.md#unreleased) for the next release.

## Shared memory admission

`WithMemoryBudget(IMemoryBudget)` borrows an application-owned shared allowance through
Awk's local interfaces. A budget carried by the invocation takes precedence and follows
nested command descriptors. Awk owns its child scopes and reservations, never the supplied
budget. Scopes share the root allowance; they do not receive independent limits.

Major frontend, compiled-program and runtime owners admit retained capacity and temporary
growth peaks before allocation or mutation. Estimates are deliberately coarse, not a heap
cap or an attempt to track every object. Replacement can require both old and new capacity.
Streaming accounts for live storage, not cumulative bytes processed.

**Any refused admission fails the command**, even for optional cache storage. No retry,
waiting for memory, spilling, eviction or algorithm change recovers from refusal. Earlier
effects remain committed. Unused admission is released on failed allocation.

Refund follows actual ownership: pending host reads/writes/commands keep borrowed storage
funded until they finish. Cancellation alone does not end that lifetime. Reused compiled
storage stays charged through active executions; disposal prevents new users and refunds
after existing users finish. Routine bounded-cache eviction is distinct from refusal recovery.

```csharp
// memoryBudget and cancellationToken belong to the embedding application.
var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(memoryBudget);
var exitCode = await awk.ExecuteAsync(host, policy, cancellationToken);
```

## Cancellation and cleanup

Checkpoints remain active with observational counters disabled. Caller cancellation
propagates after cleanup. It requests that runtime, host and regex work stop; it does not
forcibly terminate work or authorize early refunds. A non-cooperating host can delay completion.

Removed for the next release: `ExecutionTimeout`, `CommandTimeout`, `HostOperationTimeout`,
`CleanupTimeout` and their `With...` methods. Use a caller-owned `CancellationTokenSource`
with `CancelAfter` when a deadline is needed. Cancellation propagates as cancellation,
without a separate Awk timeout diagnostic.

Awk awaits actual host completion. Acquired descriptors are registered before cancellation
unwinds execution; delivered reads retain their buffer and accounting. Essential cleanup uses
an uncancelled token and awaits every close and outstanding user before refunding storage.
A host that ignores cancellation or never completes cleanup can delay completion indefinitely.

Pipe commands share a cancellation scope owned by the existing I/O context. Parent failure
cancels that work before descriptor cleanup waits; cleanup still awaits actual command
completion before refunding borrowed storage. Successful close preserves command exit statuses.

The regex contract forwards the current token into compilation and each match, with no
adapter-owned timeout. The internal Utf8Regex adapter's pinned 0.2.0 producer lacks token-aware
APIs; checks around producer calls cannot interrupt them. Future producer adoption is separate.
Awk-owned POSIX matching polls cancellation. The default .NET backend retains its existing
fixed timeout because it cannot poll the token internally. See [regex engines](REGEX.md).

## Remaining protections

Managed recursion/syntax-depth protection, strict UTF-8, host authorization, mutation
preflights, representable allocation sizes, open-resource bounds and bounded streaming/caches
remain necessary. There are no fuel, host-call, record-count, regex-work or sorting-work quotas.

Removed for the next release: `MaxExecutionMemoryBytes`, `MaxRecordBytes`, `MaxValueBytes`,
`MaxInputBytes`, `MaxOutputBytes`, `MaxBuilderBytes`, `MaxRegexCaptureBytes`, their setters
and enforcement. Their storage concerns
belong to shared admission; cumulative I/O is not a memory charge. With no supplied budget,
Awk must not silently invent a governor. Hosts using the restricted preset must supply the
shared budget and cancellation needed for their workload when upgrading from 0.3.0.

See [host containment](SECURITY.md), [UTF-8 policy](UTF8_POLICY.md) and
[record semantics](RECORD_SEMANTICS.md) for the independent guarantees.
