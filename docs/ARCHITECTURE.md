# Interpreter contracts

This is a maintenance guide, not an execution-history log or a second awk specification.
[Runtime](RUNTIME.md) defines the embedding contract; [compatibility](COMPATIBILITY.md)
defines qualified language boundaries. The tests linked below enforce these invariants.

## Execution and suspension

The interpreter driver acquires each main record before entering the prepared rules.
Falling through the last rule starts the next read with the same root storage.
Explicit frames and continuations preserve caller ownership across calls, nonlocal
flow and pending host work. A continuation carries the caller frame and resume
site. The caller's storage supplies its prepared region. A call from the root
resumes the current machine's root storage. Execution ownership can differ from
the currently visible binding scope during retirement or file-phase reentry.
One entry point owns synchronous cleanup and hands the machine to its continuation
when execution suspends.
Buffered `getline` can complete in the interpreter. A pending `getline` keeps
its source and destination in the execution frame until the read completes.

Formatting, including printf cursors and staged output, function storage and I/O read
controls and accounting from their owning runtime, keeping cancellation, admission and
value interpretation in the same invocation. Supplied destination leases retain their
publication funding.
Lowering can store admitted directive metadata for literal `printf` formats.
The synchronous renderer still consumes the arguments. Formats with runtime
controls or parser failures use the execution-time parser.

Each invocation parses, prepares and executes its own program. Programs, token caches
and concatenation owners are not shared between executions. Concurrent invocations
remain independent; observational counters can be shared explicitly.
Command parsing publishes source and input snapshots directly to the invocation.
Help, version and option errors retain their terminal outcome and standard descriptors.

Parser results carry syntax, errors and warnings. Binding validates function
declarations and builtin call shapes before lowering and returns a prepared program
or diagnostics. Preparation reports malformed constructs that reach lowering.
Generic top-level and statement grammar failures retain the failing source span.
Range endpoint grammar failures carry the rejected span to preparation, preserving
the precedence of lexical, action syntax and binding diagnostics.
The contextual lexer consumes ordinary descriptors as it advances through the
source and resolves quoted, slash and comment forms in context. Admission funds
the token buffer before scanning. The grammar wrappers preserve absolute source
offsets. The diagnostic mapper computes file, line and column when reporting.
The top-level and statement grammars carry token indices through balanced groups,
pattern boundaries and simple statements. Their consumers read slices of the
original tokens. Pattern expressions filter comments while retaining continuation
newlines. Expression and redirection consumers interpret separators.
Builtin calls use expression preparation in every position. A direct
one-argument `close` statement flushes before evaluating its argument.
Indexed subranges address the same token owner, including when sliced again.
Grammar-owned identifier tokens retain name validation for binding. Collection
retains diagnostic references in source order and records whether dynamic names
or record headers are observed. Collection, validation and lowering use the
same invocation-local function layouts and parameter slots.
Frontend syntax walks share source-order traversal and cancellation checks.
Symbol collection and validation choose which expression children to visit.
Statement validation supplies loop and switch context at body and arm entry.
Source assembly determines when a standalone inline program uses locationless
diagnostics. The executor maps other diagnostics to their source segments and
delivers parser warnings.
Structured diagnostics carry message text and an identifier. Rendering adds the
command prefix, source location and warning label. Terminal and runtime failure
paths write their finalized UTF-8 messages at the reporting boundary.

Synchronous views borrow actual program/storage/frame state and cannot survive suspension
or callbacks that replace their backing. Publish active ownership before operations that
can fail; unwind through the owner that acquired it. Pending operations retain their
arguments/output until actual completion, including when a host ignores cancellation.
Capture return values before flushing; do not reread mutable sources after callbacks.
Call preparation, argument groups and invocation share a call-table index.
Argument groups capture values before intervening effects. Field arguments for
numeric comparison use the prepared field's numeric fact or its original spelling.
Arithmetic can stage a known number directly into the incoming frame.
Check for a new pending operation after completing the current one. Forwarding a return
can start another append while the caller still owns its frame.

Enforced by [main-input ownership](../tests/Lokad.Awk.Tests/AwkMainInputOwnershipTests.cs),
[return continuations](../tests/Lokad.Awk.Tests/AwkPreparedFunctionRegionTests.ReturnContinuations.cs)
and [function flow goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FunctionFlow.cs).

