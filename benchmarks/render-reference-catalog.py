#!/usr/bin/env python3
"""Render paired BenchmarkDotNet medians and allocations from verified fixtures."""

import argparse
import json
import math
from pathlib import Path
import sys
from urllib.parse import parse_qsl


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


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
        parameters = dict(parse_qsl(row["Parameters"], strict_parsing=True))
        key = parameters["Workload"], parameters["Scale"]
        stats = row["Statistics"]
        if key in timings or key not in stamps or stats is None:
            raise ValueError(f"Missing or duplicate measurement evidence for {key} in {report.name}")
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
        native_ns, _, native_stamp = gnu[key]
        managed_ns, memory, managed_stamp = managed[key]
        for stamp in (native_stamp, managed_stamp):
            if stamp["LibraryBuild"] != build or stamp["Workload"] != fixtures[key]:
                raise ValueError(f"Build or fixture stamp differs from the verification report for {key}")
        if native_stamp["ReferenceSha256"] != reference["Sha256"] or not memory:
            raise ValueError(f"Missing matching GNU identity or managed allocation evidence for {key}")
        rows.append((key, fixtures[key]["InputBytes"], managed_ns, native_ns, memory["BytesAllocatedPerOperation"]))

    print(f"Core build `{build}` compared with {reference['Version']}.\n")
    print("| Case | Scale | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |")
    print("| --- | --- | ---: | ---: | ---: | ---: | ---: |")
    for (name, scale), size, managed_ns, native_ns, allocated in sorted(rows, key=lambda row: -row[2] / row[3]):
        print(f"| {name} | {scale} | {size / 1024:.2f} | {managed_ns / 1e6:.3f} | {native_ns / 1e6:.3f} | "
              f"{managed_ns / native_ns:.2f}× | {allocated / 2**20:.3f} |")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("verification", "gnu_report", "gnu_log", "managed_report", "managed_log"):
        parser.add_argument(name, type=Path)
    try:
        render(parser.parse_args())
    except (OSError, KeyError, TypeError, ValueError) as error:
        parser.exit(2, f"Cannot render reference catalog: {error}\n")
