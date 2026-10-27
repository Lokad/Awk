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

The example targets published **0.2.0** for integration testing. It is **not yet a qualified
AWK provider**: ordered alternation, POSIX capture priority and supplementary Unicode behavior
have known gaps. The canonical tests retain the required AWK results. Do not infer conformance
from successful compilation or recreate selection logic in the adapter.

The contract is defined by Awk, without producer types. The adapter passes UTF-8 spans and
capture ranges, owns its compiled handles and per-call admission, and adds no input/text copies.
The current package has no token-aware calls; checks before/after calls and during capture
copying preserve the token seam but cannot interrupt a producer call. Adopt cooperative APIs
when a published package provides them, then qualify the adapter independently. No adapter
clock or core fallback is involved. See [runtime responsibilities](../docs/RUNTIME.md#regex-engines).
