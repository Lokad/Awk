# Runtime Limits Contract

`AwkRuntimeLimits` is an internal runtime policy object, carried through internal
`AwkRuntimeOptions`. Defaults preserve current command-line behavior, while tests can opt into
tight limits to prove deterministic failure semantics.

## Host Policy Limits

These limits are host-mediated and may be changed by internal hosts/tests through
`AwkRuntimeOptions`; they are not a stable public embedding API:

- `MaxHostCalls`: aggregate host operations such as open, read, write, and close across input,
  output, redirections, pipes, source loading, and cleanup for one execution.
- `MaxOutputFiles`: number of distinct redirected output files.
- `MaxOutputBytes`: bytes emitted to host-visible output streams.
- `MaxRecordBytes`: bytes in one awk-visible input record.
- `MaxValueBytes`: bytes in one awk-visible text value.
- `MaxBuilderBytes`: bytes retained by one transient UTF-8 builder. The default is 64 MiB, and
  growth is rejected before the builder changes.
- `MaxExecutionMemoryBytes`: conservative live governed runtime memory across retained ordinary
  arrays, governed transient builders, VM frames, and explicit preflighted `AwkText` byte
  allocations. Retained text estimates include capacity for the decoded-string and sparse-rune
  caches that a later compatibility operation may populate. Deferred concatenations charge their
  retained rope nodes, child text, and root cache allowance, not only the eventual flattened
  payload.

Failures use awk-shaped diagnostics and do not leak CLR exception text. Byte and memory ceilings
use `awk: memory limit exceeded`; execution-step, host-call, and output-file ceilings use their
dedicated awk diagnostics.

## Compatibility Policy Limits

These limits constrain awk execution without changing successful compatibility behavior:

- `MaxExecutionSteps`: interpreter expression evaluation, statement dispatch, and explicit
  loop-iteration budget for runaway scripts. The executable engine charges its documented VM
  budget-check points. A step is not an input record: ordinary scans consume multiple steps per
  record according to their expression and statement shape. The current default is 10,000,000.
- `MaxFunctionCallDepth`: user-function recursion depth.
- `MaxArrayElements`: associative array elements.
- `MaxFields`: current-record field materialization budget. The default matches
  `MaxArrayElements`, but hosts can now set a different field policy.
- `MaxRegexCaptureBytes`: `match(..., array)` capture payloads, preflighted from backend spans before
  group values are copied. Capture-array replacement separately preflights its complete entry count
  and retained memory before changing the destination array or `RSTART`/`RLENGTH`.
- `MaxRegexRecordSeparatorBytesScanned`: cumulative bytes submitted to the general regex record-
  separator fallback. The 64 MiB default bounds repeated cross-batch searches when a backend cannot
  resume an arbitrary partial match; specialized byte, literal, and byte-run separators do not
  consume this budget.

These defaults are intentionally generous or unlimited where ordinary awk scripts historically
expect no fixed ceiling.

The ten-million-step default is calibrated against deterministic Release counter-report workloads,
including projection, multi-key filtering, associative aggregation, and two-file comparison. It
keeps the runaway-script guard finite while allowing representative streaming work to cross the old
one-million-step ceiling. Hosts and tests can still inject a tighter execution-step budget, and the
other resource governors are unchanged. Outer cancellation and a Studio command timeout remain
independent safeguards.

`AwkRuntimeCounters.ExecutionSteps` exposes the charged total. The Release counter-report workflow
in `docs/BENCHMARKS.md` includes deterministic streaming shapes so changes to the accounting model
or default budget can be calibrated without using machine-dependent elapsed time.

## Internal Safety Invariants

These names are not public embedding policy unless a concrete host need appears:

- `DefaultInputReadBatchLineCount`: maximum lines requested per streaming host read.
- `DefaultUtf8BuilderInitialCapacity`: initial byte-builder capacity.
- `DefaultRegexCacheSize`: capacity of each compiled-regex least-recently-used cache. The runtime
  maintains independent caches for awk regex wrappers, .NET regex instances, and UTF-8 engines.
- `DefaultRegexMatchTimeoutSeconds`: .NET regex timeout guard.
- `MaxVmStackBytes`: executable runtime stack bytes. Fallback frames and opcode operand-stack
  buffers check this limit and, when the memory governor is enabled, reserve/commit/release the
  same estimated bytes through the governor.

Runtime-owned `AwkText` byte arrays, sparse rune checkpoints, and builder materialization are
preflighted with the governor; APIs accepting an already-created owned array account it at their
boundary. The transient construction charge is released after construction. Retained text is
charged separately with a conservative estimate that includes its payload, current 64-bit object
layout, and capacity for both optional caches, so populating a cache cannot take retained memory
outside the execution budget. Retained ordinary array entries additionally charge their key/value
and small-object overhead.
Materialized regex matches retain one governed lease for their selected group structures and copied
text until the consumer advances or disposes the match. Capture-array replacement selects all
groups and transfers the copied text portion of that lease into the destination array instead of
dropping or double-charging it. `gensub` leases only its observable capture prefix.
Read-only built-in special arrays such as `FUNCTAB` are static runtime metadata and are not charged
against the execution memory budget.

Array sorting reserves the entry snapshot required by every comparison mode, plus the merge buffer
and four comparator arguments only for user functions with at least two entries. A sorted key
enumeration also reserves its returned key snapshot. `asort` and `asorti` write their sorted entries
directly into the replacement array, so their preflight does not charge obsolete dictionary or
projection buffers.

## Mutation Semantics

Resource failures must occur before the observable mutation when the current surface requires it:
array insertions, array replacement, field-list growth, record processing, output writes, regex
capture array replacement, text-value copies, formatted output construction, and VM stack growth
all preflight their limit before changing awk state or host-visible output.
