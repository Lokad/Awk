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
  binding locals. Field-demand preparation, whole string/length/type operations, general builtin
  evaluation and main-record scheduling retain their deliberate call boundaries; qualify changes
  to these boundaries on complete workloads.
  The view owns no storage and cannot survive an actual host suspension.
  Known string primitives bind to their semantic interpreter operations during preparation.
  Unary casing names its canonical operand directly; only multiargument string operations
  need call descriptors. Field-demand preparation uses the same operand location.
  Substring indices retain captured readonly values and existing pure-number facts. Keep source
  formatting before conversion, and defer length conversion until the start is in range.
  Substring indices and builtin captures bind existing numeric registers directly. Other
  operands keep ordinary field/value capture; substring arity is separate from location encoding.
  After ordered captures and source formatting, substring storage/print/range kernels consume
  a byte view and optional field identity. Keep range promotion and whole-result ownership at
  the runtime owner; do not pass the comparison scalar carrier through these kernels.
  The semantic slice producer writes the consumer's view directly. It owns unit selection
  and the primitive poll; raw slicing retains its value-return boundary without range facades.
  Keep per-operation locals and shared value/byte kernels; text captures write into caller storage.
  Prepared text consumers share the existing byte view for reader and owned scalar text.
  Text captures use one24-byte stack-only snapshot of canonical scalar words or the actual
  array/manager/reader/text owner and range. Existing owners retain funding; snapshots cannot
  cross actual suspension. Preserve custom-memory access at capture, field identity, deferred
  numeric formatting and classification fallbacks. Comparisons keep their own scalar view;
  do not reconstruct that view merely to consume text or retain a mutable frame as its source.
  Borrow canonical operand metadata synchronously; keep mode-sensitive input, numeric formatting,
  array/untyped behavior and actual frame/scope fallback at their existing boundaries.
  Capture owning spelling before later effects, and keep long-input polls after the capture.
  Prepared string results use canonical text operands when an owning value is needed.
  Case/substring results bind canonical consumers in the existing instruction/operand storage;
  complemented print indices name output. Numeric text-result destinations count string units.
  Fresh scalar consumers retain their actual text slot through existing owner publication;
  used results keep their snapshots. Share the fresh-result proof with length/print and retire
  only unused storage. Scalar writes end field-demand preparation; print remains a host effect.
  Count while source/result funding is live and retire produced payload after use.
  A fresh final substring completes the shared print preparation synchronously with a
  stack-local final byte view. Remove its unused result slot/operand, fragment staging
  and separate print dispatch. Whole owned text retains its existing print owner;
  the actual output destination owns bytes before host suspension. Scalar and list
  printing share the range/output kernels. Preserve conversion order, source funding
  through admission, field-demand termination and the fused operation's host effects.
  Scalar consumers construct string values at use; never reclassify these as numeric strings.
  Print-list consumption transfers between distinct slots of that same payload owner, retires
  the source and keeps funding through the existing print/output user. Preserve general-value
  observations, skipped substring-length conversion and actual call/host continuations.
  Root record completion retains its loaded program/storage view. Reload it for actual context
  switches; preserve published pending-host locations and rule-scratch retirement.
