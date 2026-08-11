# Runtime Limits Contract

Embedding applications configure limits through the public immutable `AwkExecutionPolicy` passed
to `Awk.ExecuteAsync`. `CreateDefault()` preserves command-line compatibility. The finite
`CreateRestricted()` preset is the starting point for untrusted awk and can be refined through its
`With...` methods. Internal execution maps the public policy to `AwkRuntimeLimits` and carries it
through `AwkRuntimeOptions`.

## Invocation And Frontend Limits

The invocation is checked against `MaxArguments`, `MaxArgumentBytes`, `MaxEnvironmentEntries`, and
`MaxEnvironmentBytes` before command-line planning creates its derived lists. Source loading and the
frontend then apply:

- `MaxSourceUnits`, `MaxSourceBytes`, and `MaxExpandedSourceBytes` to the original source list,
  bytes read from the host, and each unit inserted into the expanded compilation source;
- `MaxIncludeDepth` and `MaxIncludeFiles` before following a nested include;
- `MaxFrontendCharacters`, `MaxFrontendTokens`, and `MaxFrontendDiagnostics` before or during
  lexing; and
- `MaxSyntaxNestingDepth`, `MaxSymbols`, and `MaxIdentifierBytes` before binding creates deeply or
  broadly shaped program state. Dynamic `SYMTAB` scalar names are governed by the same symbol and
  identifier limits.

`AllowSourceIncludes`, `AllowDynamicMainInputPaths`, and `ExposeEnvironment` are capability controls,
not numeric limits. The restricted preset disables them, so the script cannot grow the explicit
source/input capability sets through `@include`, `AWKPATH`, or changed `ARGV` entries.

## Host And IO Limits

These limits are host-mediated and publicly configurable:

- `MaxHostCalls`: ordinary host operations such as open, read, write, and close across input,
  output, redirections, pipes, and source loading for one execution. Cleanup calls are recorded by
  runtime counters but are allowed to proceed within the aggregate cleanup deadline even after this
  budget has been exhausted.
- `HostOperationTimeout`: maximum await time for one ordinary host operation. `CommandTimeout`
  independently bounds pipe commands. `CleanupTimeout` is one shared best-effort deadline for all
  cleanup calls in a source-loading or runtime phase, rather than a fresh timeout for every open
  descriptor. A normal descriptor close uses the earlier of the host-operation and cleanup
  timeouts so shutdown cannot extend beyond either policy.
- `MaxOutputFiles`: number of distinct redirected output files.
- `MaxInputFiles`: number of concurrently open redirected input files.
- `MaxPipes`: number of concurrently open input and output command pipes.
- `MaxInputBytes` and `MaxRecords`: aggregate runtime input admitted across descriptors and the
  number of records made visible to awk.
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
- `MaxHostErrorBytes`: largest structured host error that may be copied into a diagnostic.
  `ExposeHostErrorDetails` disables such disclosure entirely; the restricted preset uses a generic
  message.

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
- `MaxSyntaxNestingDepth`: delimiter, statement, and expression-tree depth, checked before binding
  and execution so hostile nesting cannot exhaust the CLR stack.
- `MaxArrayElements`: associative array elements.
- `MaxFields`: current-record field materialization budget. The default matches
  `MaxArrayElements`, but hosts can now set a different field policy.
- `MaxRegexPatternBytes` and `MaxRegexGroups`: breadth limits checked before compiling source or
  runtime regex patterns.
- `MaxRegexCompilations` and `MaxRegexCacheBytes`: aggregate compilation work and estimated retained
  regex-cache capacity.
- `MaxRegexOperations`: aggregate regex search/match calls, including cache hits.
- `MaxRegexCaptureBytes`: `match(..., array)` capture payloads, preflighted from backend spans before
  group values are copied. Capture-array replacement separately preflights its complete entry count
  and retained memory before changing the destination array or `RSTART`/`RLENGTH`.
- `MaxRegexRecordSeparatorBytesScanned`: cumulative bytes submitted to the general regex record-
  separator fallback. The 64 MiB default bounds repeated cross-batch searches when a backend cannot
  resume an arbitrary partial match; specialized byte, literal, and byte-run separators do not
  consume this budget.
- `MaxSortComparisons`: built-in `asort`/`asorti` comparator calls. Cancellation and this budget are
  checked from inside the comparison loop.

Compatibility defaults remain intentionally generous or unlimited where ordinary awk scripts
historically expect no fixed ceiling. The restricted preset makes invocation, frontend, host-call,
input, record, value, output, symbol, regex, sort, VM-stack, aggregate execution-memory, source,
include, file, pipe, syntax, field, array, and execution-step work finite. It also supplies a
cooperative `ExecutionTimeout`; caller cancellation continues to take precedence and remains
observable as cancellation.

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

These batching and cache names remain internal implementation policy:

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
