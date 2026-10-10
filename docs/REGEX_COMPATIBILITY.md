# AWK Regex Compatibility Matrix

This matrix records the compatibility contract for the byte-native regex adapter rollout.
The default string compatibility path remains `System.Text.RegularExpressions`, while
`AwkRegexEngineKind.Utf8Adapter` and byte-mode non-ASCII matching route `AwkText` input through
`Lokad.Utf8Regex`. `AwkRegexEngineKind.DotNetCompatibility` remains the default engine target.

The former byte-mode non-ASCII hard failure has been removed for valid UTF-8 input. Regex
matching, `match()`, regex `FS`/`RS`, `patsplit`, `sub`, `gsub`, and `gensub` now have adapter
coverage in the `AwkText` runtime flow. The .NET string engine remains the compatibility route
when configured explicitly and for documented UTF-8 adapter pattern gaps.

IGNORECASE's effects are enabled only in GNU/default mode. POSIX and traditional modes
keep the assigned value visible but force case-sensitive regex matching and separators.
String comparisons, index, and sorting use the invariant scalar/byte policy recorded in
`COMPATIBILITY_CONTRACT.md`; regex Unicode case/word equivalence still follows its
separate backend compatibility contract.

The compatibility profile also governs literal and dynamic escape translation, including
precompiled rule, operator and switch patterns. GNU word/space/buffer operators are enabled
only in GNU mode and are ordinary escaped characters in POSIX/traditional modes. Unknown
escapes such as `\d`, `\A`, `\p` and `\q` denote literal characters rather than .NET
operators. Octal/hexadecimal metacharacters retain their ERE effect in GNU/POSIX mode;
traditional mode quotes them. POSIX mode does not decode hexadecimal regex escapes.
Numeric decoding precedes ERE escape interpretation: GNU `\134w` is a word operator,
whereas `\134y` is a literal y. Bracket escapes retain character-class context.

Named POSIX bracket classes accept the 12 standard lowercase names. Unknown names,
including `word` and `ascii`, and unterminated named-class tokens fail through the
existing invalid-regexp diagnostic in every profile, for lexical and dynamic patterns.
Ordinary bracket text such as `[:word:]` keeps its character-set meaning. Admission
respects escaped openers, leading literal brackets and inherited backend comments,
including scoped whitespace mode. Its mode buffer is admitted before allocation and
released on return, failure or cancellation; ordinary ERE classes need no mode buffer.
`[:punct:]` maps to Unicode punctuation and symbols, so `$`, `+`, `|` and currency or
mathematical symbols are included. This is the invariant backend category policy;
it does not promise every GNU/libc locale or Unicode-version membership equivalence.
`[:graph:]` uses Unicode letters, marks, numbers, punctuation, symbols, format and
private-use categories (L/M/N/P/S/Cf/Co). `[:print:]` additionally includes space
separators (Zs). Both exclude controls, unassigned scalars and line/paragraph separators.
Combining marks, supplementary symbols and private-use scalars are included; printable
does not imply a visible glyph or a positive display width. Complete scalar atoms and
user capture numbers are preserved by the existing translation boundary.

