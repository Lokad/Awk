# Runtime limits

`WithMemoryBudget(IMemoryBudget)` admits frontend/runtime storage against a borrowed shared host allowance.
The library declares the interfaces; hosts implement adapters over their own governor.
Each execution owns a child scope and a coarse charge. Command descriptors carry that active
budget into nested execution. Refusal uses the existing awk memory-limit diagnostic.
Retained runtime storage and simultaneous construction peaks share that charge; their
refunds wait for actual pending host users, without a host reservation for each peak.
An invocation's budget takes precedence over the policy budget to preserve transitive ownership.
`MaxExecutionMemoryBytes` remains an optional execution quota, not a copied root allowance.

Governed cleanup waits for actual host-task completion before refunding borrowed storage;
cancellation or a timeout only ends the operation's wait. Hosts must cooperate with cancellation.
Without a supplied budget, existing deadline/default behavior is unchanged. Runtime buffers,
retained state and major construction peaks are covered. Source copies and lexer/parser/binder
storage use one coarse frontend charge, retained through execution. Governed internal compiled reuse
owns a surviving scope: disposal stops new executions, waits for active users and refunds it.
Source/token envelopes can refuse conservatively; minor metadata, process caches and backend
internals remain approximate. This is not complete allocation coverage.

Pass an immutable `AwkExecutionPolicy` to `Awk.ExecuteAsync`. `CreateDefault()` favors
ordinary compatibility; `CreateRestricted()` reduces capabilities and supplies finite
budgets for untrusted scripts. Refine either with its public `With...` methods.

## Choose a preset

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

Specialized regex-operation, sort-comparison and regex-separator scan quotas are optional
in both presets. Positive `WithMaxExecutionSteps` enables a shared logical-work allowance;
zero in a preset means absent and is not a valid setter value. Fuel is deterministic
accounting, not a CPU-time limit. Counters observe work and never enable admission rules.
Prepared numeric trees and eligible pure scalar loops use the same execution checkpoints
and mutation preflights. Functions retain their validated asynchronous stack boundaries;
host/pending work, lint and buffered diagnostics retain normal interpreter execution.
Synchronous eligibility is bounded to expression depth 16 and 16 body statements;
larger shapes use the normal executor, bounding preparation work and metadata.
Synchronous builtins stage arguments directly into the existing admitted buffer, in order;
pending operands retain its lease until completion, failure or cancellation. Async builtins
retain lazy argument evaluation. This changes neither admission thresholds nor checkpoints.
Eligible field/string-literal comparisons, text comparisons with a right-hand field and
numeric-only field reads, including binary arithmetic operands, can avoid copied text
and scalar caches. Numeric comparisons,
cached/modified fields and case/nondecimal fallbacks retain ordinary values. Value limits,
mutation preflights and synchronous borrowing remain; charges reflect avoided storage.
Length-only ASCII/byte field reads can avoid text/cache allocation. An initial ASCII-record
classification is cancellable and charged, so an explicit fuel quota may reject earlier.

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
Array deletion refunds the stored key and value, including equivalent numeric/text lookups.

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
immutable process metadata and host storage have separate ownership. The shared host allowance
also admits major frontend/compiled storage; the numeric execution quota remains runtime-only.
Streaming reads remain bounded; general regex RS can rescan buffered data.

Prepared slots belong to each execution; their vector is admitted before allocation.
Ordinary global reads and updates reuse those slots. Scalar creation stays lazy; a first
write binds only after its normal assignment succeeds. The vector charge can cause a
tight finite-memory budget to reject earlier.

Already-parsed mode-independent numeric input can reuse the number field in an existing
slot. Original text/classification, value-size admission and long-read cancellation remain
intact; no extra retained storage is introduced.

Without a finite memory budget, small field-value dictionary buffers are reused
across records. Governed execution also reuses the smallest cache, keeping a coarse
320-byte capacity charge until reset; cached values are cleared and larger buffers discarded.
The added backing-capacity allowance can make tight finite quotas reject earlier.

Empty lists used only for estimates are shared; each frame keeps its own zero-capacity
placeholder. Published/staged field ownership and logical charges remain unchanged.

Capture metadata reuses scoped UTF-8 scratch sized before key construction and released
before array publication. Its initial capacity can shift tight-budget rejection earlier.

Owned long scans/copies observe periodic cancellation and publish completed caches only.
Framework allocation/conversion/search, parser operations and regex constructors can be
indivisible; matching deadlines do not bound constructor time. Data/depth admission,
bounded regex caches and backend safeguards remain independent protections. No universal
cancellation latency or hard CPU preemption is promised.

Host-operation and command waits observe deadlines; they cannot force host tasks to terminate.
Governed cleanup still awaits actual storage users. Normal close uses the earlier applicable host/cleanup deadline.
Hosts must supply their own cancellation, backpressure and effect containment.

Function entry and unwind check CLR stack pressure, including before argument evaluation,
and suspend when needed. Syntax-depth settings above 128 retain unconditional suspension
at both boundaries. The function-frame quota owns recursion admission; suspension counters
are observational. The interpreter remains the default, with the executable engine opt-in
and sharing the same cancellation, resource and mutation contracts.

Use the existing `--counter-report` benchmark command to qualify changed accounting or
byte/string boundaries; see [benchmarks](BENCHMARKS.md). Numeric defaults, validation and
presets have one immutable owner. Execution accounting and scoped leases report owned storage
to the borrowed host allowance; the numeric memory ceiling remains an optional execution quota.