During preparation, emit the consumer before selecting an adjacent producer for
fusion. The emitter only replaces a contiguous tail after the last poll. A poll
inserted between a producer and consumer keeps their operations separate. Boundary
sweeps check output, status, cleanup and admission against the same source semantics.
Opcode effects describe host exposure, the field preparation window and array-owner
reuse together. Each pass still checks operands and runtime-dependent effects where
needed, including user comparators and pending output.

## Admission, cancellation and stack safety

One live execution total covers storage held during preparation, publication and use.
Charge staged growth before allocation and refund it on failure. Preflight mutations
before publication. Refusal fails without retry or partially replacing a record, field,
array, capture array or output payload. Temporary construction peaks
and retained scratch use the same resizable lease. Compound mutations set an admitted
floor that their component charges can consume. Buffered field copies use the same
floor through cache publication. Capture source values before admission callbacks
and re-locate mutable destinations afterward. Retire all funding
and descriptors on success, failure and cancellation; cleanup must not abandon owners
because the execution token is canceled.

The optional fixed budget shares one root allowance through empty scopes. Subtree
aggregates refund without enumerating reservations; descendant operations reject disposed
ancestors. It does not replace a host's existing governor or introduce per-execution caps.

Input-dependent depth uses explicit continuations or concrete stack bounds. Computational
regions, backedges, calls, host boundaries and long primitives provide bounded cancellation
responsiveness. Admission/refund callbacks can cancel or reenter: preserve post-callback
checks before effects, rather than relying only on an earlier computation poll.

Enforced by [small-stack public execution](../tests/Lokad.Awk.Tests/AwkStackSafetyTests.cs),
[invocation admission](../tests/Lokad.Awk.Tests/AwkInvocationAdmissionTests.cs),
[scan cancellation](../tests/Lokad.Awk.Tests/AwkScanCancellationTests.cs) and
[prepared admission](../tests/Lokad.Awk.Tests/AwkPreparedFunctionRegionTests.Admission.cs).

## Values, text and calls

Scalars preserve number/string/numeric-string/unassigned origins, original spelling,
deferred conversion errors and immutable snapshots. Numeric facts cannot silently turn
arithmetic prefixes into strict numeric-string classification. Text owns immutable valid
UTF-8; borrowed byte views stay within their funded owner's synchronous lifetime.
Capture spelling before later effects. Formatting uses the language conversion point,
including the then-current CONVFMT/OFMT.
The runtime owns UTF-16 encoding, including sizing, destination admission and conversion
observations. Decoding is observed before cancellation-checked cache publication.
Text caches retain decoded strings, never invocation state. A borrowed casing input
uses admitted temporary strings and resolves record bytes after admission.
Builders fund and copy their bytes. The runtime observes the allocation when adopting
the owned result as text. Host byte ingress validates UTF-8 before copy admission.
Copies into already funded destinations use the caller's admission.
Text backing resolves rune ranges. The runtime funds and copies partial slices and
reuses the backing for whole slices. Byte slices must end up as valid UTF-8.
Adjacent text assignments transfer the producer's existing charge when the scalar
destination publishes. A bound destination admits any remaining growth. A discarded
field assignment can store a strictly classified number when all of the destination's
reads use arithmetic or numeric comparison. Observable spelling and unclassified
values keep the complete capture through assignment and its mutation checks.
Iteration snapshots retain compact keys. When `for-in` needs a new owning spelling,
mutation admission covers its construction and assignment even when it replaces text
of the same size. Sorted snapshots already fund their projected key text.
Concatenation starts in the runtime. A deferred pair keeps its parts, accounting
and allocation observations until it can publish a complete copy.

Incoming function storage belongs to its frame, while arguments evaluate in caller scope
and source order. Retirement clears a frame before linking it into the recycler.
Incoming and active frames can return their funded storage without allocating.
Activation reconciles aliases before publishing the callee's bindings.
Known bindings may bypass name lookup only while visibility is valid.
Forwarding a local scalar reuses the caller's classified value in the same
invocation. The incoming frame admits the spelling it retains before publication.
Field arguments and bound global input arguments reuse the input classification
produced by their storage read in that mode.
Text-only arguments can retain a record slot and range while call preparation and the
callee cannot advance input. The slot holds funded fallback bytes when admission retires
the host borrow. Text operations resolve the range after admission before copying.
Initialized special scalars use their bound cells for writes. Numeric and general
assignments enter the same format, separator and admission logic. Computed field
count reads and writes belong to the record frame.
Array arguments retain aliases and potential/untyped origins through later argument
effects; mixed observers must retain the complete value. Retirement clears active
references without invalidating captured values or pending users.
Alias repair uses the published slot bounds for aliases and untyped origins.
It polls while walking active frames, including frames with no eligible slots.