The category policy is invariant across locales and profiles. Non-breaking, figure
and narrow non-breaking spaces are printable but not graphic under this policy;
GNU/glibc can classify those spaces as graphic instead. Runtime Unicode-data versions
may also differ from native locale tables. These are explicit compatibility boundaries,
not claims of complete locale emulation. The class set is translated by Awk into the
existing backend category syntax; raw engine tables and matching code are unchanged.
See [Unicode class/capture goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UnicodePrintClasses.cs)
and [span/counter checks](../tests/Lokad.Awk.Tests/AwkUnicodePrintClassTests.cs).
[GNU class definitions](https://www.gnu.org/software/gawk/manual/html_node/Bracket-Expressions.html)
and [glibc's Unicode classifier](https://github.com/bminor/glibc/blob/glibc-2.39/localedata/unicode-gen/unicode_utils.py)
explain the native contract and the spacing/version boundary.

See [class goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RegexBracketClasses.cs)
and [admission/counter checks](../tests/Lokad.Awk.Tests/AwkRegexBracketClassTests.cs).

Source strings decode GNU 5.4 `\u` escapes before they become dynamic regex patterns.
An escaped dot, bracket, anchor, group or quantifier produced by that source decoding
has its ordinary ERE effect; an escape-produced backslash crosses the regex boundary
as pattern data. Source string decoding is a single pass.

Octal/hexadecimal regex escapes represent bytes, including eight-bit octal truncation.
Complete adjacent UTF-8 byte sequences decode to one scalar before ERE interpretation;
`/\303\251/` matches é. Default/traditional hex decoding permits mixed radix runs;
POSIX keeps escaped x literal. Traditional mode still quotes numeric metacharacters.
Incomplete/malformed byte sequences retain Awk's U+00xx fallback, as documented in
[UTF-8 policy](UTF8_POLICY.md); this intentionally does not reproduce invalid GNU bytes.
The same decoder serves source strings, options and consumed assignment operands.
See [numeric-byte goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.NumericByteEscapes.cs)
and [resource/counter checks](../tests/Lokad.Awk.Tests/AwkNumericByteEscapeTests.cs).
Numeric digit widths follow the [GNU escape contract](https://www.gnu.org/s/gawk/manual/html_node/Escape-Sequences.html).

Raw regex literals and runtime patterns also decode GNU 5.4 lower-case `\u` escapes
in default/traditional profiles, consuming one to eight hexadecimal digits. Numeric
escape decoding runs before ERE escape interpretation; `\u005c\u0041` becomes `\A`
and matches an ordinary A. Traditional mode quotes numeric metacharacters. Missing
Unicode digits retain u and warn; invalid scalars become a quoted literal `?` and
warn. POSIX keeps escaped u as an ordinary character without GNU Unicode warnings.

Literal Unicode warnings are lexical and preserve source locations, including dead
code. Runtime Unicode warnings track the expression's last pattern and compiled case
variants, independently of the shared regex cache. Distinct expressions each warn;
repeating the same pattern at one expression does not. Pattern changes reset that
history, and each compiled execution has fresh history. Pending warnings survive a
later runtime failure. FS and enabled FPAT prepare both case variants at assignment;
regex RS does so in default/traditional mode, including startup assignments before
BEGIN and programs that exit without reading input. POSIX RS keeps its first-byte
policy. Literal and recognized byte-set/run separators retain their byte-native plans.
Malformed regex RS fails at assignment rather than waiting for input.

Special-variable warning history is separate for FS, FPAT and RS. Same-value
assignments and IGNORECASE changes reuse prepared variants; changing away and back
replays Unicode warnings even when the compiled regex cache already contains the
pattern. An RS change also refreshes FS preparation while FS parsing is active.
Each compiled execution has fresh runtime history. Assignment preparation completes
before scalar storage, splitting modes, PROCINFO metadata and RS version are published.
See [assignment goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SpecialRegexAssignments.cs)
and [admission/reuse checks](../tests/Lokad.Awk.Tests/AwkSpecialRegexAssignmentTests.cs).
Unknown regexp operators warn once per escaped character across lexical literals,
including unreachable code, and runtime patterns. This invocation-wide history is
separate from string-escape and per-expression numeric warning histories, survives
regex-cache eviction and case changes, and is fresh for each compiled execution.
GNU word operators are recognized only in GNU mode; POSIX/traditional mode warns
and retains their existing literal interpretation. Escaped `8` and `9` have their
own plain-digit warning wording. History follows GNU's first escaped UTF-8 byte;
diagnostics render complete scalars. Rejected diagnostics do not record an escape.
Missing regex `\x` digits use the existing per-expression numeric-warning history;
POSIX mode keeps its literal `x` policy. Numeric and operator warnings keep scan order.
See [warning goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RegexEscapeWarnings.cs),
[versioned survey cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RegexEscapeWarningSurvey.cs)
and [history/admission tests](../tests/Lokad.Awk.Tests/AwkRegexEscapeWarningTests.cs).

Ordinary unknown-string escapes share typed invocation-wide history across source,
options and consumed operands; repeated unknown escapes warn once. Missing-hex and
Unicode warnings remain per occurrence. First source locations survive compiled reuse,
and each execution has fresh history. Unknown non-ASCII diagnostics render complete
scalars under the strict UTF-8 policy. Regexp operator warnings use the separate
invocation-wide history described above.
Regex scalar casing/boundary engine defects belong upstream in Lokad.Utf8Regex;
Awk retains dialect translation and qualified dependency integration.

An unmatched closing `)` denotes a literal character, including in patterns without
backslashes. Balanced groups and parentheses inside brackets keep their ERE meanings.
The translation returns the original string when no rewriting is needed.

ASCII letter classes containing one letter in either or both cases are normalized to
equivalent literal atoms. This avoids the UTF-8 adapter 0.2.0 boolean-match prefilter's
incorrect case folding for classes such as `[W]` and `[Ww]`, while retaining byte-native
matching, quantifiers and capture numbering.

POSIX record separators select their first UTF-8 byte. For a non-ASCII prefix, the
scanner consumes the complete input scalar with that leading byte; it can therefore
consume a different scalar from the one written in RS. Traditional mode retains
multi-character regex RS. `--posix` overrides `--characters-as-bytes` in either option
order and emits an ordinary warning before execution.
In GNU mode, a one-byte RS remains literal and case-sensitive. A non-ASCII single-scalar
RS uses regex matching when IGNORECASE is enabled, preserving case equivalence for that
separator; it retains the byte-native literal plan when case-sensitive.

This qualification does not establish full profile conformance. Newer minimized
repetitions remain deferred; Unicode word-boundary engine qualification belongs upstream.
Versioned separator and final native qualification remain separate release gates.
GNU 5.4 MinRX also loses uppercase members in classes such as `[[:alpha:]A]`; GNU 5.2
and GNU 5.4 with `GAWK_GNU_MATCHERS=1` retain them. These reference differences must
remain explicit when selecting compatibility expectations.

Regex routing evidence uses dedicated counters. `RegexDotNetTextMatches` counts `AwkText`
regex operations routed through the .NET string engine, whether that engine is the configured
default or a compatibility fallback. `RegexPatternGapFallbacks` counts translated patterns such as
GNU buffer anchors, the absolute end anchor used for awk `$`, and EREs needing longest-match or capture selection that force that route in UTF-8
adapter mode, and `RegexStringAdapterMatches`
counts string-based match calls, including structured ERE selection, after a caller has crossed the string boundary. These counters
are separate from `AwkTextAsStringCalls` and decode misses.
Multi-match operations adapt an input to a .NET string once and reuse that string for the complete
scan; `RegexStringAdapterMatches` continues to count the individual backend match calls.
Runtime routing keeps the selected engine, fallback reason, and byte-vs-UTF-16 coordinate policy as
separate decisions so a compatibility fallback cannot silently change offset units or counter
classification.
First-match consumers such as `match()`, regex record separators, and `sub()` use first-match
runtime APIs instead of materializing full match lists. `sub()` and `gsub()` expand whole-match
references from byte spans without materializing capture groups; capture values are reserved for
`match(..., array)` and the references actually observable by `gensub()`. A `gensub` replacement
with no `&` or `\0`-to-`\9` reference materializes no groups; otherwise it materializes group zero
through the highest referenced group, capped by the groups present in the match. Full enumeration
remains reserved for regex field splitting, `patsplit`, `gsub`, and other paths that genuinely need
every match.

Character atoms consume complete Unicode scalars in both backends. Translation expands dot,
negated brackets, supplementary literals/ranges, and supported Unicode character categories into
scalar atoms while preserving user-visible capture numbering. Dot includes LF; `^` and `$` refer
to absolute string boundaries. Escaped and bracketed dots/dollars remain literals. The absolute
end anchor is translated to `\z`, a documented adapter pattern gap, rather than relying on the
backend's match-before-final-LF behavior. Empty capturing groups preserve their empty capture
through a zero-count literal atom to avoid an adapter construction defect.

Whole matches and observable captures are checked for scalar alignment before byte conversion,
allocation, replacement, or capture publication. An inherited backend extension that selects half
of a surrogate pair fails with `awk: regular expression splits a Unicode character`; it cannot
publish malformed `AwkText`. This also applies in byte mode, whose coordinates count bytes while
text remains valid UTF-8. Normal-mode regex offsets remain UTF-16 units under the existing policy.

Empty-match rules belong to the consumer. Regex field/record separators ignore empty matches and
retain all skipped input. Global `gsub`/`gensub` and FPAT/`patsplit` suppress an empty match
immediately following a nonempty match. Numeric `gensub` occurrences count that empty match.
Other empty matches progress by one complete scalar; empty input has no FPAT/`patsplit` fields.
Capture arrays omit groups that did not participate, including their start/length metadata.
Observable ERE matches select the longest match at the earliest scalar boundary, including
overlapping alternatives and competing repetitions. A structural width analysis retains the
selected backend for fixed-width patterns and unambiguous greedy repetition. Traditional EREs
needing longest-match or observable subexpression selection use a common managed structured NFA
in either configured mode. This route adapts input to a counted .NET string and uses bounded .NET
predicates for character classes, case folding, and assertions. Boolean matching retains the
selected backend because the chosen path is not observable.

The structured matcher resolves construct priority before leaving alternatives, groups, and
repetitions. Matching `(a|aa)(a*)` against `"aaa"` captures `"aa"`/`"a"`, and matching
`(a?|ab)*` against `"a"` retains the final nonempty capture. Descendant captures are reset on
each parent iteration; participating empty groups remain distinct from absent groups. Match
arrays and `gensub` references share this selection. Full-input anchors and word boundaries
remain visible when a search resumes at a later position. One operation deadline covers whole
match and capture selection; cancellation checkpoints occur during compilation and matching.
Compilation preflights interval expansion, and the memory governor preflights fixed match buffers
before allocation. Capture materialization uses the existing governed UTF-8 boundary checks.

Reference version and matcher must be recorded for capture comparisons. GNU 5.4 introduced
MinRX to replace matchers that were not fully POSIX compliant; `GAWK_GNU_MATCHERS` selects
the older matchers. Consequently, matching GNU 5.2.1 capture output alone does not qualify
POSIX subexpression selection. The benchmark reference remains separately pinned. See
[GNU's matcher documentation](https://www.gnu.org/software/gawk/manual/html_node/Choosing-Matchers.html).

Some inherited backend constructs, including named/noncapturing groups, arbitrary lookarounds,
balancing groups, conditionals, atomic groups, and
whitespace/comment mode, retain their existing compatibility search and capture semantics.
Named groups retain .NET numeric ordering: unnamed captures precede named captures.
Participating empty captures remain distinct from absent groups; private translation
groups never enter match arrays. The class/capture goldens cover these existing contracts.
Escape sequences follow the awk profile rules above: backend escapes such as `\k`, `\G`
and `\p` are ordinary escaped characters, and numeric escapes follow awk decoding.
The legacy endpoint constraint's private groups remain hidden from user captures and counters.
These extensions are outside the traditional awk ERE contract. Repeated unary
quantifiers such as `a**` retain the invalid-regexp diagnostic, although GNU accepts
some such forms. Inherited `a+?`/`a??` forms keep their existing .NET lazy priority.
POSIX 2024/GNU 5.4 minimized repetitions remain a separate compatibility follow-up.
Record separators that reduce to one or more bytes from a punctuation-only membership set use an
incremental byte-run plan, including `([,;])+`, `(?:[,;])+`, and the equivalent quantifier
inside the wrapper. Equivalent punctuation-only alternations such as `([,]|;)+` use the same
plan. Captures are not observable from `RS`; `RT` still receives the entire greedy run. Other
regex separators stay on the selected regex backend. An arbitrary unbounded match can
begin in an earlier host batch, and the current backends do not expose resumable partial-match
state, so that correctness path may retry borrowed accumulated input after a batch arrives. It
does not copy the accumulated snapshot or advance a checkpoint that could skip a cross-batch match.
Those retries check cancellation and consume any explicit fuel allowance for every offered input
span. Without an explicit quota there is no aggregate CPU/scan ceiling; operation-local safeguards
and caller/deadline ownership remain separate. `MaxRegexRecordSeparatorBytesScanned`
remains an optional secondary ceiling and defaults to unlimited in both presets.
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
| Anchors and dot/newline | Dot includes LF; anchors denote absolute string boundaries | Scalar dot and `\z` translation | Same translation, with counted .NET fallback for the absolute end anchor |
| Empty matches | Each consumer applies its own empty-match rules | Scalar progress; empty separators ignored; suppression depends on operation | Same consumer rules and complete-scalar progress |
| Match selection | Longest match at the earliest position | Structured NFA for ambiguous traditional EREs; full-input assertion context | Counted string route for observable selection; boolean and unambiguous matches retain the selected backend |
| Captures | `match(..., arr)`, replacement expansion, `gensub` captures | Structured POSIX selection where needed; scalar-aligned UTF-16 coordinates; nonparticipating groups absent | Same selection before byte alignment checks; `gensub` materializes only the prefix through its highest observable `\0`-to-`\9` reference |
| Replacement syntax | `&`, `\&`, backslash literal rules, capture references in `gensub` | `sub`/`gsub` expand whole-match spans; `gensub` expands its selected capture prefix | `AwkReplacement` streams matches into governed UTF-8 builders without retaining the full match sequence |
| `IGNORECASE` | Dynamic special variable affects regex operations | Runtime recompiles or re-runs with `RegexOptions.IgnoreCase` | Adapter must include `IGNORECASE` in cache keys and dynamic operation behavior |
| Locale/culture | Current policy is ordinal/C-locale oriented | All regex construction uses `RegexOptions.CultureInvariant`, including dynamic `IGNORECASE` matching | Adapter and compatibility paths must remain independent of the ambient .NET culture |
| Byte offsets | `--characters-as-bytes` reports byte units for string operations | .NET exposes UTF-16 indices | `match()`, `RSTART`, `RLENGTH`, and capture `start`/`length` use byte units in byte mode and UTF-16-compatible units otherwise |
| Timeout/cancellation | Compile/match/replace/enumeration must observe timeout/cancellation | .NET timeout plus runtime cancellation checks | Adapter must expose timeout or bounded execution hooks and preserve cancellation checkpoints |
| Invalid UTF-8 | Ingress and regex-produced text remain valid UTF-8 | Validate match/capture boundaries before encoding or slicing | Validate backend byte alignment before slicing or copying |

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

Record field splitting uses immutable case settings captured with the record. Ordinary
regex operations continue to resolve the current `IGNORECASE` dynamically; fixed-case
split handles have distinct cache keys. The single-character FS exception and native
FPAT case-setting quirk are documented in `docs/RECORD_SEMANTICS.md`.

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
