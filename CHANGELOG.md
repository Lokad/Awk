# Changelog

## Unreleased

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
