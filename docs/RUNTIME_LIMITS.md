# Runtime Limits Contract

`AwkRuntimeLimits` is an internal runtime policy object, carried through internal
`AwkRuntimeOptions`. Defaults preserve current command-line behavior, while tests can opt into
tight limits to prove deterministic failure semantics.

## Host Policy Limits

These limits are host-mediated and may be changed by internal hosts/tests through
`AwkRuntimeOptions`; they are not a stable public embedding API:

- `MaxHostCalls`: total host operations such as open, read, write, and close.
- `MaxOutputFiles`: number of distinct redirected output files.
- `MaxOutputBytes`: bytes emitted to host-visible output streams.
- `MaxRecordBytes`: bytes in one awk-visible input record.
- `MaxValueBytes`: bytes in one awk-visible text value.
- `MaxExecutionMemoryBytes`: live governed runtime memory across retained ordinary arrays,
  governed transient builders, VM frames, and explicit preflighted `AwkText` byte allocations.

Failures use awk-shaped diagnostics and do not leak CLR exception text. Byte and memory ceilings
use `awk: memory limit exceeded`; execution-step, host-call, and output-file ceilings use their
dedicated awk diagnostics.

## Compatibility Policy Limits

These limits constrain awk execution without changing successful compatibility behavior:

- `MaxExecutionSteps`: statement dispatch and loop-iteration budget for runaway scripts.
- `MaxFunctionCallDepth`: user-function recursion depth.
- `MaxArrayElements`: associative array elements.
- `MaxFields`: current-record field materialization budget. The default matches
  `MaxArrayElements`, but hosts can now set a different field policy.
- `MaxRegexCaptureBytes`: materialized regex capture payloads before writing capture arrays.

These defaults are intentionally generous or unlimited where ordinary awk scripts historically
expect no fixed ceiling.

## Internal Safety Invariants

These names are not public embedding policy unless a concrete host need appears:

- `DefaultInputReadBatchBytes`: streaming read batch size.
- `DefaultUtf8BuilderInitialCapacity`: initial byte-builder capacity.
- `DefaultRegexCacheSize`: compiled regex cache capacity before clearing.
- `DefaultRegexMatchTimeoutSeconds`: .NET regex timeout guard.
- `MaxVmStackBytes`: executable runtime stack bytes. Fallback frames and opcode operand-stack
  buffers check this limit and, when the memory governor is enabled, reserve/commit/release the
  same estimated bytes through the governor.

`AwkText` allocation itself is preflighted with the governor and then released after construction;
retained ordinary array entries are charged separately with an estimate that includes key/value
payloads and small-object overhead. Read-only built-in special arrays such as `FUNCTAB` are static
runtime metadata and are not charged against the execution memory budget.

## Mutation Semantics

Resource failures must occur before the observable mutation when the current surface requires it:
array insertions, array replacement, field-list growth, record processing, output writes, regex
capture array replacement, and VM stack growth all preflight their limit before changing awk state
or host-visible output.