Enforced by [scalar cells](../tests/Lokad.Awk.Tests/AwkFunctionLocalSlotTests.cs),
[parameter consumption](../tests/Lokad.Awk.Tests/AwkParameterConsumptionTests.cs),
[alias identity](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ArrayIdentity.cs),
[field comparison](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FieldValueComparison.cs) and
[text cancellation](../tests/Lokad.Awk.Tests/AwkTextCancellationTests.cs).

## Records and fields

The record frame owns splitting, reference-free field descriptors, numeric caches and
owned escaping values. It reads conversion, admission and cancellation from its runtime;
returned text retains its own immutable backing. Capture immutable splitting configuration
when admitting a record.
Record splitting, CSV decoding, split() and patsplit() use the invocation's conversion,
regex and admission services. FS and FPAT retain distinct warning histories.
Split configuration assignments admit the scalar, any staged width list and
`PROCINFO` growth together before publication.
Incremental splitting publishes only a successful tail. When NF or mutation outgrows
the field buffer, the writer copies the published descriptors and continues from the
saved byte cursor. It keeps the current prefix available until the replacement is ready.
Raw arithmetic, text-only reads and full-value observations retain their
different classification and ownership requirements.
Owned field reads and arithmetic use the frame for both numbered fields and the
current record. Field zero reads the record without starting a field split.
Computed indexes are validated before use. Array keys, assignment and formatting
carry the validated index, with synchronous borrowing where the backing stays live.

Current records may borrow host bytes. Promote retained slices before retiring that borrow;
escaping values own their spelling. Rebuilding/output prepares the full byte length and
admits destination storage before copying/publication. Field/cache identities survive
split completion and reconstruction; failed staging leaves earlier observations valid.
Bounded CSV current records can normalize newlines directly in the funded staging slot.
Validation completes before publication. Reading into a scalar returns owned text.
Reconstruction stages numeric spellings in field order under the active `CONVFMT`.
After destination admission, it resolves the source bytes and rebases the field
descriptors during the synchronous copy.
Immediate print, field formatting, case conversion and owned record or field copies
carry slot coordinates through destination admission. They resolve the slot's current
bytes for the synchronous copy into the destination owner. Output is owned before
host suspension.
String operations also retain slot coordinates while other operands are prepared.
A search refreshes the first span if obtaining the second retires its borrow.
Field comparisons keep numeric classification separate from the captured spelling.
The spelling uses its record range when preparing the other field promotes that slot.
The frame can reuse one funded descriptor buffer to stage a similarly wide rebuild.
Much smaller records and frame reset release the spare storage.
Short copies share a cancellation interval across fields and separators. Long copies
poll by byte chunks, and field-count checks cover runs of empty cells.
The frame transfers the staged record's funding on publication and checks cancellation after
retiring the old payload. Field mutations publish cells and `NF` before refunds.
The frame retains the published slot identity. Its owner supplies the current length and
bytes, including when a custom memory accessor promotes its own borrow during a read.
A borrowed slot has funded fallback storage before publication. Governed input preflights
the record and headers together using the staged length and captured split plan.

Enforced by [incremental fields](../tests/Lokad.Awk.Tests/AwkIncrementalFieldTests.cs),
[input ownership](../tests/Lokad.Awk.Tests/AwkInputHandoffCancellationTests.cs),
[record admission](../tests/Lokad.Awk.Tests/AwkRecordInputMemoryTests.cs) and
[field allocation](../tests/Lokad.Awk.Tests/AwkRecordFieldAllocationTests.cs).

## Arrays and sorting

Key byte identity is independent of retained input origin. Reads may insert missing
untyped entries; membership does not. Complete updates capture owning keys and prior
values before RHS effects. Never keep a dictionary-entry or page reference across RHS
evaluation, suspension or admission callbacks; validate/re-locate the current logical
owner before commit. Detached nested owners and aliases retain their language behavior.
The runtime owns key text copying, admission and conversion observations. A compact
key can be constructed directly from a bounded byte span. Field-key captures, function
arguments and missing-entry insertions resolve their source after admission. If a callback
promotes the input borrow, they copy the current bytes with the original spelling.
The array runtime resolves named operands. Operations on an already resolved array
use the variable store directly. Numeric reads classify an ordinal once and pass
any captured location by value through binding. The store validates the current
owner and key before reusing a cached cell. Field and composite keys share borrowed update
preparation, with the evaluated key's origin retained through capture.
Staged and complete updates share target preparation. Updates that span instructions
use a funded capture slot and can reuse its location after checking the key and owner.
Complete updates capture and commit within one instruction and retire composite key
scratch after capture. Both forms read the current value before RHS effects.
The compiler selects the complete numeric update instruction when it emits a
fused nested update. Owner planning reads both update forms through the same
prepared site.
Commit reuses an attached cell or locates the captured key in the current owner.
Indexed publication carries the ordinal and checks it in the current backing.

