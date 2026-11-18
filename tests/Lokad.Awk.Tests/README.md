# Test layers

Run `dotnet test Lokad.Awk.slnx -c Release` with the SDK pinned in global.json.
CI builds/tests Release on Windows and Linux. Use the narrowest boundary that observes
the contract; the [architecture guide](../../docs/ARCHITECTURE.md) maps enduring invariants
to enforcing tests.

## Coverage map

| Behavior | Fixtures / entry point | What to assert |
| --- | --- | --- |
| Language and diagnostics | [AwkGoldenCorpus](AwkGoldenCorpus.cs), [AwkGoldenCorpusTests](AwkGoldenCorpusTests.cs) | Canonical stdout/stderr/status, file effects and unsupported forms. New ordinary behavior belongs here first. |
| Lexer/parser/binder | AwkLexerTests, AwkParserTests, AwkBinderTests, AwkGrammarBoundaryTests | Spans, bindings, diagnostic order and source acceptance; avoid a duplicate language corpus. |
| Source loading | [AwkSourceLoaderTests](AwkSourceLoaderTests.cs), AwkSourceDiagnosticMapTests, AwkSourceCloseTests | Include/search identity, mapped coordinates, cancellation and cleanup. |
| Stack safety | [AwkStackSafetyTests](AwkStackSafetyTests.cs) | Public execution on 256 KiB threads in isolated children: deep syntax/calls, pending hosts, failure, cancellation and refunds. |
| Execution and calls | AwkPreparedFunctionRegionTests.*, [AwkMainInputOwnershipTests](AwkMainInputOwnershipTests.cs), AwkFunctionFlowTests | Values/effects through calls, returns, file phases and nonlocal flow; restored owners after actual suspension. |
| Records and text | AwkIncrementalFieldTests, AwkRecordInputMemoryTests, AwkInputHandoffCancellationTests, AwkTextCancellationTests | Captured configuration/spelling, strict UTF-8, chunk boundaries, lazy fields, mutation and borrowed lifetimes. |
| Arrays and sorting | AwkIndexedArrayStorageTests, AwkNestedArrayStorageTests, AwkSortingResourceTests, AwkSortingContinuationTests | Origin/alias behavior, admission before mutation, detached targets, snapshot ordering and callbacks. |
| Host contracts | [AwkHostContractTests](AwkHostContractTests.cs), AwkHostStreamingTests, AwkOpaquePipeTests | Original path requests, authorization, reads/appends, opaque commands, close/flush and terminal failure. |
| Shared admission | AwkExecutionPolicyBudgetTests, AwkFrontendMemoryTests, AwkRuntimeMemoryTests | Refusal fails, existing state survives failed publication, pending users stay funded, final reservations are refunded. |
| Regex | AwkHostRegexTests, AwkRegexProviderSemanticsTests, AwkRegexProviderOwnershipTests, AwkUtf8RegexAdapterTests | Translation/capture boundaries, provider lifetimes, token forwarding and adapter integration; gaps below. |
| Mechanism evidence | [AwkBenchmarkCounterTests](AwkBenchmarkCounterTests.cs), focused allocation/layout tests | Actual conversions, field preparation, copying and transport. Retire assertions when their mechanism disappears. |
| Preparation boundaries | [AwkFusionBoundaryTests](AwkFusionBoundaryTests.cs) | Canonical results/effects under inert filler shapes and pending-output controls, without opcode/route expectations. |
| Native comparisons | AwkDifferentialTests, AwkDifferentialFilesTests, AwkDifferentialComparisonTests | Live reference stdout, declared output files, diagnostic class and status; explicit opt-in below. |

Goldens run through public command execution. Focused internal preparation/ownership
fixtures are not a public compilation API; their planned migration must preserve
independent contracts. Source-level checks cover additional parser/binder behavior.

## Writing and running checks

Harness policies record unexpected library failures. An assembly-level after-test
assertion fails even when command status/diagnostics otherwise match; throwing from
the production observer alone would be ignored. Host/provider callback faults remain
separate. `AwkInternalFailureTests` covers notification ordering and fault classification.

Select a class or method with `--filter FullyQualifiedName~AwkHostContractTests`, for
example. A run discovering zero tests is invalid. DisplayName filters do not reliably
select individual golden data rows; filter their actual entry methods instead.

Use `AwkTestHarness.AssertFriendlyAwkFailure` / `AssertNoClrExceptionLeak` for diagnostics.
Check visible prefixes, stopped suffixes, descriptor retirement and final refunds. Include
genuinely pending reads/appends and hosts that ignore cancellation when changing lifetimes.
Cancellation tests establish responsiveness, effect order and cleanup, not a fixed number
of polls. Failed admission must not partially publish an array, record, field or output.

Allocation/GC fixtures use `[Collection(AwkAllocationMeasurementCollection.Name)]`.
Keep existing warm ceilings when isolating tests. Thread-local/BCL allocation diagnostics
are not substitutes for semantic/ownership checks or quiet-machine throughput measurements.

## Regex provider qualification

Tests compile the copyable adapter against published Lokad.Utf8Regex 0.4.0. It lacks
required POSIX whole/subcapture selection. `AwkRegexProviderGaps` defers 22 goldens and
three focused results: 25 results total, with expectations
preserved. Other translation, scalar, host, cancellation and ownership tests remain active.
New failures must not silently expand the list.

Before adopting a producer, disable `AwkRegexProviderGaps.SkipReason` locally and run the
suite; remove only qualified entries. Green CI with deferrals is not full regex conformance.
The published adapter also misses an escaped-endpoint bracket range (`[\[-\]]` against
backslash); this is a separately reproduced producer issue, not another skipped test.
Core has no fallback matcher. Contract fixtures isolate core capture/storage validation
from provider selection. Published engine allocation coverage is coarse, not complete
shared admission. See the [runtime provider contract](../../docs/RUNTIME.md#regex-engines).

## Native comparisons and benchmarks

Set `LOKAD_AWK_ORACLE_EXE` to an absolute independently installed GNU awk executable
(5.3.2 is the qualified CSV reference), then run:

```powershell
dotnet test Lokad.Awk.slnx -c Release --filter FullyQualifiedName~AwkDifferential
```

Without the variable the native theory is visibly skipped; comparison/golden tests run.
The harness exercises public execution under ordinary and shared-memory policies. It
records core revision, native version/hash, arguments/input, C.UTF-8/UTC and both results.
It compares live stdout and declared output-file bytes, stderr class and exact declared
statuses. Goldens retain exact diagnostic expectations. Missing expected files fail even
when both implementations omit them. Each native invocation owns isolated fixture files;
source/argument spelling stays unchanged.

Curated script/stdin idioms, file cases and generated boundary variants cover ordinary
processing and parsing. Custom environments, injected read failures and permission
differences require explicit fixtures. External suites are local review inputs; re-author
confirmed independent findings as goldens instead of importing foreign suites or normalizing
away unexplained differences. Reference bugs, unsupported extensions and provider gaps
must stay distinct from core regressions.

Benchmarks are opt-in diagnostics, not another correctness suite. The shared process
runner uses argument lists, bounded capture, streamed input, cancellation and process-tree
cleanup. `--reference-contracts` checks equivalent native work; `--driver-smoke` checks the
process boundary. Known reference bugs cannot be equivalent performance cases. See the
[benchmark workflow](../../docs/BENCHMARKS.md) for focused development and milestone refreshes.
