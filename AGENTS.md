# Agent Notes

## Scope

These notes apply to the whole repository.

## What This Project Is

Lokad.Awk is an embeddable awk runtime for .NET. The public surface is intentionally small:
`Awk.TryParse`, `Awk.ExecuteAsync`, `AwkCommandInvocation`, `IAwkHost`, descriptors, paths, and host
result types. All IO is host-mediated; the runtime must not grow direct filesystem, process, console,
or environment dependencies.

## Commands

- Use the stable SDK selected in `global.json` (10.0.401, patch roll-forward only).
- Restore/build/test: `dotnet test .\Lokad.Awk.slnx -c Release`
- Package build checks: `dotnet build .\src\Lokad.Awk\Lokad.Awk.csproj -c Release`
- Only `src/Lokad.Awk` produces a NuGet package. The application-side regex adapter
  is source under `examples/`; tests and benchmarks compile it against the published provider
  package and remain non-packable.
- Runtime-counter evidence: `dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj -- --counter-report`
- Full benchmarks are opt-in and local-environment dependent. Do not commit `BenchmarkDotNet.Artifacts/`.

## Architecture Rules

- The destination is one production interpreter. A compact prebound instruction representation
  may replace the current execution machinery. Temporary overlap requires explicit removal
  milestones in the local plan; progressively delete displaced trees, delegates and guards.
  Do not restore the abandoned backend as an alternative, add public engine-selection flags
  or VM policy/qualification machinery, or introduce dynamic IL. Existing preparation paths
  are useful foundations, not a requirement to retain their current representation.
- Main input, rule conditions/bodies and record backedges use one prepared program.
  Preserve rule-scratch retirement and actual pending-host continuations; do not restore
  a parallel per-rule scheduler or reset/rebind the main cursor on ordinary record progression.
- Complete synchronous array updates keep targets local and capture owned keys before retiring
  key scratch and evaluating the RHS. Only cross-instruction captures need retained target slots
  and rule cleanup. Preserve admitted owners and stable cells across mutation/calls/suspension;
  never retain a dictionary entry reference across RHS evaluation.
- Preserve strict UTF-8 ingress. `AwkText` represents valid UTF-8 bytes; hot paths should stay
  byte-native and cross to `string` only at documented compatibility boundaries.
- Persistent record cells contain slices, numeric caches and frame-cache indices without managed
  references. Owned spelling and scalar values live in the frame cache; detached byte views carry
  no record-cache state. Preserve mappings during split completion, mutation and reconstruction.
  Text-only decoded CSV entries must not invoke the input-value factory early.
- The record frame owns its splitting and value-construction services. Keep input and recomposed
  value origins separate, captured plans immutable, and mutation separators evaluated at the
  language boundary. Conditional field copies resolve their separator only after validating slices.
  All record producers write into the frame's funded record-cell destination. Keep staging in frame
  cleanup through failure/cancellation; admit descriptor growth and escaped CSV text before allocation.
  Extend prefixes within admitted capacity and complete splitting before taking field-mutation views.
  Scanners bound long work by bytes; owners keep admission and publication cancellation checks.
  Byte-backed CSV/character/width fields need no whole-record text copy. Regex retains its owned subject.
- Arithmetic field numbers live in their funded record cells. Separate classification entries
  may omit text; full value reads must still own original spelling. Comparison views require a proven record-preserving RHS;
  dirty reconstruction and potentially invalidating operands retain owned capture.
- The frame owns two record payload slots and their bounded funded fallback storage. A current
  record may borrow immutable host bytes; the reader promotes matching slots before ending that
  borrow, without allocation or user/cancellation callbacks. Only the owner rebinds the frame's
  cached view. Main-input staging uses the inactive slot; getline keeps owned input. Escaping
  values own text and raw spans stay synchronous. Preserve publication preflights and funding
  through actual users and cleanup.
- A crossing newline copies only the required prefix. Preserve the uncopied suffix through RS
  changes and transfer all read-ahead before releasing a stdin borrow. Invalid record-value
  caches need no dummy text value; genuine initialization and observed-value origins stay distinct.
- Internal record pulls return completion status. Take and clear cursor-owned metadata before
  publication, reentry or another read; pending host users retain their existing buffer ownership.
  Reuse the admitted split plan for publication. RT text keeps string origin with conversion on use;
  absent terminators are unassigned. Keep header preflights before observable changes.
- Keep unsupported host capabilities explicit. Host failures must become awk-shaped diagnostics,
  not leaked CLR exception text.
- Keep the host command callback singular and pipes streaming: `RunCommandAsync` returns one
  status, and `CreatePipeAsync` accepts only a cancellation token.
