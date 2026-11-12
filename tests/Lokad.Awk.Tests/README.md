# Test layers

Use the narrowest boundary that observes the contract. Ordinary language behavior belongs in
`AwkGoldenCorpus`; host lifetime, admission and deterministic counters need focused fixtures.
Run `dotnet test Lokad.Awk.slnx -c Release`. CI builds and tests Release on Windows and Linux.

## Language and frontend

Goldens are the canonical stdout/stderr/status expectations, exercised through direct and
internally prepared execution. New semantics and diagnostics need a golden unless they are
observable only at an internal boundary. Assert exact diagnostics and no CLR exception leakage
with `AwkTestHarness.AssertFriendlyAwkFailure`.

Lexer/parser/binder tests cover spans, symbols, outlines and diagnostic order. They should not
be a second language corpus. Deep-source tests distinguish linear size from nesting depth and
preserve untaken-branch and effect/error order. Small deterministic property tests cover
malformed input, generated arithmetic and parser/runtime consistency.

Source-loading tests cover includes, AWKPATH, source identity, mapped coordinates and failures.
Compiled executions emit their own warnings: the harness must not append them independently.
Governed compiled artifacts retain their admission owner until their users finish.

## Execution, ownership and hosts

There is one production interpreter. Bind source for execution tests; do not rebuild a reference
evaluator or assert obsolete eligibility routes. Internal prepared programs are test/benchmark
facilities, not a public compilation API.

Use real immediate and genuinely pending host operations to cover continuation order, single
consumption, nested calls, file-phase reentry, nonlocal flow, failure and ignored cancellation.
Check the visible prefix, stopped suffix, frame restoration, descriptor cleanup and final refunds.
Cursor semantic tests may use `ReadRecordAsync`; completion and ownership tests use the production
`MoveNextAsync`/`TakeRecord(out ...)` boundary directly.
Structured read failures are recoverable `getline`; faulted host operations are redacted failures.
Mock file truncation occurs at open and appended data becomes visible after close.
Append tests cover completed faults, synchronous throws and cancellation during callbacks,
as well as pending stdout/warning borrows. Check stopped effects, cleanup and funding until
actual completion, including hosts that ignore cancellation.
Return-owner tests retain the funded callee through both ready and pending host completion,
including numeric/text captures and cancellation before caller mutation.
Scalar-publication tests exercise both canonical source kinds and actual local/global owners,
including tight admission, fallback failures and refund reentry after source retirement.
Owner-mode tests preserve spelling, input origins and deferred errors across scalar/array
writes; sort goldens also cover ordinary string keys and nondecimal input values.
Text projection also preserves untyped origins and charges, and rejects array scalars.
Final-string print tests cover direct copying into actual output, numeric-buffer growth,
refusal before publication and pending stdout/redirected payloads through cancellation.
Check source/output ownership and visible effects rather than retired fragment-cache mechanics.

Memory tests distinguish ordinary execution from a supplied shared budget. Refusal fails the
command without retry or fallback. Check admission before mutation and keep storage funded through
actual pending users; cancellation alone does not end their lifetime. Assert owner retirement and
final refunds, not exact aggregate bookkeeping deltas when other live owners can grow.

Focused field/value/array tests preserve numeric-string origins and original spelling, lazy
splitting/reconstruction, byte-native consumers and owned escapes. Captured updates retain their
key and prior value across RHS mutation, deletion, replacement and rehash. Never keep dictionary
entry references across mutation or suspension. Membership must not insert missing elements.
Repeated captured updates cover changing keys, cells, values and array storage. Hash counters
measure lookup reuse across field, literal, variable, alias, temporary and composite keys.
Key formatting and SUBSEP changes remain observable; pending RHS tests preserve prior numbers,
funded users and cancellation through key changes and array replacement.
Ordinal entries hold owning values directly; captured updates re-locate after RHS work,
while dictionary captures may retain real cells. Promotion admits new cells plus overlapping
backing. Cover partial-page and table growth, logical values and payload retirement
rather than ordinal object identity. Pending-host updates also cross page boundaries.
Numeric-index reads cover prepared source categories, spelling, scalar reading mode and missing
insertion; refusal/cancellation must precede insertion and later effects.
Ordinary byte-key assignments retain spelling without classifying/caching a field; insertion
preflights cover both key and value before copying or publishing either.
Key equality and hashes agree across inline, ordinal, owned and borrowed forms; long comparisons
and hashes retain cancellation coverage.
Direct record/cache tests use `AwkFieldTestContext` with real splitting services and provider
ownership. Preserve input versus recomposed-value factories and owner-specific refunds.
Call and sort/traversal tests exercise aliases, snapshots and captured payloads through actual
pending calls. Cancellation tests establish bounded responsiveness, effect order and cleanup,
not historical checkpoint counts. Public host tests retain capability/path/pipe/append contracts.
Reader-backed text arguments also use poisoned buffers and custom memory to verify original
spelling, retirement and admission before an actual owning copy. Borrow forwarding needs no
future-copy allowance; materialization refusal/cancellation keeps the reader cell unchanged.
Stored-text consumers preserve scalar origins, scope hiding and earlier captures; exercise
their restored frame after actual host suspension, including cancellation and final refunds.
Immediate case-length counters retain actual casing preparation/conversion and byte-native
consumption; goldens also observe assignments, nested producers, formatting and input origins.

`AwkRuntimeCounters` assertions measure actual byte conversion, field preparation, copies and
transport. Retire counters when their mechanism disappears. Tests that force GC or measure
thread-local allocation use `[Collection(AwkAllocationMeasurementCollection.Name)]`.
Keep warm allocation ceilings unchanged when isolating tests. A few exact BCL allocation guards
use isolated threads and bounded no-GC regions; semantics and lifetime coverage run separately.

## Regex provider qualification

Tests compile the example adapter against published Lokad.Utf8Regex 0.4.0. It lacks required
POSIX longest/subcapture selection: `AwkRegexProviderGaps` explicitly defers 22 golden cases in
both execution entries and three focused cases, 47 results total. Their original expectations
remain. Other regex, translation, scalar, host, cancellation, capture and ownership tests run.
New failures must not silently expand the list.

Before adopting a provider, disable `AwkRegexProviderGaps.SkipReason` locally and run the suite;
remove only resolved entries. Green CI with deferrals does not establish full regex conformance.
Core stays independent of a concrete provider. Contract fixtures isolate core storage and capture
validation from provider gaps. Adapter tests cover byte restarts, token forwarding, disposal,
selection and sparse group publication. Published 0.4.0 lacks shared engine admission; adapter
reservations are coarse, not full engine accounting.

## Benchmarks and oracle checks

Benchmarks are not another correctness suite. Reference canonical scenarios or describe generated
workloads. `AwkBenchmarkCounterTests` holds deterministic runtime invariants. Harness and input
tests cover byte consumption, quiet gates, claims, statistics and expected-output contracts.

Native comparisons remain opt-in through `LOKAD_AWK_ORACLE_EXE` or explicit `--awk`.
`AwkOracleCapture` records command/version/environment and accepted differences;
`--reference-contracts` requalifies native signatures separately. `--driver-smoke` checks the
external process boundary. Known reference bugs must not become equivalent performance cases.
See [benchmark workflow](../../docs/BENCHMARKS.md) for focused development and milestone refreshes.
