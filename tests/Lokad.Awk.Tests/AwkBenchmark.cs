using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

internal static class AwkBenchmark
{
    private const int Iterations = 5;
    private const int LargeScriptStatementCount = 600;

    public static async Task WriteAsync()
    {
        Console.WriteLine("Lokad awk benchmark");
        Console.WriteLine("Iterations: " + Iterations);
        Console.WriteLine();

        var scenarios = CreateScenarios();
        for (var index = 0; index < scenarios.Count; index++)
        {
            var scenario = scenarios[index];
            await RunOnceAsync(scenario).ConfigureAwait(false);

            var elapsed = new List<long>(Iterations);
            var allocatedBytes = new List<long>(Iterations);
            var stdoutBytes = 0;
            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var result = await RunOnceAsync(scenario).ConfigureAwait(false);
                elapsed.Add(result.ElapsedMilliseconds);
                allocatedBytes.Add(result.AllocatedBytes);
                stdoutBytes = result.StdOutLength;
            }

            elapsed.Sort();
            allocatedBytes.Sort();
            Console.WriteLine(
                scenario.Name +
                ": min=" + elapsed[0] + "ms" +
                " median=" + elapsed[elapsed.Count / 2] + "ms" +
                " max=" + elapsed[^1] + "ms" +
                " alloc=" + FormatBytes(allocatedBytes[0]) +
                "/" + FormatBytes(allocatedBytes[allocatedBytes.Count / 2]) +
                "/" + FormatBytes(allocatedBytes[^1]) +
                " stdout=" + stdoutBytes + "B");
        }
    }

    internal static IReadOnlyList<AwkBenchmarkScenario> CreateScenarios()
    {
        return
        [
            new AwkBenchmarkScenario(
                "column extraction",
                "Generated TSV rows for field-selection throughput.",
                ["{print $2}", "/data.tsv"],
                [new AwkBenchmarkFile("/data.tsv", BuildColumnData())],
                1),

            new AwkBenchmarkScenario(
                "aggregation",
                "Generated key/value rows for associative-array aggregation throughput.",
                ["{sum[$1]+=$2} END{for(k in sum) print k, sum[k]}", "/sales.txt"],
                [new AwkBenchmarkFile("/sales.txt", BuildAggregationData())],
                3),

            new AwkBenchmarkScenario(
                "csv-ish delimiter scan",
                "Generated semicolon-delimited rows for field splitting and regex scanning throughput.",
                ["-F;", "NR>1{for(i=1;i<=NF;i++) if($i ~ /error/) c++} END{print c}", "/semi.csv"],
                [new AwkBenchmarkFile("/semi.csv", BuildSemicolonData())],
                4),

            new AwkBenchmarkScenario(
                "log filtering",
                "Generated log lines for pattern filtering throughput.",
                ["/ERROR|WARN/{print $1,$3}", "/app.log"],
                [new AwkBenchmarkFile("/app.log", BuildLogData())],
                1),

            new AwkBenchmarkScenario(
                "find summary",
                "Generated path list for extension-summary throughput.",
                ["{n=split($0, parts, \".\"); ext=(n>1 ? parts[n] : \"(none)\"); count[ext]++} END{for(e in count) print e,count[e]}", "/find.txt"],
                [new AwkBenchmarkFile("/find.txt", BuildFindData())],
                4),

            new AwkBenchmarkScenario(
                "large statement script",
                "Generated many-statement script for parser, binder, lowering, and runtime allocation tracking.",
                [BuildLargeStatementScript(), "/large.tsv"],
                [new AwkBenchmarkFile("/large.tsv", BuildLargeStatementData())],
                2 * LargeScriptStatementCount + 1),

            new AwkBenchmarkScenario(
                "large ASCII print",
                "Generated large ASCII records for default newline decode, record state, and heavy print throughput.",
                ["{print $0}", "/ascii.txt"],
                [new AwkBenchmarkFile("/ascii.txt", BuildLargeAsciiData())],
                1),

            new AwkBenchmarkScenario(
                "large UTF-8 string functions",
                "Generated non-ASCII UTF-8 records for length, substr, and index allocation tracking.",
                ["{total += length($0); hits += index($0, \"é\"); sample = substr($0, 2, 5)} END{print total, hits, sample}", "/utf8.txt"],
                [new AwkBenchmarkFile("/utf8.txt", BuildLargeUtf8Data())],
                1),

            new AwkBenchmarkScenario(
                "default whitespace fields",
                "Generated whitespace-separated rows for the default FS=\" \" splitter path.",
                ["{sum += length($1) + length($NF)} END{print sum}", "/space.txt"],
                [new AwkBenchmarkFile("/space.txt", BuildWhitespaceFieldData())],
                1),

            new AwkBenchmarkScenario(
                "single-byte field separator",
                "Generated pipe-delimited rows for literal single-byte field splitting.",
                ["-F", "|", "{sum += length($2); if($3 ~ /flag/) hits++} END{print sum, hits}", "/pipe.txt"],
                [new AwkBenchmarkFile("/pipe.txt", BuildPipeFieldData())],
                1),

            new AwkBenchmarkScenario(
                "single-byte record separator",
                "Generated single-byte RS stream for custom record separator scanning.",
                ["BEGIN{RS=\"|\"} {sum += length($0)} END{print NR, sum}", "/rs.txt"],
                [new AwkBenchmarkFile("/rs.txt", BuildRecordSeparatorData())],
                1),

            new AwkBenchmarkScenario(
                "regex record separator",
                "Generated mixed separator stream for true regex RS allocation tracking.",
                ["BEGIN{RS=\"[|;]+\"} {sum += length($0)} END{print NR, sum}", "/rs.txt"],
                [new AwkBenchmarkFile("/rs.txt", BuildRecordSeparatorData())],
                1),

            new AwkBenchmarkScenario(
                "paragraph record separator",
                "Generated paragraph records for RS=\"\" buffering and field splitting.",
                ["BEGIN{RS=\"\"} {sum += NF + length($0)} END{print NR, sum}", "/paragraph.txt"],
                [new AwkBenchmarkFile("/paragraph.txt", BuildParagraphData())],
                1),

            new AwkBenchmarkScenario(
                "split and gsub",
                "Generated comma rows for split and gsub allocation tracking.",
                ["{n=split($0,a,\",\"); gsub(/[0-9]+/,\"#\",$0); total += n + length($0)} END{print total}", "/split.csv"],
                [new AwkBenchmarkFile("/split.csv", BuildSplitSubstitutionData())],
                1)
        ];
    }

    private static async Task<AwkBenchmarkResult> RunOnceAsync(AwkBenchmarkScenario scenario)
    {
        var fileSystem = new MockFileSystem();
        for (var index = 0; index < scenario.Files.Count; index++)
        {
            var file = scenario.Files[index];
            fileSystem.AddFile(file.Path, file.Content);
        }

        var tool = Awk.TryParse(AwkTestHarness.BuildInvocation("awk", scenario.Arguments, []));
        if (tool is not Awk awk)
            throw new InvalidOperationException("Could not parse awk benchmark command.");

        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();
        var exitCode = await awk.ExecuteAsync(fileSystem, CancellationToken.None).ConfigureAwait(false);
        stopwatch.Stop();
        var allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                "Benchmark scenario failed: " +
                scenario.Name +
                Environment.NewLine +
                fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        }

        return new AwkBenchmarkResult(
            stopwatch.ElapsedMilliseconds,
            allocatedAfter - allocatedBefore,
            fileSystem.GetOutput(AwkFileDescriptor.StdOut).Length);
    }

    private static string FormatBytes(long bytes)
    {
        const long kibibyte = 1024;
        const long mebibyte = kibibyte * 1024;

        if (bytes >= mebibyte)
            return (bytes / (double)mebibyte).ToString("0.0", CultureInfo.InvariantCulture) + "MiB";

        if (bytes >= kibibyte)
            return (bytes / (double)kibibyte).ToString("0.0", CultureInfo.InvariantCulture) + "KiB";

        return bytes.ToString(CultureInfo.InvariantCulture) + "B";
    }

    private static string BuildColumnData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 20_000; index++)
        {
            builder.Append("sku");
            builder.Append(index);
            builder.Append('\t');
            builder.Append("value");
            builder.Append(index % 97);
            builder.Append('\t');
            builder.Append("tail");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildAggregationData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 20_000; index++)
        {
            builder.Append("bucket");
            builder.Append(index % 32);
            builder.Append(' ');
            builder.Append(index % 100);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildSemicolonData()
    {
        var builder = new StringBuilder("Sku;Status;Comment;Qty\n");
        for (var index = 0; index < 15_000; index++)
        {
            builder.Append("S");
            builder.Append(index);
            builder.Append(';');
            builder.Append(index % 13 == 0 ? "KO" : "OK");
            builder.Append(';');
            builder.Append(index % 17 == 0 ? "error: delayed" : "clean");
            builder.Append(';');
            builder.Append(index % 50);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildLogData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 25_000; index++)
        {
            builder.Append("2026-07-04T12:");
            builder.Append((index % 60).ToString("00"));
            builder.Append(":00Z ");
            builder.Append(index % 29 == 0 ? "ERROR" : index % 11 == 0 ? "WARN" : "INFO");
            builder.Append(" worker-");
            builder.Append(index % 8);
            builder.Append(" message-");
            builder.Append(index);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildFindData()
    {
        var extensions = new[] { "cs", "md", "json", "txt", "csv", "ion" };
        var builder = new StringBuilder();
        for (var index = 0; index < 20_000; index++)
        {
            builder.Append("/repo/dir");
            builder.Append(index % 128);
            builder.Append("/file");
            builder.Append(index);
            builder.Append('.');
            builder.Append(extensions[index % extensions.Length]);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildLargeStatementScript()
    {
        var builder = new StringBuilder();

        builder.AppendLine("BEGIN {");
        for (var index = 0; index < LargeScriptStatementCount; index++)
        {
            builder.Append("    v");
            builder.Append(index);
            builder.Append(" = ");
            builder.Append(index);
            builder.AppendLine();
        }

        builder.AppendLine("}");
        builder.AppendLine("{");
        for (var index = 0; index < LargeScriptStatementCount; index++)
        {
            builder.Append("    s");
            builder.Append(index % 16);
            builder.Append(" += $");
            builder.Append(index % 4 + 1);
            builder.AppendLine();
        }

        builder.AppendLine("}");
        builder.Append("END { print ");
        for (var index = 0; index < 16; index++)
        {
            if (index > 0)
                builder.Append(" + ");

            builder.Append('s');
            builder.Append(index);
        }

        builder.AppendLine(" }");
        return builder.ToString();
    }

    private static string BuildLargeStatementData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 200; index++)
        {
            builder.Append(index % 97);
            builder.Append('\t');
            builder.Append(index % 89);
            builder.Append('\t');
            builder.Append(index % 83);
            builder.Append('\t');
            builder.Append(index % 79);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildLargeAsciiData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 15_000; index++)
        {
            builder.Append("alpha-");
            builder.Append(index);
            builder.Append(" beta-");
            builder.Append(index % 97);
            builder.Append(" tail ");
            builder.Append(index % 11);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildLargeUtf8Data()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 12_000; index++)
        {
            builder.Append("caf\u00e9-");
            builder.Append(index);
            builder.Append(" na\u00efve r\u00e9sum\u00e9 東京 ");
            builder.Append(index % 31);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildWhitespaceFieldData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 18_000; index++)
        {
            builder.Append("  key");
            builder.Append(index % 128);
            builder.Append('\t');
            builder.Append("value");
            builder.Append(index);
            builder.Append("   tail");
            builder.Append(index % 17);
            builder.AppendLine("  ");
        }

        return builder.ToString();
    }

    private static string BuildPipeFieldData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 18_000; index++)
        {
            builder.Append("sku");
            builder.Append(index);
            builder.Append('|');
            builder.Append("value");
            builder.Append(index % 211);
            builder.Append('|');
            builder.Append(index % 19 == 0 ? "flagged" : "clean");
            builder.Append('|');
            builder.Append(index % 43);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildRecordSeparatorData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 20_000; index++)
        {
            builder.Append("record");
            builder.Append(index);
            builder.Append(index % 2 == 0 ? '|' : ';');
        }

        return builder.ToString();
    }

    private static string BuildParagraphData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 8_000; index++)
        {
            builder.Append("paragraph ");
            builder.Append(index);
            builder.AppendLine(" first line");
            builder.Append("paragraph ");
            builder.Append(index);
            builder.AppendLine(" second line");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildSplitSubstitutionData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 16_000; index++)
        {
            builder.Append(index);
            builder.Append(',');
            builder.Append("sku");
            builder.Append(index % 257);
            builder.Append(',');
            builder.Append(index % 13 == 0 ? "delayed" : "ready");
            builder.Append(',');
            builder.Append(index % 1000);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    internal sealed class AwkBenchmarkScenario(
        string name,
        string description,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkBenchmarkFile> files,
        int scriptStatementCount)
    {
        public string Name { get; } = name;

        public string Description { get; } = description;

        public IReadOnlyList<string> Arguments { get; } = arguments;

        public IReadOnlyList<AwkBenchmarkFile> Files { get; } = files;

        public int ScriptStatementCount { get; } = scriptStatementCount;
    }

    internal readonly struct AwkBenchmarkFile(string path, string content)
    {
        public string Path { get; } = path;

        public string Content { get; } = content;
    }

    private readonly struct AwkBenchmarkResult(long elapsedMilliseconds, long allocatedBytes, int stdOutLength)
    {
        public long ElapsedMilliseconds { get; } = elapsedMilliseconds;

        public long AllocatedBytes { get; } = allocatedBytes;

        public int StdOutLength { get; } = stdOutLength;
    }
}
