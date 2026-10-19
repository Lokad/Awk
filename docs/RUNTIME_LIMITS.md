# Runtime resource control

The intended ownership is simple: the host's shared governor admits memory, and the caller's
cancellation controls how long work may continue. `IAwkHost` controls effects. The released
0.3.0 API still has additional byte quotas and timeout settings; their removal is planned,
not implemented by this documentation update. See the [public policy](../src/Lokad.Awk/AwkExecutionPolicy.cs)
for the current API and [migration notes](../CHANGELOG.md#030---2026-10-19) for changes already shipped.

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

Version 0.3.0 also has `ExecutionTimeout`, `CommandTimeout`, `HostOperationTimeout` and
`CleanupTimeout` with corresponding `With...` methods. They currently create distinct deadline
behavior. The planned cleanup removes these parallel controls in favor of caller/host-owned
cancellation, including cleanup waiting, without abandoning outstanding users.

The internal Utf8Regex adapter currently checks around producer calls rather than inside
them. Forwarding cancellation and removing its separate timeout contract are planned;
see [regex engines](REGEX.md#cooperative-cancellation). Awk-owned POSIX matching polls
cancellation. The default .NET backend cannot poll that token internally and currently
uses a fixed match timeout; that backend limitation is separate from adapter policy.

## Remaining protections and planned deletion

Managed recursion/syntax-depth protection, strict UTF-8, host authorization, mutation
preflights, representable allocation sizes, open-resource bounds and bounded streaming/caches
remain necessary. There are no fuel, host-call, record-count, regex-work or sorting-work quotas.

The next simplification removes `MaxExecutionMemoryBytes`, `MaxRecordBytes`, `MaxValueBytes`,
`MaxInputBytes` and `MaxOutputBytes`, their setters and enforcement. Their storage concerns
belong to shared admission; cumulative I/O is not a memory charge. With no supplied budget,
Awk must not silently invent a governor. Hosts using the restricted preset must supply the
shared budget and cancellation needed for their workload when these legacy controls are removed.

See [host containment](SECURITY.md), [UTF-8 policy](UTF8_POLICY.md) and
[record semantics](RECORD_SEMANTICS.md) for the independent guarantees.
