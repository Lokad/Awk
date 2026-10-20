# Lokad.Awk.Utf8Regex

Internal UTF-8 regex adapter for Lokad.Awk qualification, using pinned Lokad.Utf8Regex 0.2.0.
This project is non-packable and is not distributed as a NuGet package. Repository tests
reference the project directly. The core Awk package runs independently with .NET regex compatibility.

```csharp
using Lokad.Awk;
using Lokad.Awk.Utf8Regex;

var policy = AwkExecutionPolicy.CreateDefault()
    .WithRegexEngine(AwkUtf8RegexEngine.Instance);
// Execute the parsed Awk tool with your IAwkHost, policy and cancellation token.
```

Awk retains pattern translation, longest-match/capture selection, replacement, UTF-8
boundary validation and ordinary text/capture admission. The adapter funds producer construction,
compiled storage and synchronous match scratch through the borrowed budget; core supplies no
pattern/group ceilings. Refused admission fails execution rather than requesting fallback.
Compiled handles belong to bounded execution caches;
the application owns the stateless supplied engine. Input spans stay synchronous.
Every operation receives the current cancellation token; cached handles retain no operation
token. The adapter has no independent match timeout. Pinned producer 0.2.0 has no token-aware
APIs, so cancellation checks surround its calls and run during capture copying; they cannot
interrupt work inside a producer call. A future cooperative producer can use the same contract
after separate qualification. No producer upgrade is required by the core package.
See [runtime regex integration](../../docs/RUNTIME.md#regex-engines) for integration and cancellation status, and
[compatibility](../../docs/COMPATIBILITY.md#regex-semantics) for AWK semantics.
