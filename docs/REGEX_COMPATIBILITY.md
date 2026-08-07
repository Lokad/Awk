# AWK Regex Compatibility Matrix

This matrix records the compatibility contract for the byte-native regex adapter rollout.
The default string compatibility path remains `System.Text.RegularExpressions`, while
`AwkRegexEngineKind.Utf8Adapter` and byte-mode non-ASCII matching route `AwkText` input through
`Lokad.Utf8Regex`. `AwkRegexEngineKind.DotNetCompatibility` remains the default engine target.

The former byte-mode non-ASCII hard failure has been removed for valid UTF-8 input. Regex
matching, `match()`, regex `FS`/`RS`, `patsplit`, `sub`, `gsub`, and `gensub` now have adapter
coverage in the `AwkText` runtime flow. The .NET string engine remains the compatibility route
when configured explicitly and for documented UTF-8 adapter pattern gaps.

Regex routing evidence uses dedicated counters. `RegexDotNetTextMatches` counts `AwkText`
regex operations routed through the .NET string engine, whether that engine is the configured
default or a compatibility fallback. `RegexPatternGapFallbacks` counts translated patterns such as
GNU buffer anchors that force that route in UTF-8 adapter mode, and `RegexStringAdapterMatches`
counts .NET string-engine match calls after a caller has crossed the string boundary. These counters
are separate from `AwkTextAsStringCalls` and decode misses.
Runtime routing keeps the selected engine, fallback reason, and byte-vs-UTF-16 coordinate policy as
separate decisions so a compatibility fallback cannot silently change offset units or counter
classification.
First-match consumers such as `match()`, regex record separators, and `sub()` use first-match
runtime APIs instead of materializing full match lists. `sub()` and `gsub()` expand whole-match
references from byte spans without materializing capture groups; capture values are reserved for
`match(..., array)` and `gensub()`. Full enumeration remains reserved for regex field splitting,
`patsplit`, `gsub`, and other paths that genuinely need every match.
Record separators that reduce to one or more bytes from a punctuation-only membership set use an
incremental byte-run plan, including `([,;])+`, `(?:[,;])+`, and the equivalent quantifier
inside the wrapper. Equivalent punctuation-only alternations such as `([,]|;)+` use the same
plan. Captures are not observable from `RS`; `RT` still receives the entire greedy run. Other
regex separators stay on the selected regex backend. An arbitrary unbounded match can
begin in an earlier host batch, and the current backends do not expose resumable partial-match
state, so that correctness path may retry borrowed accumulated input after a batch arrives. It
does not copy the accumulated snapshot or advance a checkpoint that could skip a cross-batch match.
Those retries are preflighted against `MaxRegexRecordSeparatorBytesScanned`, whose finite default
prevents adversarial host batching from causing unbounded aggregate rescanning.
Engine-neutral regex handles retain the initially selected backend, so a dynamic UTF-8 regex does
not first compile an unused .NET regex. Runtime caches evict the least-recently-used entry instead
of clearing all compiled patterns when their bounded capacity is reached.
Frontend regex diagnostics validate translated AWK literals before lowering; runtime backend
selection remains an execution-time concern.

Regex constants are context-sensitive. In ordinary scalar expressions, `/pattern/` evaluates the
numeric result of matching the current record, including in logical expressions, assignments,
printing, conditionals, and user-function arguments. Direct literal operands in grammar-defined
pattern positions instead supply pattern text: the right side of `~` and `!~`, regex rule and range
patterns, regex switch labels, and the pattern parameters of `match`, `split`, `patsplit`, `sub`,
`gsub`, and `gensub`. Parenthesizing a regex constant in a scalar expression does not turn that
expression back into a direct literal-pattern operand.