Indexed growth/promotion privately constructs admitted replacements, funding old/new
overlap before publication. Reusable locations carry current values, never stale numbers.
Split and patsplit reuse the measured indexed field size at publication. Separator
captures use the dictionary's actual capacity at commit. Named and resolved
single-array destinations share the replacement and refund path.
CSV split counts fields and measures transient storage in the same scan before
filling the admitted list.
Sort selectors share one mode across parsing, projection and comparison. Builtin
selectors are read as UTF-8, with decoding for function lookup and diagnostics.
Sort snapshots own their keys/value observations; callbacks cannot mutate that snapshot.
The sort lease holds the destination pages and any child copies during construction.
Publication uses the measured stored values and transfers funding to the destination.
Destination publication is preflighted, including after a suspending user comparator.

Enforced by [key origins](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ArrayKeyOrigins.cs),
[indexed storage](../tests/Lokad.Awk.Tests/AwkIndexedArrayStorageTests.cs),
[nested storage](../tests/Lokad.Awk.Tests/AwkNestedArrayStorageTests.cs),
[sort continuations](../tests/Lokad.Awk.Tests/AwkSortingContinuationTests.cs) and
[sort resources](../tests/Lokad.Awk.Tests/AwkSortingResourceTests.cs).

## Host and regex boundaries

Tag faults only at actual application callbacks. Expected AWK/refusal signals and
cancellation keep their contracts; unexpected library faults reach the policy observer
once before a generic internal diagnostic. Observer failures do not interrupt cleanup.

The host resolves and authorizes original path requests before effects. Command text is
opaque, independent of path spelling. Read borrows must be consumed/disposed before
another descriptor operation; appends retain bytes through actual completion. Terminal
failures never replay payloads or diagnostics. Standard descriptors remain borrowed.
Source loading and runtime I/O take their observer and diagnostic policy from the
invocation options. Descriptor readers observe received bytes before checking for
cancellation. Cleanup observes close attempts and uses an independent token.
Source lookup continues after a missing candidate. A hard read failure retains its
diagnostic through include expansion and wins over a later close failure. Resolved
script text can be shared while each request keeps the selected candidate's spelling.
The main-input executor holds the action phase through the record loop and any pending
host operation. File actions enter their own phases and restore the enclosing phase.
The main and redirected stdin cursors preserve each other's unread tails before a host
read on their shared descriptor. Each cursor can consume its own buffered records
without transferring the other cursor's borrow.
Main input and redirections share the host adapter for opening and closing files.
An owned main-input close checks cancellation before starting and awaits
retirement with an independent token.

Regex providers own matching, scalar boundaries, POSIX selection and engine admission.
Compilation reports an explicit status; only core's dedicated syntax signal denotes
AWK class-validation failure. Provider argument faults must not become pattern syntax.
Core translates AWK patterns and validates/publishes captures and replacements. Search
scopes borrow one complete immutable UTF-8 subject, using absolute byte coordinates.
The regex runtime funds and observes owned capture copies through the same invocation
ledger used for the search. Captures keep their own lease through publication.
Active calls/searches pin compiled handles through cache eviction. Unsupported patterns
fail clearly without a fallback engine; provider deferrals do not qualify missing semantics.
Provider entry checks the invocation token after subject materialization and capture
storage admission.

Enforced by [host traversal](../tests/Lokad.Awk.Tests/AwkHostContractTests.cs),
[streaming](../tests/Lokad.Awk.Tests/AwkHostStreamingTests.cs),
[opaque pipes](../tests/Lokad.Awk.Tests/AwkOpaquePipeTests.cs),
[provider ownership](../tests/Lokad.Awk.Tests/AwkRegexProviderOwnershipTests.cs) and
[provider semantics](../tests/Lokad.Awk.Tests/AwkRegexProviderSemanticsTests.cs).

Changes may replace mechanisms while preserving these contracts. Use the
[test guide](../tests/Lokad.Awk.Tests/README.md) for focused and full validation;
performance evidence must cover complete programs as well as targeted diagnostics.
