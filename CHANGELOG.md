# Changelog

## Unreleased

- A borrowed `IMemoryBudget` can govern major runtime storage across overlapping
  invocations and nested host commands. Shared admission precedes mutation; owned
  scopes refund only after actual host users finish, including canceled/timed-out
  work. Existing numeric memory quotas remain optional. Frontend and persistent
  compiled-artifact ownership are still being integrated.

- Global scalar reads avoid repeated local/scalar classification while preserving
  function-local promotion, array diagnostics and lazy scalar memory admission.

- Lazy field splitting keeps the fresh per-record cache instead of creating a second
  empty dictionary, removing 80 allocated bytes per split record. Cache lifetime,
  field materialization and memory/cancellation boundaries are unchanged.

- Supplementary regex category metadata now scans scalars once, admits buffer growth
  incrementally and publishes one immutable table on success. Failed/canceled construction
  remains retryable; translated patterns and warm/cold memory envelopes are unchanged.

- Immediate memory charges share governor rollback bookkeeping. Staged allocation and
  publication keep their existing preflight, commit, cleanup and counter behavior.

- User functions now suspend only when CLR stack space is low, with independent entry
  and unwind checks. Raised syntax-depth budgets retain suspension at every call.
  Ordinary shallow calls avoid thread-pool scheduling; function-depth admission, explicit
  fuel, cancellation, argument evaluation and failure cleanup retain their contracts.

- Numeric policy budgets now share one immutable limits value with runtime execution.
  Public configuration methods, both presets, optional specialized quotas and diagnostics
  retain their existing behavior; argument/environment/host-error budgets remain host-owned.

- Field and `NF` access now reuse the record-splitting callback within an execution,
  removing per-read delegate allocations while preserving lazy splitting and record plans.

- Reference tooling now checks nullable record separators, scalar empty replacement,
  successive gensub captures and mixed alpha/literal classes separately. Exact GNU
  defect signatures retain raw byte evidence and cannot qualify equivalent-work timings.

- Runtime regex dialect translation now admits builder scratch and intermediate
  strings, retaining decoded and translated text through cache publication. Structured
  POSIX predicates use the same governed scalar preparation and cancellation checks;
  their expanded expressions contribute to cache weight. Wrapper caches also account
  for distinct retained ERE text. Matching and profile translation rules are unchanged.

- Literal regex preparation now admits its escaped result and conservative pooled
  scratch before calling the existing BCL escape API. Unchanged literals share their
  text; final results stay charged through cache publication. Regex warning-history
  searches, comparisons and dictionary copies now observe cancellation, preserving
  prior entries on canceled growth. Matching and warning-profile rules are unchanged.

- Finite-memory regex execution now preflights Awk's scalar translation builders,
  strings, range buffers and supplementary category metadata. Conservative scratch
  charges release on completion or failure; exact output stays charged through cache
  publication. Scalar-range sorting observes cancellation during comparisons and
  preserves cancellation tokens and Awk diagnostics. Matching rules are unchanged.

- POSIX `graph` and `print` regex classes now use invariant Unicode categories,
  matching non-ASCII letters, marks, symbols, format and private-use scalars.
  Print also includes Unicode space separators; controls, unassigned scalars and
  line/paragraph separators remain excluded. Scalar spans, public capture numbering,
  profile rules and byte-native class predicates retain their existing contracts.

- Numeric byte escapes now combine complete UTF-8 scalars in strings, options,
  consumed assignment operands and literal/dynamic regexes: `\303\251` denotes é.
  Octal values wrap to eight bits. Profile-specific hex/metacharacter rules and
  malformed-byte text fallback are preserved. Scalar lookahead adds no heap buffer;
  existing memory admission and cancellation boundaries remain in place.

- Named POSIX regex classes now reject unknown or unterminated names through Awk's
  invalid-regexp diagnostic in every profile. Literal, escaped and backend-comment
  contexts retain their meanings. Admission observes cancellation and admits/releases
  scoped comment-mode storage. `[:punct:]` now includes Unicode symbols as well as
  punctuation, fixing missing `$`, `+`, `|` and currency/mathematical symbol matches.

- Unknown regexp operators and escaped `8`/`9` now emit their profile-specific
  warnings once per escaped character across source literals and runtime patterns.
  Unreachable literals warn during compilation; typed source locations and history
  survive compiled reuse with fresh state for every execution. String warnings use
  separate history. Missing regex `\x` digits now warn through the existing numeric
  warning history. Rejected diagnostics leave escapes eligible for retry; regex
  matching engines and translations remain unchanged.

- Runtime regex width analysis now admits exact value-frame buffers, including old/new
  overlap during growth, and releases them on completion or failure. POSIX token
  replacement admits one exact result string, keeps its charge through preparation
  and cache publication, and removes the intermediate builder buffer. No-op and empty
  results share existing storage. Cancellation, shortfalls and invalid regexes release
  staging; matching behavior and logical execution charges remain unchanged.

