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
  Available-record progression reports completion directly. Cursor and scheduler share their
  synchronous algorithm with thin awaitable adapters; only pending work populates the existing
  machine continuation. Preserve awaited failure behavior and prepare immediate newline eligibility once.
  Execution location uses direct code/storage/position fields. Saved caller frames describe
  execution ownership, which can differ from the binding stack during retirement and root reentry.
  Publish the activated frame before preparing callee scratch; its saved caller owns failed-entry
  unwinding. Synchronous computation uses one stack-only view binding the actual state,
  program, storage, frame and their existing instruction/operand/number arrays. Keep instruction
  position separate; do not restore a parallel captured helper environment or duplicate driver
  binding locals. Whole string/length/type operations and main-record scheduling retain their
  deliberate call boundaries; qualify changes to these boundaries on complete workloads.
  The view owns no storage and cannot survive an actual host suspension.
  Root record completion retains its loaded program/storage view. Reload it for actual context
  switches; preserve published pending-host locations and rule-scratch retirement.
- Complete synchronous array updates keep targets local and capture owned keys before retiring
  key scratch and evaluating the RHS. Only cross-instruction captures need retained target slots
  and rule cleanup. Fund ordinary key/cell holding with target capacity and larger live captures
  through the existing payload lease. Preserve admitted owners and stable cells across mutation/calls/suspension;
  never retain a dictionary entry reference across RHS evaluation.
  Array identities retain one prepared byte-lookup handle and derive the dictionary view from it.
  Prepare it at construction/replacement/detach; use only the current owner's handle synchronously.
  Never capture it across RHS mutation or suspension, or keep a duplicate dictionary reference.
  Plain field/composite assignments consume located cells directly; preserve captured RHS values,
  special-array behavior and owned-key admission on misses without repeating a successful lookup.
  Ordinary byte-key misses admit retained key construction and insertion together, without
  constructing a field scalar/cache just to transport key bytes. No RHS or host work may
  intervene. Cold name binding still publishes only after success.
  Short byte keys own their bytes in the existing 16-byte key words; keep numeric ordinals
  distinct. Language/sort projections admit owned text when materialization is required.
  Runtime comparers share randomized hashing for owned/borrowed bytes. Compact equality
  does not prepare mixed-representation buffers; raw key views stay synchronous. Short-byte
  runtime hashing shares the compact word encoding across owned, borrowed and ordinal projections.
  Complete nested reads/assignments/updates consume bound path components synchronously, preserving
  intermediate promotion before later keys. Only intervening key/RHS work retains a logical path;
  its dense scratch slot stays funded through actual users. Complete path metadata owns no scratch.
  Re-resolve detached logical owners before using a captured cell; keep old-value/key capture before RHS.
  Repeated nested owners share funded value scratch only within proven computation windows.
  Resolve at first use and clear at last use; calls, writes and control flow end the window.
  Empty statement boundaries retain the reference. Before an actual flush, retire it before
  any host callback; later consumers resolve the current prefix at use. Keep field-demand and
  actual completion ordering. Unbound keys retain their ordinary captured path.
  Complete updates publish postfix old-number results directly; adjacent result reads need no retained target.
  Prepared path slots retain funded owner/key capacity while retirement clears active keys and payload.
  Rebind root/binding/depth on reuse and preserve cached owners during slot-array growth. TakePath
  transfers its independent owner/lease; transferred or actually pending references are never reused.
- Reusable key scratch owns its captured separator directly. Retire that snapshot after
  assembly, before target admission; keep completed bytes until consumption. Nested or pending
  components retain their own scratch. Do not stage separators through general value slots.
- Preserve strict UTF-8 ingress. `AwkText` owns valid immutable UTF-8 bytes or concatenated text;
  it is an unboxed owning view. Scalar/slot object payloads retain its backing directly;
  shared mutable caches belong to that backing, and storage identity compares backing references.
  Select cached metadata before byte-array casts in common access; preserve default empty text,
  checked casts and the private invariant that compact backing owns short ASCII bytes.
  Borrowed record/regex bytes use their existing views and owners, not scalar-text wrappers.
  Preserve regex resolution, pinning, admission and subject-materialization order. Hot paths should stay
  byte-native and cross to `string` only at documented compatibility boundaries.
- Persistent record cells contain slices, numeric/scalar-length caches and frame-cache indices without managed
  references. Owned spelling and scalar values live in the frame cache; detached byte views carry
  no record-cache state. Preserve mappings during split completion, mutation and reconstruction.
  Text-only decoded CSV entries must not invoke the input-value factory early.
  NF reads reuse the frame's current value. Explicit assignments retain spelling/origin
  until a record or field-count transition resets it.
