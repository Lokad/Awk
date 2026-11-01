# Benchmarks

Current comparison with **GNU awk 5.3.2**. The source column gives the measured revision
and UTC date in 2026; retained rows are **not reruns of the latest source**. All measurements
use Release, the interpreter, SDK 10.0.401/.NET 10.0.12, C locale and UTC on the quiet Linux
machine. Regex cases use the source adapter with published Utf8Regex 0.4.0; the 47 provider
selection deferrals remain separate from these passing workloads.

Times are median milliseconds per complete invocation; **Lokad.Awk/awk** divides Lokad
elapsed by GNU elapsed (higher is slower). Managed allocations include parsing, execution
and host storage, not retained/peak memory or GNU allocations. Four warmups, five measured
iterations targeting 500 ms, one launch and standard BDN outlier filtering. Source/binary,
output and quiet checks pass. This comparison is a snapshot; raw results remain local.

## Representative processing

The GNU table covers 46 [workload families](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs)
without shared admission. Two additional nested-call families have current default/shared
profiles below. Most have 4,096 records; wide all-field, numeric-wide and long-field cases have 256.
Join has 4,096 records per file. Rows are ordered by elapsed ratio. GNU-marked cases do not
imply full GNU conformance; CSV requires GNU 5.3 or later.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB | Measured UTC / source |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| gsub and output | 56.00 | 10.676 | 1.789 | 5.97× | 23.75 | 10-30 / [4360424] |
| ERE field separator | 48.00 | 7.633 | 1.649 | 4.63× | 3.19 | 10-30 / [4360424] |
| Loop with repeated field arguments | 25.08 | 15.430 | 4.008 | 3.85× | 0.95 | 10-30 / [9facac7] |
| ERE boolean matching | 32.00 | 3.417 | 0.968 | 3.53× | 7.19 | 10-31 / [5e53a18] |
| Loop with five-argument calls | 8.00 | 19.884 | 5.710 | 3.48× | 0.57 | 10-31 / [0e73d92] |
| ERE capture arrays (GNU) | 28.00 | 23.846 | 7.324 | 3.26× | 34.40 | 10-30 / [4360424] |
| Loop with repeated numeric field reads | 25.08 | 9.770 | 3.097 | 3.15× | 0.52 | 10-31 / [6d279b5] |
| Text function with branches and local temporary | 62.00 | 4.509 | 1.651 | 2.73× | 2.12 | 10-30 / [a6a5a71] |
| Numeric array sort (GNU) | 18.92 | 5.504 | 2.018 | 2.73× | 2.30 | 10-30 / [a6a5a71] |
| Two-file membership / join | 77.83 | 6.606 | 2.483 | 2.66× | 2.69 | 10-30 / [a6a5a71] |
| Composite-key sum / sorted output (GNU) | 24.00 | 3.193 | 1.214 | 2.63× | 1.42 | 10-31 / [5e53a18] |
| Project three columns / output | 60.00 | 3.147 | 1.211 | 2.60× | 0.55 | 10-30 / [a6a5a71] |
| Loop with two-argument calls and local temporary | 8.00 | 10.732 | 4.274 | 2.51× | 0.55 | 10-30 / [4209c95] |
| Function with locals, loop and early return | 11.58 | 7.082 | 2.880 | 2.46× | 0.59 | 10-31 / [0e73d92] |
| Project a column / output | 60.00 | 2.332 | 0.974 | 2.39× | 1.18 | 10-30 / [4360424] |
| Copy field / rebuild record | 60.00 | 3.107 | 1.320 | 2.35× | 1.19 | 10-31 / [c510078] |
| Unicode length/substr / output | 60.00 | 3.904 | 1.669 | 2.34× | 1.78 | 10-30 / [a6a5a71] |
| Loop with two-argument calls | 8.00 | 8.335 | 3.741 | 2.23× | 0.59 | 10-30 / [a6a5a71] |
| ASCII substr/index/length/casing | 52.00 | 2.775 | 1.252 | 2.22× | 1.01 | 10-30 / [a6a5a71] |
| Branching function, two arguments | 8.00 | 8.812 | 3.997 | 2.20× | 0.62 | 10-30 / [a6a5a71] |
| Nested user calls | 8.00 | 3.167 | 1.452 | 2.18× | 0.52 | 10-31 / [0e73d92] |
| Post-test arithmetic loop | 8.00 | 5.015 | 2.333 | 2.15× | 0.58 | 10-30 / [a6a5a71] |
| Branching function, one argument | 8.00 | 7.855 | 3.708 | 2.12× | 0.61 | 10-30 / [a6a5a71] |
| Loop with unary arithmetic calls | 8.00 | 7.240 | 3.484 | 2.08× | 0.59 | 10-30 / [a6a5a71] |
| Five-part string concatenation | 20.00 | 2.788 | 1.350 | 2.07× | 1.39 | 10-30 / [a6a5a71] |
| Literal field separator | 48.00 | 2.071 | 1.007 | 2.06× | 0.57 | 10-30 / [a6a5a71] |
| Keyed sum / sorted output (GNU) | 24.00 | 2.151 | 1.050 | 2.05× | 0.11 | 10-31 / [5e53a18] |
| For loop and numeric arithmetic | 8.00 | 4.797 | 2.396 | 2.00× | 0.57 | 10-30 / [a6a5a71] |
| Mixed narrow-record processing | 60.00 | 3.707 | 1.867 | 1.99× | 0.60 | 10-30 / [a6a5a71] |
| While loop and numeric arithmetic | 8.00 | 4.732 | 2.393 | 1.98× | 0.57 | 10-30 / [a6a5a71] |
| printf | 24.00 | 3.390 | 1.722 | 1.97× | 0.52 | 10-30 / [a6a5a71] |
| Literal index search in a field | 70.00 | 2.294 | 1.226 | 1.87× | 0.58 | 10-30 / [a6a5a71] |
| Field-key membership | 48.56 | 1.854 | 0.996 | 1.86× | 0.63 | 10-30 / [a6a5a71] |
| Field-key reads, existing/new | 63.15 | 2.518 | 1.362 | 1.85× | 1.30 | 10-30 / [a6a5a71] |
| Filter three fields | 24.00 | 2.182 | 1.210 | 1.80× | 0.54 | 10-30 / [a6a5a71] |
| Sum one numeric column | 25.08 | 1.807 | 1.016 | 1.78× | 0.07 | 10-31 / [bf2975d] |
| Alternate reads from two lookup arrays | 21.50 | 1.976 | 1.264 | 1.56× | 0.15 | 10-31 / [7d0684f] |
| Labeled printf | 24.00 | 3.667 | 2.367 | 1.55× | 0.53 | 10-30 / [a6a5a71] |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 3.298 | 2.362 | 1.40× | 1.69 | 10-30 / [a6a5a71] |
| Field loop, 64 fields | 80.00 | 2.544 | 1.875 | 1.36× | 0.22 | 10-30 / [a6a5a71] |
| Sum every numeric field (128 columns) | 101.00 | 4.174 | 3.374 | 1.24× | 0.28 | 10-31 / [6d279b5] |
| Byte-run ERE record separator (GNU) | 36.00 | 1.212 | 1.132 | 1.07× | 0.96 | 10-30 / [a6a5a71] |
| Changed ASCII case | 580.00 | 2.627 | 2.489 | 1.06× | 2.97 | 10-30 / [a6a5a71] |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 2.066 | 2.028 | 1.02× | 0.65 | 10-31 / [5e53a18] |
| Multicharacter record separator (GNU) | 52.00 | 1.121 | 1.162 | 0.96× | 0.55 | 10-30 / [a6a5a71] |
| Filter `$2` after a 4 KiB field | 1,025.50 | 0.214 | 1.552 | 0.14× | 1.13 | 10-30 / [a6a5a71] |