- Awk-owned regex preparation now checks cancellation during dialect translation,
  scalar/width analysis, pattern/group limit scans and cache-weight measurement.
  Final checks precede all compiled-regex cache publications. Interrupted decoding
  releases scratch and can be retried; completed regexes and shared Unicode metadata
  retain no construction callback. Existing matching behavior and logical charges
  are unchanged. Native constructors and shared metadata initialization remain
  noninterruptible boundaries.

- Finite-memory execution now admits exact print/printf argument and segment arrays.
  Numeric print values and separators keep their formatting charge through append;
  printf results stay charged through pending redirected writes. Failure releases
  staging and preserves earlier output. Ordinary built-in argument buffers are also
  admitted across delayed evaluation. VM stacks use their already admitted exact
  capacity, while unlimited execution keeps pooling. Print staging and printf result
  copying observe cancellation without extra logical execution charges.

- String-value array sorting now admits newly formatted numeric text for the full
  staging lifetime, including custom CONVFMT results. Numeric tie comparisons release
  their formatting charge after each comparison. Existing text values stay shared;
  sorting nonfinite values transfers only its materialized destination text at commit.
  Failed admission preserves source and destination storage. Leased custom-format
  result copies observe cancellation without adding logical execution charges.

- Array snapshots and sorting now admit newly encoded SYMTAB keys and temporary
  ordinal-key text for their full lifetime. Ordinary text keys and unsorted ordinal
  traversal retain their shared-key contracts. Key sorting transfers temporary text
  into its destination charge, avoiding duplicate admission. Measurement, sorted-key
  copying and user-sort merge tails observe cancellation without adding logical fuel.

- SYMTAB inventory, key encoding and ordinary array snapshots now pass cancellation
  checkpoints through long scans. Standard-output staging uses the runtime checkpoint
  for large byte copies and preserves earlier output when interrupted. ASCII record
  separators reuse fixed shared terminator text instead of allocating per-plan arrays
  and one-byte values. Logical execution charges and memory admission are unchanged.

- Owned record-separator scans and classification now check cancellation in byte
  chunks, including byte sets/runs and paragraph leaders/continuations. Short literal
  searches keep separator overlap across windows; long needles retain the framework
  path. Interrupted scans preserve unread records and delivered batches. UTF-16 regex
  offset counting uses whole-scalar chunks and publishes its cache only after success.
  Logical scan/fuel charges and regex engines are unchanged.

- Ordinary unknown-string escapes now warn once across source literals, -v/-F and
  consumed assignment operands. Missing hex/Unicode digits and invalid Unicode values
  retain per-occurrence warnings. Typed history preserves the first warning's source
  location and resets for each compiled execution; duplicate warnings do not consume
  the frontend diagnostic budget. Unknown non-ASCII escapes render complete scalars.

- FS, enabled FPAT and regex RS assignments now prepare both case variants before
  publishing storage or parser metadata. Unicode warnings follow special-variable
  reassignment and fresh compiled execution; malformed regex RS fails at assignment.
  Same-value/case changes reuse prepared regexes, and byte-native separator fast
  paths remain available. Compilation quota failures preserve prior values/fields.

- POSIX/byte-mode option warnings now precede source-loading, parsing and external
  assignment failures and are retained once per compiled execution. They share the
  frontend diagnostic cap. Documented and covered optimization/interval aliases and
  invariant numeric-locale behavior across profiles and option spellings.

- Added a public language compatibility matrix with golden evidence and POSIX/GNU
  references. Documented the intentional main-input directory limitation: unhandled host
  failures remain fatal; existing BEGINFILE recovery and redirected getline are preserved.
  New GNU language features and host system() remain later-release work.

- Failed record reads now preserve buffered input: newline consumption and switching
  to buffered RS commit after admission/copying, delivered host batches survive failed
  appends, and paragraph leaders remain with their record through batch boundaries.
  Retrying does not repeat a delivered host read or its input-byte charge. Staged tail
  copies and batch appends use cooperative cancellation; failed staging and disposal
  release buffer charges. Execution defaults and logical work charges are unchanged.

- Finite execution-memory policies now admit gensub-part and heap capture-coordinate
  scratch before allocation and retain their leases while the buffers are live.
  Gensub counts actual parsed parts before allocating; long literals use one part.
  Governed scratch uses exact-sized arrays, while unbounded execution retains pooling.
  Capture scratch is released before returning the retained match. Cancellation,
  admission failure and failed substitution release temporary charges without changing
  match/field/output state. This adds no logical fuel or byte charges.

- Replacement classification, substitution/gensub scans, appends and result copies
  now propagate cooperative cancellation. Capture measurement, coordinate conversion
  and selected-group construction check before returning a completed match, and
  cancellation releases its lease and rented boundary storage. UTF-16 byte counting
  preserves surrogate pairs across chunks. Empty-match advancement and general regex
  record copies use the operation callback. No additional fuel/byte charge is added.

- Long field/NF expansion and truncation now stage field lists and retained value
  caches before publication. Cancellation during analysis/copying preserves old
  fields, NF and caches; temporary old/new buffer overlap is governed and released
  or transferred once. Smaller changes retain their bounded commit path. Finite
  memory policies must admit staging as well as final retained state.

