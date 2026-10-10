# Changelog

## Unreleased

- Recoverable getline open/read failures now return -1 and set GNU ERRNO without
  writing a runtime stderr diagnostic or consuming output budget. Record, RT and
  destination values are preserved after destination resolution. Fatal main-input,
  resource and host-policy failures, and command stderr, keep their existing behavior.

- Bare file-redirection and command-pipe getline now work in expression positions,
  including status assignments, comparisons, arithmetic and control conditions.
  File source expressions stop before following concatenation/comparison/logical
  operators; optional targets use variable, array and field syntax. Pipe commands
  remain host-mediated.

- Direct postfix field mutations used as getline destinations now receive a specific
  diagnostic before execution. Use $(i++) or $(i--) for a destination index mutation.
  GNU's accidental direct-field form can discard records or abort on numeric fields;
  those reference bugs are excluded from the compatibility target.

- Getline now resolves its source and then its destination before opening or reading input.
  Destination index side effects run once on successful reads, EOF and host read failures;
  missing array elements and fields are admitted without assigning a record at EOF.
  Destination changes to RS affect the upcoming read, including main input and pipes.

- Paragraph reads that discard only leading blank lines now carry an explicit EOF
  directive to clear GNU RT without admitting a record. Main input applies it before
  ENDFILE; empty/repeated EOF and read failures preserve RT. File admission preserves
  the current record unless BEGINFILE actions require the established empty frame.

- Mixed regex/expression range endpoints now lower in either order, with default or
  explicit actions. Paragraph EOF preserves the final consumed newline in GNU RT.
  Multibyte literal FS no longer adds implicit LF field boundaries in paragraph mode;
  single-byte separators retain that rule. Lazy field plans, UTF-8 scalar boundaries,
  and preflight before owned record/terminator allocation are preserved.

- Regex escape translation now follows GNU/POSIX/traditional profiles for literal and
  dynamic patterns. Unknown escapes denote ordinary characters; numeric escapes retain
  the selected profile's metacharacter rules. POSIX RS follows its first-byte scanner,
  POSIX mode overrides byte-unit options with a warning, and GNU IGNORECASE applies to
  non-ASCII single-scalar RS. Equivalent ASCII letter
  class atoms avoid incorrect UTF-8 adapter boolean case folding. Remaining warning,
  ERE grammar, blank-only paragraph EOF/RT and Unicode boundary gaps stay open.

- Compatibility profiles now govern lexical keywords, reserved names, callable binding,
  formal parameters and GNU special-variable effects. Disabled GNU names can be ordinary
  variables, arrays or user functions; local names retain their lexical classification.
  POSIX 2024 length(array) is supported. Non-GNU profiles reject match capture arrays and
  diagnose split separator arrays at execution without replacing their destinations.
  Remaining regex profile differences stay open.

- Published writable PROCINFO["FS"] metadata on actual splitter transitions, including
  array aliases and shared SYMTAB setters. Empty FPAT/FIELDWIDTHS select their modes;
  fixed-width syntax accepts final remainder designators and rejects malformed values
  before mutation. Short records no longer gain unreached fields. POSIX/traditional
  empty FS follows whole-record/paragraph behavior, while split's empty separator retains
  character splitting. Regex assignments validate immediately, and scalar/metadata/buffer
  growth preflights together before changing the active splitter.

- fflush now warns about unopened and input-only streams without assigning ERRNO.
  Numeric and input-numeric arguments follow normal/fatal lint rules; ordinary flush
  warnings remain nonfatal. Standalone close now preserves its failure ERRNO result.
  Flushing a file retains its logical opening and output-limit slot, supports repeated
  flush/close, and reopens for append. Mixed-direction lookups select the latest opening.
  Warning admission preflights memory/output, and stderr host exceptions become awk errors
  in both engines while releasing queued diagnostics.

- Normalized mktime calendar overflow with bounded byte-native Gregorian arithmetic,
  including year zero and negative years. UTC/fixed zones support GNU's tm-year range;
  dynamic zones use injected TimeZoneInfo rules, explicit DST fields and a deterministic
  later-occurrence ambiguity policy. Parsing checks shared work/cancellation limits,
  and ambiguous-offset buffers are memory-governed.

