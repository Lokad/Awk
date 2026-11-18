# Interpreter contracts

This is a maintenance guide, not an execution-history log or a second awk specification.
[Runtime](RUNTIME.md) defines the embedding contract; [compatibility](COMPATIBILITY.md)
defines qualified language boundaries. The tests linked below enforce these invariants.

## Execution and suspension

One prepared interpreter program runs main input, rule conditions/bodies and record
backedges. Explicit frames and continuations preserve caller ownership across calls,
nonlocal flow and real pending host work. Execution ownership can differ from the
currently visible binding scope during retirement or file-phase reentry.

Formatting, function storage and I/O read controls and accounting from their owning
runtime, keeping cancellation, admission and value interpretation in the same invocation.

Each invocation parses, prepares and executes its own program. Programs, token caches
and concatenation owners are not shared between executions. Concurrent invocations
remain independent; observational counters can be shared explicitly.

Synchronous views borrow actual program/storage/frame state and cannot survive suspension
or callbacks that replace their backing. Publish active ownership before operations that
can fail; unwind through the owner that acquired it. Pending operations retain their
arguments/output until actual completion, including when a host ignores cancellation.
Capture return values before flushing; do not reread mutable sources after callbacks.

Enforced by [main-input ownership](../tests/Lokad.Awk.Tests/AwkMainInputOwnershipTests.cs),
[return continuations](../tests/Lokad.Awk.Tests/AwkPreparedFunctionRegionTests.ReturnContinuations.cs)
and [function flow goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FunctionFlow.cs).

## Admission, cancellation and stack safety

Admission funds actual retained owners, temporary overlap and pending users. Preflight
mutations before publication; refusal fails without retry or partially replacing a
record, field, array, capture array or output payload. Capture source values before
admission callbacks and re-locate mutable destinations afterward. Retire all funding
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

Incoming function storage belongs to its frame, while arguments evaluate in caller scope
and source order. Known bindings may bypass name lookup only while visibility is valid.
Array arguments retain aliases and potential/untyped origins through later argument
effects; mixed observers must retain the complete value. Retirement clears active
references without invalidating captured values or pending users.

Enforced by [scalar cells](../tests/Lokad.Awk.Tests/AwkFunctionLocalSlotTests.cs),
[parameter consumption](../tests/Lokad.Awk.Tests/AwkParameterConsumptionTests.cs),
[alias identity](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ArrayIdentity.cs),
[field comparison](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FieldValueComparison.cs) and
[text cancellation](../tests/Lokad.Awk.Tests/AwkTextCancellationTests.cs).

## Records and fields

The record frame owns splitting, reference-free field descriptors, numeric caches and
owned escaping values. Capture immutable splitting configuration when admitting a record.
Record splitting, split() and patsplit() share the splitter's conversion/admission context;
FS and FPAT retain distinct warning histories.
Incremental splitting publishes only a successful tail; NF and mutation complete the
necessary scan. Raw arithmetic, text-only reads and full-value observations retain their
different classification and ownership requirements.

Current records may borrow host bytes. Promote retained slices before retiring that borrow;
escaping values own their spelling. Rebuilding/output prepares the full byte length and
admits destination storage before copying/publication. Field/cache identities survive
split completion and reconstruction; failed staging leaves earlier observations valid.

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

Indexed growth/promotion privately constructs admitted replacements, funding old/new
overlap before publication. Reusable locations carry current values, never stale numbers.
Sort snapshots own their keys/value observations; callbacks cannot mutate that snapshot.
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

Regex providers own matching, scalar boundaries, POSIX selection and engine admission.
Compilation reports an explicit status; only core's dedicated syntax signal denotes
AWK class-validation failure. Provider argument faults must not become pattern syntax.
Core translates AWK patterns and validates/publishes captures and replacements. Search
scopes borrow one complete immutable UTF-8 subject, using absolute byte coordinates.
Active calls/searches pin compiled handles through cache eviction. Unsupported patterns
fail clearly without a fallback engine; provider deferrals do not qualify missing semantics.

Enforced by [host traversal](../tests/Lokad.Awk.Tests/AwkHostContractTests.cs),
[streaming](../tests/Lokad.Awk.Tests/AwkHostStreamingTests.cs),
[opaque pipes](../tests/Lokad.Awk.Tests/AwkOpaquePipeTests.cs),
[provider ownership](../tests/Lokad.Awk.Tests/AwkRegexProviderOwnershipTests.cs) and
[provider semantics](../tests/Lokad.Awk.Tests/AwkRegexProviderSemanticsTests.cs).

Changes may replace mechanisms while preserving these contracts. Use the
[test guide](../tests/Lokad.Awk.Tests/README.md) for focused and full validation;
performance evidence must cover complete programs as well as targeted diagnostics.
