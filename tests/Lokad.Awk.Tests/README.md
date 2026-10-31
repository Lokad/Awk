# Awk Test Layers

The awk suite is intentionally split by boundary. New coverage should start in the narrowest layer that observes the behavior without depending on unrelated plumbing.

## Golden Corpus

`AwkGoldenCorpus` is the canonical behavior and diagnostic suite for the awk language surface. Add cases here for accepted awk programs, runtime behavior, compatibility-mode differences, diagnostics, host-policy errors surfaced through awk, and stderr/stdout ordering that is visible to awk callers.

Compiled executions emit their own frontend warnings. The golden harness must not append
`compiled.Warnings` separately; doing so duplicates diagnostics and bypasses execution's
resource and host-boundary checks. `AwkCompiledWarningTests` covers artifact reuse, mapped
source locations, warning admission and pending/failed host appends.

`AwkFrontendMemoryTests` covers shared source/parser admission and owned compiled-artifact
lifetimes. Governed internal compilation needs a caller budget that survives artifact reuse;
dispose the artifact after its users finish. Warning views borrow that lifetime.

Control-header regex goldens cover statement-start context and division controls. Lexer checks
pin token spans. Provider ownership tests cover literal/dynamic admission; host-engine tests
cover selected handles across literal forms and separate caches for prepared-program reuse.
Regex escape-warning checks cover source-backed spans without pattern-sized scratch,
alongside the canonical warning-order, scalar-rendering and history cases.
Host-engine checks reject nonpositive group counts before later script effects and dispose
the acquired handle; adapter checks include whole-match, named and sparse-numbered counts.

Composite-key field goldens preserve spelling, dynamic index order, cached/mutated values,
record replacement and getline. Counter cases distinguish owned final keys from temporary
component text; pending host-read cases check the earlier component survives record reuse,
refusal/cancellation stops later effects and all execution owners refund.
Repeated-update counters also distinguish retained keys from lookup-only copies. Composite
key/owner snapshots survive pending RHS clear/rehash and special-array aliases without
retaining dictionary entry references.
Entry-cell cases cover same-key RHS writes, deletion/reinsertion, table replacement,
payload retirement and refused insertion. Numeric footprint checks include cell storage.

Failing golden cases must assert the awk exit code, exact stderr, expected stdout preservation, and no CLR exception leakage through `AwkTestHarness.AssertFriendlyAwkFailure`.

## Published regex-provider qualification

The pinned published provider, Lokad.Utf8Regex 0.4.0, lacks required POSIX longest/subcapture
selection. Its scalar, simple-class predicate and empty-group cases now run normally.
`AwkRegexProviderGaps` lists the exact affected golden cases. Standard xUnit data/fact skips
report those cases explicitly; the original AWK expectations and assertions remain in place.
All parser/lowering checks run. Only 22 golden cases (in both direct and prepared execution) and three
focused selection cases are deferred. Other regex, translation, host, cancellation, memory,
capture-publication and ownership checks remain active.
New failures are not automatically added to this list.

Adapter boundary checks cover range-only versus detailed selection, dense publication of sparse
provider groups, token forwarding and disposal. Published0.4.0 has no shared-budget hook; the
adapter's handle/search reservations are coarse estimates, not full engine allocation admission.
Native-pattern checks pin byte content through translation and equivalent cache keys, including
supplementary scalars and NUL. Core conversion counters exclude the example adapter's temporary
pattern decoding for the published provider's string-only compilation API.
Cache-admission cases use a compact, conforming provider adapter to separate retained core
keys from provider storage estimates. They cover public refusal/no later effects/refunds and
key admission surviving eviction until active searches finish.
Byte-search checks cover scoped subject lifetimes, current tokens, byte restarts in either
direction and eviction during active use. Coordinate goldens cover supplementary scalars,
combining sequences, NUL, empty/absent captures and byte mode. Core counter checks require
linear scalar publication walks and no coordinate work for span-only/replacement searches.

Before adopting a provider release, set `AwkRegexProviderGaps.SkipReason` to `null` locally
and run the full suite. Qualify the unchanged semantic expectations, then remove resolved
entries/skip annotations. Green CI with these deferrals does not establish complete regex
conformance. The core package remains independent of the concrete provider.

## End-to-End Host Tests