- Preserved untyped array elements and argument origins during typeof/isarray, with
  one-time index effects, shared initial typing, and scalar copies on assignment/return.
  Empty user-function returns now have unassigned type. Grouped variable arguments use
  scalar context, while bare arrays and array-valued SYMTAB arguments retain aliases.
  Untyped cells are memory-governed without widening the 24-byte value/slot layouts;
  failed reads and local assignments preflight before changing entries or origin types.

- Missing SYMTAB/FUNCTAB reads now fail with awk diagnostics while membership tests remain
  legal. SYMTAB includes declared globals before first use, excludes function parameters
  and itself, and preserves untyped globals during indirect reads. Existing governed
  dynamic SYMTAB writes and shared special-variable setters retain their explicit policy.
  Non-variable isarray arguments now evaluate their effects once.

- Made split separator arrays sparse: only actual separators are published, including
  empty separators between character fields. Numeric captures retain input classification;
  patsplit keeps its complete boundary captures. Direct `/ /` uses literal-space splitting.
  Both replacement arrays preflight actual counts and memory together, with indexed capture
  buffers and growth headroom governed during scanning.

- Deferred numeric field conversion until record reconstruction under the current CONVFMT.
  OFS writes preserve the prior separator and freeze dirty numeric records before replacement;
  direct and SYMTAB writes share this behavior. Reconstructed records retain string type.
  Conversion preflights complete record size and memory, releases temporary charges on failure,
  and checks the shared work/cancellation allowance. Field-buffer estimates now include
  value-type entries and headers.

- Enforced expression newline boundaries and non-associative relational comparisons,
  while preserving grouped results, match precedence, array membership, and assignment
  operands. Added valid rule-pattern and range continuations, rejected extra null statements
  before else, and kept explicit continuations from merging tokens or extending comments.
  Syntax diagnostics identify the offending token; POSIX mode rejects ternary newlines.

- Applied IGNORECASE to string comparisons, index, and predefined string sort comparisons,
  with original-byte sort tie-breaks and case-sensitive array keys. Folded scans operate
  on UTF-8 bytes with invariant scalar casing, preserve spelling, and check the shared
  work/cancellation allowance without folded buffers. POSIX/traditional modes disable
  case effects, including regexes; GNU Unicode edge differences have an explicit policy.

- Added `@val_type_asc`, `@val_type_desc`, and `@unsorted` for sorting and traversal.
  Default asort now groups and orders numeric values before strings; default asorti
  orders indices. Repaired descending tie-breaks, numeric-prefix index conversion,
  numeric-value text ties, and NaN ordering. Array replacements preflight resource
  failures, and unsorted traversal uses only a governed key snapshot.
- Preserved signed NaN in byte-native default scalar formatting across all profiles.
  Explicit string-value sorting also preserves GNU's nonfinite destination types.

- Added variadic `and`, `or`, and `xor`, unsigned bitwise results, and GNU 5.4 oversized
  shift behavior. Fractions truncate after negative-value checks; complete folds preserve
  exactly representable high bits. Native out-of-range casts have an explicit portable
  policy, and both engines retain one-time argument effects and shared work checkpoints.

- Made strtonum preserve numeric arguments and convert string prefixes with the correct
  raw-byte base selection. Added GNU 5.4 hex float conversion and POSIX C-style hex input,
  removed signed-long range limits, and retained conversion errors until numeric/type use.
  Hex floats round directly to binary64, including subnormals, with bounded byte-native state.

- Corrected NaN equality, ordering, and truth, and preserved signed special-number input.
  Numeric conversion now accepts all ASCII C whitespace without changing field splitting;
  executable string literals honor the invocation's numeric-conversion profile.
- Added friendly negative sqrt/log and finite exp range warnings, including discarded
  results. Queued diagnostics now preflight retained memory and buffer growth, with
  cleanup on failed host writes and sink disposal.

- Accepted recursive unary prefixes, including signed field indices, while preserving
  exponent and mutation binding. Unary plus now converts to a numeric value in both
  engines, retaining signed zero and participating in the shared execution budget.