Scalar summation isolates numeric field consumption from keyed updates. The repeated-field
loops update 16 keys eight times per record, with and without a helper. Numeric-wide
reads each of its 128 fields once, contrasting reuse with one-pass processing.
Early wide filtering reads `$1` on every record and `$3` on one in eight. Array reads cover
existing/new keys and alternating lookup tables; field search includes Unicode prefixes.

## Larger inputs

65,536 records per case, per file for join; numeric-wide has 4,096 records and 128 fields.
Aggregations and repeated-field loops retain 16 keys. Composite
aggregation uses two field components and the same input/output oracle as ordinary aggregation.
These reduce the relative
effect of process startup. No shared admission.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB | Measured UTC / source |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| ERE boolean matching | 512.00 | 53.869 | 4.930 | 10.93× | 112.19 | 10-31 / [5e53a18] |
| Composite-key sum / sorted output (GNU) | 384.00 | 47.593 | 8.766 | 5.43× | 20.77 | 10-31 / [5e53a18] |
| Keyed sum / sorted output (GNU) | 384.00 | 34.566 | 6.634 | 5.21× | 0.24 | 10-31 / [5e53a18] |
| Sum one numeric column | 401.40 | 29.337 | 6.213 | 4.72× | 0.19 | 10-31 / [bf2975d] |
| Copy field / rebuild record | 960.00 | 51.159 | 11.219 | 4.56× | 18.19 | 10-31 / [c510078] |
| Loop with repeated field arguments | 401.40 | 226.210 | 53.017 | 4.27× | 13.26 | 10-30 / [9facac7] |
| Loop with repeated numeric field reads | 401.40 | 150.334 | 36.770 | 4.09× | 6.74 | 10-31 / [6d279b5] |
| Two-file membership / join | 1,386.30 | 121.835 | 31.088 | 3.92× | 35.99 | 10-30 / [a6a5a71] |
| Sum every numeric field (128 columns) | 1616.00 | 67.338 | 42.755 | 1.57× | 2.20 | 10-31 / [6d279b5] |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 31.796 | 26.172 | 1.21× | 7.60 | 10-31 / [5e53a18] |

