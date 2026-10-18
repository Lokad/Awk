# Runtime limits

Pass an immutable `AwkExecutionPolicy` to `Awk.ExecuteAsync`. `CreateDefault()` favors
ordinary compatibility; `CreateRestricted()` supplies finite budgets and fewer capabilities.
Refine either with its `With...` methods.

| Budget | Default | Restricted |
| --- | --- | --- |
| Execution fuel | No implicit quota | No implicit quota |
| Function depth / syntax depth | 1,000 / 128 | 32 / 64 |
| Ordinary host calls | 100,000 | 100,000 |
| Optional execution-memory quota | Unlimited | 64 MiB |
| Record / value / builder bytes | Unlimited / unlimited / 64 MiB | 8 MiB each |
| Runtime input / output bytes | Unlimited | 64 MiB / 16 MiB |
| Fields / entries per array | 1,000,000 | 100,000 |
| Host-operation / command deadline | 5 minutes | 10 seconds |
| Aggregate cleanup deadline | 30 seconds | 5 seconds |
| Whole-execution deadline | Caller-owned | 30 seconds, cooperative |

Specialized regex-operation, sorting and separator-scan quotas remain optional in both
presets. Positive `WithMaxExecutionSteps` enables deterministic logical fuel, not a CPU-time
limit. Zero in a preset means absent; it is not a valid setter value. Counters observe work
and never enable admission rules. `WithExecutionTimeout` produces an awk failure on expiry;
caller cancellation remains cancellation. Every logical checkpoint still polls cancellation,
including default execution without fuel or counters.
Other frontend, I/O, regex and VM quotas are documented by the public policy getters/XML.

## Shared memory

`WithMemoryBudget(IMemoryBudget)` borrows the host's shared allowance. The library declares
the interfaces; hosts adapt their own governor. Each execution owns a scope and a coarse
charge for retained storage and major construction peaks. Descriptors carry the active budget
into nested execution; an invocation's budget takes precedence over the policy budget.
`MaxExecutionMemoryBytes` is an additional optional quota, not a copied root allowance.
It covers runtime storage; frontend/compiled ownership is separate. Field buffers charge
retained capacity, including spare slots after shrinking `NF`, until that storage is replaced
or released. Growth admits overlapping old/new buffers before publishing a field mutation.

Frontend and compiled storage have separate coarse ownership. Governed compiled reuse keeps
its charge through active executions; disposal stops new executions, waits for users and
refunds it. Minor metadata, process caches and backend internals are approximate; these
estimates are not a managed-heap cap. Buffer reuse and avoided copies can change optional
memory thresholds.

Memory failures preflight before observable mutation. Unrepresentable array memory totals
fail with the memory-limit diagnostic. Staging admits overlapping old/new
storage, so a final result can fit while its construction peak does not. Earlier state/output
survives; preflight does not roll back prior script or host effects. Array deletion refunds
retained keys and values. Byte/memory refusal uses `awk: memory limit exceeded`; other limits
retain their specific awk diagnostics, without CLR exception details. Warnings use ordinary
output/host budgets; cleanup shares one aggregate deadline after the ordinary host-call allowance is exhausted.

Any refused host admission fails the command, including its initial reservation. The runtime
stops script execution and returns a memory diagnostic and nonzero status; it does not retry,
wait for allowance, spill, switch algorithms or drop/evict storage to recover. Cleanup and
actual pending users finish before relinquished storage is refunded. Retained compiled storage
stays charged; a later command can reuse released allowance. Refusal is distinct from caller
cancellation and an actual `OutOfMemoryException`.

Refunds wait for actual pending host users: cancellation or timeout ends a wait, not the
underlying buffer borrow. Hosts must cooperate with cancellation. Without a supplied budget, existing deadline behavior remains.
Framework/parser/regex construction can be indivisible; no hard CPU preemption or universal
cancellation latency is promised. Matching deadlines do not bound regex construction.

Supplied regex engines borrow the active budget and current checkpoint. Admission refusal
is fatal; unsupported-pattern fallback is separate. Execution caches own compiled handles
and dispose them on eviction or cleanup. No regex engine owns the supplied host budget.

## Execution and streaming

The interpreter remains the default. Its bounded synchronous preparation and the opt-in
executable engine preserve logical checkpoints, stack safety, evaluation order and mutation
preflights. Preparation and buffer reuse do not enable quotas or reduce host authority checks.

Fields split lazily where supported; tight quotas still validate the whole record before
publishing a prefix. NF or mutation can complete the scan. Classification and rescans remain
cancellable and charged, so explicit fuel can reject earlier. See
[record semantics](RECORD_SEMANTICS.md) and [strict UTF-8 policy](UTF8_POLICY.md).

Each statement flushes queued stdout and diagnostics before control continues. Output and
formatting failures discard unpublished bytes; prior committed effects remain. Streaming
buffers and caches are bounded by their existing capacities and configured limits.

For finite execution memory with caller-owned cancellation:

```csharp
var policy = AwkExecutionPolicy.CreateDefault()
    .WithMaxExecutionMemoryBytes(64L << 20);
var exitCode = await awk.ExecuteAsync(host, policy, cancellationToken);
```

See [host containment](SECURITY.md) for capabilities and application responsibilities.
