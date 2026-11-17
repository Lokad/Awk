# Agent notes

These instructions apply to the whole repository.

## Project and commands

Lokad.Awk embeds awk in .NET. Public integration uses `Awk.TryParse`, `Awk.ExecuteAsync`,
invocations, `IAwkHost`, descriptors/paths/results, `AwkExecutionPolicy`, `IMemoryBudget`
and `IAwkRegexEngine`. Read [architecture contracts](docs/ARCHITECTURE.md) before runtime changes.

- Use SDK 10.0.401 from `global.json` (patch roll-forward only), targeting net10.0.
- Build/test: `dotnet test .\Lokad.Awk.slnx -c Release`.
- Package build: `dotnet build .\src\Lokad.Awk\Lokad.Awk.csproj -c Release`.
- Counters: `dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks -- --counter-report`.
- Only `src/Lokad.Awk` packs. Tests/benchmarks compile the source adapter in `examples/`
  against its published provider. Preserve frozen release artifacts; publication is separate.

## Design boundaries

- Maintain one production interpreter. Replace displaced machinery progressively; temporary
  overlap needs removal milestones. No second backend, public engine selector or dynamic IL.
- Prepared/persisted programs are a non-goal; do not add a public compilation API.
- All I/O and effects belong to the host. No direct filesystem, process, console or environment access.
- Providers own regex matching, selection and engine admission. Core owns AWK translation,
  capture validation and replacements. No core matcher, fallback, regex quota or private timeout.
- Preserve bounded managed-stack use and cancellation responsiveness in input-dependent work.
  Poll computation regions, loop backedges, calls, hosts and long primitives; preserve checks
  after admission callbacks and before effects. No fuel or work quotas.
- Admit retained growth and mutation storage before publication. Refusal fails the command;
  owners and pending users retain funding until actual completion. Cleanup must retire them.
- Preserve strict UTF-8, byte-native processing, spelling and value origins. Keep the 24-byte
  scalar representation; verify layout changes rather than adding parallel carriers.
- A fast path needs a motivating program beyond the benchmark that exposed it.
- Dependencies are centrally versioned and allowlisted; do not add them casually.

## Implementation and validation

- Nullable and implicit usings are enabled. Prefer small internal types and explicit results.
  Do not use null-forgiving `!`; model absence. Validate arguments at public/shared boundaries.
- Use `ConfigureAwait(false)` where actual host suspension is possible. Completed ValueTasks
  are not inherently allocations; measure dispatch, conversion and ownership costs.
- Ordinary behavior belongs in canonical goldens. Use focused tests for binding, ownership,
  admission, cancellation and counters; see [test layers](tests/Lokad.Awk.Tests/README.md).
- Assert visible results, diagnostics, effects and cleanup, not historical checkpoint counts
  or execution routes. Qualify changed ownership with pending hosts and refused admission.
- Use affected workloads plus contrasting controls during development. Refresh complete
  benchmarks and Windows/Linux/package checks at coherent delivery milestones.
- Keep `docs/BENCHMARKS.md` a current snapshot (at most three tables, ratios and allocations).
  Identify measured revisions; archive experiments locally. Claim speedups only when measured.
- Document integration contracts and qualified differences; link ordinary awk semantics.
  Keep README examples aligned with public APIs and update affected docs with changes.
- Keep local plans, external reference sources and raw/history evidence ignored. Never commit
  `BenchmarkDotNet.Artifacts/` or private machine details.
