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
capture ranges, owns its compiled handles and per-call admission, and adds no input/text copies.
Compilation and each search receive the current cancellation token through the producer's
cooperative APIs. The compilation token is not retained by a reusable handle. Producer match
and construction timeouts are disabled; no adapter clock or core fallback is involved.
See [runtime responsibilities](../docs/RUNTIME.md#regex-engines).
