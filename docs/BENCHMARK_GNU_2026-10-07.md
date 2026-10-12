# Historical GNU awk comparison — 2026-10-07

This preserves the recorded full-catalog campaign at private snapshot `fc6b930`,
with origin `e4126b3`. The isolated snapshot/source/archive identity below matters;
these are not current 0.3.0 timings or a clean upstream release-source claim.
See [current results and workflow](BENCHMARKS.md) for newer representative evidence.
The table retains its recorded qualification labels for that historical source.

<!-- BEGIN GENERATED AWK COMPARISON -->
## Recorded GNU awk comparison

Recorded UTC: 2026-10-07 12:57:05Z; mode qualify.
Revision: fc6b9306243433a4a8a81e2902fc643dbc075a7d; SDK 10.0.401; .NET 10.0.12; Ubuntu 24.04.4 LTS.
Machine: AMD EPYC 9V45 96-Core Processor; 4 logical processors; MemTotal:       16373452 kB; SwapTotal:             0 kB.
Assembly SHA-256: benchmark 2698bb4c0047b11599bd7b3b57717bfd11c2ed4418ced01bba8a343bce49cf25; library e876254ec24d58d2341e891be31e0a7b3647271c9d08a224e83f85c6be8c3c29.
Private source snapshot from e4126b39aaa4cbc101a6b9fa7b38fe9850e2fff0 (origin clean: False); source SHA-256 a7f53544801913a997fe7b71eb251cbf206955f2796659c85b36997b5084db5c; archive SHA-256 924dbfe5325b8c85b634c669007f14ff6aceb8db98a2381e72d792678f0f2d01.
Reference: GNU Awk 5.2.1, API 3.2, PMA Avon 8-g1, (GNU MPFR 4.2.1, GNU MP 6.3.0); SHA-256 9150b42e78b22231bd83131e1db2530a7a7fa0677e189ac7c5cc1520c42c9e88.

Warm embedded Lokad.Awk public full invocation versus GNU awk CLI. Both include compilation, execution, complete output consumption and SHA-256. GNU includes launch and pipes; multi-file cases use native fixture files versus in-memory host files. Fixture generation and preflight are excluded. No startup subtraction or overall speedup.