CSV goldens use pinned GNU 5.3.2 outcomes. `AwkCsvHostTests` checks byte/chunk boundaries,
reused host buffers, stdin/files/pipes, pending cancellation, refusal, strict UTF-8 and refunds;
prepared counter checks preserve byte-native processing and linear record scans. CSV decoding
does not require a regex provider; explicit FPAT assignments retain regex validation.

The mock filesystem publishes successful file creation/truncation at open; appended
data stays buffered until descriptor close. Close-mode goldens cover files;
`AwkIoCloseDirectionTests` runs file/pipe contracts through public execution with
shared admission and cleanup checks. `MockFileSystemTests` checks open-time effects.
Hosts return one command status. `AwkHostContractTests` checks synchronous failure before
handoff and host endpoint retirement after a returned faulted/canceled task;
partial/structured-close tests retain pending-failure, ownership and no-replay coverage.
Append tests distinguish complete acceptance, downstream closure and typed failure for stdout,
files and pipes. They cover pending-buffer funding through cancellation, one terminal diagnostic,
failed stderr without retry and cleanup without running later statements or END actions.
Path cases exercise original spelling/cwd/root forwarding and host traversal with links,
missing intermediate directories and trailing slashes. They check policy before effects,
resolved source identity/reuse and unchanged exact redirection stream names.
`AwkHostReadFailureTests` covers consumed-prefix commit/disposal, unread suffixes after early
exit, terminal data/error ordering, source/getline behavior, scalar-splitting chunks and
stdin aliases written only after releasing their read borrow. Regex windows admit only
complete validated UTF-8; an incomplete trailing scalar waits for the next byte chunk.

Use host-facing tests when the risk includes command quoting, pipelines, command substitution, descriptor wiring, working-directory behavior, source-file paths, or integration with other host tools. Do not duplicate plain language behavior here unless a host boundary is the point of the test.

## Lexer, Syntax, Parser, and Binder Tests

Use lexer, syntax parser, parser, and binder tests for internal structure: token classification, source spans, statement outlines, expression outlines, symbol discovery, parser acceptance, and semantic diagnostics. These tests should not become a second behavior corpus for ordinary awk execution.

## Source Loader Tests

Use `AwkSourceLoaderTests` for source-unit assembly, include ordering, AWKPATH lookup, source segments, continuation normalization, and structured source-load failures. Shell-facing source-load diagnostics still need golden or end-to-end coverage.
Public source-search cases also cover unused AWKPATH tails under shared admission and
candidate refusal/cancellation before host access.
Automatic-input goldens and host checks cover BEGIN-only completion, explicit getline,
unused invalid input, and the END/main/file-hook rules that still require input.
Invocation admission/startup checks cover borrowing immutable entry environments, bounded
planning allocation, hidden environments and caller/ENVIRON isolation across reuse.

Directive goldens and public-boundary cases cover same-line source, malformed directives and
top-level context. Source-loader checks preserve original tail coordinates after empty includes;
`AwkDirectiveAdmissionTests` bounds allocation before refused preprocessing and verifies cleanup.

## Property Tests

Use property-style tests for broad invariants such as malformed-program robustness, no CLR exception leakage, generated expression arithmetic, and parser/runtime consistency over many small inputs. Keep generated cases small and deterministic.

## Current-Surface Tests

Current-surface tests are for internals that are hard to observe through a golden case: cancellation, injected clock/time-zone/random services, resource-limit thresholds, direct runtime services, and batching helpers. Prefer moving ordinary user-visible behavior to the golden corpus.

Record-input memory cases check that header preflight ends before later actions: clearing
a large value makes allowance available to a separate host user under shared pressure,
while same-execution replacement, mutation refusal and pending-user cleanup remain covered.
Budget-callback cancellation during main-input or getline admission preserves the prior
record, counters and terminator, and cleanup still closes the admitted input.

`AwkAdmissionRefusalTests` verifies command termination on any refused host admission,
without retry or fallback, and reuse by later commands after actual cleanup. Lower-level
memory tests inspect unchanged reservations and refunds after false; that primitive contract
does not authorize a running command to recover from refusal.

Scalar/array memory-mode checks cover numeric overwrites followed by refused growth,
equivalent retained keys, and OFS preparation that grows scalar storage or fails before
publication. References into scalar storage must be reacquired after preparation.

