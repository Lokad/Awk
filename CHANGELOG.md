# Changelog

## Unreleased

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