| Case / scale | Records | Input bytes | Status | Lokad ms | GNU ms | GNU / Lokad, 95% CI | Interpretation |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| projection/small | 16 | 240 | Qualified | 0.038 | 1.165 | 30.547 [30.059, 31.176] | Lokad.Awk faster |
| projection/medium | 4096 | 61440 | Qualified | 4.479 | 1.523 | 0.340 [0.337, 0.342] | GNU awk faster |
| projection/large | 65536 | 983040 | Qualified | 71.852 | 6.905 | 0.096 [0.095, 0.097] | GNU awk faster |
| filter/small | 16 | 96 | Qualified | 0.061 | 1.172 | 19.237 [18.801, 19.493] | Lokad.Awk faster |
| filter/medium | 4096 | 24576 | Qualified | 6.118 | 1.758 | 0.288 [0.286, 0.292] | GNU awk faster |
| filter/large | 65536 | 393216 | Qualified | 96.088 | 10.328 | 0.108 [0.106, 0.108] | GNU awk faster |
| aggregate/small | 16 | 96 | Qualified | 0.098 | 1.182 | 12.023 [11.971, 12.184] | Lokad.Awk faster |
| aggregate/medium | 4096 | 24576 | Qualified | 5.832 | 1.549 | 0.266 [0.263, 0.269] | GNU awk faster |
| aggregate/large | 65536 | 393216 | Qualified | 91.531 | 7.204 | 0.079 [0.078, 0.080] | GNU awk faster |
| profile-narrow/small | 16 | 240 | Qualified | 0.125 | 1.181 | 9.453 [9.251, 9.549] | Lokad.Awk faster |
| profile-narrow/medium | 4096 | 61440 | Qualified | 18.189 | 2.509 | 0.139 [0.137, 0.141] | GNU awk faster |
| profile-narrow/large | 65536 | 983040 | Qualified | 285.667 | 22.518 | 0.080 [0.078, 0.081] | GNU awk faster |
| profile-wide/small | 16 | 5120 | Qualified | 1.236 | 1.262 | 1.030 [1.009, 1.033] | Lokad.Awk faster |
| profile-wide/medium | 256 | 81920 | Qualified | 19.032 | 2.513 | 0.131 [0.131, 0.135] | GNU awk faster |
| profile-wide/large | 4096 | 1310720 | Qualified | 298.056 | 22.629 | 0.076 [0.075, 0.078] | GNU awk faster |
| two-file/small | 32 | 236 | Qualified | 0.111 | 1.203 | 10.789 [10.714, 10.896] | Lokad.Awk faster |
| two-file/medium | 8192 | 79700 | Qualified | 16.071 | 3.227 | 0.201 [0.198, 0.213] | GNU awk faster |
| two-file/large | 131072 | 1419572 | Qualified | 259.491 | 33.919 | 0.131 [0.130, 0.135] | GNU awk faster |
| fs-literal/small | 16 | 192 | Qualified | 0.056 | 1.175 | 20.916 [20.545, 21.192] | Lokad.Awk faster |
| fs-literal/medium | 4096 | 49152 | Qualified | 4.754 | 1.607 | 0.337 [0.336, 0.342] | GNU awk faster |
| fs-literal/large | 65536 | 786432 | Qualified | 75.252 | 7.842 | 0.104 [0.103, 0.105] | GNU awk faster |
| fs-regex/small | 16 | 192 | Qualified | 0.066 | 1.208 | 18.317 [18.112, 18.671] | Lokad.Awk faster |
| fs-regex/medium | 4096 | 49152 | Qualified | 6.428 | 2.272 | 0.353 [0.348, 0.356] | GNU awk faster |
| fs-regex/large | 65536 | 786432 | Qualified | 101.781 | 18.053 | 0.177 [0.176, 0.179] | GNU awk faster |
| rs-literal/small | 16 | 208 | Qualified | 0.045 | 1.214 | 26.812 [26.607, 27.078] | Lokad.Awk faster |
| rs-literal/medium | 4096 | 53248 | Qualified | 2.904 | 1.692 | 0.581 [0.580, 0.587] | GNU awk faster |
| rs-literal/large | 65536 | 851968 | Qualified | 45.946 | 8.920 | 0.194 [0.193, 0.194] | GNU awk faster |
| rs-regex/small | 16 | 144 | Qualified | 0.046 | 1.215 | 26.724 [26.075, 26.876] | Lokad.Awk faster |
| rs-regex/medium | 4096 | 36864 | Qualified | 3.085 | 1.658 | 0.537 [0.534, 0.542] | GNU awk faster |
| rs-regex/large | 65536 | 589824 | Qualified | 49.367 | 8.277 | 0.167 [0.166, 0.169] | GNU awk faster |
| substitution/small | 16 | 224 | Qualified | 0.060 | 1.201 | 20.167 [19.683, 20.457] | Lokad.Awk faster |
| substitution/medium | 4096 | 57344 | Qualified | 8.020 | 2.392 | 0.301 [0.293, 0.303] | GNU awk faster |
| substitution/large | 65536 | 917504 | Qualified | 127.039 | 20.249 | 0.160 [0.152, 0.161] | GNU awk faster |
| formatting/small | 16 | 96 | Qualified | 0.051 | 1.168 | 22.827 [22.638, 23.492] | Lokad.Awk faster |
| formatting/medium | 4096 | 24576 | Qualified | 6.342 | 2.003 | 0.316 [0.314, 0.320] | GNU awk faster |
| formatting/large | 65536 | 393216 | Qualified | 99.939 | 14.369 | 0.144 [0.143, 0.145] | GNU awk faster |
| sorting/small | 16 | 39 | Qualified | 0.114 | 1.190 | 10.486 [10.312, 10.569] | Lokad.Awk faster |
| sorting/medium | 4096 | 19373 | Qualified | 12.066 | 2.869 | 0.236 [0.232, 0.248] | GNU awk faster |
| sorting/large | 65536 | 382110 | Qualified | 210.960 | 29.510 | 0.140 [0.135, 0.147] | GNU awk faster |
| unicode/small | 16 | 240 | Qualified | 0.055 | 1.315 | 23.789 [23.414, 24.021] | Lokad.Awk faster |
| unicode/medium | 4096 | 61440 | Qualified | 6.579 | 2.372 | 0.360 [0.359, 0.367] | GNU awk faster |
| unicode/large | 65536 | 983040 | Qualified | 104.158 | 18.743 | 0.180 [0.180, 0.181] | GNU awk faster |
| startup-empty/control | 0 | 0 | Qualified | 0.016 | 1.125 | 70.079 [64.492, 71.989] | Lokad.Awk faster |
| startup-tiny/control | 1 | 6 | Qualified | 0.021 | 1.165 | 55.956 [54.920, 57.596] | Lokad.Awk faster |

<!-- END GENERATED AWK COMPARISON -->