- Complete synchronous array updates keep targets local and capture owned keys before retiring
  key scratch and evaluating the RHS. Only cross-instruction captures need retained target slots
  and rule cleanup. Fund ordinary key/cell holding with target capacity and larger live captures
  through the existing payload lease. Preserve admitted owners and useful dictionary cells across mutation/calls/suspension;
  completed captures may keep attached inline/ordinal locations in that funded capacity.
  Reuse validates the actual owner, key and attached cell and captures its current pure number;
  never reuse the earlier number. Rule/frame retirement clears retained references, and other
  key payloads keep ordinary completion refunds. Keep ordinary lookup/conversion on misses.
  never retain a dictionary entry reference across RHS evaluation.
  Array identities own either indexed storage or one prepared dictionary byte-lookup
  handle. Synchronous byte views follow the current owner; never capture a backing across RHS
  mutation or suspension or retain a duplicate dictionary reference. Bulk 1..N producers and
  ordinary canonical ordinal population share admitted insertion. Available indexed slots keep
  their backing and hold nullable owning values in pages. Growth retains earlier full pages;
  partial pages and reference tables are copied privately under admission before publication.
  Keep staged replacements out of the live table through post-admission checks, and fund
  actual page/table overlap and owner headers. Reads/enumeration project owning values.
  Ordinal reads and complete numeric writes carry pure numbers into the actual indexed owner
  without decimal-key transport. Dictionary, sparse, string and special-array cases retain their
  ordinary key/policy boundaries. Share actual growth and payload mutation; re-locate indexed
  slots after callbacks instead of retaining page references.
  Visible local bindings, string/strnum spelling, ordinary missing-read admission and scalar
  reading mode remain at their existing boundaries; never retain page references across callbacks.
  Ordinal targets keep owner/key/prior number and re-locate at commit; dictionary targets may keep their
  real stable cell. Promotion admits the new dictionary cells and old/new backing overlap before
  construction. Sparse/noncanonical insertion retains private dictionary promotion. Preserve aliases,
  independent clones, parentage and atomic refusal/cancellation. Enumeration bounds hole scans;
  cleanup estimates and retirement remain noncancellable after their preflight.
  Plain field/composite assignments consume located values and optional dictionary cells directly; preserve captured RHS values,
  special-array behavior and owned-key admission on misses without repeating a successful lookup.
  Ordinary byte-key misses admit retained key construction and insertion together, without
  constructing a field scalar/cache just to transport key bytes. No RHS or host work may
  intervene. Reads, numeric consumers and update captures complete ordinary misses at that
  owner, creating an untyped origin after admission. Membership keeps its non-inserting path;
  absent and special arrays keep their promotion/policy boundary. Cold name binding still
  publishes only after success.
  Short byte keys own their bytes in the existing 16-byte key words; keep numeric ordinals
  distinct. Ordinals whose full decimal spelling fits those words prepare it once at creation;
  the origin flag survives packed storage while hashing/equality ignore it. Larger ordinals
  keep their numeric payload. Language/sort projections admit owned text when materialization is required.
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
  Consecutive key assembly runs through one synchronous computation kernel. Preserve its
  ordered primitives and return to the main driver for intervening computation, polls, calls
  and hosts. Interrupted segments resume without replaying earlier captures; consumers still
  own key-buffer retirement.
- Sort entries retain owning keys directly. Value sorts and traversal avoid unused key-text
  projection; repeated language-key consumers and asorti retain one admitted text projection.
  Copy snapshot keys directly at traversal consumption; keep publication transfer matched to ownership.
  Comparisons retain one owning entry snapshot and admitted integer ordering. User merges keep
  indices; final entry permutation stays private and cancellation-safe before publication.
  Builtin ordering binds its complete index comparison directly to the semantic kernel once.
  Kernels borrow entries by reference and consume the actual runtime/counters. Snapshot
  admission follows the actual entry layout. Descending
  order preserves operand/error order; numeric tie-formatting leases end before key fallback.
  Builtin snapshots freeze mutable untyped scalars and capture their array/scalar distinction.
  Type-order comparisons consume prepared scalar kinds and numeric payloads. Deferred conversion
  failures stay at comparison use, after array partitioning; do not force them for 0/1-entry sorts.
  The array bit is separate from numeric/error facts so scalar pairs share one check.
  User comparators retain original value origins and continuations. Preserve deferred errors,
  funded projections, mutation-independent snapshots and destination publication preflights.
- Preserve strict UTF-8 ingress. `AwkText` owns valid immutable UTF-8 bytes or concatenated text.
  Shared validation uses bounded BCL windows, completing a crossing scalar before its next poll.
  Invalid crossings still fail before that poll and before allocation/publication.
  Ready span access is constant-time and uses the surrounding computation/primitive polls.
  Long owned-record captures poll explicitly after saving their immutable source coordinates;
  concatenation materialization retains its own entry, waiting, copying and publication checks.
  It is an unboxed owning view. Scalar/slot object payloads retain its backing directly;
  restore that backing only after its value/cell role proves text. Non-text empty/charge
  projections stay at value boundaries; byte consumers retain their checked casts.
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
  The same cache owns raw spelling snapshots for repeated text captures and later full-value
  promotion. TextOnly may retain an arithmetic prefix without becoming a scalar or strict
  classification. Keep cached-number reads and allocating text capture at owner boundaries;
  raw arithmetic and shared argument staging need no owning-value preparation.
  NF reads reuse the frame's current value. Explicit assignments retain spelling/origin
  until a record or field-count transition resets it.
