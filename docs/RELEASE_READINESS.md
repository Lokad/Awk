# Release Readiness Gate

The [0.3.0 core candidate qualification](RELEASE_QUALIFICATION_0.3.0.md) records the
current qualified source, exact package identity, consumer checks and pending release action.

Do not mark the UTF-8 runtime and VM migration acceptance criteria complete until a release note,
PR, or handoff record contains the evidence below.

## Required Commands

Run the complete test suite:

```powershell
dotnet test .\Lokad.Awk.slnx
```

Run the benchmark project when runtime, allocation, VM, regex, field splitting, record splitting,
or counter behavior changed. Use the full suite for release baselines, or a documented
`--filter`/`--job short` smoke subset when the change needs focused evidence:

```powershell
dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj
```

Generate the runtime-counter evidence table for fallback and byte-native claims:

```powershell
dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj -- --counter-report
```

## Evidence Checklist

- Tests: include the exact `dotnet test` command, result, date, and commit.
- Byte-native counters: include `StringDecodes`, `AwkTextAsStringCalls`,
  `AwkTextAsStringDecodeMisses`, `Utf8Encodes`, `ByteTextComparisons`, and formatting/string
  interop fallback counts for representative hot-path tests or benchmarks. Include
  `ConcatenationBytesCopied` when concatenation or formatting changed.
- Regex compatibility: include the tested regex engine mode, corpus status, unsupported cases,
  `RegexDotNetTextMatches`, `RegexPatternGapFallbacks`, `RegexStringAdapterMatches`,
  `RegexPolicyInputBytesInspected`, `RegexGroupsMaterialized`, byte-mode valid UTF-8 behavior,
  and any `RSTART`/`RLENGTH` offset policy evidence.
- Record streaming: include `RecordSeparatorResolutions`, `RecordSeparatorBytesScanned`,
  `RecordSeparatorTailBytesMoved`, and batched-host coverage when record separators, `getline`, or
  input cursor behavior changed. Dynamic `RS` evidence must cover unread buffered bytes without
  requiring the descriptor to reach EOF. Distinguish incremental separator plans from general
  regex plans that can rescan, and verify that rescans do not copy accumulated input snapshots.
  Include grouped punctuation byte-run plans in the incremental evidence when their recognition
  or continuation behavior changes.
- VM coverage: include execution engine mode, `VmInstructions`, `VmBasicBlockEntries`,
  `VmFallbackCalls`, `VmOpcodeKindsExecuted`, `VmBudgetChecks`, and `VmMaxStackDepth`.
- Executable default gate: keep `AwkExecutionEngineKind.Interpreter` as the default unless the
  same evidence record proves executable golden-corpus parity, fallback coldness or intentional
  fallback acceptance for hot paths, cancellation/resource-limit parity, benchmark deltas, and a
  rollback path for production users.
- Memory governor: include reserve/commit/release counts, governed peak live bytes, and tests or
  benchmarks that exercise arrays, builders, output, VM frames, and known allocation-heavy paths.
- Host containment: verify that the restricted preset freezes source and main-input capabilities,
  hides `ENVIRON` and structured host errors, bounds an uncooperative host operation, and applies one
  aggregate cleanup deadline. Confirm that timeout and host failures remain awk-shaped diagnostics.
- Benchmarks: include before/after median time, allocation, throughput, and counter deltas for the
  changed scenarios when a comparable baseline exists. For newly curated scenarios, record the
  current baseline and command so the next change has a comparison point. Checked-in benchmark
  snapshots must be refreshed intentionally, not as incidental build output.

## Blocking Conditions

Keep the release blocked when any of these are true:

- The golden corpus has new expected failures or untriaged compatibility changes.
- Executable mode is proposed as the default without the executable default-gate evidence listed
  above.
- A byte-native claim lacks counter evidence showing the fallback path is cold or intentionally
  measured.
- Regex or VM rollout docs claim behavior beyond the implementation and tests.
- Resource-limit failures leak CLR exceptions or mutate visible output/state contrary to tests.
- Benchmark output changed materially without a recorded reason and comparison.