- Field-width parsing, whitespace/literal/character separator scans, rune traversal,
  field copying and record recomposition now observe cooperative cancellation.
  Split payload and memory estimates complete before field-state publication;
  interrupted record or field copying releases transient memory and leaves caches
  available for a fresh retry. The whitespace separator probe avoids flattening
  long text. Field/NF mutation loops and other remaining callers are still under audit.

- Lazy text materialization, strict UTF-8 validation, byte hashing/comparison and
  runtime key/string callers now propagate cooperative cancellation. Interrupted
  copying releases live memory and retains complete parts for retry; text caches
  retain no callback. Formatting caches publish their key and decoded format only
  after success. Runtime array comparers are invocation-owned; the shared comparer
  remains callback-free. Framework decoding/randomized hashing/search still use
  boundary checks, and remaining field/record/replacement callers are under audit.

- Printf flag/digit parsing, Unicode width/precision traversal and floating-point
  postprocessing now propagate cooperative cancellation without extra fuel charges.
  Pending formatted output is discarded on cancellation and builder reservations are
  released. Numeric formatting/encoding/copy/padding framework calls remain documented
  one-shot boundaries with existing output and memory preflights.

- Grammar reductions, token preparation/navigation, source/include assembly, directives,
  continuation maps and diagnostic mapping now propagate cooperative cancellation.
  Interrupted frontend cache construction publishes no partial snapshot and can be retried;
  completed syntax retains no callback. Lokad.Parsing token matching/parser shifts and
  one-shot framework copies/sorting remain documented boundaries. Frontend budgets,
  public policy, explicit fuel enforcement and runtime counter admission are unchanged.

- Frontend binding, symbol/semantic passes, lowering and executable compilation now
  propagate cooperative cancellation. Owned lexer/literal/string-diagnostic scans,
  parameter/layout construction, newline metadata and nesting audits check internally;
  returned artifacts retain no caller callback. Parser dependency calls and one-shot
  framework construction are documented cancellation boundaries; frontend budgets and runtime
  fuel/counter admission remain unchanged.

- Runtime numeric scans, UTF-8 rune counting and sparse-index construction, ASCII case
  conversion and printf numeric arguments now reuse the execution's cancellation callback.
  Interrupted rune scans publish no partial cache and release index reservations.
  Logical byte admission and positive explicit fuel quotas retain their semantics.
  Runtime text materialization and regex translation/backend coverage remain under audit.

- Ordinary and restricted execution no longer have an implicit fuel cutoff, matching
  current lython defaults. MaxExecutionSteps reports zero when unset; existing positive
  WithMaxExecutionSteps(int) quotas remain enforced, including int.MaxValue. Cancellation,
  restricted deadlines, memory/structural limits and host controls remain. Disabled fuel,
  regex-call, sort-comparison and separator-scan quotas keep no admission totals; counters
  remain observational. Long builtin/backend cancellation coverage remains under audit.

- `-v` and `-F` now decode escapes under the final compatibility profile and apply
  assignments in their original mixed option order. Assignment operands decode when
  consumed, including updated ARGV entries; a BEGIN exit leaves them untouched.
  Unicode warnings precede the appropriate source/runtime effects and are retained
  by compiled artifacts. Option and source diagnostics share one frontend limit.
  Include filenames remain raw text; POSIX/traditional reject `@include` before IO.
  Unknown string-escape warning deduplication remains pending.

- Raw regex literals and runtime patterns now decode GNU 5.4 `\u` escapes before
  ERE interpretation in default/traditional profiles. Literal Unicode warnings retain
  source locations; runtime warnings follow each expression's pattern and case history
  independently of compiled-regex caching. Pending warnings survive later runtime
  errors. Warning history uses the memory governor. General unknown-ERE warnings and
  FS/FPAT/RS assignment-warning parity remain pending.

- Source string literals now support GNU 5.4 `\u` escapes with one to eight hexadecimal
  digits in default/traditional profiles. Supplementary characters remain valid UTF-8;
  invalid scalars become `?` with a source warning. POSIX strings treat `\u` and `\x`
  as plain `u` and `x` without GNU escape warnings. Missing Unicode digits keep `u`
  and warn; uppercase `\U` remains an unknown escape.

- Regex translation now treats unmatched closing parentheses as literals even when
  the pattern has no backslashes. Unchanged patterns retain their original string;
  bracket and balanced-group behavior is preserved.

- Precompiled interpreter and executable runs now emit their frontend warnings before
  execution, once per run, with source locations preserved. Warning appends share host
  and output admission, cancellation and deadlines with the execution. The golden
  harness no longer supplies a separate warning append that bypasses those limits.

- Input pipes now retain blank-only paragraph bytes at EOF, matching GNU's repeated
  RT clearing and allowing a later RS change to read those bytes. Prefix scans are
  cached across batches and repeated EOF. Retained capacity stays under the memory
  governor and is released on close; regular-file and main-input EOF behavior is preserved.

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
  ENDFILE; empty input, repeated file EOF and read failures preserve RT. File admission preserves
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
