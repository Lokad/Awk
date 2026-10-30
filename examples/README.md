# Regex provider example

Copy [Utf8RegexAdapter.cs](Utf8RegexAdapter.cs) into the embedding application and reference
Lokad.Awk plus the chosen published Lokad.Utf8Regex package. Core references neither the
producer nor this adapter. Tests and benchmark tools compile the same source, pinned centrally
in `Directory.Packages.props`; they do not use a sibling producer checkout.

```csharp
using Lokad.Awk;
using Lokad.Awk.Examples;

var policy = AwkExecutionPolicy.CreateDefault()
    .WithRegexEngine(AwkUtf8RegexEngine.Instance);
var exit = await awk.ExecuteAsync(host, policy, cancellationToken);
```

The example targets published **0.4.0** for integration testing. Unicode-scalar matching is
qualified by the active tests. It is **not yet a qualified AWK provider**: POSIX longest-match
and subcapture selection still have known gaps. The canonical tests retain the required AWK
results. Do not infer conformance from successful compilation or recreate selection logic
in the adapter. The separate Pcre2 and PythonRe profiles are not used by this AWK adapter.

The contract is defined by Awk, without producer types. The adapter passes UTF-8 spans and
capture ranges, owns its compiled handles and per-call admission, and adds no subject or
capture-text copies. `TryCompile` borrows UTF-8 pattern bytes. Published 0.4.0 requires a
string pattern, so the adapter decodes once per compilation after admission. This temporary
conversion belongs to the adapter and is outside core conversion counters.
Compilation and each search receive the current cancellation token through the producer's
cooperative APIs. The compilation token is not retained by a reusable handle. Producer match
and construction timeouts are disabled; no adapter clock or core fallback is involved.
The published API has no shared-budget hook. Handle and search reservations are coarse
adapter estimates, not complete coverage of engine allocations. Range-only searches use the
provider's value-match API; capture searches map defined provider groups to dense AWK slots.
See [runtime responsibilities](../docs/RUNTIME.md#regex-engines).
