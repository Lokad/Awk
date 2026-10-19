# Regex engines and adapter

Lokad.Awk uses .NET regex by default and has no Lokad.Utf8Regex runtime dependency.
Applications can supply the small AWK-specific `IAwkRegexEngine` contract. The repository's
`Lokad.Awk.Utf8Regex` adapter is internal, non-packable, and currently pins producer 0.2.0.
Newer producer adoption remains a separate qualification decision; core work and releases
do not depend on it.

AWK pattern translation, longest-match/capture selection, replacement and UTF-8 boundary
rules belong to Awk. They are described in [compatibility](COMPATIBILITY.md#regex-semantics),
not delegated to the supplied engine. Some patterns or observable captures still use Awk's
managed selector or .NET fallback; selecting a UTF-8 engine does not make every path byte-native.

## Supplying an engine

```csharp
using Lokad.Awk;

static AwkExecutionPolicy WithApplicationEngine(IAwkRegexEngine engine) =>
    AwkExecutionPolicy.CreateDefault().WithRegexEngine(engine);
```

Implement the [engine contract](../src/Lokad.Awk/Regex/IAwkRegexEngine.cs):

- `TryCompile` receives translated .NET-compatible pattern text, invariant case choice,
  a borrowed active memory budget and the current cancellation token. Compilation must
  support concurrent callers. Return false only for unsupported syntax.
- Compiled handles consume valid UTF-8 spans synchronously. Searches start at a UTF-16
  offset while preserving complete-input assertion context. Fill every supplied capture
  slot, including unmatched groups, with absolute byte/UTF-16 coordinates. Awk validates ranges.
- Refused admission throws `AwkRegexAdmissionException`;
  cancellation propagates with the current token. These must not request fallback.

The application owns the engine; execution-local bounded caches own compiled handles and
dispose them on eviction or cleanup. Handle disposal is idempotent and non-throwing.
Retain no borrowed budget ownership, input/capture spans or per-call token. The adapter
adds no input or capture-text copies; producer internals and compatibility routes can allocate.

## Cooperative cancellation

`TryCompile`, `IsMatch` and `Match` each receive the current operation's
`CancellationToken`. Engines should poll it during construction, searching/backtracking and
result shaping. Compiled handles must not retain a construction or match token for later calls.
There is no match-timeout parameter or adapter-owned deadline.

The internal adapter still pins producer **0.2.0**, whose published API has no token-aware
constructors or matching methods. It checks the supplied token before/after producer calls
and during capture copying, and explicitly disables the producer's match clock. A long
producer call remains indivisible; these checks do not prove in-flight cancellation inside
that producer. Core and adapter remain buildable against this pin. A later token-aware
producer adoption needs its own qualification and must not block independent Awk work.

Caller/host cancellation owns deadlines. Await actual users before refunds; do not abandon
background work, independently match input chunks or add work quotas. Awk-owned POSIX selection
polls cancellation. The default .NET backend and .NET semantic fallbacks retain their existing
fixed timeout because those calls cannot poll Awk's token internally.

## Validation and measurements

[Host-engine tests](../tests/Lokad.Awk.Tests/AwkHostRegexTests.cs) cover ownership, malformed
ranges, fatal refusal and token forwarding into cooperative test-engine work. [Adapter tests](../tests/Lokad.Awk.Tests/AwkUtf8RegexAdapterTests.cs)
cover direct/fallback routes, coordinates, separators, replacements, no-decode paths,
current-token reuse and entry cancellation. Real producer in-flight cancellation remains
unqualified for the current pin.

`RegexDotNetTextMatches` counts .NET text operations; `RegexPatternGapFallbacks` identifies
adapter pattern/semantic fallbacks. `RegexStringAdapterMatches` counts string-based backend
calls, including structured selection, separately from text decoding. Use focused adapter
measurements with explicit source revisions; the [current baseline](BENCHMARKS.md) uses .NET.
