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

Sources containing explicit line continuations receive a token-aware normalization pass
under the same frontend token ceiling and cancellation token. Sources without continuations
reuse their original text. Invalid expression separators and comparison chains count toward
the frontend diagnostic ceiling and fail before any rule or function executes.

## Host And IO Limits

These limits are host-mediated and publicly configurable:

- `MaxHostCalls`: ordinary host operations such as open, read, write, and close across input,
  output, redirections, pipes, and source loading for one execution. Cleanup calls are recorded by
  runtime counters but are allowed to proceed within the aggregate cleanup deadline even after this
  budget has been exhausted. Both presets allow 100,000 ordinary host calls by default.
  Each ordinary host entry also checks cancellation and consumes one execution step.
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
  arrays, record/field and assigned-NF text, governed transient builders, VM frames, and
  explicit preflighted `AwkText` byte
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

Numeric field conversion is deferred until record reconstruction. Its temporary formatted
text, field buffers, and destination record are governed while the old record remains live;
the field-buffer estimate includes 24-byte value-type entries and object/array headers.
The complete composed value is checked before fields or `$0` are replaced. OFS assignment
reserves its scalar growth before requesting this reconstruction, so either failure preserves
the previous separator and dirty numeric fields. Each converted field checks cancellation
before formatting and charges its resulting byte work against the shared execution allowance.

Split separator arrays charge their actual published entries, including actual empty
separators between characters, and exclude absent boundaries. The two replacement arrays
preflight their element counts and combined retained memory before either is published.
Transient capture buffers account for indexed 16-byte entries and list growth headroom.
This applies to `split` and `patsplit` without merging their different boundary rules.

## Compatibility Policy Limits

These limits constrain awk execution without changing successful compatibility behavior:

- `MaxExecutionSteps`: one shared logical-work allowance for either engine. Both presets default
  to 50,000,000 steps, aligned with Lokad.Lython. Explicit overrides remain effective.
- `MaxFunctionCallDepth`: active user-function frames. The compatibility default is 1,000,
  aligned with Lokad.Lython; the restricted preset retains 32. Nested function invocations
  suspend at entry and unwind to keep completed asynchronous operations from growing the CLR
  stack. Argument evaluation receives the same protection before a callee frame is pushed.
- `MaxSyntaxNestingDepth`: delimiter, statement, and expression-tree depth, checked before binding
  and execution so hostile nesting cannot exhaust the CLR stack.
- `MaxArrayElements`: associative array elements.
- `MaxFields`: current-record field materialization budget. The default matches
  `MaxArrayElements`, but hosts can now set a different field policy.
- `MaxRegexPatternBytes` and `MaxRegexGroups`: breadth limits checked before compiling source or
  runtime regex patterns.
- `MaxRegexCompilations` and `MaxRegexCacheBytes`: aggregate compilation work and estimated retained
  regex-cache capacity. Observable ERE selection compiles its structured program on first use,
  counts that compilation, and retains it in a bounded cache keyed by pattern and effective case
  sensitivity. Cache capacity is partitioned among handles, .NET, UTF-8, legacy selection
  constraints, and structured programs. Iterative compilation preflights expanded interval nodes
  and temporary memory before construction, and observes cancellation and a two-second deadline.
- `MaxRegexOperations`: optional secondary ceiling on aggregate regex search/match calls,
  including cache hits. It defaults to unlimited in both presets; searches draw from the common
  execution allowance. Explicit `WithMaxRegexOperations` overrides retain their existing diagnostic.
- `MaxRegexCaptureBytes`: `match(..., array)` capture payloads, preflighted from backend spans before
  group values are copied. Capture-array replacement separately preflights its complete entry count
  and retained memory before changing the destination array or `RSTART`/`RLENGTH`.
  Selection's private prefix/endpoint groups do not consume user capture payload or group budgets.
  Their compiled pattern and mapping contribute to estimated cache capacity. Structured whole
  match and capture selection share one governed regex operation and one two-second deadline.
  The memory governor preflights fixed workspace buffers sized by compiled nodes, nesting, and
  requested groups, independent of input length. Their lease remains live through capture
  materialization and is released on success, failure, timeout, or cancellation. Inherited backend
  extensions retain their existing selection constraint and two-second backend timeout.
- `MaxRegexRecordSeparatorBytesScanned`: cumulative bytes submitted to the general regex record-
  separator fallback. It defaults to unlimited in both presets; every repeated backend search
  consumes the common execution allowance for its offered search span. Explicit overrides retain
  their existing diagnostic. Specialized byte, literal, and byte-run separators do not consume
  this secondary ceiling. Case-sensitive multi-character separators containing no regex
  metacharacters use the literal cursor, preserving only a possible partial separator at
  batch boundaries instead of submitting overlapping suffixes to the regex backend.
- `MaxSortComparisons`: optional secondary ceiling on built-in and user-defined array-sort
  comparisons. It defaults to unlimited in both presets; each comparison checks cancellation
  and consumes one execution step. Explicit overrides retain their existing diagnostic.

Compatibility defaults remain intentionally generous or unlimited where ordinary awk scripts
historically expect no fixed ceiling. The restricted preset makes invocation, frontend, host-call,
input, record, value, output, symbol, regex, sort, VM-stack, aggregate execution-memory, source,
include, file, pipe, syntax, field, array, and execution-step work finite. It also supplies a
cooperative `ExecutionTimeout`; caller cancellation continues to take precedence and remains
observable as cancellation.

