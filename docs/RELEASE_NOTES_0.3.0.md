# Lokad.Awk 0.3.0 core release candidate

This candidate qualifies ordinary awk text-processing inside .NET: projection,
filtering, aggregation, records/fields, flat arrays and user functions, with effects
supplied by an application-owned IAwkHost. It is not full POSIX/GNU conformance.
The package is prepared locally; publication is a separate release action.

## Correctness and predictable resource behavior

Supported-surface fixes cover grammar and value semantics, UTF-8/escapes, record and
field transitions, getline/EOF, array origins/sorting, numeric formatting and time,
profile diagnostics, and host failures. Resource-limit failures continue to preflight
observable mutation. See the [compatibility matrix](COMPATIBILITY_MATRIX.md) for the
qualified surface and [changelog](../CHANGELOG.md) for individual fixes.

Execution defaults now align with Lython: cooperative cancellation and host-call/
function-depth bounds, with no implicit execution fuel. Hosts can configure explicit
fuel and specialized quotas. The memory governor retains conservative logical
accounting, scoped leases and bounded streaming behavior; it is not a total-process
heap cap or hard CPU preemption. Host effects, strict UTF-8 ingress, host/cleanup
deadlines, bounded caches and stable resource-failure diagnostics remain in force.
See [runtime limits](RUNTIME_LIMITS.md) and [security boundaries](SECURITY.md).

Protection code is simpler: public/runtime numeric budgets share immutable values;
ordinary nested calls suspend under validated stack pressure; regex category metadata
uses one admitted construction pass; immediate memory charges share rollback ownership.
Public numeric configuration methods and both presets remain available.

## Measured execution cost

Focused quiet-box comparisons demonstrate:

- Wide-record allocation falls 52.85% after eliminating repeated split callbacks.
- Nested-function scenarios improve 2.04–3.00x with stack safety preserved.
- Cold finite-memory regex category preparation falls 17.4%, with unchanged semantics
  and memory envelope.
- Scalar-read simplification improves filtering 4.4–7.0% across repeated runs.
- Eliminating a duplicate field cache saves 80 allocated bytes per split record;
  projection/filtering/aggregation allocations fall 7.37–9.02% in the representative subset.

These are qualified scenario results, not a universal speedup or parity claim against
GNU awk. Evidence: [field/policy](BENCHMARK_SUMMARY_2026-10-11.md),
[function scheduling](BENCHMARK_SUMMARY_2026-10-12.md),
[regex preparation](BENCHMARK_SUMMARY_2026-10-12_REGEX.md), and
[representative processing](BENCHMARK_SUMMARY_2026-10-12_PROCESSING.md).

## Defaults and remaining limitations

The interpreter remains the default; the executable engine stays opt-in under the
[VM rollout contract](VM_ROLLOUT.md). DotNetCompatibility remains the regex default,
with Lokad.Utf8Regex pinned to 0.2.0. Adoption of the prepared newer Utf8Regex integration
is explicitly postponed; it does not block independent Awk work or this core release.
Known backend differences remain in the [regex contract](REGEX_COMPATIBILITY.md).

CSV, nested arrays, system(), namespaces, typed regex values, indirect calls and newer
GNU syntax remain deferred until concrete script demand justifies their cost. Numbers
remain invariant-culture binary64, ingress remains strict UTF-8 even in byte mode, and
general GNU lint coverage remains partial. The library supplies no default filesystem
or process host. Embedding applications retain ownership of those effects.
