# Agent Notes

## Scope

These notes apply to the whole repository.

## What This Project Is

Lokad.Awk is an embeddable awk runtime for .NET. The public surface is intentionally small:
`Awk.TryParse`, `Awk.ExecuteAsync`, `AwkCommandInvocation`, `IAwkHost`, descriptors, paths, and host
result types. All IO is host-mediated; the runtime must not grow direct filesystem, process, console,
or environment dependencies.

## Commands

- Restore/build/test: `dotnet test .\Lokad.Awk.slnx`
- Package build checks: `dotnet build .\src\Lokad.Awk\Lokad.Awk.csproj -c Release`
- Runtime-counter evidence: `dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj -- --counter-report`
- Full benchmarks are opt-in and local-environment dependent. Do not commit `BenchmarkDotNet.Artifacts/`.

## Architecture Rules

- Keep the default execution engine as the interpreter. Executable/VM work stays opt-in.
  A default change needs golden parity, resource/cancellation parity, fallback counter evidence,
  representative benchmark gains and a rollback path for the same source revision.
- Preserve strict UTF-8 ingress. `AwkText` represents valid UTF-8 bytes; hot paths should stay
  byte-native and cross to `string` only at documented compatibility boundaries.
- Keep unsupported host capabilities explicit. Host failures must become awk-shaped diagnostics,
  not leaked CLR exception text.
- Resource-limit failures must preflight before observable mutation: arrays, fields, records,
  output, capture arrays, and VM stack growth should not partially mutate on failure.
- Do not add package dependencies casually. Package references are allowlisted in
  `Directory.Build.targets` and versioned centrally in `Directory.Packages.props`.

## C# Style

- Target `net10.0`; nullable reference types and implicit usings are enabled.
- Prefer file-scoped namespaces, small sealed/internal types, readonly record structs for tiny value
  objects, and explicit result/status types over nullable control flow.
- Do not use the null-forgiving operator. Treat each temptation to write `!` as a design smell:
  model absence with a value object, result type, `Try...` shape, or clear precondition.
- Use `ArgumentNullException.ThrowIfNull` at public or shared boundaries, not defensive null plumbing
  throughout hot paths.
- Async runtime paths use `Task`/`ValueTask` and `ConfigureAwait(false)`. Executable expressions are
  async-only where host or fallback behavior can be involved.
- Avoid unnecessary allocations in record, field, regex, formatting, array, and VM paths. If a change
  deliberately crosses a string boundary, update or add counter evidence.

## Tests

- Add ordinary awk behavior to the golden corpus first. It is the canonical user-visible behavior
  suite.
- Use narrower tests for parser/binder shape, source loading, resource limits, counters, VM internals,
  and host boundary behavior. See `tests/Lokad.Awk.Tests/README.md` before adding a new test layer.
- Diagnostics should assert exact stdout/stderr/exit behavior and no CLR exception leakage.
- For performance-sensitive runtime changes, add or refresh `AwkRuntimeCounters` assertions before
  relying on prose.

## Docs And Artifacts

- Keep README examples aligned with the actual public API. The package does not provide a default
  filesystem host; examples should make the `IAwkHost` boundary explicit.
- Update docs under `docs/` when changing UTF-8 policy, runtime limits, regex compatibility,
  executable rollout behavior, benchmark workflow, or release readiness assumptions.
- Maintain `docs/BENCHMARKS.md` as the single current comparison, with at most three tables,
  elapsed-time ratios and managed allocations. Present a performance snapshot: replace superseded
  rows and keep optimization timelines, before/after experiments and qualification logs local.
  Use affected workloads and contrasting controls
  during development; refresh all families at declared milestones. Keep retained measurement
  revisions/dates explicit; keep raw/history evidence local. Preserve full correctness checks.
- Leave local planning notes untracked unless they are intended as public project documentation.
