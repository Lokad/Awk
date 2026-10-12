# Language Compatibility

Lokad.Awk implements common awk workflows over valid UTF-8, with all IO supplied by
`IAwkHost`. This matrix describes the current implementation. It is not a claim of
complete POSIX or GNU awk conformance. The interpreter is the default; the executable
engine remains opt-in.

Status meanings:

- **Supported**: the listed behavior has golden coverage, within the linked policy.
- **Partial**: useful behavior is implemented, with the stated remaining gaps.
- **Planned**: currently unsupported; reserved for a later release.
- **Excluded**: an intentional embedding, text or host limitation.

The test links identify the canonical golden cases or case families. A passing rejection
case proves the current diagnostic, not support for the rejected feature. Controls for a
planned feature likewise do not prove that feature works. Narrow resource/host tests
supplement the goldens; see the [test layers](../tests/Lokad.Awk.Tests/README.md).

The reference columns link to POSIX and GNU specifications. GNU 5.2 and 5.4 differ in
several cases; a reference version, regex implementation, locale and time zone must be
recorded for a comparison. The [detailed contract](COMPATIBILITY_CONTRACT.md) and
[regex matrix](REGEX_COMPATIBILITY.md) classify intentional differences and known native
failures. Oracle checks remain opt-in through `LOKAD_AWK_ORACLE_EXE`; ordinary tests do
not require GNU awk. See the [benchmark workflow](BENCHMARKS.md) for qualified timing.

## Supported Surface And Core Work

