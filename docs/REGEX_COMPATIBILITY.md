# Regex compatibility

Awk owns pattern translation, match/capture selection, replacement and UTF-8 validation.
The default engine is .NET compatibility, including non-ASCII byte mode. Core has no
Lokad.Utf8Regex runtime dependency. Applications can supply an `IAwkRegexEngine` through
`AwkExecutionPolicy.WithRegexEngine`; unsupported patterns retain the .NET fallback.

The internal `Lokad.Awk.Utf8Regex` adapter uses producer 0.2.0 for repository qualification;
it is not distributed as a NuGet package. Newer producer adoption is postponed and does
not block independent core work or releases. Selecting an engine does not promise that
every pattern runs byte-native.

## Patterns and profiles

Ordinary ERE operators, intervals, grouping, alternation, anchors and bracket expressions
are supported within this contract. Dot consumes a complete scalar, including LF; `^`/`$`
denote absolute input boundaries. Awk translates the end anchor to `\z`, which the optional
adapter currently routes through .NET. Escaped/bracketed dots and dollars remain literals;
unmatched closing `)` is literal. Private translation groups do not change user capture numbers.

GNU/default mode enables IGNORECASE and GNU word/space/buffer operators. POSIX/traditional
modes keep the assigned IGNORECASE value but force case-sensitive regex behavior.
Unknown escapes such as `\d`, `\A`, `\p` and `\q` denote ordinary characters, not .NET operators.
Numeric-byte and `\u` decoding follow [UTF-8 policy](UTF8_POLICY.md#escapes) before the
ERE interpretation pass. Numeric metacharacters retain their ERE effect in GNU/POSIX
mode; traditional mode quotes them. POSIX does not decode hex/Unicode regex escapes.

The twelve standard lowercase POSIX class names are recognized; unknown names, including
`word`/`ascii`, and unterminated class tokens fail as invalid regexes. Classes use invariant
Unicode categories rather than ambient locale tables: `punct` includes punctuation/symbols;
`graph` includes L/M/N/P/S/Cf/Co; `print` additionally includes Zs. Controls, unassigned scalars
and line/paragraph separators are excluded from graph/print. Locale/Unicode-version
membership and supplementary case/word equivalence can differ from native awk.
Supported single-character equivalence/collation tokens do not emulate general locale collation.

Named/noncapturing groups, lookarounds and other inherited .NET constructs retain backend
compatibility semantics outside the traditional ERE contract. Named groups use .NET numeric
ordering. Existing lazy `a+?`/`a??` forms do not establish modern POSIX/GNU minimized
repetition support; repeated unary forms such as `a**` remain invalid.

In ordinary scalar expressions, `/pattern/` evaluates to matching the current record.
Direct regex literals supply pattern text in grammar-defined positions: rule/range patterns,
switch labels, the RHS of `~`/`!~`, and pattern arguments of match/split/patsplit/sub/gsub/gensub.
Grouping a literal as a scalar expression does not turn its result back into pattern text.

## Observable matches and replacements

Observable traditional ERE matches select the longest match at the earliest scalar boundary.
Ambiguous whole-match/subexpression selection uses Awk's managed structured matcher;
boolean and structurally unambiguous matches can retain the selected engine. This can
require a counted string conversion even with a supplied UTF-8 engine.

Capture priority is shared by `match(..., array)` and `gensub`: for example,
`(a|aa)(a*)` on `aaa` captures `aa` and `a`. Nonparticipating groups and their metadata
are omitted; participating empty groups remain present. Normal-mode start/length uses
UTF-16 units, byte mode uses UTF-8 bytes. Whole matches and observable captures must be
scalar-aligned before slicing, allocation or publication; otherwise execution fails with
`awk: regular expression splits a Unicode character`.

`sub`/`gsub` use awk whole-match `&` and backslash rules; `gensub` additionally supports
numbered references. These are Awk replacement semantics, not backend replacement syntax.
Only observable capture text is materialized. Replacement/capture output preflights
memory and publication under the ordinary resource policy.

Empty-match behavior belongs to each consumer. Regex FS/RS ignores empty matches without
discarding skipped input. Global gsub/gensub and FPAT/patsplit suppress an empty match
immediately after a nonempty match; numeric gensub occurrences count it. Other empty matches
advance by a complete scalar. Empty input has no FPAT/patsplit fields.

FS/FPAT/RS preparation completes before assignment publishes state. Fields retain captured
separator/case settings; ordinary regex operations use current IGNORECASE. Malformed regex
RS fails at assignment. General regex RS may rescan accumulated input to preserve cross-batch
matches; cooperative cancellation and command deadlines apply. There is no separate scan-work
quota. See [records](RECORD_SEMANTICS.md) and [limits](RUNTIME_LIMITS.md).

## Warnings

Lexical warnings retain source locations, including unreachable literals. Runtime Unicode
warning history belongs to the expression's current pattern/case variants; FS, FPAT and RS
have separate assignment histories. Changing away and back can warn again despite a cache hit.
Unknown ERE operators warn once per escaped character across an execution, independently
of ordinary string-escape and per-expression numeric-warning histories. Each execution has
fresh history, including reused prepared programs; rejected diagnostics do not consume history.

## Supplying an engine

```csharp
using Lokad.Awk;

static AwkExecutionPolicy WithApplicationEngine(IAwkRegexEngine engine) =>
    AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine);
```

For a custom engine, implement the [AWK-specific contract](../src/Lokad.Awk/Regex/IAwkRegexEngine.cs):

- `TryCompile` receives translated .NET-compatible pattern text, invariant case choice,
  match timeout, borrowed active memory budget and current checkpoint. It must allow
  concurrent compilation. Return false only for unsupported syntax; do not translate again.
- Admission refusal throws `AwkRegexAdmissionException`; matching expiry throws
  `RegexMatchTimeoutException`; cancellation propagates. None requests fallback.
- Compiled handles consume valid UTF-8 synchronously. Match searches preserve complete-input
  assertion context and use a UTF-16 start offset. Fill every requested capture slot with
  absolute byte/UTF-16 ranges; report unmatched slots explicitly. Awk validates published ranges.
- Retain no input/capture spans or checkpoint. The application owns the engine; bounded,
  execution-local caches own compiled handles, dispose evictions and release remaining handles
  at cleanup. Handle disposal is idempotent and non-throwing. The supplied budget is borrowed.

Awk still chooses longest/capture selection and compatibility routes; the interface does not
delegate those policies. Awk-owned POSIX selection polls cancellation without a separate stopwatch deadline;
.NET calls retain timeout protection. Engine timeout does not bound indivisible construction. The adapter
accepts spans without adding input/capture-text copies; an engine's own internals can allocate.
Engine defects belong with that engine; Awk retains dialect, routing and boundary responsibility.

## Evidence and reference boundaries

Golden coverage includes [longest matches](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.LongestRegex.cs),
[profiles](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RegexProfiles.cs),
[classes](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RegexBracketClasses.cs),
[Unicode categories](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UnicodePrintClasses.cs),
[escapes](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UnicodeRegex.cs) and
[warning history](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RegexEscapeWarnings.cs).
[Host-engine tests](../tests/Lokad.Awk.Tests/AwkHostRegexTests.cs) check ownership,
malformed ranges, fatal refusal, cancellation and timeout;
[adapter tests](../tests/Lokad.Awk.Tests/AwkUtf8RegexAdapterTests.cs) cover direct/fallback
routes, byte coordinates, separators, replacements and no-decode paths.

`RegexDotNetTextMatches` counts text operations using .NET; `RegexPatternGapFallbacks`
identifies semantic/pattern fallbacks in adapter mode. `RegexStringAdapterMatches` counts
string-based backend calls, including structured selection. Multi-match scans reuse a
decoded input where possible; these counters are distinct from text decode counters.

Native behavior depends on version, matcher, locale and Unicode data. Reference checks
classify nullable-RS data loss, supplementary-byte replacement and versioned capture/class
differences rather than reproducing them. Exact bytes and binary/matcher identity must
qualify equivalent-work timings. See the [benchmark workflow](BENCHMARKS.md) and
[opt-in reference checks](../benchmarks/Lokad.Awk.Benchmarks/ReferenceContractRunner.cs).
