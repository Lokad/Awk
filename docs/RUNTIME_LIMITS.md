# Runtime limits

Pass an immutable `AwkExecutionPolicy` to `Awk.ExecuteAsync`. `CreateDefault()` favors
ordinary compatibility; `CreateRestricted()` reduces capabilities and supplies finite
budgets for untrusted scripts. Refine either with its public `With...` methods.

## Choose a preset

| Budget | Default | Restricted |
| --- | --- | --- |
| Execution fuel | No implicit quota | No implicit quota |
| Function depth / syntax depth | 1,000 / 128 | 32 / 64 |
| Ordinary host calls | 100,000 | 100,000 |
| Governed execution memory | Unlimited | 64 MiB |
| Record / value / builder bytes | Unlimited / unlimited / 64 MiB | 8 MiB each |
| Runtime input / output bytes | Unlimited | 64 MiB / 16 MiB |
| Fields / entries per array | 1,000,000 | 100,000 |
| Host-operation / command deadline | 5 minutes | 10 seconds |
| Aggregate cleanup deadline | 30 seconds | 5 seconds |
| Whole-execution deadline | Caller-owned | 30 seconds, cooperative |

Specialized regex-operation, sort-comparison and regex-separator scan quotas are optional
in both presets. Positive `WithMaxExecutionSteps` enables a shared logical-work allowance;
zero in a preset means absent and is not a valid setter value. Fuel is deterministic
accounting, not a CPU-time limit. Counters observe work and never enable admission rules.

For ordinary execution with finite memory and caller-owned cancellation:

```csharp
var policy = AwkExecutionPolicy.CreateDefault()
    .WithMaxExecutionMemoryBytes(64L << 20);
var exitCode = await awk.ExecuteAsync(host, policy, cancellationToken);
```

`WithExecutionTimeout` adds an owned cooperative deadline. Its expiry is an awk failure;
caller cancellation remains cancellation. See [host containment](SECURITY.md) for
restricted capabilities and the application's responsibilities.

## Admission and failure behavior

Invocation/source/frontend limits bound arguments, environment, source bytes/units,
expanded source/includes, tokens, diagnostics, symbols, identifiers and syntax depth.
Runtime limits independently bound input/records, text/builders, fields/arrays, output,
open redirections/pipes, regex preparation/captures/cache retention and VM stack storage.
The public policy getters and XML documentation are the complete numeric reference.

Arrays, fields, records, output, capture arrays and VM growth preflight before observable
mutation. Staged work admits overlapping old/new storage, publishes only a completed result
and releases temporary reservations on failure/cancellation. A mutation can fail when its
final size fits but its staging peak does not. Previously committed state/output survives;
preflight does not roll back earlier script or host effects.

Byte/memory ceilings use `awk: memory limit exceeded`; fuel, host-call and other limits
retain their specific awk diagnostics. Unexpected CLR exception details are not exposed.
Warnings use the same governed output/host budgets as execution, including warnings retained
by compiled programs. Cleanup can run after the ordinary host-call allowance is exhausted,
within one aggregate cleanup deadline per phase; it does not receive a new deadline per file.

## Memory and cancellation scope

`MaxExecutionMemoryBytes` measures conservative live storage owned by the runtime:
values/arrays, record/field caches, prepared global scalar slots, builders, staged output, VM frames and admitted regex
preparation/scratch. Estimates include retained capacity and cache headroom. It is not a
measurement of managed allocations or process heap use. Frontend/compiled artifacts,
immutable process metadata, host storage and parser/backend internals have separate limits
or ownership. Streaming reads remain bounded; general regex RS can rescan buffered data.

Prepared slots belong to each execution; their vector is admitted before allocation.
Ordinary global reads and updates reuse those slots. Scalar creation stays lazy; a first
write binds only after its normal assignment succeeds. The vector charge can cause a
tight finite-memory budget to reject earlier.

Capture metadata reuses scoped UTF-8 scratch sized before key construction and released
before array publication. Its initial capacity can shift tight-budget rejection earlier.

Owned long scans/copies observe periodic cancellation and publish completed caches only.
Framework allocation/conversion/search, parser operations and regex constructors can be
indivisible; matching deadlines do not bound constructor time. Data/depth admission,
bounded regex caches and backend safeguards remain independent protections. No universal
cancellation latency or hard CPU preemption is promised.

Host-operation and command deadlines stop awaiting an uncooperative host; they cannot force
its task to terminate. Normal close uses the earlier applicable host/cleanup deadline.
Hosts must supply their own cancellation, backpressure and effect containment.

Function entry and unwind check CLR stack pressure, including before argument evaluation,
and suspend when needed. Syntax-depth settings above 128 retain unconditional suspension
at both boundaries. The function-frame quota owns recursion admission; suspension counters
are observational. The interpreter remains the default, with the executable engine opt-in
and sharing the same cancellation, resource and mutation contracts.

Use the existing `--counter-report` benchmark command to qualify changed accounting or
byte/string boundaries; see [benchmarks](BENCHMARKS.md). Policy numeric defaults, validation
and presets have one immutable owner; the memory governor and scoped leases retain their
existing ownership model.
