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
It covers runtime storage; frontend/compiled ownership is separate.

Source/parser/binder storage uses a coarse frontend charge. Governed compiled reuse retains
its owner through active executions; disposal stops new executions, waits for users and
refunds it. Admission is conservative, and minor metadata, process caches and backend
internals are approximate. These estimates are not a managed-heap cap.

Memory failures preflight before observable mutation. Staging admits overlapping old/new
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

## Interpreter preparation and buffers

Eligible numeric, field, string and logical expressions and scalar loops reuse bounded synchronous
interpreter regions. They preserve logical checkpoints, argument/conversion/error order,
borrowed numeric field reads (including nested field indices) and original setters. Eligibility stops at expression depth 16,
16 body statements and four acyclic function levels. Host/pending/lint work and larger shapes
retain ordinary execution; function entry/unwind stack checks remain. Immutable preparation
belongs to the existing compiled owner. The interpreter remains the default engine.
Stored Number scalars can feed prepared global numeric reads and updates directly; other kinds
retain ordinary value admission and conversion.
Function locals share scalar/array value conversion and retain prepared numeric values/spelling.
Prepared scalar arguments reuse current kinds; value-size admission and long-text cancellation
remain in ordinary evaluation. Missing locals and aliases retain original resolution.
Established ordinary global arrays retain their kind after successful access; storage and
mutation checks remain original. Special arrays and live function contexts retain name resolution.
Syntax-depth settings above 128 retain unconditional function-entry/unwind suspension.
Eligible loops and complete scalar actions also reuse known-call metadata for zero through
four arguments. Multiple arguments use a fixed stack temporary and the original function frames;
omitted/extra arguments and larger arities retain ordinary evaluation. Calls retain
their original executor at VM, lint, pending-output, raised-syntax and stack-safety boundaries.

`index`, `substr`, `tolower` and `toupper` evaluate arguments into value locals before
conversion; other builtins use admitted staging where required. Actual result allocations
preflight. `printf` publishes its admitted staging buffer atomically; `sprintf` returns owned
text. Eligible literal field operands can borrow bytes/numbers during synchronous formatting.
Avoided copies and rounded capacities can change optional memory thresholds.
Stdout `print` with one through four values uses locals instead of staging arrays; formatting
remains leased and the complete append preflights before publication. Larger, redirected and
executable-engine prints retain staging. Statement flush boundaries remain.
Two to four literal positive field operands can borrow field slices through synchronous output
copying. Modified/cached fields retain print formatting; every complete append still preflights.

Fields split incrementally where supported; tight quotas still validate the whole record
before publishing a prefix. NF or mutation can complete the scan. Record classification and
rescans remain cancellable and charged, so explicit fuel can reject earlier. See
[record semantics](RECORD_SEMANTICS.md) and [UTF-8 policy](UTF8_POLICY.md).

For finite execution memory with caller-owned cancellation:

```csharp
var policy = AwkExecutionPolicy.CreateDefault()
    .WithMaxExecutionMemoryBytes(64L << 20);
var exitCode = await awk.ExecuteAsync(host, policy, cancellationToken);
```

See [host containment](SECURITY.md) for capabilities and application responsibilities.