| Area | Status and boundary | Golden evidence | Reference |
| --- | --- | --- | --- |
| Command sources and assignments | Supported: inline/repeated sources, `-f`, `-i`, `-F`, `-v`, assignment operands, ARGV changes and stdin scheduling; portable host path policy applies. | [Command/source cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `repeated-source-options`, `source-include-directive`; [assignment scheduling](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ArgvCompatibility.cs). | [POSIX awk][posix]; [GNU options][options] |
| Expressions and statements | Supported: arithmetic, concatenation, assignment expressions, conditionals, loops and ordinary separators; user-function calls require adjacency, built-ins allow spaces. | [Grammar cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GrammarBoundaries.cs); [unary operators](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UnaryPrefix.cs); [assignment operands](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.AssignmentOperands.cs). | [POSIX awk][posix] |
| Patterns and input phases | Supported: ordinary/range patterns, BEGIN/END, next, nextfile and GNU BEGINFILE/ENDFILE, subject to profile and phase restrictions. | [Record/pattern cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs); [function input control](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FunctionFlow.cs); [range/paragraph cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RangeParagraph.cs). | [POSIX awk][posix]; [GNU input][input] |
| User functions | Supported: recursion, scalar-local parameters, array aliases, forwarding and missing arguments; recursion/resource caps apply. | [Function cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `user-function-recursive-return-expression`; [typing cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UntypedState.cs). | [POSIX awk][posix]; [GNU functions][functions] |
| Scalar and flat-array values | Supported: numeric strings, untyped origins, associative keys, SUBSEP compound keys, membership, delete and array length; compound keys are not nested arrays. | [Array/value cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `multidimensional-array-subsep`, `array-membership-does-not-autovivify`; [untyped cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UntypedState.cs). | [POSIX awk][posix]; [GNU arrays][arrays] |
| Records and fields | Supported: default/character/regex/paragraph RS, FS, FPAT, FIELDWIDTHS, NF mutation and lazy fields; CRLF paragraphs and retained pipe EOF have explicit policies. | [Field modes](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FieldModes.cs); [NF values](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FieldCountValues.cs); [paragraph EOF](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ParagraphEof.cs); [pipe EOF](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ParagraphPipeEof.cs). | [POSIX awk][posix]; [GNU input][input] |
| `getline` | Supported: main, redirected and one-way pipe expressions with variable/array/field destinations. Recoverable redirected errors return -1; main-input failures are fatal. Direct postfix field destinations are explicitly rejected. | [Expressions](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GetlineExpressions.cs); [destinations](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GetlineDestinations.cs); [failures](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GetlineFailures.cs); [postfix policy](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GetlinePostfix.cs). | [POSIX awk][posix]; [GNU getline][getline] |
| Output, redirection, close and flush | Supported through the host: print/printf, files, one-way pipes and logical close/flush. Path aliases and publication follow the host contract. | [Output cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `fflush-function-return-values`; [flush/close cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FlushCompatibility.cs). | [POSIX awk][posix]; [GNU IO functions][io] |
| Formatting | Partial: common conversions, `%a`/`%A`, positional arguments, widths/precision and integer behavior are implemented. Newer `%C`/`%S`, D/H/w controls and `%P` remain later work. | [Formatting syntax](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FormattingSyntax.cs); [integer formatting](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.IntegerFormatting.cs). These cover implemented controls. | [POSIX awk][posix]; [GNU printf][printf] |
| Numbers and math | Supported within binary64/invariant-culture policy: decimal grammar, numeric coercion, nondecimal conversion/`strtonum` and math diagnostics. Full locale-aware and arbitrary-precision execution are excluded. Hexadecimal floating source literals remain planned. | [Numeric semantics](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.NumericSemantics.cs); [conversion](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.StrtonumConversion.cs); [numeric fields](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.NumericFields.cs). | [POSIX awk][posix]; [GNU conversion][conversion] |
| String units and byte mode | Supported: default string operations count Unicode scalars; byte mode counts UTF-8 bytes. Regex coordinates use the separate documented contract. Byte-mode FS="" requires ASCII. | [Text cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `supplementary-utf8-characters-count-as-characters`, `characters-as-bytes-string-functions`, `characters-as-bytes-empty-fs-non-ascii-is-unsupported`. | [GNU string functions][strings]; [UTF-8 policy](UTF8_POLICY.md) |
| Regex matching and captures | Partial: supported ERE matching, replacement, split and leftmost-longest/capture selection are covered. Inherited backend syntax and supplementary case/word differences remain documented compatibility boundaries. | [Regex cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `match-capture-array-metadata`; [longest matches](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.LongestRegex.cs); [profile cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RegexProfiles.cs). | [GNU ERE operators][ere]; [regex contract](REGEX_COMPATIBILITY.md) |
| Unicode escapes and warnings | Partial: GNU 5.4-style `\u` decoding is implemented in source, regex and command callers; invalid scalars follow the valid-UTF-8 policy. Special-variable Unicode warnings and invocation-wide ordinary string warning history are qualified; unknown-ERE warnings are qualified with shared per-character history. | [Source strings](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UnicodeStrings.cs); [regex](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UnicodeRegex.cs); [special assignments](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SpecialRegexAssignments.cs); [command callers](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UnicodeCallers.cs); [shared string warnings](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.StringEscapeWarnings.cs). | [GNU escapes][escapes]; [string contract](COMPATIBILITY_CONTRACT.md#source-string-escapes) |
| GNU split, substitution and sorting functions | Supported in GNU mode: gensub, patsplit, separator/capture arrays, asort/asorti and sorted traversal within the flat-array contract. | [Split captures](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SplitSeparators.cs); [sorting](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SortingCompatibility.cs); [substitution cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs). | [GNU strings][strings]; [GNU array sorting][sorting] |
| GNU bitwise and calendar functions | Supported within the documented unsigned-number and injected-time-zone contracts; native overflow/locale accidents are not reproduced. | [Bitwise cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.BitwiseCompatibility.cs); [calendar cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.CalendarNormalization.cs); [time cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `time-functions-explicit-utc`. | [GNU bitwise][bitwise]; [GNU time][time] |
| Introspection and virtual arrays | Partial: typeof/isarray, flat-array origins, SYMTAB/FUNCTAB/PROCINFO behavior and documented special-array differences are implemented; nested-array introspection waits for nested arrays. | [Special-array cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SpecialArrayReads.cs); [untyped cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UntypedState.cs). | [GNU type functions][types]; [special-array contract](COMPATIBILITY_CONTRACT.md#special-array-reads) |
| POSIX/traditional profiles and lint | Partial: name availability, special-variable effects, extension rejection and many warnings are implemented. General GNU lint coverage remains partial. These flags do not promise full conformance. | [Profile names](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ProfileNames.cs); [regex profiles](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RegexProfiles.cs); [frontend warnings](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FrontendWarnings.cs). | [GNU options][options]; [profile contract](COMPATIBILITY_CONTRACT.md#compatibility-profiles-and-names) |
| Accepted compatibility options and locale | Supported within the declared policy: optimization/interval flags are aliases; numeric-locale flags retain invariant conversion/formatting. POSIX byte-option warnings precede source/assignment failures and are retained by compiled executions. General GNU lint gaps remain in the profile row. | [Option contract goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.OptionCompatibility.cs); [option parser tests](../tests/Lokad.Awk.Tests/AwkCurrentSurface.CommandLineTests.cs); [compiled warning tests](../tests/Lokad.Awk.Tests/AwkCompiledWarningTests.cs). | [GNU options][options]; [alias contract](COMPATIBILITY_CONTRACT.md#command-line-compatibility-aliases) |

## Deferred Additions

The next release focuses on the supported surface above. These additions remain future
work; their current rejection diagnostics stay part of the contract.

| Area | Status and current behavior | Golden evidence | Reference |
| --- | --- | --- | --- |
| Arrays of arrays | Planned: `a[1][2]` is rejected; SUBSEP compound keys remain supported. | [Array cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `arrays-of-arrays-unsupported`, `multidimensional-array-subsep`. | [GNU nested arrays][nested] |
| Strongly typed regex values | Planned: `@/pattern/` is rejected. Ordinary regex literals and string patterns remain supported. | [Capability cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `strongly-typed-regex-unsupported`. | [GNU typed regex][typed] |
| Indirect calls | Planned: `@name(...)` is rejected. | [Capability cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `indirect-function-call-unsupported`. | [GNU indirect calls][indirect] |
| Namespaces | Planned: `@namespace` and qualified names are rejected; GNU `@include` works within portable host paths. | [Capability/source cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `namespace-directive-unsupported`, `namespace-qualified-identifier-unsupported`, `source-include-directive`. | [GNU namespaces][namespaces] |
| CSV mode | Planned: `-k`/`--csv` currently fail option parsing; quoted/multiline CSV parsing is not implemented. | [Contract cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.CompatibilityContract.cs): `csv-short-option-rejected`, `csv-long-option-rejected`. | [GNU CSV][csv] |
| Modern POSIX/GNU forms | Planned: minimized regex repetition, newer printf controls and hexadecimal floating source grammar. Current .NET lazy syntax and runtime numeric conversion do not establish these contracts. | [Longest-match controls](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.LongestRegex.cs); [printf controls](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FormattingSyntax.cs); [conversion controls](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.StrtonumConversion.cs). Feature acceptance coverage must accompany implementation. | [GNU minimized repetition][minimized]; [GNU printf][printf]; [GNU constants][constants] |
| POSIX `system()` | Planned host feature: calls fail with an explicit unsupported diagnostic. Existing one-way pipes do not imply system() support. | [Host-policy cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `system-function-unsupported`. | [POSIX awk][posix]; [GNU IO functions][io] |

## Intentional Exclusions And Differences

| Area | Status and current behavior | Golden evidence | Reference |
| --- | --- | --- | --- |
| GNU main-input directory skipping | Excluded for this release: an unhandled host open/read failure is fatal in every profile. Prior output survives, later operands stop and stdin fallback is not attempted. Existing BEGINFILE/nextfile recovery can explicitly skip an open failure; redirected getline remains recoverable. | [Contract cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.CompatibilityContract.cs): `main-directory-getline-preserves-output`, `main-directory-does-not-fall-back-to-stdin`; [input cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `input-directory-without-beginfile-fails`, `beginfile-missing-file-nextfile-suppresses-error`; [redirected/profile cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GetlineFailures.cs). | [GNU directories][directories]; [host limitation](COMPATIBILITY_CONTRACT.md#main-input-directories) |
| Arbitrary raw bytes and ambient locale | Excluded: strict UTF-8 ingress stays active under C locale/byte mode. Locale values do not select a codec; numerical conversion uses invariant culture. | [Byte-mode cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs); [strict ingress tests](../tests/Lokad.Awk.Tests/AwkHostReadFailureTests.cs). | [GNU locale][locales]; [UTF-8 policy](UTF8_POLICY.md) |
| Dynamic/native extensions | Excluded: `-l`/`--load` and `@load` are rejected; no native loading or default filesystem host. | [Capability cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `load-option-unsupported`, `load-directive-unsupported`. | [GNU dynamic extensions][dynamic]; [host contract](COMPATIBILITY_CONTRACT.md#unsupported-capabilities) |
| Coprocesses and network special files | Excluded: `|&` and `/inet` paths are rejected. One-way pipes use explicit host capabilities and policy. | [Host-policy cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `coprocess-output-unsupported`, `coprocess-getline-unsupported`, `getline-expression-unsupported-special-file-fails-after-prior-output`. | [GNU two-way IO][twoway]; [GNU special files][special] |
| Arbitrary precision, debugger and profiler | Excluded: binary64 is the value model; bignum/debug/profile switches report unsupported options. | [Option cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `bignum-option-unsupported`, `short-profile-option-unsupported`, `debug-option-unsupported`. | [GNU options][options]; [GNU arbitrary precision][precision] |
| Internationalization functions | Excluded: translated string markers and gettext-family functions are rejected; supplied ENVIRON remains ordinary host data. | [Capability cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs): `internationalized-string-marker-unsupported`, `dcgettext-function-unsupported`. | [GNU internationalization][i18n] |

This matrix does not turn an accepted flag into an implemented capability, a native crash
into required semantics, or an opt-in executable backend into the default engine. Changes
to any row require updated behavior evidence and the relevant public contract.

[posix]: https://pubs.opengroup.org/onlinepubs/9799919799/utilities/awk.html
[options]: https://www.gnu.org/software/gawk/manual/html_node/Options.html
[input]: https://www.gnu.org/software/gawk/manual/html_node/Reading-Files.html
[functions]: https://www.gnu.org/software/gawk/manual/html_node/User_002ddefined.html
[arrays]: https://www.gnu.org/software/gawk/manual/html_node/Arrays.html
[getline]: https://www.gnu.org/software/gawk/manual/html_node/Getline.html
[io]: https://www.gnu.org/software/gawk/manual/html_node/I_002fO-Functions.html
[printf]: https://www.gnu.org/software/gawk/manual/html_node/Format-Modifiers.html
[conversion]: https://www.gnu.org/software/gawk/manual/html_node/Conversion.html
[strings]: https://www.gnu.org/software/gawk/manual/html_node/String-Functions.html
[ere]: https://www.gnu.org/software/gawk/manual/html_node/Regexp-Operators.html
[escapes]: https://www.gnu.org/software/gawk/manual/html_node/Escape-Sequences.html
[sorting]: https://www.gnu.org/software/gawk/manual/html_node/Array-Sorting-Functions.html
[bitwise]: https://www.gnu.org/software/gawk/manual/html_node/Bitwise-Functions.html
[time]: https://www.gnu.org/software/gawk/manual/html_node/Time-Functions.html
[types]: https://www.gnu.org/software/gawk/manual/html_node/Type-Functions.html
[nested]: https://www.gnu.org/software/gawk/manual/html_node/Arrays-of-Arrays.html
[typed]: https://www.gnu.org/software/gawk/manual/html_node/Strong-Regexp-Constants.html
[indirect]: https://www.gnu.org/software/gawk/manual/html_node/Indirect-Calls.html
[namespaces]: https://www.gnu.org/software/gawk/manual/html_node/Namespaces.html
[csv]: https://www.gnu.org/software/gawk/manual/html_node/Comma-Separated-Fields.html
[minimized]: https://www.gnu.org/software/gawk/manual/html_node/Non_002dgreedy-operators.html
[constants]: https://www.gnu.org/software/gawk/manual/html_node/Nondecimal_002dnumbers.html
[directories]: https://www.gnu.org/software/gawk/manual/html_node/Command_002dline-directories.html
[locales]: https://www.gnu.org/software/gawk/manual/html_node/Locales.html
[dynamic]: https://www.gnu.org/software/gawk/manual/html_node/Dynamic-Extensions.html
[twoway]: https://www.gnu.org/software/gawk/manual/html_node/Two_002dway-I_002fO.html
[special]: https://www.gnu.org/software/gawk/manual/html_node/Special-Files.html
[precision]: https://www.gnu.org/software/gawk/manual/html_node/Arbitrary-Precision-Arithmetic.html
[i18n]: https://www.gnu.org/software/gawk/manual/html_node/Internationalization.html