- The record frame owns its splitting and value-construction services. Keep input and recomposed
  value origins separate, captured plans immutable, and mutation separators evaluated at the
  language boundary. Conditional field copies resolve their separator only after validating slices.
  All record producers write into the frame's funded record-cell destination. Keep staging in frame
  cleanup through failure/cancellation; admit descriptor growth and escaped CSV text before allocation.
  The scoped producer carries its current staging lease; frame cleanup disposes it and rolls back
  unpublished cache entries. Do not duplicate that lease through an external byref handle.
  Incremental raw-slice scans consume the frame's admitted destination directly; frame cleanup
  retains their staging lease. General writers serve producers that actually grow or retain text.
  Extend prefixes within admitted capacity and complete splitting before taking field-mutation views.
  Prepared field demand requires already-available record/separator bytes and respects unknown
  effects, conditional fallthrough and actual flush completion; it must not move cold admission.
  Scanners bound long work by bytes; owners keep admission and publication cancellation checks.
  Byte-backed CSV/character/width fields need no whole-record text copy. Regex retains its owned subject.
  Record/print producers determine complete output byte lengths during preparation. The builder
  admits that length and copies once; keep size checks after argument formatting, and publish
  Length only after complete cancellation-safe copying. Do not restore a second field-sizing pass.
  Record output resolves cell bytes directly from the record/cache view, without constructing
  detached field-text values. Those spans stay within the synchronous owned-output copy.
  Reconstructed copies share cancellation intervals across short fragments; retain long-copy,
  overlap and empty-field metadata bounds, post-admission checks and final Length publication.
  Stored-text printing needs no formatting lease; numeric fields/terminators keep their existing
  leases through the synchronous copy. Preserve original spelling and numeric formatting admission.
- Arithmetic field numbers live in their funded record cells. Separate classification entries
  may omit text; full value reads must still own original spelling. Comparison views require a proven record-preserving RHS;
  dirty reconstruction and potentially invalidating operands retain owned capture.
- The frame owns two record payload slots and their bounded funded fallback storage. A current
  record may borrow immutable host bytes; the reader promotes matching slots before ending that
  borrow, without allocation or user/cancellation callbacks. Only the owner rebinds the frame's
  cached view. Main-input staging uses the inactive slot; getline keeps owned input. Escaping
  values own text and raw spans stay synchronous. Preserve publication preflights and funding
  through actual users and cleanup.
  Record slots retain the reader's positive ASCII proof with their bytes through promotion;
  unproved replacements and disposal clear it. Field lengths consume that canonical proof without
  another whole-record scan or a duplicate frame flag.
- A crossing newline copies only the required prefix. Preserve the uncopied suffix through RS
  changes and transfer all read-ahead before releasing a stdin borrow. Invalid record-value
  caches need no dummy text value; genuine initialization and observed-value origins stay distinct.
- Internal record pulls return completion status. Take and clear cursor-owned metadata before
  publication, reentry or another read; pending host users retain their existing buffer ownership.
  Capture into caller-owned result storage and forward it read-only during synchronous publication.
  Reuse the prepared split plan and record-memory estimate at publication. RT text keeps string origin with conversion on use;
  absent terminators are unassigned. Keep header preflights before observable changes.
  Unchanged implicit RT publication retains identical immutable text storage and its existing
  funding. Changed origins/backing and unfunded scalar baselines keep ordinary mutation admission.
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
  An owned mutation's admitted funding covers its components. Keep actual growth and nested/outer
  refunds at the provider boundary, and disposed-ancestry checks outside the admitted region.
- Builtins consume contiguous caller scratch; user arguments bind into admitted incoming
  frames while evaluation remains in caller scope. Preserve argument order, live pending
  storage, scalar captures and current array aliases. Bound locals retain name resolution
  when local bindings are hidden or unavailable. Refusal fails the command without retry.
  Arithmetic field arguments stage cached numbers directly; deferred errors and owned values
  keep ordinary capture. Builtin scratch retains its value consumer.
  Function cells share mutually exclusive scalar/key payload words and retain full array names.
  Storage views preserve owned references, raw bits and origins without classification or callbacks;
  only the cell role selects their interpretation. Fund slot arrays at their actual element width.
  Resolved values convert at the incoming-frame owner; pure numbers use funded scalar storage.
  Numeric arguments initialize cleared incoming cells directly at that frame, once per index.
  Other captured scalars initialize those cells after payload admission without transient local cells.
  Copy source payloads before callbacks; no live source-slot references survive admission.
  Later numeric writes reuse only missing/numeric cells; other roles keep ordinary assignment.
  Private payload mutation preserves independent value/key projections and prior copies.
  Retirement clears active cells; inactive capacity was cleared at its previous retirement.
  Known owned keys arrive as keys and construct their immutable local cell at that owner;
  retained text keeps admission and post-admission cancellation before publication.
  Strings and aliases retain their existing capture, admission and pending-user lifetimes.
  One parameter-consumption graph prepares arithmetic and key-only inputs. Key cells own bytes;
  writes, observers and general scalar scratch retain full values. Capture before later arguments;
  numeric keys still use formatting at their actual use, including after CONVFMT changes.
  Prepared calls bind source locations in canonical argument definitions.
  Source category, callee consumption and array eligibility stay independent prepared data;
  do not recombine them into cross-product execution tags.
  Preserve actual dynamic kind/name resolution; capture caller cells before admission callbacks.
  Adjacent staging uses one ordered segment
  and one incoming-frame lookup; earlier segments remain before intervening effects, and the
  final segment belongs to Call. Keep long-segment cancellation polls without per-argument dispatch.
  Adjacent frame preparation also belongs to Call,keeping initial cancellation/admission before
  final staging. Intervening argument work retains its registered incoming owner at the original
  boundary. Borrow immutable call and argument descriptors during synchronous preparation;
  local/global staging reads their source, name and consumption data at the actual consumer.
  Prepare field demand once per segment and consume it at the first static field after earlier
  arguments stage. Keep cold admission, unknown-effect boundaries and current-record revalidation.
  Activate the actual staged frame directly; sort callbacks also forward their prepared frame.
