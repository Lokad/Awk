# Changelog

## 0.1.0

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
- Accept parenthesized argument lists for `print` and `printf` statements.
- Evaluate regex constants against the current record in scalar expression contexts.
- Document and test that strict UTF-8 ingress is unchanged by C locale settings.
- Raise the default execution-step budget from one million to ten million after calibrating
  representative streaming workloads; explicitly injected tighter limits remain enforced, and
  sufficiently large aggregations can still exhaust the finite default.
- Initial standalone extraction target for Lokad.Awk.
