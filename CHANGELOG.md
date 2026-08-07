# Changelog

## 0.1.0

- Match awk paragraph-mode record semantics for whitespace-only lines, lone carriage returns, and
  single-character field separators, with GNU awk oracle coverage for the corrected edge cases.
- Keep `FS=""`, `FPAT`, and `FIELDWIDTHS` record fields as lazy byte slices until a value actually
  escapes the record, pass redirected print buffers directly to the host boundary, and materialize
  ordinal array keys once per snapshot.
- Retain typed array names through local and global resolution, centralize shared sort and numeric
  classification rules, and remove dead string runtime adapters, queued-IO variants, separator
  state, and production-only test buffers.
- Make helper defaults explicit at call sites, trust internal non-nullable contracts after public
  validation, and keep one-use parsing, sizing, and UTF-8 details scoped to their callers.
- Make case-insensitive regex matching independent of the ambient .NET culture, consistently with
  the runtime's C-locale-oriented compatibility policy.
- Preserve record boundaries and `RT` when a running program changes `RS` from a custom separator
  to the default newline separator.
- Separate host read failures from EOF through structured `AwkReadResult` outcomes; main input,
  `getline`, and source loading now surface failed reads instead of silently truncating input.
- Close prepared inputs, explicit inputs, redirections, pipes, and source descriptors after runtime
  failure or cancellation without allowing cleanup failures to replace the primary exception.
- Preserve the invocation working directory and the current awk `ENVIRON` snapshot when creating
  host commands for input and output pipes.
- Narrow `IAwkHost` to the six capabilities used by the runtime, type append and command statuses
  as `AwkExitCode`, reject malformed command result batches, and diagnose failed descriptor closes.
- Make governed regex enumerations and array-key snapshots return explicit disposable leases, and
  reject access after the reservation has been released.
- Share buffered-tail handling and byte-oriented scanning across record-separator strategies while
  retaining bounded cancellation checks and separator-specific matching.
- Remove the duplicate runtime-facade layer and dead string-based replacement helpers so variable,
  array, record, formatting, and regex operations have one resource-checked state boundary.
- Replace boxed source-loading and lowering outcomes with typed success, missing, and failure cases
  that cannot pair a status with the wrong payload.
- Simplify runtime-limit variants with validated record copies, use execution-step terminology
  consistently, and reject compiler stack underflow instead of silently clamping it.
- Validate public API inputs, snapshot caller-owned invocation collections, and make default host
  error/path-segment values safe to inspect.
- Accept parenthesized argument lists for `print` and `printf` statements.
- Evaluate regex constants against the current record in scalar expression contexts.
- Document and test that strict UTF-8 ingress is unchanged by C locale settings.
- Raise the default execution-step budget from one million to ten million after calibrating
  representative streaming workloads; explicitly injected tighter limits remain enforced, and
  sufficiently large aggregations can still exhaust the finite default.
- Initial standalone extraction target for Lokad.Awk.
