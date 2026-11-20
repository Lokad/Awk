using System.Globalization;
using System.Reflection;
using System.Text;
using Lokad.Awk;
using Lokad.Awk.Benchmarking;

if (args.Length > 0 && args[0] == "--governed-profile")
{
    Environment.ExitCode = ReferenceComparison.RunBenchmarks([typeof(GovernedAllocationProfile)], args.Skip(1).ToArray());
    return;
}

if (args.Length == 1 && string.Equals(args[0], "--counter-report", StringComparison.Ordinal))
{
    await BenchmarkCounterReport.RunAsync(CancellationToken.None).ConfigureAwait(false);
    return;
}

if (args.Length > 0 && args[0] is "--compare" or "--smoke" or "--reference-benchmarks" or "--check-machine" or "--driver-smoke" or "--reference-contracts")
{
    Environment.ExitCode = await ReferenceComparison.RunCommandAsync(args, CancellationToken.None).ConfigureAwait(false);
    return;
}

Environment.ExitCode = ReferenceComparison.RunBenchmarks(
    [typeof(FrontendLayerBenchmarks), typeof(FrontendScalingBenchmarks), typeof(WorkloadAllocationBenchmarks)], args);

internal static class BenchmarkCounterReport
{
    private const long CounterExecutionMemoryLimitBytes = 1L << 30;
    private static readonly CounterScenario[] Scenarios;