`AwkPathCancellationTests` bounds allocation after path admission requests cancellation,
covering redirected reads/writes and main input before resolution or host opens.

`AwkFunctionStorageTests` covers pre-admission of large argument/frame buffers, funded
frame/alias caches, setup cancellation and final refunds.
General-arity preparation checks cover cancellation through action/loop preparation and
reuse after the compilation token is canceled. Counter cases exercise 5, 9 and 65 arguments;
goldens cover caller order, aliases, spelling and omitted locals. Pending-argument host cases
preserve the ordinary path and keep storage funded through actual completion or cancellation.
Function-flow checks also cover nested returns and pending conditions/return values.
Long-block goldens cover actions, branches and for/while/do bodies at several sizes.
Artifact reuse and pending-argument cases after long loops check cancellation authority,
actual-user funding, refusal and cleanup independently of preparation eligibility.
Ordinary short/long function counters preserve numeric cells, bounded text allocation,
zero text conversions and refunds. The reference catalog includes a complete function
with local mutation, a loop and early returns, independently of prepared return shapes.
Single-consumption ValueTask sources verify completion effects are flushed before flow
propagates, without replaying earlier statements or running later ones after failure.
Parameter-context goldens preserve identity spelling, string truth/comparison, missing
values and array aliases. Field-index cases retain later getline and i++ effects; unused
numeric-conversion errors stay deferred through argument evaluation.
Compound-update goldens preserve numeric-string spelling across scalar, field and nested-array targets.
Pending record replacement checks retain prior values and target/key funding through completion,
cancellation and refused admission. Literal and regex separators retain their distinct case behavior.
`AwkPartialCloseTests` covers
public-boundary partial-close failures and cleanup without replaying successful closes.
`AwkEvaluationOwnershipTests` covers retained scalar/key/snapshot funding after source mutation,
nested-call refusal, and funding through actual completion of pending/canceled host users.
Nested-array goldens cover hierarchy, promotion, aliases, builtin operands and captured paths;
storage tests cover child admission, replacement and retirement. Evaluation/getline ownership
checks retain ancestor keys through mutation and actual host completion, including cancellation
and refused reconstruction. Counter checks cover owner reuse, byte-native aggregation and
execution-local owners when a prepared program is reused.
Immediate regex operands use stack-only byte views; dynamic RHS operands and capture searches
keep owned text. Canonical record cases preserve numeric-$0/CONVFMT, field-index effects,
Unicode/NUL, range subjects and getline mutation. Counter checks bound record/field
materialization across input chunks; suspending-index tests consume each operand once.
Record-value goldens preserve numeric-string origins and spelling across helpers, mutation,
getline and reconstruction. Record ownership checks cover persistent escapes, admission
refusal and pending/canceled helpers, including forced stack boundaries; setting checks
verify FS/FPAT do not retain unrelated old record buffers.
Direct record-output cases preserve captured separators, numeric formatting and owned
escapes, with complete-append refusal/cancellation and funding through pending host users.
`AwkMainInputOwnershipTests` covers sequential main files, main getline, nextfile/exit,
refusal after open and funding through pending/canceled closes. Partial-close tests also
suspend table retirement and logical flush, preserving every remaining stream and avoiding
successful-close replay.
`AwkStructuredCloseTests` covers structured close causes and disclosure, retained versus
retired failures across source/main/redirected/pipe inputs and output, primary failures,
failed stderr, suspended cancellation and actual command completion before refunds.
`AwkOutputOwnershipTests` covers nested print/printf argument admission with output digests,
and resolved output names surviving pending/canceled operands. `AwkFormattingOwnershipTests`
covers relinquished format text and reuse of AwkText's decode cache.
`AwkPrintfFormattingTests` guards character selection against whole-value decoding and
input-sized allocation, and string precision against whole-value decoding. The semantics
live in the golden corpus. `AwkBoundedSubstringTests` bounds short-result allocation/scanning
and preserves lazy length conversion, mutation refusal and refunds.
Literal-format goldens cover mixed directives, percent signs and scalar-aligned chunk edges;
focused formatting/admission/cancellation checks preserve funded capacity, prior output and
bounded cancellation without asserting fixed reservation or checkpoint counts.
`AwkRuntimeStartupTests` bounds default random setup and unused/UTC time-zone preparation;
it also bounds option preparation, terminal assignments, unconsumed assignment planning and
function-inventory text construction, with early text validation and decoding cancellation checks.
Calendar and text-cancellation tests cover lazy date materialization and local DST behavior.
`AwkCommandForwardingTests` bounds metadata copying during budget forwarding while preserving
caller snapshots, command context and the active budget; pipe/cooperative tests cover pending users.
Public-boundary tests cover malformed host strings and authorization of actual source-search
candidates. Source-search semantics live in the golden corpus; pipe tests cover empty-name
`ENVIRON` export. Condition completion tests exercise expression truth/short-circuit behavior
directly, including suspended and failed operands.
`AwkGetlineOwnershipTests` covers source/key captures across destination evaluation and
input phases, pending users, cancellation, refusal and refunds. `AwkCaseMemoryTests` covers
Unicode conversion preflights and byte-native mixed-text casing; `AwkGensubAllocationTests`
bounds sparse replacement instruction storage.
`AwkStdinBorrowingTests` covers independent stdin streams with reusable host buffers,
admission/cancellation lifetimes and the ordinary borrowed newline path.
`AwkRecordCursorContextTests` runs canonical record cases with stdin, files and pipes,
different host batch sizes, shared admission and the example provider. A focused counter
check guards against copying or decoding every shrinking unread tail.
`AwkRecordInputMemoryTests` covers provider admission refusal before record effects, borrowed
UTF-8 view invalidation and pending/canceled output lifetimes.
The concrete provider is the published NuGet package, compiled with the source in `examples/`.
Canonical AWK assertions stay unchanged where that provider has conformance gaps.

