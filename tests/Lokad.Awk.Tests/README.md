# Awk Test Layers

The awk suite is intentionally split by boundary. New coverage should start in the narrowest layer that observes the behavior without depending on unrelated plumbing.

## Golden Corpus

`AwkGoldenCorpus` is the canonical behavior and diagnostic suite for the awk language surface. Add cases here for accepted awk programs, runtime behavior, compatibility-mode differences, diagnostics, host-policy errors surfaced through awk, and stderr/stdout ordering that is visible to awk callers.

Failing golden cases must assert the awk exit code, exact stderr, expected stdout preservation, and no CLR exception leakage through `AwkTestHarness.AssertFriendlyAwkFailure`.

## End-to-End Host Tests

Use host-facing tests when the risk includes command quoting, pipelines, command substitution, descriptor wiring, working-directory behavior, source-file paths, or integration with other host tools. Do not duplicate plain language behavior here unless a host boundary is the point of the test.

## Lexer, Syntax, Parser, and Binder Tests

Use lexer, syntax parser, parser, and binder tests for internal structure: token classification, source spans, statement outlines, expression outlines, symbol discovery, parser acceptance, and future semantic diagnostics. These tests should not become a second behavior corpus for ordinary awk execution.

## Source Loader Tests

Use `AwkSourceLoaderTests` for source-unit assembly, include ordering, AWKPATH lookup, source segments, continuation normalization, and structured source-load failures. Shell-facing source-load diagnostics still need golden or end-to-end coverage.

## Property Tests

Use property-style tests for broad invariants such as malformed-program robustness, no CLR exception leakage, generated expression arithmetic, and parser/runtime consistency over many small inputs. Keep generated cases small and deterministic.

## Current-Surface Tests

Current-surface tests are for internals that are hard to observe through a golden case: cancellation, injected clock/time-zone/random services, resource-limit thresholds, direct runtime services, and batching helpers. Prefer moving ordinary user-visible behavior to the golden corpus.

## Benchmarks

`AwkBenchmark` is not a correctness suite. Benchmark scripts should either reference canonical golden scenarios or document generated workloads, and benchmark smoke checks should not create independent behavior expectations.

## Oracle Workflow

Use `AwkOracleCapture` for ad-hoc oracle reports from a local harness. The report records the oracle command, version, locale, time zone, per-case command line, environment, assumptions, and accepted divergence classification.

Oracle comparison remains opt-in so CI stays deterministic unless a developer explicitly wires an oracle executable through `LOKAD_AWK_ORACLE_EXE`.

## Review Gates

Every new awk feature, compatibility decision, or diagnostic needs a golden case unless the behavior is only observable at a narrower structural layer. Parser and runtime refactors should state which duplicate current-surface or end-to-end tests were removed, narrowed, or intentionally retained.
