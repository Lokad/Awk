# Lokad.Awk 0.3.0 release qualification

Qualified source: `5dc6544b6c55098dfd6669218ac8e21bee3fc672`, local tag `v0.3.0`.
The package and tag are prepared locally; external publication is pending.
See [release notes](RELEASE_NOTES_0.3.0.md) for scope and compatibility boundaries.

## Exact package identity

The candidate is built from a clean checkout with .NET SDK 10.0.401, runtime 10.0.12,
on Ubuntu 24.04.4. The same package is consumed on Windows and Linux.

| Artifact | SHA-256 |
| --- | --- |
| Lokad.Awk.0.3.0.nupkg | CC730EC0097C6641006D4F3EA99BE816EC2AAE7A9FF7F002B154BA3CD679DEDE |
| Lokad.Awk.0.3.0.snupkg | 9B0B87FC78B523C23178241B5528CB094241C66A4B12EB48B91FC75B6B18C34F |
| Packaged/consumed Lokad.Awk.dll | DD26C6B977CEB1C556140FE85D4D3C70E52E3FF7EA332F29B72901F0C80B491F |

Nuspec, assembly version/informational revision and portable-PDB SourceLink identify
0.3.0 and the qualified commit. README, license, notices, icon, changelog and XML API
documentation are present. The only runtime package dependencies are Lokad.Parsing
1.1.0 and Lokad.Utf8Regex 0.2.0; SourceLink tooling is private to the build.

## Validation

- Runtime change `cebddb04e4d3`: Windows/Linux Debug/Release each pass 71,497 tests.
- Final source `5dc6544b6c55`: Linux Release passes 71,497 tests with locked restore.
- Actual pinned-package consumers: Windows Release and Linux Release each pass all
  71,497 tests. Consumer projects contain package references and no library project
  references; consumed DLL hashes equal the packaged and source-built DLL.
- All 44 generated reference cases agree exactly with GNU awk 5.2.1: stdout, stderr
  and exit behavior. The final-source report identifies a clean checkout and the
  qualified source/assembly hashes.
- All 31 Linux runtime-counter rows match the preceding qualified runtime, including
  finite-budget memory peaks, byte/string boundaries and VM observations.
- Representative before/after throughput/allocation evidence and passed quiet gates
  are retained in the [processing report](BENCHMARK_SUMMARY_2026-10-12_PROCESSING.md).

The final increment changes package metadata/documentation after the measured runtime;
it does not change execution code. Interpreter and DotNetCompatibility remain the
defaults, both presets have no implicit fuel, and Utf8Regex adoption remains postponed.
The completed stack, regex-preparation and protection qualification remains documented
in the linked [release notes](RELEASE_NOTES_0.3.0.md).

## Remaining release action

Push the qualified source and local tag, then publish the exact package/symbol artifacts
through the project's release workflow when authorized. SourceLink resolves against
that commit once the source is pushed. A rebuilt artifact needs its own identity and
consumer checks; do not substitute another same-version package silently.