`AwkFieldCapacityTests` covers retained descriptor capacity, geometric growth, `NF` shrink,
mutation refusal/cancellation and refunds using a shared host budget.
`AwkIndexedFieldCacheTests` covers sparse late reads, prefix completion, defaults, retained
storage and escaped snapshots. Canonical field-count goldens cover out-of-order reads followed
by truncation/growth; memory transition tests preserve pending users during immediate charges.
`AwkRecordOwnershipTests` covers dirty-record separator funding, unused retention, refusal
and pending/canceled output lifetimes.

`AwkArrayEstimateTests` covers representable/overflow totals with lazy text and verifies
array replacement preflights and refunds without constructing enormous dictionaries.

`AwkBorrowedIndexTests` covers short-field search without retained input, byte/scalar
positions, case rules, fallback boundaries, admission/refunds, cancellation.

`AwkBorrowedSubstringTests` covers short-field substring ownership, ordinary fallbacks,
UTF-8/byte slicing, cancellation, mutation refusal and refunds.

`AwkConcatStagingTests` covers small/wide and suspended concatenations, operand ordering,
prefix lifetime during nested work, cancellation, refused result admission and refunds.
`AwkConcatAllocationTests` bounds argument-staging allocations after warmup.

Memory-mode tests distinguish ungoverned execution from a shared host budget using the
existing owned fixtures. Assert the selected mode and refunds; do not retain the removed
private-memory mode as a duplicate theory case. `AwkSourceCloseTests` exercises failed
source closes with hosts that retain the descriptor until cleanup, including cancellation.
`AwkFileRedirectionMemoryTests` covers long-path admission before host opens, retained logical
flush state, alias/reopen behavior and refunds after actual close completion.

Stream-identity goldens compare exact names independently of canonical host paths.
`AwkStreamIdentityBoundaryTests` covers governed execution, ordinal names, host authorization,
concurrent-resource bounds and supplied stdin aliases, including custom descriptors and pending
cancellation. Stdin-borrowing tests also cover explicit main `/dev/fd/0` with reusable buffers.

Operand-path goldens cover selection timing and literal filenames across input, source and
redirection paths. `AwkOperandPathBoundaryTests` preserves restricted original-operand authority,
root confinement, resolution refusal, cancellation and refunds. Argument-scheduling counters
keep original paths byte-native. Metadata tags describe test layers and behavior; they do not
certify oracle review. Native comparisons retain separate receipts through the opt-in workflow.

Computed-field goldens cover nonfinite references through reads, targets, array keys and
prepared calls. `AwkComputedFieldIndexTests` adds public execution and suspended-target state
checks. `AwkSourceCancellationTests` bounds post-cancel source copying and preserves pending
read ownership. `AwkVersionTests` compares brief output with the actual assembly metadata,
excluding build details.