The execution governor charges one step for each evaluated expression node, dispatched statement,
admitted input record, scanned main-input operand position, loop iteration, user-function invocation,
ordinary host entry, regex search, and sort comparison. Short-circuited expressions charge only the
selected path. VM lowering retains
expression checkpoints even for folded field indexes, and fallback wrappers do not add steps.
Variadic bitwise calls evaluate every argument once using those expression checkpoints;
their unsigned fold adds no separate allowance or short circuit, even after a zero operand.
Both engines flush standard output at statement boundaries so host admission and already emitted
output agree when a later statement exhausts the allowance.

Byte-oriented built-in work additionally charges one step per started 4,096-byte chunk of offered
input: field splitting, FIELDWIDTHS parsing, Unicode string length, string search and slicing,
pure-string `strtonum` conversion,
separator scanning, and each regex search's remaining input. Regex rescans charge the offered
span again. Chunk admission is atomic; an insufficient allowance fails before backend matching,
array replacement, or record publication. Incremental separator cursors charge bytes actually
inspected before consuming the record. Cached operations retain the same documented allowance.
Cleanup and emergency diagnostics bypass exhausted execution and host allowances.

These units measure logical admitted work, not elapsed CPU time or input records. Backend regex
timeouts, pattern and compilation safeguards, host deadlines, caller cancellation, and the
restricted preset's 30-second execution deadline remain independent safeguards. Memory,
reserve/commit/release, and size limits retain their awk-specific policy. To migrate from the old
independent CPU ceilings, configure `WithMaxExecutionSteps`; keep the secondary `With...` overrides
only when a separate regex, scan-byte, or comparison ceiling is wanted.

`AwkRuntimeCounters.ExecutionSteps` exposes the charged total. The Release counter-report workflow
in `docs/BENCHMARKS.md` includes deterministic streaming shapes so changes to the accounting model
or default budget can be calibrated without using machine-dependent elapsed time.

## Internal Safety Invariants

These batching and cache names remain internal implementation policy:

- `DefaultInputReadBatchLineCount`: maximum lines requested per streaming host read.
- `DefaultUtf8BuilderInitialCapacity`: initial byte-builder capacity.
- `DefaultRegexCacheSize`: capacity of each compiled-regex least-recently-used cache. The runtime
  maintains independent caches for awk regex wrappers, .NET regex instances, UTF-8 engines,
  legacy selection constraints, and structured ERE programs.
- `DefaultRegexMatchTimeoutSeconds`: backend regex timeout and structured compilation/match guard.
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
The immutable declared-global inventory used by `SYMTAB` is compiled program metadata,
bounded by source symbol/identifier limits. Indirect reads and membership checks do not
materialize scalar values or charge execution memory. Dynamic SYMTAB writes still
preflight the ordinary symbol, identifier, value, and execution memory limits.

An untouched ordinary array element retains a governed 24-byte type cell in addition
to the usual entry/key/slot charge. Untouched scalar function arguments retain the
same origin cell, conservatively charged with each frame slot that holds it. Values
and variable slots remain 24-byte structs. First-read count/memory checks run before
array creation or alias promotion; local scalar assignment reserves its growth before
promoting the caller's source type. Failure preserves entries, typing and committed
memory and leaves no outstanding reservation. Scalar assignment/return copies drop
the origin cell; replacing or deleting original storage does not invalidate arguments.

Lazy record frames retain the admitted splitting configuration as described in
`docs/RECORD_SEMANTICS.md`. Custom separator or FPAT text and retained FIELDWIDTHS list
capacity are charged with the record before replacement. A failed admission preserves
the previous record and plan. Successful field materialization releases the plan's
retained charge; file-boundary reset releases the record, fields, caches, and plan.

Array sorting reserves the entry snapshot required by every comparison mode, plus the merge buffer
and four comparator arguments only for user functions with at least two entries. A sorted key
enumeration also reserves its returned key snapshot. `asort` and `asorti` write their sorted entries
directly into the replacement array, so their preflight does not charge obsolete dictionary or
projection buffers.

Type-aware modes retain numeric values without eagerly formatting them. Numeric
value modes format only equal numeric values for their string tie-break. Every actual
comparison retains its work/cancellation checkpoint and sort-comparison limit.
`@unsorted` traversal reserves only the returned key snapshot and performs no sort
comparisons; unsorted `asort`/`asorti` still reserve their entry snapshot and preflight
the destination replacement. Memory and comparison failures preserve source values
and the destination and release transient reservations.

Case-insensitive text comparisons and index searches admit bounded scan chunks through
the shared work governor, checking cancellation before each chunk. The charge conservatively
covers decoded scalars and repeated candidate comparisons; an index search's overlapping
matches cannot bypass the allowance by charging only the haystack length. These operations
allocate no folded copies or search tables. Early comparison mismatches avoid charging the
unexamined suffix, and a folded sort failure occurs before destination replacement.

Runtime diagnostic queues charge their retained messages and backing capacity to the
same memory governor. Admission reserves message bytes and any new backing array while
the old array is still live, before adding a warning. Conservative per-message headroom
also covers normalization and UTF-8 encoding during flush. Flush releases queued-message
charges even when a host write fails; retained reusable capacity stays charged until
sink disposal. A warning admission failure preserves previously queued diagnostics.

## Mutation Semantics

Resource failures must occur before the observable mutation when the current surface requires it:
array insertions, array replacement, field-list growth, record processing, output writes, regex
capture array replacement, text-value copies, formatted output construction, and VM stack growth
all preflight their limit before changing awk state or host-visible output.
