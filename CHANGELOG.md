# Changelog

## 0.1.0

- Preserve numeric values and full precision through `printf`, custom `OFMT`/`CONVFMT`, `asort`,
  user sort comparators, and host environment snapshots instead of formatting and reparsing them;
  project array values to text only for value-string sort modes that consume that projection.
- Preserve scalar type, missing-value state, and array-storage identity when forwarding function
  locals; let omitted parameters become frame-owned arrays; and match GNU awk when
  `typeof(array[index])` materializes the array without inserting the element.
- Apply `CONVFMT` consistently at comparison, regex, substitution, switch, record, field, separator,
  array-order, and host string boundaries while keeping record and field separator classification
  byte-native.
- Track intentional field-separator and `SYMTAB` string crossings while avoiding symbol-table
  encode/decode round trips during value snapshots.
- Avoid allocations on missing-array probes, parse unsupported frontend directives once, and model
  action-flow, input-record, and separator-cache states explicitly.
- Refresh the checked-in ShortRun and deterministic runtime-counter baseline on .NET 10.0.8 with
  reproducible host, SDK, command, timing, allocation, and memory-governor evidence.
- Enforce `MaxHostCalls` once across every input and output path, retain source-open failures rather
  than treating them as missing files, and reject successful host reads that permit more data
  without making progress.
- Bound indexed access to deeply composed token lists by materializing one thread-safe snapshot on
  demand, avoiding accidental quadratic parsing work.
- Preflight runtime-owned UTF-8 copies, field materialization, regex captures, records, and formatted
  output before allocation or visible mutation, with runtime-counter coverage for deliberate string
  compatibility boundaries.
- Return `AwkExitCode` through the public and internal execution pipeline, validate file descriptor
  identifiers at construction, and remove the unused string-based path-resolution facade.
- Remove forwarding async state machines, read-only collection wrappers, zero-argument pool rents,
  and unused runtime conveniences; cache rune counts alongside ASCII classification without adding
  per-value storage.
- Share postfix trailing-token scanning, function-local assignment, pooled argument-buffer handling,
  and async array-sort entry selection; keep single-purpose numeric sort parsing scoped to its caller.
- Recognize capture-group and noncapturing-group wrappers and equivalent punctuation-only
  alternations around byte-run record separators, preserving greedy cross-batch `RT` while
  scanning the adversarial counter workloads once instead of invoking the general regex backend.
- Keep regex replacements byte-native through `sub`, `gsub`, and `gensub`, share the
  single/all traversal engines without allocating on the single-match route, return the original
  input before building output when a requested numbered occurrence is absent, and reuse pooled
  argument, frame, and slot storage for interpreted user-function calls.
- Bypass per-line include parsing for ordinary source files, cache successful canonical include
  reads, compose continuation maps without copying their existing offsets on every append, append
  contiguous expanded-source chunks in batches, binary-search source maps, and defer physical-line
  indexing until a diagnostic actually needs it.
- Share owned and record-slice field traversal, preserve source names and coordinates as value
  types, follow interface naming conventions, and remove dead text-conversion and source
  projection plumbing.
- Remove the duplicate test-only benchmark harness, test-only synchronous sort facade,
  zero-consumer metadata properties, and forwarding-only runtime conveniences; centralize getline
  read-failure conversion; and document buffered record-cursor invariants.
- Route main input and all `getline` sources through one streaming, `RS`-aware record cursor;
  preserve unread buffered bytes across separator changes without draining descriptors to EOF,
  and retain paragraph field-splitting semantics when `getline` assigns `$0`.
- Snapshot host-provided line offsets and document borrowed read-content lifetimes per descriptor,
  while keeping independent compiled executions safe to run concurrently against separate hosts.
- Replace nullable regex backend state and flush-all sentinels with explicit variants, keep lowered
  identifiers strongly typed, consolidate substitution traversal, and remove dead runtime helpers
  and the forwarding frontend classifier.
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
- Preserve record boundaries and `RT` for every running-program `RS` transition, including newline
  to custom, custom to newline, paragraph, literal, byte-set, non-ASCII, and regex separators.
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
- Share byte-oriented scanning through a pull-style record cursor while retaining bounded
  cancellation checks, separator-specific matching, and the batched default-newline path.
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