- The record frame owns its splitting and value-construction services. Keep input and recomposed
  value origins separate, captured plans immutable, and mutation separators evaluated at the
  language boundary. Conditional field copies resolve their separator only after validating slices.
  All record producers write into the frame's single funded record-cell buffer. Keep staging in frame
  cleanup through failure/cancellation; admit descriptor growth and escaped CSV text before allocation.
  The scoped producer carries its current staging lease; frame cleanup disposes it and rolls back
  unpublished cache entries. Do not duplicate that lease through an external byref handle.
  Incremental raw-slice scans write an admitted tail with local result counters, then publish
  the logical count after existing checks. Failed scans leave the earlier count intact;
  reference-free unpublished cells need no rollback. Keep the scoped growth lease. General
  writers share this buffer for producers that actually grow or retain text.
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
  Stored-text printing needs no formatting lease; owned numeric formatting retains its leases.
  General print can stage integer fragments in an admitted region builder. Preserve formatting
  order, complete-record preflights and payload slices; keep the buffer funded through actual
  redirected-write completion. Preserve original spelling and numeric formatting admission.
  Prepared print lists consume ready numeric registers directly. Keep those registers in the
  caller's existing scratch through later arguments; field/value fallbacks retain funded captures.
  General scalar and list printing share canonical descriptors and formatting ownership.
  One-part output avoids record-segment staging. A final bound source is
  captured before formatting; stored spelling and separators borrow their live owners only
  through synchronous copying. Fresh numeric formatting uses the existing payload destination.
  Earlier arguments keep their captures, and actual host suspension keeps the output owner.
  Only complete fixed-field lists may defer field reads into the synchronous print primitive.