## Shared-admission profiles

Rows state their measured source/date; retained entries are not reruns of the latest source.
They compare the existing thread-safe 512 MiB test budget with default execution through
public invocation on the same machine/catalog fixtures. Application budgets can cost
differently. No GNU timing or native-allocation claim.
Arithmetic forwarding calls a function that prints a literal; text forwarding prints the
original field value. The integer rows use `int($1)` directly and through a function;
the binary row sums `(atan2($1,$2)>0)`, with `0007` in each input field. Startup rows
prepare and execute one named call with no input.
The AND case uses `0007 0003`; the long function performs 64 integer updates per record.
The square-root case uses positive inputs and retains ordinary diagnostic boundaries.
Integer-loop processing performs four conversions per record; the long action performs 64.
Numeric filters test `$1 > 0`, a numeric helper or `int($1)`, then sum positive inputs.
Do-while processing performs two integer updates per record. File comparison retains 5,000
keys, with every tenth right-hand value changed; the expected result is 500 differences.

The comparison runner supplies a live token for its 15-second invocation deadline.
This table's independent snapshot starts with no caller token; governed execution creates
its own live token. Published regex-provider costs differ between these token setups, so
its regex rows do not isolate budget bookkeeping. The existing `--governed-profile` command
supplies a live caller deadline in both modes and measures that distinct configuration.

Nested integer output formats two `int(int($1))` arguments; wide formatting repeats
`$1+0` 65 times. Both use 4,096 records containing `0007`. The ordinary five-argument loop includes
a switch boundary; the prepared loop uses the same computation. Startup timing varies
substantially across processes; its medians do not establish a causal default/shared difference.