    static BenchmarkCounterReport()
    {
        Scenarios = CreateScenarios();

        static CounterScenario[] CreateScenarios()
        {
            var streamingRows = BuildCounterStreamingRows();
            var streaming = new BenchmarkFile("/stream.tsv", streamingRows);
            var projection = new BenchmarkFile("/data.tsv", "sku1\tvalue1\ttail\nsku2\tvalue2\ttail\nsku3\tvalue3\ttail\n");
            return [
            new(
            "counter column extraction",
            ["{print $2}", "/data.tsv"],
            [projection],
            "value1\nvalue2\nvalue3\n"),
        new(
            "counter multi-column projection",
            ["{print $1,$3,$2}", "/data.tsv"],
            [projection],
            "sku1 tail value1\nsku2 tail value2\nsku3 tail value3\n"),
        new(
            "counter streaming projection calibration",
            ["{print $2}", "/stream.tsv"],
            [streaming],
            string.Concat(Enumerable.Range(0, CounterStreamingRecordCount)
                .Select(row => FormattableString.Invariant($"region{row % 11}\n")))),
        new(
            "counter three-key filter calibration",
            ["$1 != \"\" && $2 != \"\" && $3 == \"open\" {matches++} END{print matches+0}", "/stream.tsv"],
            [streaming], "2500\n"),
        new(
            "counter associative aggregation calibration",
            ["{sum[$1,$2]+=$4; count[$3]++} END{print length(sum), length(count)}", "/stream.tsv"],
            [streaming], "1111 2\n"),
        new(
            "counter two-file comparison calibration",
            ["NR==FNR{left[$1]=$2; next} ($1 in left) && left[$1]!=$2{differences++} END{print differences+0}", "/left.tsv", "/right.tsv"],
            [
                new BenchmarkFile("/left.tsv", BuildCounterComparisonRows(changed: false)),
                new BenchmarkFile("/right.tsv", BuildCounterComparisonRows(changed: true))
            ], "500\n"),
        new(
            "counter arithmetic loop",
            ["BEGIN{for(i=1;i<=100;i++) total += i * 2; print total}"],
            [], "10100\n"),
        new(
            "counter print-heavy action",
            ["BEGIN{for(i=1;i<=50;i++) print \"row\", i}"],
            [],
            string.Concat(Enumerable.Range(1, 50).Select(row => FormattableString.Invariant($"row {row}\n")))),
        new(
            "counter regex field separator",
            ["BEGIN{FS=\"[|;]\"} {sum += NF + length($2)} END{print sum}", "/pipe.txt"],
            [new BenchmarkFile("/pipe.txt", "a|beta;flag\nb|delta;plain\nc|gamma;flag\n")], "23\n"),
        new(
            "counter dynamic record separator",
            ["BEGIN{RS=\"|\"} NR==1{RS=\"[,;]\"} {total += length($0)} END{print NR, total}", "/dynamic-rs.txt"],
            [new BenchmarkFile("/dynamic-rs.txt", "first|second,third;fourth;")], "4 22\n"),
        new(
            "counter grouped byte-run record separator",
            ["BEGIN{RS=\"([,;])+\"} {print length($0)}", "/stream.tsv"],
            [streaming],
            FormattableString.Invariant($"{streamingRows.Length}\n")),
        .. new[] { 100, 400, 1_600 }.Select(CreateLiteralRecordSeparatorScenario),
        new(
            "counter non-ASCII record separator",
            ["BEGIN{RS=\"\u00e9\"} {total += length($0)} END{print NR, total}", "/non-ascii-rs.txt"],
            [new BenchmarkFile("/non-ascii-rs.txt", "caf\u00e91\u00e9r\u00e9sum\u00e92\u00e9")], "5 9\n"),
        new(
            "counter field modes",
            ["NR%2{FPAT=\"[A-Z]+|[0-9]+\"; FIELDWIDTHS=\"\"; fp += NF; split($0,a,/[0-9]+/,s); sep += length(s[1]); next} {FPAT=\"\"; FIELDWIDTHS=\"2 3\"; fw += NF} END{print fw, fp, sep}", "/field-modes.txt"],
            [new BenchmarkFile("/field-modes.txt", "A-12-B\nabcdef\nC-34-D\nghijkl\n")], "0 3 4\n"),
        new(
            "counter UTF-8 FIELDWIDTHS",
            ["BEGIN{FIELDWIDTHS=\"4 5 2\"} {total += length($1) + length($2) + NF} END{print total}", "/fieldwidths.txt"],
            [new BenchmarkFile("/fieldwidths.txt", "caf\u00e9bravo12\nna\u00efveplain34\n")], "24\n"),
        new(
            "counter large split fields",
            ["{n=split($0,a,\" \"); total += n + length($1) + length($NF) + length(a[1]) + length(a[n])} END{print total}", "/split-fields.txt"],
            [new BenchmarkFile("/split-fields.txt", BuildCounterSeparatedRows())], "896\n"),
        new(
            "counter large scalar growth",
            ["{x = x $0} END{print length(x)}", "/scalar.txt"],
            [new BenchmarkFile("/scalar.txt", "alpha\nbeta\ngamma\n")], "14\n"),
        new(
            "counter large field list",
            ["{total += NF + length($8)} END{print total}", "/fields.txt"],
            [new BenchmarkFile("/fields.txt", "a b c d e f g h i j\n1 2 3 4 5 6 7 8 9 10\n")], "22\n"),
        new(
            "counter small text split arrays",
            ["{n=split($0,a,\"\"); total += n + length(a[1]) + length(a[n])} END{print total}", "/small-split.txt"],
            [new BenchmarkFile("/small-split.txt", "abcdef\nbcdefg\n")], "16\n"),
        new(
            "counter regex replacement expansion",
            ["{gsub(/([[:alpha:]]+)([[:digit:]]+)/,\"<&>-<&>-<&>\",$0); total += length($0)} END{print total}", "/replace.txt"],
            [new BenchmarkFile("/replace.txt", "sku1 item22\nsku3 item44\n")], "94\n"),
        new(
            "counter regex capture array",
            ["BEGIN{if(match(\"abc123\",/([a-z]+)([0-9]+)/,m)) print m[1],m[2]}"],
            [], "abc 123\n"),
        new(
            "counter gensub capture selection",
            ["BEGIN{print gensub(/(x)(a)?(b)?(c)?(d)?(e)?(f)?(g)?(h)?(i)?(j)?/,\"<\\\\1>\",\"g\",\"xx\")}"],
            [], "<x><x>\n"),
        new(
            "counter regex pattern gap",
            ["BEGIN{s=\"abc\"; print match(s,/\\`a/)}"],
            [], "1\n"),
        new(
            "counter array sorting",
            ["{a[$1] += $2} END{PROCINFO[\"sorted_in\"]=\"@ind_str_asc\"; for(k in a) print k, a[k]}", "/array-sort.txt"],
            [new BenchmarkFile("/array-sort.txt", "b 2\na 1\nb 3\n")], "a 1\nb 5\n"),
        new(
            "counter small ordinal array",
            ["BEGIN{for(i=1;i<=64;i++) a[i]=i; for(i=1;i<=64;i++) total += a[i]; print total}"],
            [], "2080\n"),
        new(
            "counter user-function loop",
            ["function add(x,y){return x+y} BEGIN{for(i=1;i<=80;i++) total=add(total,i); print total}"],
            [], "3240\n"),
        new(
            "counter UTF-8 rune slicing",
            ["{for(i=1;i<=length($0);i++) last=substr($0,i,1)} END{print last}", "/runes.txt"],
            [new BenchmarkFile("/runes.txt", new string('\u00e9', 4_096) + "\n")], "\u00e9\n"),
        new(
            "counter byte concat",
            ["--characters-as-bytes", "{x = $1 \"|\" $2 \"|\" $3; total += length(x)} END{print total}", "/concat.txt"],
            [new BenchmarkFile("/concat.txt", "a b c\nx y z\n")], "10\n")
            ];
            static CounterScenario CreateLiteralRecordSeparatorScenario(int recordCount)
            {
                const string script = "BEGIN{RS=\"</row>\"} {n++} END{print n}";
                var row = "<row><cell>" + new string('a', 1_000) + "</cell></row>";
                var input = string.Concat(Enumerable.Repeat(row, recordCount));
                return new CounterScenario(
                    "counter literal record separator (" + recordCount + " records)",
                    [script, "/literal-rs.txt"],
                    [new BenchmarkFile("/literal-rs.txt", input)],
                    FormattableString.Invariant($"{recordCount}\n"));
            }

            static string BuildCounterStreamingRows()
            {
                var builder = new StringBuilder();
                for (var index = 0; index < CounterStreamingRecordCount; index++)
                {
                    builder.Append("sku");
                    builder.Append(index % 101);
                    builder.Append('\t');
                    builder.Append("region");
                    builder.Append(index % 11);
                    builder.Append('\t');
                    builder.Append(index % 4 == 0 ? "open" : "closed");
                    builder.Append('\t');
                    builder.Append(index % 97);
                    builder.AppendLine();
                }

                return builder.ToString();
            }

            static string BuildCounterComparisonRows(bool changed)
            {
                var builder = new StringBuilder();
                for (var index = 0; index < CounterComparisonRecordCount; index++)
                {
                    builder.Append("key");
                    builder.Append(index);
                    builder.Append('\t');
                    builder.Append(changed && index % 10 == 0 ? index + 1 : index);
                    builder.AppendLine();
                }

                return builder.ToString();
            }

            static string BuildCounterSeparatedRows()
            {
                var builder = new StringBuilder();
                for (var row = 0; row < 32; row++)
                {
                    for (var field = 0; field < 24; field++)
                    {
                        if (field > 0)
                            builder.Append(' ');

                        builder.Append('a');
                    }

                    builder.AppendLine();
                }

                return builder.ToString();
            }
        }
    }

    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("# Lokad.Awk Benchmark Counter Report");
        Console.WriteLine();
        Console.WriteLine("Command: `dotnet run -c Release --project .\\benchmarks\\Lokad.Awk.Benchmarks\\Lokad.Awk.Benchmarks.csproj -- --counter-report`");
        Console.WriteLine("Date UTC: " + DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        Console.WriteLine("Machine: " + Environment.MachineName);
        Console.WriteLine("OS: " + Environment.OSVersion);
        Console.WriteLine(".NET: " + Environment.Version);
        var library = typeof(Awk).Assembly;
        Console.WriteLine("Library build: " + (library.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unavailable"));
        Console.WriteLine("Library SHA256: " + await ReferenceReport.HashFileAsync(library.Location, cancellationToken).ConfigureAwait(false));
        Console.WriteLine("Benchmark SHA256: " + await ReferenceReport.HashFileAsync(typeof(BenchmarkCounterReport).Assembly.Location, cancellationToken).ConfigureAwait(false));
        Console.WriteLine();
        var observed = Enum.GetValues<AwkCounter>().Where(counter => counter != AwkCounter.Count).ToArray();
        Console.WriteLine("| Scenario | " + string.Join(" | ", observed) + " |");
        Console.WriteLine("| --- |" + string.Concat(Enumerable.Repeat(" ---: |", observed.Length)));

        for (var index = 0; index < Scenarios.Length; index++)
        {
            var scenario = Scenarios[index];
            var result = await RunScenarioAsync(scenario, cancellationToken).ConfigureAwait(false);
            PrintRow(scenario, result);
        }

        static async Task<AwkRuntimeCounters> RunScenarioAsync(
            CounterScenario scenario,
            CancellationToken cancellationToken)
        {
            var counters = AwkRuntimeCounters.CreateEnabled();
            var options = new AwkRuntimeOptions(
                () => new AwkUnixTimestampSeconds(0),
                TimeZoneInfo.Utc,
                new AwkRandomGenerator(),
                counters,
                AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance) with
            { MemoryBudget = AwkMemoryBudget.CreateFixed(CounterExecutionMemoryLimitBytes) };
            using var host = new ReferenceBenchmarkHost(scenario.Files, [], capture: false);
            var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", scenario.Arguments, []);
            var exitCode = await AwkExecutor.ExecuteAsync(host, command, cancellationToken, options)
                .ConfigureAwait(false);
            var run = host.Finish(exitCode.Value);
            ReferenceExecution.Verify(run, scenario.ExpectedOutput, scenario.Name);

            return counters;
        }

        static void PrintRow(CounterScenario scenario, AwkRuntimeCounters counters)
        {
            var values = Enum.GetValues<AwkCounter>().Where(counter => counter != AwkCounter.Count)
                .Select(counter => counters[counter].ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("| " + EscapeCell(scenario.Name) + " | " + string.Join(" | ", values) + " |");

            static string EscapeCell(string value) =>
                value.Replace("|", "\\|", StringComparison.Ordinal);
        }
    }
    private const int CounterStreamingRecordCount = 10_000;
    private const int CounterComparisonRecordCount = 5_000;

}

internal sealed class CounterScenario(string name, IReadOnlyList<string> arguments,
    IReadOnlyList<BenchmarkFile> files, string expectedOutput)
{
    public string Name { get; } = name;
    public IReadOnlyList<string> Arguments { get; } = arguments;
    public IReadOnlyList<BenchmarkFile> Files { get; } = files;
    public OutputDigest ExpectedOutput { get; } = OutputDigest.FromBytes(Encoding.UTF8.GetBytes(expectedOutput));
}
