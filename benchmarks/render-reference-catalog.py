#!/usr/bin/env python3
"""Render paired BenchmarkDotNet medians and allocations from verified fixtures."""

import argparse
import json
import math
from pathlib import Path
import re
import sys


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def workload_identity(row):
    # BenchmarkDotNet truncates display parameters. FullName retains the fixture names.
    match = re.fullmatch(r'[^()]+\(Scale: "([^"]+)", Workload: "([^"]+)"\)', row["FullName"])
    if match is None:
        raise ValueError(f"Cannot read workload identity from {row['FullName']}")
    scale, name = match.groups()
    return name, scale


def load_timings(report, log, expected_type, expected_method):
    stamps = {}
    for line in log.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("Awk benchmark "):
            stamp = json.loads(line.removeprefix("Awk benchmark "))
            workload = stamp["Workload"]
            key = workload["Name"], workload["Scale"]
            if key in stamps and stamps[key] != stamp:
                raise ValueError(f"Conflicting fixture stamps for {key} in {log.name}")
            stamps[key] = stamp

    timings = {}
    for row in read_json(report)["Benchmarks"]:
        if (row["Type"], row["Method"]) != (expected_type, expected_method):
            raise ValueError(f"Unexpected method in {report.name}: {row['FullName']}")
        key = workload_identity(row)
        stats = row["Statistics"]
        if key in timings or (stats is not None and key not in stamps):
            raise ValueError(f"Missing or duplicate measurement evidence for {key} in {report.name}")
        if stats is None:
            timings[key] = None
            continue
        median = stats["Median"]
        if not math.isfinite(median) or median <= 0:
            raise ValueError(f"Invalid elapsed-time median for {key} in {report.name}")
        timings[key] = median, row.get("Memory"), stamps[key]
    return timings


def render(args):
    verified = read_json(args.verification)
    if (verified["Mode"] != "verify" or not verified["Complete"] or not verified["Reference"] or
            verified["RequestedCases"] != len(verified["Cases"])):
        raise ValueError("Supply a complete native verification report.")
    fixtures = {}
    for row in verified["Cases"]:
        workload, check = row["Workload"], row["Verification"]
        key = workload["Name"], workload["Scale"]
        if key in fixtures or check["Status"] != "Equivalent" or not (
                check["LibraryOutput"] == check["NativeOutput"] == workload["ExpectedOutput"]):
            raise ValueError(f"Unverified fixture {key}")
        fixtures[key] = workload

    gnu = load_timings(args.gnu_report, args.gnu_log, "ReferenceAwkBenchmarks", "GnuAwkCli")
    managed = load_timings(args.managed_report, args.managed_log, "WorkloadAllocationBenchmarks", "LokadFullInvocation")
    if not gnu or gnu.keys() != managed.keys() or not gnu.keys() <= fixtures.keys():
        raise ValueError("GNU and managed reports need the same verified cases.")

    build = verified["Host"]["LibraryBuild"]
    if not build or build == "Unavailable":
        raise ValueError("Missing core build identity in the verification report.")
    reference = verified["Reference"]
    rows = []
    for key in gnu:
        native = gnu[key]
        lokad = managed[key]
        for timing in (native, lokad):
            if timing and (timing[2]["LibraryBuild"] != build or timing[2]["Workload"] != fixtures[key]):
                raise ValueError(f"Build or fixture stamp differs from the verification report for {key}")
        if (native and native[2]["ReferenceSha256"] != reference["Sha256"]) or (lokad and not lokad[1]):
            raise ValueError(f"Missing matching GNU identity or managed allocation evidence for {key}")
        native_ns = native[0] if native else None
        managed_ns = lokad[0] if lokad else None
        allocated = lokad[1]["BytesAllocatedPerOperation"] if lokad else None
        ratio = managed_ns / native_ns if native_ns is not None and managed_ns is not None else None
        rows.append((key, fixtures[key]["InputBytes"], managed_ns, native_ns, allocated, ratio))

    def cell(value, divisor=1, digits=3, suffix=""):
        return "—" if value is None else f"{value / divisor:.{digits}f}{suffix}"

    print(f"Core build `{build}` compared with {reference['Version']}.\n")
    print("| Case | Scale | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |")
    print("| --- | --- | ---: | ---: | ---: | ---: | ---: |")
    for (name, scale), size, managed_ns, native_ns, allocated, ratio in sorted(rows, key=lambda row: -(row[5] or 0)):
        print(f"| {name} | {scale} | {size / 1024:.2f} | {cell(managed_ns, 1e6)} | {cell(native_ns, 1e6)} | "
              f"{cell(ratio, digits=2, suffix='×')} | {cell(allocated, 2**20)} |")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("verification", "gnu_report", "gnu_log", "managed_report", "managed_log"):
        parser.add_argument(name, type=Path)
    try:
        render(parser.parse_args())
    except (OSError, KeyError, TypeError, ValueError) as error:
        parser.exit(2, f"Cannot render reference catalog: {error}\n")
