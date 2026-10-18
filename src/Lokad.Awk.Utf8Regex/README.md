# Lokad.Awk.Utf8Regex

Optional UTF-8 regex adapter for Lokad.Awk, using pinned Lokad.Utf8Regex 0.2.0.
The core Awk package runs independently with .NET regex compatibility.

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
See the repository's regex compatibility document for qualified behavior and limitations.
