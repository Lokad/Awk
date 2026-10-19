# Language compatibility

Lokad.Awk supports ordinary awk text processing over valid UTF-8 with host-mediated I/O.
This is the current supported product, not full POSIX/GNU conformance. The interpreter is the only execution engine. Regex defaults to .NET; applications can
explicitly supply a regex engine.

**Supported** means the listed behavior has golden coverage within its documented boundary.
**Partial** identifies implemented behavior with remaining differences. **Deferred** means
unsupported, without a delivery commitment. Passing rejection tests is not feature support.
The [compatibility contract](COMPATIBILITY_CONTRACT.md) defines cross-cutting behavior.

## Current surface

| Area | Status and boundary | Evidence / detailed contract |
| --- | --- | --- |
| Sources and command arguments | Supported: inline/repeated sources, `-f`, `-i`, `-F`, `-v`, includes, assignment operands and ARGV scheduling, within host/policy authority. | [Source cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs), [ARGV](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ArgvCompatibility.cs) |
| Expressions and statements | Supported: arithmetic, concatenation, assignments, conditionals, loops and switch; profile/name rules apply. | [Grammar](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GrammarBoundaries.cs), [contract](COMPATIBILITY_CONTRACT.md#compatibility-profiles-and-names) |
| Rules and input phases | Supported: ordinary/range patterns, BEGIN/END, next/nextfile and GNU BEGINFILE/ENDFILE, with phase restrictions. | [Function flow](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FunctionFlow.cs), [record semantics](RECORD_SEMANTICS.md) |
| User functions | Supported: recursion, local scalar parameters, missing arguments and forwarded array aliases; depth/resource limits apply. | [Untyped values](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UntypedState.cs), [limits](RUNTIME_LIMITS.md) |
| Scalars and flat arrays | Supported: numeric strings/origins, associative and SUBSEP compound keys, membership, delete and array length. Compound keys are not nested arrays. | [Value contract](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting), [goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs) |
| Records and fields | Supported: FS/FPAT/FIELDWIDTHS, character/regex/paragraph RS, RT, lazy fields and NF mutation. Captured settings and CRLF paragraph rules are explicit. | [Field modes](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FieldModes.cs), [record semantics](RECORD_SEMANTICS.md) |
| Getline | Supported: main/redirected/one-way pipe expressions with scalar, array or field destinations. Redirected I/O errors can return -1; unsupported/policy/resource failures are fatal. Direct postfix field destinations are rejected. | [Destinations](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GetlineDestinations.cs), [failures](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.GetlineFailures.cs) |
| Output and streams | Supported through the host: print/printf, file/one-way pipe redirection, close and logical flush. | [Flush cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FlushCompatibility.cs), [host contract](COMPATIBILITY_CONTRACT.md#host-io-and-getline) |
| Formatting | Partial: common conversions, `%a`/`%A`, positional arguments, width/precision and integer rules. `%C`/`%S`, D/H/w controls and `%P` remain unsupported. | [Formatting](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FormattingSyntax.cs), [integers](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.IntegerFormatting.cs) |
| Numbers and math | Supported within binary64/invariant-culture policy: numeric coercion, math diagnostics, nondecimal conversion and strtonum. Hexadecimal floating source literals are deferred. | [Numbers](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.NumericSemantics.cs), [strtonum](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.StrtonumConversion.cs) |
| Strings and escapes | Supported scalar/byte units within strict UTF-8. GNU lowercase `\u` and numeric-byte escapes work with profile-aware warnings; locale/invalid-byte emulation is excluded. | [UTF-8 policy](UTF8_POLICY.md), [escape goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.NumericByteEscapes.cs) |
| Regex | Partial: ERE matching, longest/capture selection, replacement, split and boundary checks. Supplementary folding, inherited backend syntax and modern minimized forms have explicit limits. Core is independent of the optional Utf8Regex adapter. | [Regex contract](REGEX_COMPATIBILITY.md), [longest matches](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.LongestRegex.cs) |
| GNU string/array helpers | Supported in GNU mode: gensub, patsplit, separator/capture arrays, asort/asorti and sorted traversal within flat arrays. | [Splitting](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SplitSeparators.cs), [sorting](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SortingCompatibility.cs) |
| Bitwise and calendar helpers | Supported within unsigned-number and policy clock/time-zone behavior, not every native overflow/locale quirk. | [Bitwise](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.BitwiseCompatibility.cs), [calendar](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.CalendarNormalization.cs) |
| Introspection | Partial: typeof/isarray, SYMTAB/FUNCTAB/PROCINFO and flat-array origins. Special-array writes/reads have explicit GNU differences; nested arrays are unsupported. | [Special arrays](COMPATIBILITY_CONTRACT.md#special-array-reads), [goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SpecialArrayReads.cs) |
| Profiles and flags | Partial: POSIX/traditional name/effect restrictions, lint and warnings. Optimization/interval flags are aliases; numeric-locale flags retain invariant conversion. No full profile-conformance claim. | [Profile cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ProfileNames.cs), [options](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.OptionCompatibility.cs) |

## Deferred capabilities

CSV is provisionally first, nested arrays next, subject to a motivating script and explicit
acceptance criteria. Other extensions remain demand-driven; they do not block the supported core.

| Capability | Current behavior |
| --- | --- |
| CSV (`-k`/`--csv`) | Rejected. Literal comma splitting does not handle quoted or multiline CSV; preprocess in the host. |
| Nested arrays (`a[1][2]`) | Rejected; `a[1,2]` SUBSEP keys work. |
| `system()` | Explicit unsupported diagnostic; one-way pipes do not imply support. |
| Namespaces | `@namespace` and qualified identifiers are rejected; host-mediated `@include` works. |
| Typed regex / indirect calls | `@/pattern/` and `@name(...)` are rejected. |
| Newer language forms | Modern minimized regex repetitions, newer printf controls and hexadecimal floating source grammar are not qualified. |

Current rejection evidence is in the [golden corpus](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs)
and [contract cases](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.CompatibilityContract.cs).

## Intentional boundaries

- All I/O requires the host. Native/dynamic loading, coprocesses, `/inet` special files,
  debugger/profiler switches and gettext-style internationalization are excluded.
- Text must be valid UTF-8, even under C locale or byte mode. Arbitrary precision,
  ambient locale codecs/numeric rules and general locale collation are excluded.
- Unhandled main-input errors, including directories, are fatal. There is no automatic
  directory skipping; BEGINFILE/nextfile can recover explicitly. See
  [main input policy](COMPATIBILITY_CONTRACT.md#main-input-directories).
- Evaluation order, numeric formatting, Unicode folding and special-array behavior have
  documented GNU differences. Reference crashes/data loss are not required semantics.

The [golden corpus](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs) defines observable behavior;
[test layers](../tests/Lokad.Awk.Tests/README.md) add host/resource/counter checks. GNU oracle
checks are opt-in through `LOKAD_AWK_ORACLE_EXE`; ordinary tests require no GNU executable.
Reference version, matcher, locale and zone matter. The
[POSIX specification](https://pubs.opengroup.org/onlinepubs/9799919799/utilities/awk.html)
and [GNU manual](https://www.gnu.org/software/gawk/manual/html_node/) describe the broader
languages, not additional Lokad support. See [benchmarks](BENCHMARKS.md) for measured workloads.
