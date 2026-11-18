#!/usr/bin/env bash
set -euo pipefail

repository_root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
cd -- "$repository_root"
sdk_version=$(python3 -c 'import json; print(json.load(open("global.json"))["sdk"]["version"])')
runtime_version=10.0.12
if [[ -n "${AWK_BENCH_DOTNET_ROOT:-}" ]]; then
    dotnet="$AWK_BENCH_DOTNET_ROOT/dotnet"
elif ! dotnet=$(command -v dotnet); then
    echo 'Put dotnet on PATH or set AWK_BENCH_DOTNET_ROOT.' >&2
    exit 2
fi
dotnet=$(readlink -f -- "$dotnet")
sdk_root=$(dirname -- "$dotnet")
reference="${AWK_BENCH_GAWK:-/usr/bin/gawk}"
expected_reference_sha="${AWK_BENCH_GAWK_SHA256:-9150b42e78b22231bd83131e1db2530a7a7fa0677e189ac7c5cc1520c42c9e88}"
export DOTNET_ROOT="$sdk_root"
export PATH="$sdk_root:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export LC_ALL=C
export TZ=UTC
if [[ -f "${repository_root}.json" && -z "${AWK_BENCH_SNAPSHOT_MANIFEST:-}" ]]; then
    export AWK_BENCH_SNAPSHOT_MANIFEST="${repository_root}.json"
fi

if [[ "$("$dotnet" --version)" != "$sdk_version" ]]; then
    echo "Expected pinned .NET SDK $sdk_version from global.json." >&2
    exit 2
fi
runtime_info=$("$dotnet" --list-runtimes)
if [[ "$runtime_info" != *"Microsoft.NETCore.App $runtime_version "* ]]; then
    echo "Expected pinned .NET runtime $runtime_version." >&2
    exit 2
fi
actual_reference_sha=$(sha256sum -- "$reference")
if [[ "${actual_reference_sha%% *}" != "$expected_reference_sha" ]]; then
    echo "GNU awk binary differs from the qualified toolchain identity." >&2
    exit 2
fi

# Keep restore/build separate from machine qualification and measurement windows.
project=benchmarks/Lokad.Awk.Benchmarks
driver=("$dotnet" exec --fx-version "$runtime_version" "$project/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll")
case "${1:-}" in
    build) exec "$dotnet" build "$project/Lokad.Awk.Benchmarks.csproj" -c Release -p:RestoreLockedMode=true ;;
    driver-smoke) exec "${driver[@]}" --driver-smoke --awk "$reference" ;;
    check-machine) exec "${driver[@]}" --check-machine ;;
    verify)
        shift
        exec "${driver[@]}" --compare --awk "$reference" --mode verify "$@"
        ;;
    catalog)
        case "${2:-}" in
            gnu)
                catalog=(--reference-benchmarks --awk "$reference")
                filter='*ReferenceAwkBenchmarks.GnuAwkCli*'
                ;;
            managed)
                catalog=()
                filter='*WorkloadAllocationBenchmarks.LokadFullInvocation*'
                ;;
            *) echo 'Expected catalog gnu or catalog managed.' >&2; exit 2 ;;
        esac
        shift 2
        if [[ "$#" == 0 ]]; then
            set -- --filter "$filter"
        fi
        exec "${driver[@]}" "${catalog[@]}" "$@"
        ;;
    *)
        echo 'Usage: bash benchmarks/run-reference-linux.sh build|driver-smoke|check-machine|verify [options]|catalog gnu|managed [BenchmarkDotNet options]' >&2
        exit 2
        ;;
esac