- Arithmetic prefixes and strict input-classification facts share their funded, reference-free
  record cells; a prefix is not a comparison fact. Already-owned decoded spelling keeps its
  cache slot and classification through reconstruction and copying. Full value reads still own
  original spelling. Comparison views require a proven record-preserving RHS;
  dirty reconstruction and potentially invalidating operands retain owned capture.
  Comparisons and multiargument text calls share a stack-local scalar operand. Unary text
  consumers use byte views directly; earlier captures still own their spelling. Whole results
  and nonlocal scalar slicing reuse the existing field owner and index, not a detached-view cache.
  Preserve index's first-argument scalar promotion, formatting/conversion order and strict byte
  boundaries. Unicode casing's temporary owned source stays funded through compatibility conversion.
  Speculative numeric probes consume only established field facts without splitting,
  classification or admission. Ordinary evaluation keeps cold effects and diagnostics.
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
  Share append completion/status handling. Ready completion preserves awaited failures and
  post-host cancellation; pending users keep their payload and funding until actual completion.
  Flush boundaries share completion and field-demand handling. Successful ready completion needs
  no saved continuation; preserve pending/faulted/canceled completion through the existing awaited
  route. Qualify host-call view boundaries on complete workloads.
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
  Function scalars retain canonical values in those words, without a variable-slot projection
  or read conversion callback. Normalize reading mode at the frame owner; preserve input spelling,
  deferred conversion errors and long-input cancellation. Global and array storage share that
  canonical payload, prepared once in the actual owner's fixed numeric mode while retaining
  its backing directly. Bulk factories receive that mode; clones copy canonical slots while
  preserving independent array/untyped construction. Runtime mode reads reuse the owner;
  input text keeps classification at read. Ready values return without reconstruction. Preserve
  original spelling, mutation preflights and the same 24-byte footprint.
  Prepared full-value reads use the actual execution frame; standalone/dynamic reads keep visible-scope
  resolution. The cell captures payload words before polling and materializes the value at return.
  Disabled observation counters need no value-kind classification.
  Pure numbers use funded scalar storage.
  Arithmetic and complete comparisons may consume existing scalar numeric facts without changing
  spelling/origins. Compare numerically only when both probes succeed; otherwise capture the
  original left before RHS effects. Keys, arguments and owner writes retain strict kind checks.
  Numeric arguments initialize cleared incoming cells directly at that frame, once per index.
  Other captured scalars initialize those cells after payload admission without transient local cells.
  Copy source payloads before callbacks; no live source-slot references survive admission.
  Later numeric writes reuse only missing/numeric cells; other roles keep ordinary assignment.
  Private payload mutation preserves independent value/key projections and prior copies.
  Retirement clears active cells; inactive capacity was cleared at its previous retirement.
  Known owned keys arrive as keys and construct their immutable local cell at that owner;
  retained text keeps admission and post-admission cancellation before publication.
  Strings and aliases retain their existing capture, admission and pending-user lifetimes.
  One parameter-consumption graph uses the builtin's positional arithmetic/text contract.
  Prepared string and general builtin arguments use that same contract at early captures and
  final consumption. Text snapshots include record zero; assigned numbers retain formatting
  at actual use. Buffered-record materialization stays at its owner, outside cached reads.
  Origin observers, mixed uses and lexical comparisons retain full values. Key cells own bytes.
  Text-only field arguments may capture the immutable reader owner and absolute range in existing
  frame-cell words when prepared calls and later arguments exclude host work and staging has no
  pending output/diagnostics. Function-level effects stay separate from parameter consumption.
  Borrowed text retains the host's charge; frame cells retain their funded capacity. Full-value
  materialization admits an owning copy at actual use, caching its charge only in the original
  live cell. Preserve captured results and temporary funding if callbacks retire/reuse that cell.
  Field mutation cannot change that captured spelling. Owned/promoted/decoded fields and unproved
  calls keep owned ordinary text, without input-number classification. Mixed observations retain
  full input values, and numeric arguments keep formatting at actual use. Do not use key cells as
  general scalar text or retain raw record spans across a function call or host suspension.
  Reader arguments initialize their actual cleared incoming cells from captured borrow coordinates;
  frame preparation already funds their capacity. Keep materialization funding and source capture
  before callbacks; reader roles need no alias-index work or temporary general local cells.
  Comparisons against proved numeric results may stage established numeric field facts directly.
  Ordinary strings, deferred errors and observed spelling retain full capture; arithmetic prefix caches
  are not comparison facts. The use lattice shares one bounded-membership propagation worklist.
  Qualified synchronous builtin results use existing numeric registers. Result kinds are independent
  of argument consumption; preserve formatting at use and general-value/actual-host continuations.
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
  Bound-local length shares the visible value projection with scalar reads. Missing, potential and hidden
  locals retain ordinary promotion/name fallback; never bypass visibility with the saved caller frame.
  Ordinary global-array arguments reuse the existing execution binding, preserving their alias
  name and actual root owner. Missing/potential and special arrays keep their resolution paths.
  Forward actual alias names/owners into incoming cells without dropping a known owner. Keep
  activation's detached-child check and potential-origin promotion after later argument effects.
  Frame activation owns the call-entry cancellation poll. Functions carry initial admitted-field
  demand as immutable metadata, consumed after scratch initialization; keep cold admission and
  long initialization polls. Standalone internal function execution establishes its own entry poll.
  Return operands are captured before flushing; the return operation keeps its callee and
  storage until actual output completion. Pending flushes carry their prepared field demand.
  Return binds its source and owning destination together, without a separate value-capture
  dispatch. Actual pending completion retains the captured scalar in the existing pending-value
  field; never reread the source or replay return preparation after a host callback.
  Owning return capture establishes the scalar invariant; return dispatch consumes it without
  repeating scalar normalization. Keep validation at actual capture and mutation owners.
  Terminal scalar locals can transfer their existing claim into that owning capture. Read
  at the actual frame owner; only token polling may intervene before publication. Other
  local roles retain ordinary resolution and materialization.
  Non-number return operands name canonical owning value/text scratch. Move their existing
  payload claim to caller scratch before retiring the callee. Existing retirement clears
  the source; keep the handoff adjacent to that retirement rather than clearing slots twice.
  Immediate numeric consumers convert within the funded callee snapshot. Keep caller scratch
  through actual target admission and preserve scalar origins and overwritten-user refunds;
  do not restore a duplicate conversion lease or admit the same return handoff again.
  Discarded canonical text/value assignments and consumed scalar returns share actual
  local/global publication from their funded source operand. Keep ordinary Assign free
  of a consumption flag. Capture before callbacks; clear the source and publish its
  transferred funding before refunds. Used snapshots retain ordinary ownership; special,
  hidden, cold and invalid targets retain their own preflights. Successful fallback
  publication releases its source; failed publication leaves it funded for cleanup.
  Saved returns carry caller scratch indices and their prepared consumer meaning, without
  general operands or names. Adjacent bound-scalar assignment descriptors can be consumed
  after restoring caller scope. Used results and escaping text retain funded snapshots;
  discarded numeric results retire any old scratch payload without value transport.
  Discarded scalar returns keep their funded caller handoff through callee retirement,
  then use that shared scalar publication. Canonical text/value operands share retirement.
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
  Special-number probes consume only existing owner facts. Never complete splitting or admit in a
  speculative comparison probe; preserve assigned NF's actual scalar, including fractions and text.
  Ordinary string slots and computed string results use one runtime factory and defer numeric
  conversion until use, including callback/traversal keys and formatting results.
  Stored-text formatting stays on the short conversion path. Integer print fragments reuse the
  same integer emitter; owned print/string conversions share one numeric formatting tail.
  Preserve print-only scalar forcing, current OFMT/CONVFMT, numeric-string
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