- Honored integer formatting precision and zero-padding rules, and removed signed-long
  saturation from printf/sprintf. Large signed decimals preserve their full value;
  unsigned-range fallbacks use `%g` with bounded buffers and preflighted output sizes.
  Nonfinite formatting and documented version-sensitive edges follow the GNU 5.4 policy,
  with a working GNU 5.2 reference for POSIX nonfinite cases.
- Added hexadecimal float formatting, positional values and dynamic width/precision,
  and ignored `h/j/l/L/t/z` format modifiers. Runtime profile checks and friendly argument
  diagnostics apply to printf, sprintf, and custom numeric conversion formats.
- Accepted `**` and `**=` as aliases for `^` and `^=` in GNU/default and traditional
  modes, preserving precedence, right associativity, and one-time assignment targets.
  POSIX mode rejects the aliases before execution.
- Allowed `next` and `nextfile` in user functions, with runtime phase checks and complete
  nested-call unwinding. Functions now execute in rule patterns, and `exit` unwinds caller
  expressions. Sort comparators reject record/file/exit control with awk diagnostics.
- Accepted unparenthesized assignments in logical, comparison, and conditional operands,
  preserving full right-hand expressions, short-circuit effects, and lvalue restrictions.
- Added POSIX longest-match and subexpression selection for traditional EREs across regex
  consumers and both regex modes, including nullable and nested repetitions. A governed
  structured matcher preserves assertion context, offsets, counted compatibility routing,
  cache limits, capture preflight, cancellation, and one operation deadline.
- Preserved assigned NF values and types while resizing fields from their truncated numeric
  value. Unified updates, indirect access, field growth, and record resets in both engines,
  with governed text retention and atomic limit failures.
- Made main input follow live `ARGC`/`ARGV` edits, including added files, changed assignments,
  sparse entries, file actions, and plain `getline`. Preserved restricted path capabilities,
  descriptor cleanup, and byte-native original paths; operand scanning uses the common governor.
- Aligned the execution defaults with Lokad.Lython: 50 million shared work steps, 100,000 host
  calls, and 1,000 active function frames in the compatibility preset. Restricted execution keeps
  its 32-frame cap, 30-second deadline, capability restrictions, and memory/size limits.
- Unified interpreter and executable accounting, including regex searches, sort comparisons,
  repeated separator scans, and byte-oriented built-ins. Explicit secondary CPU caps remain
  supported; their defaults are unlimited under the common governor.
- Made nested function entry and unwind suspend safely, including argument evaluation, and
  aligned VM output flushes with interpreter statement boundaries for budget exhaustion.

## 0.2.0

- Expanded awk compatibility across multi-source programs, `print` and `printf`, scalar regexes,
  switch fallthrough, dynamic record separators, paragraph mode, numeric conversions, and function
  argument semantics.
- Reworked input and `getline` around one streaming, `RS`-aware record cursor that preserves buffered
  data and `RT` across separator changes, reports host failures, and reliably closes descriptors.
- Strengthened the public host boundary with typed exit, read, and pipe outcomes; validated invocation,
  path, and descriptor inputs; and a smaller `IAwkHost` contract.
- Added a public restricted execution policy that freezes source and main-input capabilities, hides
  the environment and host-error details, and applies lexical path roots, deterministic time,
  uniform host deadlines, and finite invocation, frontend, runtime, IO, regex, and memory budgets.
- Enforced `--sandbox` redirection denial, bounded hostile syntax, regex, and sort work, randomized
  associative-array hashing, isolated pipe environments, and converted path, timeout, and unexpected
  host failures to stable diagnostics.
- Made resource-limit failures preflight and remain atomic across arrays, fields, records, output,
  formatting, regex captures and replacements, source loading, transient arrays, and VM stack growth.
- Kept UTF-8 hot paths byte-native while fixing Unicode `printf`, culture-independent regex matching,
  conversion formatting, numeric precision, and byte-oriented replacement and sorting behavior.
- Reduced allocation and nonlinear work across parsing, includes, concatenation, field splitting,
  record recomposition, regex compilation and scans, function calls, and array sorting, with refreshed
  deterministic counter and benchmark evidence.
- Simplified and typed the frontend, interpreter, and executable engine contracts while keeping the
  interpreter as the default and executable mode opt-in.

## 0.1.0

- Initial standalone extraction target for Lokad.Awk.