| Area | AWK requirement | Current .NET path | Byte-native candidate requirement |
| --- | --- | --- | --- |
| ERE operators | `.`, `*`, `+`, `?`, `{m,n}`, alternation, grouping, anchors | `AwkRegex.TranslatePattern` maps awk regex text into backend syntax | Adapter must run translated awk EREs or explicitly translate unsupported syntax before matching |
| Bracket expressions | POSIX character classes, equivalence classes, collating symbols, negation | Translator expands supported POSIX classes and C-locale equivalence/collation cases | Adapter must match current translator output or document a golden-case mismatch |
| Escapes | AWK escapes, octal/hex escapes, GNU word/buffer operators | Translator handles current supported escape set | Adapter must preserve translated escape semantics, including GNU extensions currently accepted |
| Anchors and dot/newline | AWK line-oriented matching over runtime strings | .NET regex behavior after translation | Adapter must keep current record/string matching semantics; newline behavior changes require explicit tests |
| Empty matches | `match`, `sub`, `gsub`, `gensub`, split loops must progress | Current loops rely on .NET `Match` and explicit runtime logic | Adapter must expose empty-match positions and allow progress logic equivalent to current behavior |
| Captures | `match(..., arr)`, replacement expansion, `gensub` captures | .NET `Match.Groups` with UTF-16 indices | Adapter detailed matches materialize group values and byte-aware offsets for AWK capture arrays and replacement expansion |
| Replacement syntax | `&`, `\&`, backslash literal rules, capture references in `gensub` | `sub`/`gsub` expand whole-match spans; `gensub` expands capture-bearing matches | `AwkReplacement` streams matches into governed UTF-8 builders without retaining the full match sequence |
| `IGNORECASE` | Dynamic special variable affects regex operations | Runtime recompiles or re-runs with `RegexOptions.IgnoreCase` | Adapter must include `IGNORECASE` in cache keys and dynamic operation behavior |
| Locale/culture | Current policy is ordinal/C-locale oriented | All regex construction uses `RegexOptions.CultureInvariant`, including dynamic `IGNORECASE` matching | Adapter and compatibility paths must remain independent of the ambient .NET culture |
| Byte offsets | `--characters-as-bytes` reports byte units for string operations | .NET exposes UTF-16 indices | `match()`, `RSTART`, `RLENGTH`, and capture `start`/`length` use byte units in byte mode and UTF-16-compatible units otherwise |
| Timeout/cancellation | Compile/match/replace/enumeration must observe timeout/cancellation | .NET timeout plus runtime cancellation checks | Adapter must expose timeout or bounded execution hooks and preserve cancellation checkpoints |
| Invalid UTF-8 | AWK ingress rejects invalid UTF-8 before regex | Regex layer sees valid .NET strings | Byte-native path should assume well-formed UTF-8 and does not need a raw invalid-byte fallback |

## Candidate Policy

`Lokad.Utf8Regex` is the first byte-native adapter because the core package is net10.0-oriented,
span-based, and exposes byte-coordinate matching/enumeration plus replacement/split APIs and
timeout support. This does not make it semantically equivalent to AWK: the core package follows
.NET regex semantics, so awk ERE translation remains a required compatibility layer.

`Lokad.Utf8Regex.PythonRe` is not a default candidate: AWK is not Python `re`, and adopting
Python semantics would add mismatches without evidence. `Lokad.Utf8Regex.Pcre2` remains an
optional candidate only if a separate golden-corpus comparison shows that PCRE2-style
translation is closer to GNU awk than the core .NET-style engine.

## Default Policy

`DotNetCompatibility` remains the default regex engine until the adapter has enough release
evidence to change the default without surprising compatibility shifts. `Utf8Adapter` is the
stable opt-in byte-native mode, and byte-mode non-ASCII `AwkText` operations route to the adapter
so byte offsets remain meaningful for valid UTF-8 input.

Current adapter evidence: regex golden cases pass in `Utf8Adapter`; byte-mode valid
UTF-8 matching has tests for non-ASCII input, byte `RSTART`/`RLENGTH`, capture array
`start`/`length`, regex `FS`/`RS`, `patsplit`, separator arrays, `sub`, `gsub`, `gensub`, and
replacement expansion. Remaining compatibility gaps should be tracked as specific pattern
translation fallbacks and any timeout behavior not covered by the adapter's bounded matching
hooks.

## Matrix Evidence

The focused `Awk_Utf8RegexAdapterCoversTranslationMatrix` test covers POSIX classes,
single-character equivalence and collating tokens, GNU word and buffer escapes, empty matches,
dynamic `IGNORECASE`, capture arrays, Unicode string offsets, and replacement expansion. Broader
golden coverage runs the tagged regex corpus under `Utf8Adapter`; pattern-gap counters are covered
separately for GNU buffer anchors that deliberately route through the .NET compatibility engine.
Cancellation-resource tests cover adapter enumeration cancellation checks and replacement-heavy
`gsub` loops with repeated cancellation checkpoints. Runtime regex timeout exceptions are mapped
to the awk-shaped regex timeout diagnostic.
Regex action patterns, regex range patterns, regex switch labels, and dynamic `~`/`!~` expressions
are covered under `Utf8Adapter` with non-ASCII `AwkText` inputs and explicit no-decode assertions.
