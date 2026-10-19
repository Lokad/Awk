# Runtime limits

Pass an immutable `AwkExecutionPolicy` to `Awk.ExecuteAsync`. `CreateDefault()` supports
ordinary compatibility; `CreateRestricted()` supplies tighter budgets and fewer host
capabilities. Public execution uses the interpreter; regex defaults to .NET and can be
supplied by the host. Refine a preset with its `With...` methods.

| Budget | Default | Restricted |
| --- | --- | --- |
| Execution fuel | No implicit quota | No implicit quota |
| Function depth / syntax depth | 1,000 / 128 | 32 / 64 |
| Ordinary host calls | 100,000 | 100,000 |
| Execution-memory quota | Unlimited | 64 MiB |
| Record / value / builder bytes | Unlimited / unlimited / 64 MiB | 8 MiB each |
| Runtime input / output bytes | Unlimited | 64 MiB / 16 MiB |
| Fields / entries per array | 1,000,000 | 100,000 |
| Regex match timeout | 2 seconds | 2 seconds |
| Host-operation / command deadline | 5 minutes | 10 seconds |
| Aggregate cleanup deadline | 30 seconds | 5 seconds |
| Whole-execution deadline | Caller-owned | 30 seconds, cooperative |

Specialized regex-operation, sorting and separator-scan quotas are optional in both
presets. Positive `WithMaxExecutionSteps` enables logical fuel, not CPU time. The preset
value zero means no fuel; setters require positive values. Counters do not enable quotas.
Remaining frontend, regex, I/O and VM limits are listed in the
[public policy](../src/Lokad.Awk/AwkExecutionPolicy.cs).

## Shared memory

`WithMemoryBudget(IMemoryBudget)` borrows an application-owned shared allowance through
Awk's local interfaces. A budget carried by the invocation takes precedence and follows
nested command descriptors. Awk owns child scopes and reservations, never the supplied
budget. Each major frontend, compiled-program and runtime owner admits retained storage
and construction peaks; the optional execution-memory quota is an additional runtime cap.

Accounting is conservative and approximate, not a managed-heap cap: minor metadata,
process caches and backend internals are not measured exactly. Retained capacity stays
charged, including spare field slots after shrinking `NF`. Replacement/growth admits
overlapping old/new storage before mutation; a final result can fit while its peak does not.

**Any refused admission fails the command**, including the initial reservation. Awk stops
execution with `awk: memory limit exceeded` and nonzero status. It does not retry, wait,
spill, evict retained storage or select another algorithm/engine to recover. Refusal is
distinct from cancellation, actual `OutOfMemoryException` and unsupported-regex fallback.
Other resource failures retain their specific awk diagnostics without CLR exception text.

Mutation preflights protect arrays, fields, records, captures and output. Earlier committed
script/host effects remain. Released storage is refunded after actual users finish; a timed-out
or canceled wait does not end a host's buffer borrow. With a shared budget, cleanup waits for
those users. Retained compiled storage stays charged through active executions; disposal
prevents new users and refunds after existing users finish. A later command can reuse allowance.

Supplied regex engines borrow the active budget. Existing bounded execution caches own
compiled handles and dispose them on eviction/cleanup; routine eviction is separate from
fatal refusal. Engines must not turn admission refusal into unsupported-pattern fallback.

## Cancellation, streaming and publication

Every logical checkpoint polls cancellation even without fuel or counters. Caller cancellation
propagates; `WithExecutionTimeout` expiry is an awk failure. Host and cleanup deadlines bound
waiting, not hard termination. Regex matching has its own timeout; construction and some
framework/parser calls are indivisible. No universal cancellation latency or CPU cap is promised.

Fields split on demand where supported. Tight field quotas still validate the whole record
before publishing a prefix; `NF` and mutation can finish splitting. Streaming buffers and caches
retain bounded capacities/configured limits. Each statement flushes queued stdout/diagnostics;
formatting/output failures discard unpublished bytes and preserve prior committed effects.

For a finite runtime-memory quota with caller-owned cancellation:

```csharp
var policy = AwkExecutionPolicy.CreateDefault()
    .WithMaxExecutionMemoryBytes(64L << 20);
var exitCode = await awk.ExecuteAsync(host, policy, cancellationToken);
```

See [host containment](SECURITY.md), [UTF-8 policy](UTF8_POLICY.md) and
[record semantics](RECORD_SEMANTICS.md) for the related contracts.