- Prepared function parameters bind locally and known globals bind directly. Root/file phases
  hide caller locals at phase entry; dynamic name APIs retain their own current-scope behavior.
  Ordinary global-array arguments reuse the existing execution binding, preserving their alias
  name and actual root owner. Missing/potential and special arrays keep their resolution paths.
  Forward actual alias names/owners into incoming cells without dropping a known owner. Keep
  activation's detached-child check and potential-origin promotion after later argument effects.
  Frame activation owns the call-entry cancellation poll. Functions carry initial admitted-field
  demand as immutable metadata, consumed after scratch initialization; keep cold admission and
  long initialization polls. Standalone internal function execution establishes its own entry poll.
  Return operands are captured before flushing; the return operation keeps its callee and
  storage until actual output completion. Pending flushes carry their prepared field demand.
  Saved returns carry caller scratch indices and their prepared consumer meaning, without
  general operands or names. Adjacent bound-scalar assignment descriptors can be consumed
  after restoring caller scope. Used results and escaping text retain funded snapshots;
  discarded numeric results retire any old scratch payload without value transport.
- Function locals use one contiguous funded cell array for every arity. Keep indexed access
  direct; do not restore separate inline cells, arity-based storage paths or their counters.
  Recycled frames retain sufficient funded capacity across arities. Cleared/new cells already
  represent missing arguments with an unassigned value; retire prior references before reuse,
  without another omitted-local initialization traversal. Reconcile actual mutable arguments
  before registering aliases.
  Sufficient recycled capacity bypasses allocation preparation; new/growing owners keep admission
  before construction. Incoming publication stays shared and unchanged slot backing needs no write.
  Active local/scratch bounds govern cleanup; backing capacity and pending users keep their funding.
- Implicit special-scalar bindings reference canonical execution-owned cells. Preserve computed
  NF, lazy/disabled names and function scope. Enabled scalar names cannot be function parameters;
  their cells and cached case sensitivity apply inside functions too. Disabled names and special-array
  aliases retain ordinary scope resolution. Stored text can be consumed without reclassification;
  numeric values still use current formatting and untyped origins retain scalar promotion.
  Ordinary string slots and computed string results use one runtime factory and defer numeric
  conversion until use, including callback/traversal keys and formatting results.
  Stored-text formatting stays on the short conversion path. Print/string conversion share one
  numeric formatting tail; preserve print-only scalar forcing, current OFMT/CONVFMT, numeric-string
  spelling, admission/lease ownership and formatting diagnostics. Qualify boundary changes with
  numeric-output and text workloads.
  Retain format-decode caches at the actual scalar owner using its existing cache allowance;
  preserve slot origins, cached numbers, earlier copies and charges without formatter history.
  Keep actual input classification and mode-sensitive numeric-string reads separate.
  Plain numbers retain no text reference. Keep immutable value/slot snapshots and use
  existing numeric owner commits; type transitions still own admission and payload retirement.
  Prepared operands carry category, bound location and immutable fallback name directly. Reuse
  compiler variable descriptors; do not restore a separate variable metadata table. Runtime
  value-kind checks and real name fallback remain.
  Literal definitions keep text and prepared keys together. All nonempty keys up to seven UTF-8
  bytes own their spelling in the existing compact words, independent of producing syntax.
  Preserve scalar literal text separately and admit text when projecting compact keys; do not
  restore selective capture conversion or another literal cache.
  Numeric update sites reference canonical operand entries and consume computed number registers
  directly. Other bound-number reads borrow the canonical operand; variable stores retain binding
  indices. Instruction/operand references stay synchronous in immutable program storage.
  Typed numeric reads can finish fresh adjacent arithmetic through their existing Right/Detail
  metadata. Preserve single-consumer proof, operand order, earlier captures across RHS work and
  Call's funded conversion. Plain register arithmetic and field-demand classification stay direct.
  Numeric returns consume a bound source or number register. Fold only a fresh adjacent producer;
  preparation retains its canonical source and verifies its instruction position. Returns terminate
  field-demand preparation. Capture before flushing and keep the callee/storage alive through actual
  completion; never reread after a host callback. Value returns retain their owned snapshots.
  Known numeric comparison inputs bind to existing number locations; stored values keep their
  runtime kind checks and literal text comparisons retain their formatting/parsing semantics.
  Fresh adjacent comparison branches consume the original boolean through their existing branch
  descriptor. Preserve intervening polls, fallthrough field demand and unordered-number behavior.
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
