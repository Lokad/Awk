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
boundary validation and resource policy. Compiled handles belong to execution caches;
the application owns the stateless supplied engine. Input spans stay synchronous.
Cancellation currently checks around producer calls and during capture copying; it cannot
interrupt the producer calls themselves. In-flight cooperative cancellation is planned.
See [regex engines](../../docs/REGEX.md) for integration and cancellation status, and
[compatibility](../../docs/COMPATIBILITY.md#regex-semantics) for AWK semantics.