## Benchmarks

`Lokad.Awk.Benchmarks` is not a correctness suite. Benchmark scripts should either reference canonical golden scenarios or document generated workloads, and benchmark smoke checks should not create independent behavior expectations. Use `AwkBenchmarkCounterTests` for deterministic counter invariants that belong in the correctness suite.

## Oracle Workflow

Use `AwkOracleCapture` for ad-hoc oracle reports from a local harness. The report records the oracle command, version, locale, time zone, per-case command line, environment, assumptions, and accepted divergence classification.

Oracle comparison remains opt-in so CI stays deterministic unless a developer explicitly wires an oracle executable through `LOKAD_AWK_ORACLE_EXE`.

`AwkBenchmarkHarnessTests` checks raw byte consumption, output bounds, paired statistics,
quiet-machine gates, and report claim qualification. It compile-links the pure helpers
from benchmark tooling without referencing BenchmarkDotNet or an external process.
`AwkBenchmarkInputTests` checks the shared performance host's borrowed immutable fixtures,
bounded bytes, independent descriptor positions, consumed-prefix commits and EOF. Mutable/reused host-buffer
contracts remain covered independently by runtime host/record tests.
The optional native driver's `--driver-smoke` integration checks require explicitly
selected GNU awk and stay outside ordinary test execution.

`AwkReferenceContractTests` checks the opt-in reference contract classifier against golden
programs/inputs/expectations, exact versioned byte signatures, invalid UTF-8, and rejection of
unexpected exit/stderr/output. `--reference-contracts` in benchmark tooling performs native
requalification separately; known reference bugs cannot become equivalent performance cases.

Use Release for full qualification, including allocation checks; Debug lowering does not
provide the same compiler allocation elision. CI builds and tests Release on both OSes.

Tests that force garbage collection or measure thread-local allocation should use
`[Collection(AwkAllocationMeasurementCollection.Name)]` so unrelated collections cannot interfere with
their measured loops. Keep their allocation ceilings unchanged when adding isolation.
The exact warm-call guards for printf provider boxing, bitwise argument reuse and retained
pipe EOF run on isolated threads with bounded BCL no-GC regions, ended before checking results.
They measure per-call allocation independently of collection and shared-pool maintenance;
semantic, cancellation and memory-lifetime coverage runs outside those regions.

## Review Gates

Array-update checks cover numeric slot reads, dynamic/suspending field selection, captured
keys/prior values through RHS mutation, and the existing nested-path/virtual-array behavior.
Numeric preparation does not replace missing-read, membership, refusal or cleanup assertions.

Every new awk feature, compatibility decision, or diagnostic needs a golden case unless the behavior is only observable at a narrower structural layer. Parser and runtime refactors should state which duplicate current-surface or end-to-end tests were removed, narrowed, or intentionally retained.

Numeric field checks cover bounded one-pass wide storage, parse reuse in spare record-cache
slots, warmed/full capacity, text origins and slot remapping. Cached-field infinite loops
remain cancellable with counters enabled or disabled; cold cache refusal stays fatal.

Whole-function preparation checks compare bound scalar blocks/branches/loops against
independently lowered ordinary functions. They preserve flow, short circuit, empty and
fallthrough returns, numeric-string origins, aliases, omitted/excess arguments, host
suspension and diagnostic guards. Long-function and cancellation checks distinguish
linear statement count from recursive preparation depth. Function-local synchronous
returns remain isolated across nested calls and concurrent compiled-program reuse.

Array-parameter counters cover bound aliases across reads, membership, updates and nested
roots in both memory modes. A file-action golden keeps suspended caller aliases separate
from globals.
Numeric array counters cover stored-number reads through literal and borrowed field keys;
pending-index and refusal checks preserve owner resolution, prior values and refunds.
Array-function goldens cover missing-read insertion versus membership, numeric-string
origins and spelling, composite operand order and short circuit. These semantic expectations
apply equally to ordinary and prepared execution.

Record-input storage checks cover staged-buffer refusal/cancellation, bounded retention,
owned fallback for oversized input, actual pending output and complete refunds. Record
ownership goldens retain saved values, getline destinations, print operand order, captured
separators and dirty records across buffer reuse. Counters distinguish buffered records
from materialized AwkText; input result/value/field/cache layouts stay compact.