| Script | Shared admission | Lokad ms | Lokad allocated MiB | Measured UTC / source |
| --- | --- | ---: | ---: | --- |
| Nested-array calls, 16 keys / 4,096 records (GNU) | None | 7.544 | 1.804 | 11-01 / [10d9918] |
| Nested-array calls, 16 keys / 4,096 records (GNU) | 512 MiB | 9.531 | 2.430 | 11-01 / [10d9918] |
| Nested-array calls, 1,024 keys / 4,096 records (GNU) | None | 8.138 | 2.398 | 11-01 / [10d9918] |
| Nested-array calls, 1,024 keys / 4,096 records (GNU) | 512 MiB | 9.876 | 3.023 | 11-01 / [10d9918] |
| Loop with five-argument calls, 4,096 records | None | 16.927 | 0.568 | 11-01 / [200fe9d] |
| Loop with five-argument calls, 4,096 records | 512 MiB | 17.101 | 0.570 | 11-01 / [200fe9d] |
| Nested user calls, 4,096 records | None | 2.811 | 0.517 | 11-01 / [508228d] |
| Nested user calls, 4,096 records | 512 MiB | 4.470 | 0.519 | 11-01 / [508228d] |
| Aggregation, 65,536 records | None | 36.441 | 0.238 | 11-01 / [10d9918] |
| Aggregation, 65,536 records | 512 MiB | 50.748 | 2.241 | 11-01 / [10d9918] |
| Join, 65,536 records per file | None | 112.729 | 21.363 | 10-31 / [e9c6ef5] |
| Join, 65,536 records per file | 512 MiB | 171.680 | 29.366 | 10-31 / [e9c6ef5] |
| Scalar sum, 65,536 records | None | 32.970 | 0.193 | 11-01 / [7da72c5] |
| Scalar sum, 65,536 records | 512 MiB | 46.107 | 0.195 | 11-01 / [7da72c5] |
| ERE boolean matching, 4,096 records | None | 2.168 | 0.191 | 10-31 / [5e53a18] |
| ERE boolean matching, 4,096 records | 512 MiB | 4.125 | 7.505 | 10-31 / [5e53a18] |
| Early fields, 65,536 wide records | None | 30.588 | 7.603 | 10-31 / [5e53a18] |
| Early fields, 65,536 wide records | 512 MiB | 47.705 | 10.104 | 10-31 / [5e53a18] |
| Arithmetic forwarding with literal output, 4,096 records | None | 4.628 | 0.801 | 11-01 / [10d9918] |
| Arithmetic forwarding with literal output, 4,096 records | 512 MiB | 5.396 | 0.927 | 11-01 / [10d9918] |
| Text forwarding with field output, 4,096 records | None | 4.707 | 1.207 | 11-01 / [10d9918] |
| Text forwarding with field output, 4,096 records | 512 MiB | 6.520 | 1.333 | 11-01 / [10d9918] |
| Nested-array call startup, no input | None | 0.073 | 0.158 | 11-01 / [10d9918] |
| Nested-array call startup, no input | 512 MiB | 0.085 | 0.160 | 11-01 / [10d9918] |
| Integer field conversion, 4,096 records | None | 1.980 | 0.071 | 11-01 / [b329690] |
| Integer field conversion, 4,096 records | 512 MiB | 2.715 | 0.073 | 11-01 / [b329690] |
| Forwarded integer conversion, 4,096 records | None | 2.555 | 0.091 | 11-01 / [766104d] |
| Forwarded integer conversion, 4,096 records | 512 MiB | 3.350 | 0.405 | 11-01 / [766104d] |
| atan2 on two fields, 4,096 records | None | 2.971 | 0.081 | 11-01 / [7da72c5] |
| atan2 on two fields, 4,096 records | 512 MiB | 4.050 | 0.488 | 11-01 / [7da72c5] |
| Text forwarding with retained spelling/type, 4,096 records | None | 3.626 | 0.514 | 11-01 / [7da72c5] |
| Text forwarding with retained spelling/type, 4,096 records | 512 MiB | 5.037 | 0.516 | 11-01 / [7da72c5] |
| Integer-forwarding startup, no input | None | 0.040 | 0.079 | 11-01 / [200fe9d] |
| Integer-forwarding startup, no input | 512 MiB | 0.029 | 0.081 | 11-01 / [200fe9d] |
| Forwarded bitwise AND, 4,096 records | None | 2.722 | 0.101 | 11-01 / [b329690] |
| Forwarded bitwise AND, 4,096 records | 512 MiB | 3.504 | 0.103 | 11-01 / [b329690] |
| Function with 64 integer updates, 4,096 records | None | 18.985 | 0.808 | 11-01 / [200fe9d] |
| Function with 64 integer updates, 4,096 records | 512 MiB | 19.291 | 0.809 | 11-01 / [200fe9d] |
| Square-root function, 4,096 records | None | 2.896 | 0.091 | 11-01 / [98014bc] |
| Square-root function, 4,096 records | 512 MiB | 3.722 | 0.405 | 11-01 / [98014bc] |
| Loop with four integer field conversions, 4,096 records | None | 3.765 | 0.092 | 11-01 / [98014bc] |
| Loop with four integer field conversions, 4,096 records | 512 MiB | 5.195 | 1.344 | 11-01 / [98014bc] |
| Action with 64 integer updates, 4,096 records | None | 18.558 | 0.877 | 11-01 / [98014bc] |
| Action with 64 integer updates, 4,096 records | 512 MiB | 29.378 | 20.879 | 11-01 / [98014bc] |
| Numeric field filter and integer sum, 4,096 records | None | 2.264 | 0.082 | 11-01 / [1ac9634] |
| Numeric field filter and integer sum, 4,096 records | 512 MiB | 3.069 | 0.396 | 11-01 / [1ac9634] |
| Numeric field filter and helper sum, 4,096 records | None | 2.358 | 0.101 | 11-01 / [1ac9634] |
| Numeric field filter and helper sum, 4,096 records | 512 MiB | 3.149 | 0.103 | 11-01 / [1ac9634] |
| Integer condition and sum, 4,096 records | None | 2.274 | 0.086 | 11-01 / [1ac9634] |
| Integer condition and sum, 4,096 records | 512 MiB | 3.183 | 0.713 | 11-01 / [1ac9634] |
| Do-while integer processing, 4,096 records | None | 2.660 | 0.092 | 11-01 / [1ac9634] |
| Do-while integer processing, 4,096 records | 512 MiB | 3.578 | 0.718 | 11-01 / [1ac9634] |
| Compare two files, 5,000 records per file | None | 7.416 | 1.887 | 11-01 / [1ac9634] |
| Compare two files, 5,000 records per file | 512 MiB | 10.424 | 2.041 | 11-01 / [1ac9634] |
| Arithmetic loop, 4,096 records | None | 5.277 | 0.521 | 11-01 / [b329690] |
| Arithmetic loop, 4,096 records | 512 MiB | 6.477 | 0.522 | 11-01 / [b329690] |
| Square-root condition, 4,096 records | None | 2.489 | 0.086 | 11-01 / [1ac9634] |
| Square-root condition, 4,096 records | 512 MiB | 3.443 | 0.712 | 11-01 / [1ac9634] |
| Nested integer arguments with printf, 4,096 records | None | 4.389 | 0.769 | 11-01 / [b329690] |
| Nested integer arguments with printf, 4,096 records | 512 MiB | 5.399 | 2.114 | 11-01 / [b329690] |
| printf with 65 numeric arguments, 4,096 records | None | 24.530 | 1.225 | 11-01 / [b329690] |
| printf with 65 numeric arguments, 4,096 records | 512 MiB | 25.354 | 1.228 | 11-01 / [b329690] |
| Function with locals, branches and loop, 4,096 records | None | 6.410 | 0.185 | 11-01 / [200fe9d] |
| Function with locals, branches and loop, 4,096 records | 512 MiB | 6.726 | 0.186 | 11-01 / [200fe9d] |
| Ordinary five-argument call loop, 4,096 records | None | 20.415 | 0.579 | 11-01 / [200fe9d] |
| Ordinary five-argument call loop, 4,096 records | 512 MiB | 22.052 | 0.581 | 11-01 / [200fe9d] |
| Two-argument loop with local temporary, 4,096 records | None | 10.773 | 0.549 | 11-01 / [200fe9d] |
| Two-argument loop with local temporary, 4,096 records | 512 MiB | 16.487 | 0.676 | 11-01 / [200fe9d] |

