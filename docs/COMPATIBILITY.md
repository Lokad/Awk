# Compatibility

Lokad.Awk supports ordinary awk text processing inside .NET over valid UTF-8, with
host-controlled I/O and predictable resource admission. This is not full POSIX/GNU
conformance. The interpreter is the only execution engine; regex defaults to .NET.

**Supported** means golden coverage within the stated boundary. **Partial** means
implemented with remaining differences. **Deferred** means unsupported, without a delivery
commitment. Rejection tests do not establish feature support.

Use the [awk references](#qualification) for ordinary language semantics. The notes below
identify qualified support and Lokad-specific boundaries.

## Current surface

| Area | Status and boundary | Evidence / details |
| --- | --- | --- |
| Sources and command arguments | Supported: inline/repeated sources, `-f`, `-i`, `-F`, `-v`, includes, assignment operands and live ARGV scheduling, within host/policy authority. | [Source cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs), [ARGV](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ArgvCompatibility.cs) |
| Expressions and statements | Supported: arithmetic, concatenation, assignments, conditionals, loops and switch; profile/name rules apply. | [Grammar](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GrammarBoundaries.cs), [profiles](#compatibility-profiles-and-names) |
| Rules and input phases | Supported: ordinary/range patterns, BEGIN/END, next/nextfile and GNU BEGINFILE/ENDFILE, with phase restrictions. | [Function flow](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FunctionFlow.cs) |
| User functions | Supported: recursion, local scalar parameters, missing arguments and forwarded array aliases; depth/resource limits apply. | [Untyped values](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UntypedState.cs), [runtime contract](RUNTIME.md) |
| Scalars and flat arrays | Supported: numeric strings/origins, associative and SUBSEP compound keys, membership, delete and array length. Compound keys are not nested arrays. | [Value rules](#values-numbers-and-formatting), [goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs) |
| Records and fields | Supported: FS/FPAT/FIELDWIDTHS, character/regex/paragraph RS, RT, lazy fields and NF mutation. | [Field modes](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FieldModes.cs), [record differences](#fields-and-records) |
| Getline | Supported: main/redirected/one-way pipe expressions with scalar, array or field destinations; failure and destination rules apply. | [Destinations](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GetlineDestinations.cs), [failures](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GetlineFailures.cs), [I/O rules](#host-io-and-getline) |
| Output and streams | Supported through the host: print/printf, file/one-way pipe redirection, close and logical flush. | [Flush cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FlushCompatibility.cs), [I/O rules](#host-io-and-getline) |
| Formatting | Partial: common conversions, `%a`/`%A`, positional arguments, width/precision and integer rules. `%C`/`%S`, D/H/w controls and `%P` remain unsupported. | [Formatting](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FormattingSyntax.cs), [integers](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.IntegerFormatting.cs) |
| Numbers and math | Supported within binary64/invariant-culture policy: numeric coercion, math diagnostics, nondecimal conversion and strtonum. Hexadecimal floating source literals are deferred. | [Numbers](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.NumericSemantics.cs), [strtonum](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.StrtonumConversion.cs) |
| Strings and escapes | Supported scalar/byte units within strict UTF-8. GNU lowercase `\u` and numeric-byte escapes have profile-aware warnings. | [Text boundary](RUNTIME.md#text-boundary), [escape cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.NumericByteEscapes.cs) |
| Regex | Partial: ERE matching, longest/capture selection, replacements, split and UTF-8 boundary checks. Supplementary folding, inherited backend syntax and minimized repetitions have explicit limits. | [Regex semantics](#regex-semantics), [longest matches](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.LongestRegex.cs) |
| GNU string/array helpers | Supported in GNU mode: gensub, patsplit, separator/capture arrays, asort/asorti and sorted traversal within flat arrays. | [Splitting](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SplitSeparators.cs), [sorting](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SortingCompatibility.cs) |
| Bitwise and calendar helpers | Supported within unsigned-number and policy clock/time-zone behavior, not every native overflow/locale quirk. | [Bitwise](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.BitwiseCompatibility.cs), [calendar](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.CalendarNormalization.cs) |
| Introspection | Partial: typeof/isarray, SYMTAB/FUNCTAB/PROCINFO and flat-array origins; special-array behavior has GNU differences. | [Special-array rules](#special-array-reads), [cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SpecialArrayReads.cs) |
| Profiles and flags | Partial: POSIX/traditional name/effect restrictions, lint and warnings. Compatibility aliases do not imply full profile conformance. | [Names](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ProfileNames.cs), [options](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.OptionCompatibility.cs) |

## Compatibility profiles and names

Default/GNU mode enables selected GNU functions, input phases and special variables.
POSIX/traditional modes disable extensions and special-variable effects; disabled soft
names can become ordinary names. User calls require adjacency to `(`; built-ins allow
space. Powers associate right and bind before unary signs; POSIX rejects `**`/`**=`.
Invalid grammar and assignment targets are diagnosed even in unreachable code.
General GNU lint remains partial.

## Command-line compatibility aliases

Optimization/interval flags are aliases, not engine or regex-policy selectors.
Numeric-locale flags retain invariant parsing/formatting. Locale values remain host
environment data, not codec/culture selectors. POSIX overrides byte mode with a warning
in either option order. Admitted invocation warnings precede runtime effects.

`-v`/`-F` apply before execution in original option order under the final profile.
Assignment operands apply when the input scheduler reaches them, including through
`getline` in a function, and target globals. BEGIN exit can skip later operands.
Empty input operands retain their `ARGV` positions and select no input file.
Sources/includes and dynamic input paths require host/policy permission.
`TryParse` recognizes the invocation; source parsing and binding occur during execution.
Empty/comment-only sources are accepted, including file, include and repeated-source fragments.
Bare source/include names follow explicit `AWKPATH` order; slash-qualified paths bypass search.
An empty search component names the current directory. Without `AWKPATH`, lookup uses the current
directory. Includes support `.awk` suffix fallback; `-f` suffix fallback and stdin program source
(`-f -`) remain unsupported.

## Source string escapes

Source strings, options and consumed operands use profile-aware, single-pass decoding;
include filenames are raw quoted paths. Consecutive numeric-byte escapes decode together
as UTF-8: `"\303\251"` is é. Malformed/incomplete runs use U+00xx mappings, so `"\377"` is ÿ;
separate values do not merge runs. Escape-generated text remains valid UTF-8.
`printf`/`sprintf` consume evaluated formats without decoding their backslashes again.
Their `%s` width and precision follow scalar/byte mode. Byte precision that cuts a scalar
is a fatal UTF-8 error, preserving earlier output, rather than emitting a partial byte sequence.

Unicode escape support uses lowercase `\u` in default/traditional modes; uppercase `\U`
is unknown. Invalid scalars become `?` with a warning; missing digits retain `u` and warn.
POSIX strings retain plain `u`/`x` for `\u`/`\x`.

[Regex patterns](#regex-patterns) have a separate ERE interpretation pass.
Warning histories and locations are execution-local, including
unreachable lexical warnings; reusing a prepared program does not share history.

## Values, numbers and formatting

Numbers are invariant-culture binary64. Whole-string numeric classification differs from
arithmetic prefix conversion, whose whitespace is ASCII C whitespace. Numeric fields
retain their spelling/origin. NaN equality and ordered comparisons are false, inequality
is true, and NaN is true as a condition. Ordinary strings retain string truth/comparison.

Missing ordinary array reads create untyped entries; membership does not. Arrays pass
by alias, including forwarded missing parameters. Scalar assignments/returns copy values.
Grouped arguments impose scalar context and reject arrays. Function parameters shadow
globals, including for `length` and forwarded array aliases.

Plain assignment evaluates the RHS before resolving its target/key. Compound assignment
reads the target and old value before the RHS. Concatenation formats each operand before
evaluating the next. Side effects on targets, SUBSEP or CONVFMT
can therefore differ from GNU awk; keep order-sensitive effects in separate statements.
Numeric OFS/ORS use current CONVFMT; use string separators to preserve assigned spelling.
Printing a numerically assigned `$0` uses OFMT, as in mawk. GNU's cached CONVFMT spelling
for single-record printing is not reproduced.

Switch accepts numeric/string constants and regex literals. Nonconstant or duplicate scalar/default
labels fail before execution; repeated regex labels remain valid. Unary signed numbers are handled
without reproducing native reference crashes.

Formatting preflights publication. Bitwise, calendar and nonfinite/range behavior follow
the golden contract, not every libc quirk. Time comes from the policy's clock/zone, with
system/local defaults and epoch/UTC in the restricted preset.
Large `%g`/`%G` precision without `#` does not require precision-sized padding storage;
alternate-form and other conversions still admit storage for their actual large results.
`strftime` widths use ASCII digits. Unknown directives retain their literal spelling with
qualified GNU padding/case handling; this does not emulate every libc or locale variant.

Case-insensitive strings use invariant scalar folding (ASCII in byte mode), without
normalization or multi-scalar expansion. Array keys remain case-sensitive. Sorting uses
a total order with original-byte/index tie-breaks; mixed keys and Unicode folding can
differ from GNU. Regex folding follows the [regex profile rules](#regex-patterns).
`tolower`/`toupper` also use invariant Unicode casing by default and ASCII-only changes
in byte mode, preserving valid UTF-8.

## Special array reads

SYMTAB/FUNCTAB missing reads diagnose misuse; membership does not mutate. Declared globals
are visible before use, excluding function parameters and SYMTAB itself. Valid SYMTAB writes
can create governed names and invoke ordinary special-variable setters. Array-valued reads
permit aliases/introspection but reject scalar conversion. FUNCTAB metadata is read-only.

## Fields and records

[Text boundaries](RUNTIME.md#text-boundary) define field units and byte-mode restrictions.
The record-handling boundaries to account for are:

- In default/GNU mode, default RS removes the CR of CRLF from `$0` and sets `RT="\n"`.
  Paragraph RS recognizes CRLF as one newline and retains the separator's original bytes
  in RT; lone CR remains data. This extends the LF-only paragraph behavior of the GNU reference.
- POSIX RS uses its first UTF-8 byte, but a match consumes the complete scalar with that
  leading byte, preserving valid UTF-8.
- Host batch boundaries do not change regex RS records or RT. Word assertions use the
  current record's context; `^` retains descriptor-start context, including after RS changes.
- Empty FS splits characters in default mode; POSIX/traditional modes keep a nonempty record
  whole, subject to paragraph LF splitting. Literal single-scalar FS remains case-sensitive,
  including non-ASCII characters.
- Direct and SYMTAB access share NF state. The classified GNU 5.2.1 stale indirect-write
  behavior is not reproduced.

Evidence: [record cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs),
[paragraph/profile cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RangeParagraph.cs) and
[NF cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FieldCountValues.cs).

## Regex semantics

These rules apply across engines. Host integration and the internal Utf8Regex adapter
are described in [runtime regex integration](RUNTIME.md#regex-engines).

### Regex patterns

Ordinary ERE operators, intervals, grouping, alternation, anchors and bracket expressions
are supported within this contract. Dot consumes a complete scalar, including LF; `^`/`$`
denote absolute input boundaries. Awk translates the end anchor to `\z`, which the internal
adapter currently routes through .NET. Escaped/bracketed dots and dollars remain literals;
unmatched closing `)` is literal. Private translation groups preserve user capture numbers.

GNU/default mode enables IGNORECASE and GNU word/space/buffer operators. POSIX/traditional
modes keep the assigned IGNORECASE value but force case-sensitive regex behavior. Unknown
escapes such as `\d`, `\A`, `\p` and `\q` denote ordinary characters, not .NET operators.
Numeric-byte/`\u` decoding follows the [escape conversion rules](#source-string-escapes) before ERE
interpretation. Numeric metacharacters retain their ERE effect in GNU/POSIX mode;
traditional mode quotes them. POSIX does not decode hex/Unicode regex escapes.

The twelve standard lowercase POSIX class names are recognized; unknown names, including
`word`/`ascii`, and unterminated class tokens fail as invalid regexes. Classes use invariant
Unicode categories: `punct` includes punctuation/symbols; `graph` includes L/M/N/P/S/Cf/Co;
`print` additionally includes Zs. Controls, unassigned scalars and line/paragraph separators
are excluded from graph/print. Locale/Unicode-version membership and supplementary case/word
equivalence can differ from native awk. Single-character equivalence/collation tokens do not
emulate general locale collation.

Named/noncapturing groups, lookarounds and inherited .NET constructs retain backend semantics
outside the traditional ERE contract. Named groups use .NET numeric ordering. Existing lazy
`a+?`/`a??` forms do not establish modern POSIX/GNU minimized repetition support; `a**` is invalid.

In scalar expressions, `/pattern/` matches the current record. Direct literals supply pattern
text in rule/range patterns, switch labels, the RHS of `~`/`!~`, and pattern arguments of
match/split/patsplit/sub/gsub/gensub. Grouping a literal does not turn its scalar result into text.

### Regex matches and replacements

Observable traditional ERE matches select the longest match at the earliest scalar boundary.
Ambiguous whole-match/subexpression selection uses Awk's managed structured matcher;
boolean and unambiguous matches can retain the selected engine. Structured selection may
require a counted string conversion even with a UTF-8 engine.

`match(..., array)` and `gensub` share capture priority: `(a|aa)(a*)` on `aaa` captures
`aa` and `a`. Nonparticipating groups/metadata are omitted; participating empty groups remain.
Normal-mode start/length uses UTF-16 units, byte mode uses UTF-8 bytes. Whole matches and
observable captures must be scalar-aligned before slicing, allocation or publication;
otherwise execution fails with `awk: regular expression splits a Unicode character`.

`sub`/`gsub` use awk whole-match `&` and backslash rules; `gensub` additionally supports
numbered references. Only observable capture text is materialized. Replacement/capture
output preflights memory and publication; backend replacement syntax does not apply.

Regex FS/RS ignores empty matches without discarding skipped input. Global gsub/gensub
and FPAT/patsplit suppress an empty match immediately after a nonempty match; numeric
gensub occurrences count it. Other empty matches advance by a complete scalar.
Empty input has no FPAT/patsplit fields.

FS/FPAT/RS preparation completes before assignment publishes state. Fields retain captured
separator/case settings; other regex operations use current IGNORECASE. Malformed regex RS
fails at assignment. General regex RS can rescan accumulated input to preserve cross-batch
matches; cancellation/deadlines apply without a separate scan-work quota.

### Regex warnings and evidence

Lexical warnings retain source locations, including unreachable literals. Runtime Unicode
warning history belongs to each expression's current pattern/case variants; FS, FPAT and RS
have separate assignment histories. Changing away and back can warn again despite a cache hit.
Unknown ERE operators warn once per escaped character per execution, independently of string
escape and per-expression numeric-warning histories. Reused programs have fresh history;
rejected diagnostics do not consume it.

Golden coverage includes [profiles](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RegexProfiles.cs),
[classes](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RegexBracketClasses.cs),
[Unicode categories](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UnicodePrintClasses.cs),
[escapes](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UnicodeRegex.cs) and
[warning history](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RegexEscapeWarnings.cs).
Native nullable-RS, supplementary-byte replacement and capture/class differences require
versioned reference qualification, not reproducing native crashes or data loss. See the
[reference checks](../benchmarks/Lokad.Awk.Benchmarks/ReferenceContractRunner.cs).

## Host I/O and getline

All source, input, output and one-way pipes cross `IAwkHost`. Successful reads distinguish
MoreData from EOF; MoreData contains bytes. Line offsets are snapshotted, while content
remains borrowed until the next read starts on that descriptor or it closes. Append buffers
remain valid until the host task completes. Other descriptors have independent lifetimes.

Redirected getline I/O failures return -1 and set enabled ERRNO without an automatic runtime
stderr diagnostic; callers must check the status. Success/EOF preserve ERRNO.
Policy denial, unsupported capabilities, resource failure and invalid UTF-8 are fatal.
Getline resolves its source/destination before reading, even at EOF: index effects and
missing-location admission can occur without a value assignment. Direct postfix field
destinations are rejected; use `getline $(i++)` for explicit index mutation.

Open-pipe commands are opaque host input, including colons, backslashes, wildcards and
quotes. Same-name streams select the latest opening. Output-pipe fflush succeeds without
closing/awaiting the command; input-only fflush warns and returns -1. close returns the
command status. Flush/close follow logical-stream and host outcomes. Failures preserve
committed output without exposing CLR exception text. Caller cancellation propagates after
cleanup; hosts schedule deadlines through caller cancellation. See [embedding](../README.md#embedding)
and the [runtime and host contract](RUNTIME.md).

## Main input directories

Unhandled main-input open/read failures, including directories, are fatal in every profile:
later operands stop and there is no stdin fallback. BEGINFILE can explicitly recover with
nextfile; redirected getline remains recoverable. Awk does not infer file kinds from error
text or reproduce GNU's automatic directory skipping.

## Deferred and excluded capabilities

CSV is provisionally first, nested arrays next, subject to a motivating script and explicit
acceptance criteria. Other extensions remain demand-driven; none blocks the supported core.

| Capability | Current behavior |
| --- | --- |
| CSV (`-k`/`--csv`) | Rejected. Literal comma splitting does not handle quoted or multiline CSV; preprocess in the host. |
| Nested arrays (`a[1][2]`) | Rejected; `a[1,2]` SUBSEP keys work. |
| `system()` | Explicit unsupported diagnostic; one-way pipes do not imply support. |
| Namespaces | `@namespace` and qualified identifiers are rejected; host-mediated `@include` works. |
| Typed regex / indirect calls | `@/pattern/` and `@name(...)` are rejected. |
| Newer language forms | Modern minimized regex repetitions, newer printf controls and hexadecimal floating source grammar are not qualified. |

Native/dynamic loading, coprocesses, `/inet` special files, debugger/profiler switches,
gettext-style internationalization and arbitrary precision are excluded. Text must remain
valid UTF-8 even under C locale or byte mode; ambient locale codecs, numeric rules and
general locale collation are excluded.

The public boundary comprises invocations, async execution, host outcomes, resource policy
and AWK-specific memory/regex contracts. Prepared-program helpers remain internal. Core
has no Utf8Regex runtime dependency; the repository adapter is internal and non-packable.
Newer producer adoption is an independent qualification decision.

## Qualification

The [golden corpus](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs) defines observable behavior,
including [unsupported forms](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.CompatibilityContract.cs).
[Test layers](../tests/Lokad.Awk.Tests/README.md) add host/resource/counter checks. GNU oracle
checks are opt-in through `LOKAD_AWK_ORACLE_EXE`; ordinary tests require no GNU executable.
Reference version, matcher, locale and zone matter; crashes/data loss are not required semantics.
The [POSIX specification](https://pubs.opengroup.org/onlinepubs/9799919799/utilities/awk.html)
and [GNU manual](https://www.gnu.org/software/gawk/manual/html_node/) describe the broader
languages, not additional Lokad support. See [benchmarks](BENCHMARKS.md) for measured workloads.