- Appends return `AwkAppendResult`, separately from command exit statuses. Only full acceptance
  leaves output writable; terminal closure/failure must never cause payload or diagnostic replay.
- Reads return bounded byte borrows (`AwkReadBuffer`). Advance an absolute consumed prefix and
  dispose before another operation on that descriptor. Preserve unread suffixes, terminal errors,
  UTF-8 chunk boundaries and funding through actual users; the host does not split lines.
- File opens pass `AwkPathRequest` unchanged to host traversal. Hosts enforce its accessible root
  before effects and return a resolved identity with successful opens. Do not restore core lexical
  authorization or path collapsing; preserve exact stream/display names independently of source identity.
- Regex providers own engine allocation admission and engine-specific protection. Keep the
  existing budget/token contract; do not reintroduce core regex quotas or parallel engine selectors.
  Providers own match selection and scalar matching; Awk owns AWK translation, validated capture
  publication, replacements and their ordinary storage. Do not add a core matching engine or fallback.
- Regex searches borrow one complete immutable UTF-8 subject until disposal and use byte-only
  restarts/captures. Keep compiled handles alive through active calls/searches, including eviction.
  Derive scalar language coordinates only when requested; do not restore core UTF-16 bookkeeping.
- Resource-limit failures must preflight before observable mutation: arrays, fields, records,
  output and capture arrays should not partially mutate on failure.
- Builtins consume contiguous caller scratch; user arguments bind into admitted incoming
  frames while evaluation remains in caller scope. Preserve argument order, live pending
  storage, scalar captures and current array aliases. Bound locals retain name resolution
  when local bindings are hidden or unavailable. Refusal fails the command without retry.
- Prepared function parameters bind locally and known globals bind directly. Root/file phases
  hide caller locals at phase entry; dynamic name APIs retain their own current-scope behavior.
- Implicit special-scalar bindings reference canonical execution-owned cells. Preserve computed
  NF, lazy/disabled names and function scope. Stored text can be consumed without reclassification;
  numeric values still use current formatting and untyped origins retain scalar promotion.
- Do not add package dependencies casually. Package references are allowlisted in
  `Directory.Build.targets` and versioned centrally in `Directory.Packages.props`.

## C# Style

- Target `net10.0`; nullable reference types and implicit usings are enabled.
- Prefer file-scoped namespaces, small sealed/internal types, readonly record structs for tiny value
  objects, and explicit result/status types over nullable control flow.
- Do not use the null-forgiving operator. Treat each temptation to write `!` as a design smell:
  model absence with a value object, result type, `Try...` shape, or clear precondition.
- Use `ArgumentNullException.ThrowIfNull` at public or shared boundaries, not defensive null plumbing
  throughout hot paths.
- Async runtime paths use `Task`/`ValueTask` and `ConfigureAwait(false)` where host behavior
  can suspend. Computation need not propagate asynchronous machinery: a replacement driver
  may use explicit continuations and synchronous regions while preserving real suspension.
- Cancellation tests should establish bounded responsiveness, effect ordering and cleanup.
  Region/backedge/call/host/long-primitive polls may replace redundant expression polls;
  preserve checks after admission callbacks and before observable effects. Do not add fuel.
- Avoid unnecessary allocations in record, field, regex, formatting and array paths. If a change
  deliberately crosses a string boundary, update or add counter evidence.

## Tests

- Add ordinary awk behavior to the golden corpus first. It is the canonical user-visible behavior
  suite.
- Use narrower tests for parser/binder shape, source loading, resource limits, counters,
  and host boundary behavior. See `tests/Lokad.Awk.Tests/README.md` before adding a new test layer.
- Diagnostics should assert exact stdout/stderr/exit behavior and no CLR exception leakage.
- For performance-sensitive runtime changes, add or refresh `AwkRuntimeCounters` assertions before
  relying on prose.

## Docs And Artifacts

- Keep README examples aligned with the actual public API. The package does not provide a default
  filesystem host; examples should make the `IAwkHost` boundary explicit.
- Document Lokad-specific integration, supported boundaries, known differences and qualification
  evidence. Link to awk references for ordinary language semantics instead of duplicating the manual.
- Update docs under `docs/` when changing UTF-8 policy, runtime limits, regex compatibility,
  execution behavior, benchmark workflow, or release readiness assumptions.
- Maintain `docs/BENCHMARKS.md` as the single current comparison, with at most three tables,
  elapsed-time ratios and managed allocations. Present a performance snapshot: replace superseded
  rows and keep optimization timelines, before/after experiments and qualification logs local.
  Use affected workloads and contrasting controls
  during development; refresh all families at declared milestones. Keep retained measurement
  revisions/dates explicit; keep raw/history evidence local. Preserve full correctness checks.
- Leave local planning notes untracked unless they are intended as public project documentation.
