# Changelog

Before 1.0, minor releases may change public APIs.

## 0.5.0

- **CSV input.** `--csv` (`-k`) handles quotes, embedded newlines, CRLF and
  two-argument `split()`.
- **Nested arrays.** Use `a[i][j]`, pass child arrays to functions, and iterate
  or delete nested entries.
- **Compatibility.** Fix parsing of `getline`, field expressions and regex literals.
  Preserve numeric-string spelling through more array and function operations and
  correct regex replacement escapes. [Supported behavior and limits](docs/COMPATIBILITY.md)
- **Performance.** The interpreter reduces dispatch, field copying and value
  allocation. [Measured workloads and allocations](docs/BENCHMARKS.md)

### Upgrading from 0.4.0

- **Files.** `OpenFileAsync` receives an `AwkPathRequest` with original path text,
  working directory and any required root. Authorize and resolve it in the host,
  then return the descriptor and canonical path. Use a `NotFound` failure for
  missing files. Path helper types and legacy open statuses have been removed.
- **Streams.** Replace `ReadLinesAsync` with bounded `ReadAsync` and a disposable
  `AwkReadBuffer`. Keep bytes stable until disposal and report the consumed prefix.
  `AppendAsync` returns `AwkAppendResult` for full acceptance or a terminal outcome.
- **Policy.** Pass a shared `IMemoryBudget` to `CreateRestricted(budget)`.
  `AwkMemoryBudget.CreateFixed(bytes)` provides a standalone budget.
  Use caller cancellation for deadlines. `WithInternalFailureObserver` can observe
  unexpected interpreter errors.
- **Regex providers.** `TryCompile` takes a UTF-8 span and returns
  `AwkRegexCompilationStatus` with an owned handle on success. Use `CreateSearch`
  for a scoped immutable subject and match using absolute byte offsets.
  AWK reports match and capture positions in Unicode scalars by default.
  The example adapter still has POSIX selection gaps.

See the [runtime contract](docs/RUNTIME.md) for host and provider obligations and the
[copyable adapter](examples/Utf8RegexAdapter.cs) for an integration example.

## 0.4.0

Reworked host command and descriptor APIs, required a host-supplied regex provider,
and moved aggregate admission and deadlines to shared memory budgets and caller
cancellation. Corrected invocation, source loading, streaming and formatting behavior.
[NuGet](https://www.nuget.org/packages/Lokad.Awk/0.4.0) · [Migration notes](https://github.com/Lokad/Awk/blob/v0.4.0/CHANGELOG.md#040)

## 0.3.0

Expanded core awk behavior, fixed command-pipe close and flush, defined recoverable
`getline` failures, and removed execution fuel and the optional bytecode engine.
[NuGet](https://www.nuget.org/packages/Lokad.Awk/0.3.0) · [Migration notes](https://github.com/Lokad/Awk/blob/v0.3.0/CHANGELOG.md#030---2026-10-19)

## 0.2.0

Expanded awk compatibility, streamed input with dynamic record separators, and
added structured host outcomes and restricted execution.
[Release notes](https://github.com/Lokad/Awk/blob/e4126b39aaa4cbc101a6b9fa7b38fe9850e2fff0/CHANGELOG.md#020)

## 0.1.0

Initial standalone release.