## Measurement contract

Lokad runs inside an already-running .NET process through `Awk.TryParse`/`Awk.ExecuteAsync`,
including source parsing/binding and execution with an in-memory host. GNU starts a process
per invocation, including launch, pipe I/O and native file fixtures. Both consume and hash
all output. Fixture generation and preflight are outside timing; startup is not subtracted.
These compare embedding with CLI execution; small cases do not isolate evaluator costs.

Managed allocations include host storage and interaction. The tables remain measured at the
source revision stated above. Runtime correctness tests independently cover mutable/reused host
buffers. A host-storage change must not be attributed to an interpreter improvement.

Host: Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; BenchmarkDotNet 0.15.8.
Governed managed-allocation totals include the test budget's bookkeeping. Some processes
show allocation variation with unchanged sources; paired observations and admitted peaks
are distinct evidence, and allocation differences alone do not establish a core regression.

Timing and allocation are separate passes with fixed warmup/measurement counts and BDN pilot
calibration. Preflight checks stdout, stderr and exit; every measured invocation verifies output.
Small improvement claims require bounded repeat confirmation beyond this single-launch snapshot.

## Reproduction and maintenance

Build once, verify, check quietness, then select affected cases and up to two contrasting
controls. For example, use the same filter for timing and managed allocation runs:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --driver-smoke --awk PATH_TO_GAWK
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-benchmarks --awk PATH_TO_GAWK --filter '*ReferenceAwkBenchmarks*medium*field-rewrite*' '*ReferenceAwkBenchmarks*medium*projection*' --warmupCount 4 --iterationCount 5 --iterationTime 500 --launchCount 1 --exporters json
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*field-rewrite*' '*WorkloadAllocationBenchmarks*medium*projection*' --warmupCount 4 --iterationCount 5 --iterationTime 500 --launchCount 1 --exporters json
```

The [Linux wrapper](../benchmarks/run-reference-linux.sh) pins the toolchain/reference.
`--driver-smoke` checks successful/nonzero early exits with unread stdin, complete input,
concurrent output drains, output bounds, cancellation, deadlines and child reaping.
An early reference exit preserves its status and output even when stdin was not consumed.
Invocation and allocation benchmarks share `ReferenceWorkloads` and one in-memory host.
Compare/smoke filters select family/scale identifiers before constructing input fixtures.
Their comparison counters omit memory peaks because those runs do not enable admission.
Interpreter/frontend isolation uses that catalog too; the counter report retains its small,
dedicated calibration cases. Its memory peak covers Awk-owned storage. Published provider
0.4.0 lacks shared-admission hooks; the source adapter uses coarse reservations.
Benchmark execution failures return nonzero; CLI help, list and environment-info commands
may succeed without running measurements.
Optional `--render-report PATH_TO_JSON` writes standalone Markdown beside a comparison
artifact using Lokad.Awk/awk. Raw JSON retains its GNU/Lokad statistic for existing tooling.
Existing `--governed-profile` cases cover calls, projection and aggregation with a 512 MiB
thread-safe test budget, including default-mode call/aggregation controls.
Shared-mode managed allocations include test-budget bookkeeping and have shown variation
between processes. Compare paired observations; Awk-owned admitted peaks are measured separately. Its paired
default/shared `fs-regex`, `dynamic-pattern`, `substitution` and `rs-engine` scenarios
exercise the provider boundary with the existing medium fixtures. `rs-regex` uses a byte-run
fast path; `rs-engine` uses equivalent `([,;]+){1}` separators and checks provider use before
timing. Medium arithmetic
cases compare GNU/default, POSIX and governed POSIX execution. Keep transfers/builds outside measurement
windows. Preserve the full correctness suite. Use one paired comparison and, when it could change the
decision, one reverse-order confirmation.

Refresh all families at declared milestones. Replace affected rows after qualification,
identifying their actual measured revision/date if others are retained. Keep this document
a current performance snapshot; raw data, superseded results and experiment histories stay local.
See the [runtime and host contract](RUNTIME.md) and [compatibility boundaries](COMPATIBILITY.md).

[0e73d92]: https://github.com/Lokad/Awk/commit/0e73d924efd5fb1a6873748a67e97e4ac5111a13
[4209c95]: https://github.com/Lokad/Awk/commit/4209c95fac69967bf6968c8b2619aefd7ad1bcfe
[4360424]: https://github.com/Lokad/Awk/commit/43604247da2bdd88ec79a312617fc4d366c45e10
[5e53a18]: https://github.com/Lokad/Awk/commit/5e53a18a84ca6e8889f5becd8abfa9ba2a4bdf98
[6d279b5]: https://github.com/Lokad/Awk/commit/6d279b564f681e59fda47e7fb28d74b877d581b1
[9facac7]: https://github.com/Lokad/Awk/commit/9facac7d45566d24f3f6d919c3c84a759f3eebde
[a6a5a71]: https://github.com/Lokad/Awk/commit/a6a5a7136fedf066361de1aaef8f18bcccf3ae9d
[c510078]: https://github.com/Lokad/Awk/commit/c510078e748c209d90fc5f11a038081a264a0772

[7d0684f]: https://github.com/Lokad/Awk/commit/7d0684f4d05b32c7d65a923f59a5938f758f36d6

[e9c6ef5]: https://github.com/Lokad/Awk/commit/e9c6ef53e8498a2433158f9324651961929425e8

[feec315]: https://github.com/Lokad/Awk/commit/feec315bcd80e97823faa15e4f8ad1f3325f7efb

[bf2975d]: https://github.com/Lokad/Awk/commit/bf2975d36cd1c9f67e80ec75252ab5da37a68395

[508228d]: https://github.com/Lokad/Awk/commit/508228dfe77a15b341d3784919dbc5fc8159d435

[10d9918]: https://github.com/Lokad/Awk/commit/10d99188af3d2733b5eb3f5e4c88b74018c56697

[7da72c5]: https://github.com/Lokad/Awk/commit/7da72c50fc06b1e9239b615d4286965f9c461e66

[766104d]: https://github.com/Lokad/Awk/commit/766104d4a52d5c677fabdc263197bc9364e9775d

[98014bc]: https://github.com/Lokad/Awk/commit/98014bc1fb439cb6ffc77665740ace981ea8571a

[1ac9634]: https://github.com/Lokad/Awk/commit/1ac963428950de122cf7026ea5cac934c4f68d5d

[b329690]: https://github.com/Lokad/Awk/commit/b329690d1c5301b9af5287ca8043a34f2a7ecc4f

[200fe9d]: https://github.com/Lokad/Awk/commit/200fe9decb9714ad41a7518644ad88b933fe5ff9
