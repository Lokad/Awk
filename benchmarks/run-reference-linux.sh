#!/usr/bin/env bash
set -euo pipefail

repository_root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
sdk_root="${AWK_BENCH_DOTNET_ROOT:-$HOME/awk-bench-toolchains/dotnet/10.0.401}"
reference="${AWK_BENCH_GAWK:-/usr/bin/gawk}"
expected_reference_sha="${AWK_BENCH_GAWK_SHA256:-9150b42e78b22231bd83131e1db2530a7a7fa0677e189ac7c5cc1520c42c9e88}"
export DOTNET_ROOT="$sdk_root"
export PATH="$sdk_root:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export LC_ALL=C
export TZ=UTC
cd -- "$repository_root"
if [[ -f "${repository_root}.json" && -z "${AWK_BENCH_SNAPSHOT_MANIFEST:-}" ]]; then
    export AWK_BENCH_SNAPSHOT_MANIFEST="${repository_root}.json"
fi

if [[ "$("$sdk_root/dotnet" --version)" != "10.0.401" ]]; then
    echo "Expected pinned .NET SDK 10.0.401." >&2
    exit 2
fi
runtime_info=$("$sdk_root/dotnet" --list-runtimes)
if [[ "$runtime_info" != *"Microsoft.NETCore.App 10.0.12 "* ]]; then
    echo "Expected pinned .NET runtime 10.0.12." >&2
    exit 2
fi
actual_reference_sha=$(sha256sum -- "$reference")
if [[ "${actual_reference_sha%% *}" != "$expected_reference_sha" ]]; then
    echo "GNU awk binary differs from the qualified toolchain identity." >&2
    exit 2
fi

# Keep restore/build separate from machine qualification and measurement windows.
if [[ "${1:-}" == "build" ]]; then
    exec "$sdk_root/dotnet" build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
fi
if [[ "${1:-}" == "driver-smoke" ]]; then
    exec "$sdk_root/dotnet" exec --fx-version 10.0.12 benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --driver-smoke --awk "$reference"
fi
if [[ "${1:-}" == "check-machine" ]]; then
    exec "$sdk_root/dotnet" exec --fx-version 10.0.12 benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
fi
if [[ "${1:-}" != "verify" && "${1:-}" != "qualify" ]]; then
    echo "Usage: bash benchmarks/run-reference-linux.sh build|driver-smoke|check-machine|verify|qualify [comparison options]" >&2
    exit 2
fi
mode="$1"
shift
exec "$sdk_root/dotnet" exec --fx-version 10.0.12 benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll \
    --compare --awk "$reference" --mode "$mode" "$@"
