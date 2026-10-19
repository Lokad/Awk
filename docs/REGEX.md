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
  a legacy match timeout, a borrowed active memory budget and the current checkpoint. Compilation
  must support concurrent callers. Return false only for unsupported syntax.
- Compiled handles consume valid UTF-8 spans synchronously. Searches start at a UTF-16
  offset while preserving complete-input assertion context. Fill every supplied capture
  slot, including unmatched groups, with absolute byte/UTF-16 coordinates. Awk validates ranges.
- Refused admission throws `AwkRegexAdmissionException`, match expiry throws
  `RegexMatchTimeoutException`, and cancellation propagates. These must not request fallback.

The application owns the engine; execution-local bounded caches own compiled handles and
dispose them on eviction or cleanup. Handle disposal is idempotent and non-throwing.
Retain no borrowed budget ownership, input/capture spans or per-call checkpoint. The adapter
adds no input or capture-text copies; producer internals and compatibility routes can allocate.

## Cooperative cancellation

The current adapter checks cancellation before/after producer construction and matching,
and while copying capture metadata. It does **not** pass cancellation into those producer
calls. A long constructor, search or backtrack therefore cannot be interrupted by these
checks. Existing adapter tests cover entry cancellation and callback lifetime, not cancellation
inside producer work. A match timeout is not a substitute for caller cancellation.

The required next step is to forward cancellation through producer construction, searching,
backtracking and result shaping, and remove the adapter's timeout argument/settings. Use the
existing AWK contract with the smallest signature adjustment needed to pass cancellation.
The adapter must not impose an independent deadline. Each operation receives current cancellation;
cached patterns must not retain a construction token or callback for later calls. Qualification
can use producer source before a supporting package is published. This capability is planned,
not a guarantee of the current pinned adapter.

The caller/host owns deadline cancellation; preserve admission failures, cleanup and
unpublished-result atomicity. Do not simulate cooperation by abandoning background
work, splitting input into independently matched chunks, or introducing work quotas. Awk-owned
POSIX selection already polls cancellation; .NET matching retains timeout protection because
it cannot poll Awk's token internally. See [runtime limits](RUNTIME_LIMITS.md).

## Validation and measurements

[Host-engine tests](../tests/Lokad.Awk.Tests/AwkHostRegexTests.cs) cover ownership, malformed
ranges, fatal refusal, cancellation and timeout. [Adapter tests](../tests/Lokad.Awk.Tests/AwkUtf8RegexAdapterTests.cs)
cover direct/fallback routes, coordinates, separators, replacements and no-decode paths;
in-flight cancellation coverage remains to be added.

`RegexDotNetTextMatches` counts .NET text operations; `RegexPatternGapFallbacks` identifies
adapter pattern/semantic fallbacks. `RegexStringAdapterMatches` counts string-based backend
calls, including structured selection, separately from text decoding. Use focused adapter
measurements with explicit source revisions; the [current baseline](BENCHMARKS.md) uses .NET.
