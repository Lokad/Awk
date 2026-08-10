using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Lokad.Awk;

if (args.Length == 1 && string.Equals(args[0], "--counter-report", StringComparison.Ordinal))
{
    await BenchmarkCounterReport.RunAsync(CancellationToken.None).ConfigureAwait(false);
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

public sealed class AwkBenchmarkConfig : ManualConfig
{
    public AwkBenchmarkConfig()
    {
        Add(DefaultConfig.Instance);
        AddColumn(StatisticColumn.Median);
        AddColumn(BenchmarkThroughputColumn.MebibytesPerSecond);
        AddColumn(BenchmarkThroughputColumn.RecordsPerSecond);
    }
}

internal sealed class BenchmarkThroughputColumn : IColumn
{
    public static BenchmarkThroughputColumn MebibytesPerSecond { get; } = new(
        "MiB/s",
        "Input mebibytes processed per second.",
        metrics => metrics.InputBytes / 1048576.0);

    public static BenchmarkThroughputColumn RecordsPerSecond { get; } = new(
        "Records/s",
        "Input records processed per second.",
        metrics => metrics.RecordCount);

    private readonly Func<BenchmarkWorkloadMetrics, double> _resolveWork;

    private BenchmarkThroughputColumn(
        string columnName,
        string legend,
        Func<BenchmarkWorkloadMetrics, double> resolveWork)
    {
        ColumnName = columnName;
        Legend = legend;
        Id = nameof(BenchmarkThroughputColumn) + "." + columnName;
        _resolveWork = resolveWork;
    }

    public string Id { get; }

    public string ColumnName { get; }

    public bool AlwaysShow => true;

    public ColumnCategory Category => ColumnCategory.Custom;

    public int PriorityInCategory => 0;

    public bool IsNumeric => true;

    public UnitType UnitType => UnitType.Dimensionless;

    public string Legend { get; }

    public bool IsAvailable(Summary summary) => true;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) =>
        GetValue(summary, benchmarkCase, summary.Style);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        if (!BenchmarkScenarioCatalog.TryResolveMetrics(
                benchmarkCase.Descriptor.Type.Name,
                benchmarkCase.Descriptor.WorkloadMethod.Name,
                out var metrics))
        {
            return "NA";
        }

        var report = summary[benchmarkCase];
        if (report == null)
            return "NA";

        var statistics = report.ResultStatistics;
        if (statistics == null || statistics.Mean <= 0)
            return "NA";

        var seconds = statistics.Mean / 1_000_000_000.0;
        var value = _resolveWork(metrics) / seconds;
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}

[Config(typeof(AwkBenchmarkConfig))]
[MemoryDiagnoser]
public class FullInvocationBenchmarks
{
    // BenchmarkHost batches on newlines; synthetic lines make custom-RS scans cross host reads.
    private const int CustomRecordSeparatorRecordsPerLine = 8;

    private BenchmarkScenario _columnExtraction = BenchmarkScenario.Empty;
    private BenchmarkScenario _utf8StringFunctions = BenchmarkScenario.Empty;
    private BenchmarkScenario _singleByteFieldSeparator = BenchmarkScenario.Empty;
    private BenchmarkScenario _regexFieldSeparator = BenchmarkScenario.Empty;
    private BenchmarkScenario _regexRecordSeparator = BenchmarkScenario.Empty;
    private BenchmarkScenario _splitAndGsub = BenchmarkScenario.Empty;
    private BenchmarkScenario _dynamicRecordSeparator = BenchmarkScenario.Empty;
    private BenchmarkScenario _nonAsciiRecordSeparator = BenchmarkScenario.Empty;
    private BenchmarkScenario _fieldModes = BenchmarkScenario.Empty;
    private BenchmarkScenario _fieldWidthsUtf8 = BenchmarkScenario.Empty;
    private BenchmarkScenario _largeScalarGrowth = BenchmarkScenario.Empty;
    private BenchmarkScenario _largeFieldList = BenchmarkScenario.Empty;
    private BenchmarkScenario _smallTextSplitArrays = BenchmarkScenario.Empty;
    private BenchmarkScenario _regexReplacementExpansion = BenchmarkScenario.Empty;
    private BenchmarkScenario _arraySorting = BenchmarkScenario.Empty;
    private BenchmarkScenario _heavyConcatByteMode = BenchmarkScenario.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _columnExtraction = BenchmarkScenarioCatalog.ColumnExtraction();
        _utf8StringFunctions = new BenchmarkScenario(
            "UTF-8 string functions",
            "{total += length($0); hits += index($0, \"\u00e9\"); sample = substr($0, 2, 5)} END{print total, hits, sample}",
            ["{total += length($0); hits += index($0, \"\u00e9\"); sample = substr($0, 2, 5)} END{print total, hits, sample}", "/utf8.txt"],
            [new BenchmarkFile("/utf8.txt", CreateLargeUtf8Data())],
            recordCount: 12_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);
        _singleByteFieldSeparator = new BenchmarkScenario(
            "single-byte field separator",
            "{sum += length($2); if($3 ~ /flag/) hits++} END{print sum, hits}",
            ["-F", "|", "{sum += length($2); if($3 ~ /flag/) hits++} END{print sum, hits}", "/pipe.txt"],
            [new BenchmarkFile("/pipe.txt", CreatePipeFieldData())],
            recordCount: 18_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);
        _regexFieldSeparator = BenchmarkScenarioCatalog.RegexFieldSeparator();
        _regexRecordSeparator = new BenchmarkScenario(
            "regex record separator",
            "BEGIN{RS=\"([|]|;)+\"} {sum += length($0)} END{print NR, sum}",
            ["BEGIN{RS=\"([|]|;)+\"} {sum += length($0)} END{print NR, sum}", "/rs.txt"],
            [new BenchmarkFile("/rs.txt", CreateRecordSeparatorData())],
            recordCount: 20_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);
        _splitAndGsub = new BenchmarkScenario(
            "split and gsub",
            "{n=split($0,a,\",\"); gsub(/[0-9]+/,\"#\",$0); total += n + length($0)} END{print total}",
            ["{n=split($0,a,\",\"); gsub(/[0-9]+/,\"#\",$0); total += n + length($0)} END{print total}", "/split.csv"],
            [new BenchmarkFile("/split.csv", CreateSplitSubstitutionData())],
            recordCount: 16_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);
        _dynamicRecordSeparator = BenchmarkScenarioCatalog.DynamicRecordSeparator();
        _nonAsciiRecordSeparator = BenchmarkScenarioCatalog.NonAsciiRecordSeparator();
        _fieldModes = BenchmarkScenarioCatalog.FieldModes();
        _fieldWidthsUtf8 = BenchmarkScenarioCatalog.FieldWidthsUtf8();
        _largeScalarGrowth = BenchmarkScenarioCatalog.LargeScalarGrowth();
        _largeFieldList = BenchmarkScenarioCatalog.LargeFieldList();
        _smallTextSplitArrays = BenchmarkScenarioCatalog.SmallTextSplitArrays();
        _regexReplacementExpansion = BenchmarkScenarioCatalog.RegexReplacementExpansion();
        _arraySorting = BenchmarkScenarioCatalog.ArraySorting();
        _heavyConcatByteMode = BenchmarkScenarioCatalog.HeavyConcatByteMode();

        Validate(_columnExtraction);
        Validate(_utf8StringFunctions);
        Validate(_singleByteFieldSeparator);
        Validate(_regexFieldSeparator);
        Validate(_regexRecordSeparator);
        Validate(_splitAndGsub);
        Validate(_dynamicRecordSeparator);
        Validate(_nonAsciiRecordSeparator);
        Validate(_fieldModes);
        Validate(_fieldWidthsUtf8);
        Validate(_largeScalarGrowth);
        Validate(_largeFieldList);
        Validate(_smallTextSplitArrays);
        Validate(_regexReplacementExpansion);
        Validate(_arraySorting);
        Validate(_heavyConcatByteMode);
    }

    [Benchmark(Baseline = true)]
    public async Task ColumnExtraction()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_columnExtraction).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Utf8StringFunctions()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_utf8StringFunctions).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task SingleByteFieldSeparator()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_singleByteFieldSeparator).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task RegexFieldSeparator()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_regexFieldSeparator).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task RegexRecordSeparator()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_regexRecordSeparator).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task SplitAndGsub()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_splitAndGsub).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task DynamicRecordSeparator()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_dynamicRecordSeparator).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task NonAsciiRecordSeparator()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_nonAsciiRecordSeparator).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task FieldModes()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_fieldModes).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task FieldWidthsUtf8()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_fieldWidthsUtf8).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task LargeScalarGrowth()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_largeScalarGrowth).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task LargeFieldList()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_largeFieldList).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task SmallTextSplitArrays()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_smallTextSplitArrays).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task RegexReplacementExpansion()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_regexReplacementExpansion).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task ArraySorting()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_arraySorting).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task HeavyConcatByteMode()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_heavyConcatByteMode).ConfigureAwait(false);
    }

    private static void Validate(BenchmarkScenario scenario) =>
        BenchmarkExecution.ValidateFullInvocation(scenario);

    internal static string CreateLargeUtf8Data()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 12_000; index++)
        {
            builder.Append("caf\u00e9-");
            builder.Append(index);
            builder.Append(" na\u00efve r\u00e9sum\u00e9 \u6771\u4eac ");
            builder.Append(index % 31);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    internal static string CreatePipeFieldData()
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

    internal static string CreateRecordSeparatorData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 20_000; index++)
        {
            builder.Append("record");
            builder.Append(index);
            if (index % CustomRecordSeparatorRecordsPerLine == CustomRecordSeparatorRecordsPerLine - 1)
                builder.AppendLine();

            builder.Append(index % 2 == 0 ? '|' : ';');
        }

        return builder.ToString();
    }

    internal static string CreateSplitSubstitutionData()
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

    internal static string CreateDynamicRecordSeparatorData()
    {
        var builder = new StringBuilder();
        builder.Append("header|");
        for (var index = 0; index < 14_000; index++)
        {
            builder.Append("item");
            builder.Append(index);
            if (index % CustomRecordSeparatorRecordsPerLine == CustomRecordSeparatorRecordsPerLine - 1)
                builder.AppendLine();

            builder.Append(index % 2 == 0 ? ',' : ';');
        }

        return builder.ToString();
    }

    internal static string CreateNonAsciiRecordSeparatorData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 14_000; index++)
        {
            builder.Append("segment");
            builder.Append(index);
            builder.Append(" café ");
            builder.Append(index % 31);
            if (index % CustomRecordSeparatorRecordsPerLine == CustomRecordSeparatorRecordsPerLine - 1)
                builder.AppendLine();

            builder.Append('\u00e9');
        }

        return builder.ToString();
    }

    internal static string CreateFieldModeData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 10_000; index++)
        {
            builder.Append("AB");
            builder.Append((index % 1000).ToString("000", CultureInfo.InvariantCulture));
            builder.Append(" CD-");
            builder.Append(index % 37);
            builder.Append(";EF");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    internal static string CreateFieldWidthsUtf8Data()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 10_000; index++)
        {
            builder.Append("caf\u00e9bravo");
            builder.Append((index % 100).ToString("00", CultureInfo.InvariantCulture));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    internal static string CreateLargeScalarGrowthData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 2_500; index++)
        {
            builder.Append("chunk");
            builder.Append(index);
            builder.Append('-');
            builder.Append(index % 17);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    internal static string CreateLargeFieldListData()
    {
        var builder = new StringBuilder();
        for (var row = 0; row < 2_000; row++)
        {
            for (var field = 0; field < 240; field++)
            {
                if (field > 0)
                    builder.Append(' ');

                builder.Append('f');
                builder.Append(field);
                builder.Append('_');
                builder.Append(row % 97);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    internal static string CreateSmallTextSplitArrayData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 12_000; index++)
        {
            builder.Append("abcdeabcde");
            builder.Append((char)('a' + index % 26));
            builder.AppendLine("xyz");
        }

        return builder.ToString();
    }

    internal static string CreateRegexReplacementExpansionData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 10_000; index++)
        {
            builder.Append("sku");
            builder.Append(index);
            builder.Append(" item");
            builder.Append(index % 997);
            builder.AppendLine(" tail");
        }

        return builder.ToString();
    }

    internal static string CreateArraySortingData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 8_000; index++)
        {
            builder.Append("key");
            builder.Append(index % 101);
            builder.Append(' ');
            builder.Append(index % 17);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    internal static string CreateHeavyConcatData()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 12_000; index++)
        {
            builder.Append("caf\u00e9 ");
            builder.Append(index);
            builder.Append(" r\u00e9sum\u00e9 ");
            builder.Append(index % 19);
            builder.AppendLine();
        }

        return builder.ToString();
    }
}

[Config(typeof(AwkBenchmarkConfig))]
[MemoryDiagnoser]
public class LegacyLayerBenchmarks
{
    private BenchmarkScenario _scenario = BenchmarkScenario.Empty;
    private AwkCompiledProgram _compiled =
        BenchmarkExecution.CompileLegacyAsync(BenchmarkScenarioCatalog.ColumnExtraction()).GetAwaiter().GetResult();

    [GlobalSetup]
    public void Setup()
    {
        _scenario = BenchmarkScenarioCatalog.ColumnExtraction();
        _compiled = BenchmarkExecution.CompileLegacyAsync(_scenario).GetAwaiter().GetResult();
        BenchmarkExecution.ValidatePrecompiledLegacy(_scenario, _compiled);
    }

    [Benchmark(Baseline = true)]
    public async Task FullCommandLineInvocation()
    {
        _ = await BenchmarkExecution.RunFullInvocationAsync(_scenario).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task CompileLegacyProgram()
    {
        _ = await BenchmarkExecution.CompileLegacyAsync(_scenario).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task ExecutePrecompiledLegacyProgram()
    {
        _ = await BenchmarkExecution.RunPrecompiledLegacyAsync(_compiled, _scenario).ConfigureAwait(false);
    }
}

[Config(typeof(AwkBenchmarkConfig))]
[MemoryDiagnoser]
public class FrontendLayerBenchmarks
{
    private BenchmarkScenario _scenario = BenchmarkScenarioCatalog.ColumnExtraction();
    private AwkParseResult _parsed = BenchmarkExecution.Parse(BenchmarkScenarioCatalog.ColumnExtraction());
    private AwkBindingResult _bound = BenchmarkExecution.Bind(BenchmarkExecution.Parse(BenchmarkScenarioCatalog.ColumnExtraction()));

    [GlobalSetup]
    public void Setup()
    {
        _scenario = BenchmarkScenarioCatalog.ColumnExtraction();
        _parsed = BenchmarkExecution.Parse(_scenario);
        _bound = BenchmarkExecution.Bind(_parsed);
        _ = BenchmarkExecution.CompileExecutable(_bound);
    }

    [Benchmark(Baseline = true)]
    public void ParseOnly()
    {
        _ = BenchmarkExecution.Parse(_scenario);
    }

    [Benchmark]
    public void BindAndLowerOnly()
    {
        _ = BenchmarkExecution.Bind(_parsed);
    }

    [Benchmark]
    public void CompileExecutableProgram()
    {
        _ = BenchmarkExecution.CompileExecutable(_bound);
    }
}

[Config(typeof(AwkBenchmarkConfig))]
[MemoryDiagnoser]
public class FrontendScalingBenchmarks
{
    private BenchmarkScenario _scenario = BenchmarkScenario.Empty;

    [Params(10, 100, 1_000)]
    public int RuleCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var source = new StringBuilder();
        for (var index = 0; index < RuleCount; index++)
        {
            source.Append("$1 == ");
            source.Append(index);
            source.Append(" { value");
            source.Append(index);
            source.Append(" = $2 + ");
            source.Append(index);
            source.AppendLine("; print value" + index + " }");
        }

        var programSource = source.ToString();
        _scenario = new BenchmarkScenario(
            "frontend scaling",
            programSource,
            [programSource],
            [],
            recordCount: RuleCount,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);
        _ = BenchmarkExecution.Parse(_scenario);
    }

    [Benchmark]
    public void ParseOnly()
    {
        _ = BenchmarkExecution.Parse(_scenario);
    }
}

[Config(typeof(AwkBenchmarkConfig))]
[MemoryDiagnoser]
public class ExecutableLayerBenchmarks
{
    private BenchmarkScenario _vmScenario = BenchmarkScenarioCatalog.ColumnExtraction();
    private BenchmarkScenario _fallbackScenario = BenchmarkScenarioCatalog.ExecutableFallbackDispatch();
    private AwkCompiledProgram _vmCompiled =
        BenchmarkExecution.CompileLegacyAsync(BenchmarkScenarioCatalog.ColumnExtraction()).GetAwaiter().GetResult();
    private AwkCompiledProgram _fallbackCompiled =
        BenchmarkExecution.CompileLegacyAsync(BenchmarkScenarioCatalog.ExecutableFallbackDispatch()).GetAwaiter().GetResult();
    private AwkExecutableProgram _vmExecutable =
        BenchmarkExecution.CompileExecutable(BenchmarkExecution.Bind(BenchmarkExecution.Parse(BenchmarkScenarioCatalog.ColumnExtraction())));
    private AwkExecutableProgram _fallbackExecutable =
        BenchmarkExecution.CompileExecutable(BenchmarkExecution.Bind(BenchmarkExecution.Parse(BenchmarkScenarioCatalog.ExecutableFallbackDispatch())));

    [GlobalSetup]
    public void Setup()
    {
        _vmScenario = BenchmarkScenarioCatalog.ColumnExtraction();
        _fallbackScenario = BenchmarkScenarioCatalog.ExecutableFallbackDispatch();
        _vmCompiled = BenchmarkExecution.CompileLegacyAsync(_vmScenario).GetAwaiter().GetResult();
        _fallbackCompiled = BenchmarkExecution.CompileLegacyAsync(_fallbackScenario).GetAwaiter().GetResult();
        _vmExecutable = _vmCompiled.ExecutableProgram;
        _fallbackExecutable = _fallbackCompiled.ExecutableProgram;
        BenchmarkExecution.ValidatePrecompiledExecutable(_vmScenario, _vmCompiled, _vmExecutable);
        BenchmarkExecution.ValidatePrecompiledExecutable(_fallbackScenario, _fallbackCompiled, _fallbackExecutable);
    }

    [Benchmark(Baseline = true)]
    public async Task ExecutePrecompiledLegacyProgram()
    {
        _ = await BenchmarkExecution.RunPrecompiledLegacyAsync(_vmCompiled, _vmScenario).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task ExecutePrecompiledExecutableVmProgram()
    {
        _ = await BenchmarkExecution
            .RunPrecompiledExecutableAsync(_vmCompiled, _vmExecutable, _vmScenario)
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task ExecuteExecutableFallbackDispatch()
    {
        _ = await BenchmarkExecution
            .RunPrecompiledExecutableAsync(_fallbackCompiled, _fallbackExecutable, _fallbackScenario)
            .ConfigureAwait(false);
    }
}

internal static class BenchmarkExecution
{
    private static readonly AwkRuntimeOptions ExecutableRuntimeOptions = new(
        () => new AwkUnixTimestampSeconds(0),
        TimeZoneInfo.Utc,
        new AwkRandomGenerator(),
        AwkRuntimeCounters.Disabled,
        AwkRuntimeLimits.CreateDefault(),
        AwkRegexEngineKind.DotNetCompatibility,
        executionEngineKind: AwkExecutionEngineKind.Executable);

    public static async Task<BenchmarkRunResult> RunFullInvocationAsync(BenchmarkScenario scenario)
    {
        var host = BenchmarkHost.FromFiles(scenario.Files);
        var awk = Awk.TryParse(AwkCommandInvocation.CreateWithStandardDescriptors("awk", scenario.Arguments, []));
        if (awk is null)
            throw new InvalidOperationException("Benchmark command could not be parsed: " + scenario.Name);

        var exitCode = await awk.ExecuteAsync(host, CancellationToken.None).ConfigureAwait(false);
        if (exitCode.Failed)
            throw new InvalidOperationException("Benchmark scenario failed: " + scenario.Name);

        return host.Result;
    }

    public static AwkParseResult Parse(BenchmarkScenario scenario)
    {
        var parse = AwkParser.Parse(scenario.ProgramSource);
        if (!parse.Success)
            throw new InvalidOperationException("Benchmark parse failed: " + scenario.Name);

        return parse;
    }

    public static AwkBindingResult Bind(AwkParseResult parse)
    {
        var binding = AwkBinder.Bind(parse);
        if (!binding.Success)
            throw new InvalidOperationException("Benchmark binding failed.");

        return binding;
    }

    public static AwkExecutableProgram CompileExecutable(AwkBindingResult binding)
    {
        if (!binding.Program.HasProgram)
            throw new InvalidOperationException("Benchmark binding has no program.");

        return AwkExecutableProgram.Compile(binding.Program.Program);
    }

    public static async Task<AwkCompiledProgram> CompileLegacyAsync(BenchmarkScenario scenario)
    {
        var host = BenchmarkHost.FromFiles(scenario.Files);
        return await AwkExecutor
            .CompileAsync(
                host,
                AwkCommandInvocation.CreateWithStandardDescriptors("awk", scenario.Arguments, []),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    public static async Task<BenchmarkRunResult> RunPrecompiledLegacyAsync(
        AwkCompiledProgram compiled,
        BenchmarkScenario scenario)
    {
        var host = BenchmarkHost.FromFiles(scenario.Files);
        var exitCode = await compiled.ExecuteAsync(host, CancellationToken.None).ConfigureAwait(false);
        if (exitCode.Failed)
            throw new InvalidOperationException("Precompiled benchmark scenario failed: " + scenario.Name);

        return host.Result;
    }

    public static async Task<BenchmarkRunResult> RunPrecompiledExecutableAsync(
        AwkCompiledProgram compiled,
        AwkExecutableProgram executable,
        BenchmarkScenario scenario)
    {
        var host = BenchmarkHost.FromFiles(scenario.Files);
        var exitCode = await compiled
            .ExecuteAsync(host, CancellationToken.None, ExecutableRuntimeOptions, executable)
            .ConfigureAwait(false);
        if (exitCode.Failed)
            throw new InvalidOperationException("Executable benchmark scenario failed: " + scenario.Name);

        return host.Result;
    }

    public static void ValidateFullInvocation(BenchmarkScenario scenario)
    {
        var result = RunFullInvocationAsync(scenario).GetAwaiter().GetResult();
        if (result.StandardErrorBytes != 0)
            throw new InvalidOperationException("Benchmark scenario wrote stderr: " + scenario.Name);
    }

    public static void ValidatePrecompiledLegacy(BenchmarkScenario scenario, AwkCompiledProgram compiled)
    {
        var full = RunFullInvocationAsync(scenario).GetAwaiter().GetResult();
        var precompiled = RunPrecompiledLegacyAsync(compiled, scenario).GetAwaiter().GetResult();
        if (full != precompiled)
            throw new InvalidOperationException("Benchmark layer output mismatch: " + scenario.Name);
    }

    public static void ValidatePrecompiledExecutable(
        BenchmarkScenario scenario,
        AwkCompiledProgram compiled,
        AwkExecutableProgram executable)
    {
        var legacy = RunPrecompiledLegacyAsync(compiled, scenario).GetAwaiter().GetResult();
        var executableResult = RunPrecompiledExecutableAsync(compiled, executable, scenario).GetAwaiter().GetResult();
        if (legacy != executableResult)
            throw new InvalidOperationException("Executable benchmark layer output mismatch: " + scenario.Name);
    }
}

internal static class BenchmarkCounterReport
{
    private const long CounterExecutionMemoryLimitBytes = 1L << 30;
    private static readonly BenchmarkScenario[] Scenarios = CreateScenarios();

    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("# Lokad.Awk Benchmark Counter Report");
        Console.WriteLine();
        Console.WriteLine("Command: `dotnet run -c Release --project .\\benchmarks\\Lokad.Awk.Benchmarks\\Lokad.Awk.Benchmarks.csproj -- --counter-report`");
        Console.WriteLine("Date UTC: " + DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        Console.WriteLine("Machine: " + Environment.MachineName);
        Console.WriteLine("OS: " + Environment.OSVersion);
        Console.WriteLine(".NET: " + Environment.Version);
        Console.WriteLine();
        Console.WriteLine("| Scenario | Records | Input bytes | Output bytes | Execution steps | String decodes | AsString calls | Decode misses | UTF-8 encodes | AwkText allocations | AwkText bytes | Regex matches | Regex DotNet text | Regex pattern gap | Regex string adapter | Regex policy bytes | Regex groups | RS resolutions | RS bytes scanned | RS tail bytes moved | Formatting fallbacks | String interop | Byte comparisons | Concat bytes copied | Rune slice bytes | Rune indexes | Rune index bytes | VM instructions | VM fallbacks | Memory peak |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");

        for (var index = 0; index < Scenarios.Length; index++)
        {
            var scenario = Scenarios[index];
            var result = await RunScenarioAsync(scenario, cancellationToken).ConfigureAwait(false);
            PrintRow(scenario, result);
        }
    }

    private static async Task<CounterScenarioResult> RunScenarioAsync(
        BenchmarkScenario scenario,
        CancellationToken cancellationToken)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault()
                .WithMaxExecutionMemoryBytes(CounterExecutionMemoryLimitBytes),
            executionEngineKind: scenario.CounterExecutionEngineKind,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);
        var compiled = await AwkExecutor
            .CompileAsync(
                BenchmarkHost.FromFiles(scenario.Files),
                AwkCommandInvocation.CreateWithStandardDescriptors("awk", scenario.Arguments, []),
                cancellationToken)
            .ConfigureAwait(false);
        var host = BenchmarkHost.FromFiles(scenario.Files);
        var exitCode = await compiled.ExecuteAsync(host, cancellationToken, options).ConfigureAwait(false);
        if (exitCode.Failed)
            throw new InvalidOperationException("Counter report scenario failed: " + scenario.Name);

        var run = host.Result;
        if (run.StandardErrorBytes != 0)
            throw new InvalidOperationException("Counter report scenario wrote stderr: " + scenario.Name);

        return new CounterScenarioResult(counters, run);
    }

    private static void PrintRow(BenchmarkScenario scenario, CounterScenarioResult result)
    {
        var counters = result.Counters;
        Console.WriteLine(
            string.Join(
                " | ",
                "| " + EscapeCell(scenario.Name),
                scenario.Metrics.RecordCount.ToString(CultureInfo.InvariantCulture),
                scenario.Metrics.InputBytes.ToString(CultureInfo.InvariantCulture),
                result.Run.StandardOutputBytes.ToString(CultureInfo.InvariantCulture),
                counters.ExecutionSteps.ToString(CultureInfo.InvariantCulture),
                counters.StringDecodes.ToString(CultureInfo.InvariantCulture),
                counters.AwkTextAsStringCalls.ToString(CultureInfo.InvariantCulture),
                counters.AwkTextAsStringDecodeMisses.ToString(CultureInfo.InvariantCulture),
                counters.Utf8Encodes.ToString(CultureInfo.InvariantCulture),
                counters.AwkTextAllocations.ToString(CultureInfo.InvariantCulture),
                counters.AwkTextPayloadBytes.ToString(CultureInfo.InvariantCulture),
                counters.RegexMatches.ToString(CultureInfo.InvariantCulture),
                counters.RegexDotNetTextMatches.ToString(CultureInfo.InvariantCulture),
                counters.RegexPatternGapFallbacks.ToString(CultureInfo.InvariantCulture),
                counters.RegexStringAdapterMatches.ToString(CultureInfo.InvariantCulture),
                counters.RegexPolicyInputBytesInspected.ToString(CultureInfo.InvariantCulture),
                counters.RegexGroupsMaterialized.ToString(CultureInfo.InvariantCulture),
                counters.RecordSeparatorResolutions.ToString(CultureInfo.InvariantCulture),
                counters.RecordSeparatorBytesScanned.ToString(CultureInfo.InvariantCulture),
                counters.RecordSeparatorTailBytesMoved.ToString(CultureInfo.InvariantCulture),
                counters.FormattingFallbacks.ToString(CultureInfo.InvariantCulture),
                counters.StringInteropFallbacks.ToString(CultureInfo.InvariantCulture),
                counters.ByteTextComparisons.ToString(CultureInfo.InvariantCulture),
                counters.ConcatenationBytesCopied.ToString(CultureInfo.InvariantCulture),
                counters.RuneSliceBytesScanned.ToString(CultureInfo.InvariantCulture),
                counters.RuneSliceIndexAllocations.ToString(CultureInfo.InvariantCulture),
                counters.RuneSliceIndexBytes.ToString(CultureInfo.InvariantCulture),
                counters.VmInstructions.ToString(CultureInfo.InvariantCulture),
                counters.VmFallbackCalls.ToString(CultureInfo.InvariantCulture),
                counters.MemoryPeakLiveBytes.ToString(CultureInfo.InvariantCulture) + " |"));
    }

    private static string EscapeCell(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal);

    private static BenchmarkScenario[] CreateScenarios() =>
    [
        new(
            "counter column extraction",
            "{print $2}",
            ["{print $2}", "/data.tsv"],
            [new BenchmarkFile("/data.tsv", "sku1\tvalue1\ttail\nsku2\tvalue2\ttail\nsku3\tvalue3\ttail\n")],
            recordCount: 3,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter streaming projection calibration",
            "{print $2}",
            ["{print $2}", "/stream.tsv"],
            [new BenchmarkFile("/stream.tsv", BuildCounterStreamingRows())],
            recordCount: CounterStreamingRecordCount,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter three-key filter calibration",
            "$1 != \"\" && $2 != \"\" && $3 == \"open\" {matches++} END{print matches+0}",
            ["$1 != \"\" && $2 != \"\" && $3 == \"open\" {matches++} END{print matches+0}", "/stream.tsv"],
            [new BenchmarkFile("/stream.tsv", BuildCounterStreamingRows())],
            recordCount: CounterStreamingRecordCount,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter associative aggregation calibration",
            "{sum[$1,$2]+=$4; count[$3]++} END{print length(sum), length(count)}",
            ["{sum[$1,$2]+=$4; count[$3]++} END{print length(sum), length(count)}", "/stream.tsv"],
            [new BenchmarkFile("/stream.tsv", BuildCounterStreamingRows())],
            recordCount: CounterStreamingRecordCount,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter two-file comparison calibration",
            "NR==FNR{left[$1]=$2; next} ($1 in left) && left[$1]!=$2{differences++} END{print differences+0}",
            ["NR==FNR{left[$1]=$2; next} ($1 in left) && left[$1]!=$2{differences++} END{print differences+0}", "/left.tsv", "/right.tsv"],
            [
                new BenchmarkFile("/left.tsv", BuildCounterComparisonRows(changed: false)),
                new BenchmarkFile("/right.tsv", BuildCounterComparisonRows(changed: true))
            ],
            recordCount: CounterComparisonRecordCount * 2,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter arithmetic loop",
            "BEGIN{for(i=1;i<=100;i++) total += i * 2; print total}",
            ["BEGIN{for(i=1;i<=100;i++) total += i * 2; print total}"],
            [],
            recordCount: 0,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter print-heavy action",
            "BEGIN{for(i=1;i<=50;i++) print \"row\", i}",
            ["BEGIN{for(i=1;i<=50;i++) print \"row\", i}"],
            [],
            recordCount: 0,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter regex field separator",
            "BEGIN{FS=\"[|;]\"} {sum += NF + length($2)} END{print sum}",
            ["BEGIN{FS=\"[|;]\"} {sum += NF + length($2)} END{print sum}", "/pipe.txt"],
            [new BenchmarkFile("/pipe.txt", "a|beta;flag\nb|delta;plain\nc|gamma;flag\n")],
            recordCount: 3,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter dynamic record separator",
            "BEGIN{RS=\"|\"} NR==1{RS=\"[,;]\"} {total += length($0)} END{print NR, total}",
            ["BEGIN{RS=\"|\"} NR==1{RS=\"[,;]\"} {total += length($0)} END{print NR, total}", "/dynamic-rs.txt"],
            [new BenchmarkFile("/dynamic-rs.txt", "first|second,third;fourth;")],
            recordCount: 4,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter grouped byte-run record separator",
            "BEGIN{RS=\"([,;])+\"} {print length($0)}",
            ["BEGIN{RS=\"([,;])+\"} {print length($0)}", "/stream.tsv"],
            [new BenchmarkFile("/stream.tsv", BuildCounterStreamingRows())],
            recordCount: 1,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter non-ASCII record separator",
            "BEGIN{RS=\"\u00e9\"} {total += length($0)} END{print NR, total}",
            ["BEGIN{RS=\"\u00e9\"} {total += length($0)} END{print NR, total}", "/non-ascii-rs.txt"],
            [new BenchmarkFile("/non-ascii-rs.txt", "caf\u00e91\u00e9r\u00e9sum\u00e92\u00e9")],
            recordCount: 3,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter field modes",
            "NR%2{FPAT=\"[A-Z]+|[0-9]+\"; FIELDWIDTHS=\"\"; fp += NF; split($0,a,/[0-9]+/,s); sep += length(s[1]); next} {FPAT=\"\"; FIELDWIDTHS=\"2 3\"; fw += NF} END{print fw, fp, sep}",
            ["NR%2{FPAT=\"[A-Z]+|[0-9]+\"; FIELDWIDTHS=\"\"; fp += NF; split($0,a,/[0-9]+/,s); sep += length(s[1]); next} {FPAT=\"\"; FIELDWIDTHS=\"2 3\"; fw += NF} END{print fw, fp, sep}", "/field-modes.txt"],
            [new BenchmarkFile("/field-modes.txt", "A-12-B\nabcdef\nC-34-D\nghijkl\n")],
            recordCount: 4,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter UTF-8 FIELDWIDTHS",
            "BEGIN{FIELDWIDTHS=\"4 5 2\"} {total += length($1) + length($2) + NF} END{print total}",
            ["BEGIN{FIELDWIDTHS=\"4 5 2\"} {total += length($1) + length($2) + NF} END{print total}", "/fieldwidths.txt"],
            [new BenchmarkFile("/fieldwidths.txt", "caf\u00e9bravo12\nna\u00efveplain34\n")],
            recordCount: 2,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter large split fields",
            "{n=split($0,a,\" \"); total += n + length($1) + length($NF) + length(a[1]) + length(a[n])} END{print total}",
            ["{n=split($0,a,\" \"); total += n + length($1) + length($NF) + length(a[1]) + length(a[n])} END{print total}", "/split-fields.txt"],
            [new BenchmarkFile("/split-fields.txt", BuildCounterSeparatedRows())],
            recordCount: 32,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter large scalar growth",
            "{x = x $0} END{print length(x)}",
            ["{x = x $0} END{print length(x)}", "/scalar.txt"],
            [new BenchmarkFile("/scalar.txt", "alpha\nbeta\ngamma\n")],
            recordCount: 3,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter large field list",
            "{total += NF + length($8)} END{print total}",
            ["{total += NF + length($8)} END{print total}", "/fields.txt"],
            [new BenchmarkFile("/fields.txt", "a b c d e f g h i j\n1 2 3 4 5 6 7 8 9 10\n")],
            recordCount: 2,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter small text split arrays",
            "{n=split($0,a,\"\"); total += n + length(a[1]) + length(a[n])} END{print total}",
            ["{n=split($0,a,\"\"); total += n + length(a[1]) + length(a[n])} END{print total}", "/small-split.txt"],
            [new BenchmarkFile("/small-split.txt", "abcdef\nbcdefg\n")],
            recordCount: 2,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter regex replacement expansion",
            "{gsub(/([[:alpha:]]+)([[:digit:]]+)/,\"<&>-<&>-<&>\",$0); total += length($0)} END{print total}",
            ["{gsub(/([[:alpha:]]+)([[:digit:]]+)/,\"<&>-<&>-<&>\",$0); total += length($0)} END{print total}", "/replace.txt"],
            [new BenchmarkFile("/replace.txt", "sku1 item22\nsku3 item44\n")],
            recordCount: 2,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter regex capture array",
            "BEGIN{if(match(\"abc123\",/([a-z]+)([0-9]+)/,m)) print m[1],m[2]}",
            ["BEGIN{if(match(\"abc123\",/([a-z]+)([0-9]+)/,m)) print m[1],m[2]}"],
            [],
            recordCount: 0,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter regex pattern gap",
            "BEGIN{s=\"abc\"; print match(s,/\\`a/)}",
            ["BEGIN{s=\"abc\"; print match(s,/\\`a/)}"],
            [],
            recordCount: 0,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter array sorting",
            "{a[$1] += $2} END{PROCINFO[\"sorted_in\"]=\"@ind_str_asc\"; for(k in a) print k, a[k]}",
            ["{a[$1] += $2} END{PROCINFO[\"sorted_in\"]=\"@ind_str_asc\"; for(k in a) print k, a[k]}", "/array-sort.txt"],
            [new BenchmarkFile("/array-sort.txt", "b 2\na 1\nb 3\n")],
            recordCount: 3,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter small ordinal array",
            "BEGIN{for(i=1;i<=64;i++) a[i]=i; for(i=1;i<=64;i++) total += a[i]; print total}",
            ["BEGIN{for(i=1;i<=64;i++) a[i]=i; for(i=1;i<=64;i++) total += a[i]; print total}"],
            [],
            recordCount: 0,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter user-function loop",
            "function add(x,y){return x+y} BEGIN{for(i=1;i<=80;i++) total=add(total,i); print total}",
            ["function add(x,y){return x+y} BEGIN{for(i=1;i<=80;i++) total=add(total,i); print total}"],
            [],
            recordCount: 0,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter UTF-8 rune slicing",
            "{for(i=1;i<=length($0);i++) last=substr($0,i,1)} END{print last}",
            ["{for(i=1;i<=length($0);i++) last=substr($0,i,1)} END{print last}", "/runes.txt"],
            [new BenchmarkFile("/runes.txt", new string('\u00e9', 4_096) + "\n")],
            recordCount: 1,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter byte concat",
            "{x = $1 \"|\" $2 \"|\" $3; total += length(x)} END{print total}",
            ["--characters-as-bytes", "{x = $1 \"|\" $2 \"|\" $3; total += length(x)} END{print total}", "/concat.txt"],
            [new BenchmarkFile("/concat.txt", "a b c\nx y z\n")],
            recordCount: 2,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter),
        new(
            "counter executable opcode execution",
            "{print $2}",
            ["{print $2}", "/data.tsv"],
            [new BenchmarkFile("/data.tsv", "sku1\tvalue1\ttail\nsku2\tvalue2\ttail\n")],
            recordCount: 2,
            counterExecutionEngineKind: AwkExecutionEngineKind.Executable),
        new(
            "counter executable mixed fallback dispatch",
            "function inc(x){return x+1} {print inc($2)}",
            ["function inc(x){return x+1} {print inc($2)}", "/data.tsv"],
            [new BenchmarkFile("/data.tsv", "sku1\t1\ttail\nsku2\t2\ttail\n")],
            recordCount: 2,
            counterExecutionEngineKind: AwkExecutionEngineKind.Executable)
    ];

    private const int CounterStreamingRecordCount = 10_000;
    private const int CounterComparisonRecordCount = 5_000;

    private static string BuildCounterStreamingRows()
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

    private static string BuildCounterComparisonRows(bool changed)
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

    private static string BuildCounterSeparatedRows()
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

    private readonly record struct CounterScenarioResult(
        AwkRuntimeCounters Counters,
        BenchmarkRunResult Run);
}

internal static class BenchmarkScenarioCatalog
{
    public static BenchmarkScenario ColumnExtraction() =>
        new(
            "column extraction",
            "{print $2}",
            ["{print $2}", "/data.tsv"],
            [new BenchmarkFile("/data.tsv", BuildColumnData())],
            recordCount: 20_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario DynamicRecordSeparator() =>
        new(
            "dynamic record separator",
            "BEGIN{RS=\"|\"} NR==1{RS=\"[,;]\"} {total += length($0)} END{print NR, total}",
            ["BEGIN{RS=\"|\"} NR==1{RS=\"[,;]\"} {total += length($0)} END{print NR, total}", "/dynamic-rs.txt"],
            [new BenchmarkFile("/dynamic-rs.txt", FullInvocationBenchmarks.CreateDynamicRecordSeparatorData())],
            recordCount: 14_001,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario NonAsciiRecordSeparator() =>
        new(
            "non-ASCII record separator",
            "BEGIN{RS=\"\u00e9\"} {total += length($0)} END{print NR, total}",
            ["BEGIN{RS=\"\u00e9\"} {total += length($0)} END{print NR, total}", "/non-ascii-rs.txt"],
            [new BenchmarkFile("/non-ascii-rs.txt", FullInvocationBenchmarks.CreateNonAsciiRecordSeparatorData())],
            recordCount: 14_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario RegexFieldSeparator() =>
        new(
            "regex field separator",
            "BEGIN{FS=\"[|;]\"} {sum += NF + length($2)} END{print sum}",
            ["BEGIN{FS=\"[|;]\"} {sum += NF + length($2)} END{print sum}", "/pipe.txt"],
            [new BenchmarkFile("/pipe.txt", FullInvocationBenchmarks.CreatePipeFieldData())],
            recordCount: 18_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario FieldModes() =>
        new(
            "field modes",
            "NR%2{FPAT=\"[A-Z]+|[0-9]+\"; FIELDWIDTHS=\"\"; fp += NF; split($0,a,/[0-9]+/,s); sep += length(s[1]); next} {FPAT=\"\"; FIELDWIDTHS=\"2 3\"; fw += NF} END{print fw, fp, sep}",
            ["NR%2{FPAT=\"[A-Z]+|[0-9]+\"; FIELDWIDTHS=\"\"; fp += NF; split($0,a,/[0-9]+/,s); sep += length(s[1]); next} {FPAT=\"\"; FIELDWIDTHS=\"2 3\"; fw += NF} END{print fw, fp, sep}", "/field-modes.txt"],
            [new BenchmarkFile("/field-modes.txt", FullInvocationBenchmarks.CreateFieldModeData())],
            recordCount: 10_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario FieldWidthsUtf8() =>
        new(
            "UTF-8 FIELDWIDTHS",
            "BEGIN{FIELDWIDTHS=\"4 5 2\"} {total += length($1) + length($2) + NF} END{print total}",
            ["BEGIN{FIELDWIDTHS=\"4 5 2\"} {total += length($1) + length($2) + NF} END{print total}", "/fieldwidths.txt"],
            [new BenchmarkFile("/fieldwidths.txt", FullInvocationBenchmarks.CreateFieldWidthsUtf8Data())],
            recordCount: 10_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario LargeScalarGrowth() =>
        new(
            "large scalar growth",
            "{x = x $0; if (NR % 500 == 0) sample = length(x)} END{print length(x), sample}",
            ["{x = x $0; if (NR % 500 == 0) sample = length(x)} END{print length(x), sample}", "/scalar.txt"],
            [new BenchmarkFile("/scalar.txt", FullInvocationBenchmarks.CreateLargeScalarGrowthData())],
            recordCount: 2_500,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario LargeFieldList() =>
        new(
            "large field list",
            "{total += NF + length($200)} END{print total}",
            ["{total += NF + length($200)} END{print total}", "/fields.txt"],
            [new BenchmarkFile("/fields.txt", FullInvocationBenchmarks.CreateLargeFieldListData())],
            recordCount: 2_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario SmallTextSplitArrays() =>
        new(
            "small AwkText split arrays",
            "{n=split($0,a,\"\"); total += n + length(a[1]) + length(a[n])} END{print total}",
            ["{n=split($0,a,\"\"); total += n + length(a[1]) + length(a[n])} END{print total}", "/small-split.txt"],
            [new BenchmarkFile("/small-split.txt", FullInvocationBenchmarks.CreateSmallTextSplitArrayData())],
            recordCount: 12_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario RegexReplacementExpansion() =>
        new(
            "regex replacement expansion",
            "{gsub(/([[:alpha:]]+)([[:digit:]]+)/,\"<&>-<&>-<&>\",$0); total += length($0)} END{print total}",
            ["{gsub(/([[:alpha:]]+)([[:digit:]]+)/,\"<&>-<&>-<&>\",$0); total += length($0)} END{print total}", "/replace.txt"],
            [new BenchmarkFile("/replace.txt", FullInvocationBenchmarks.CreateRegexReplacementExpansionData())],
            recordCount: 10_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario ArraySorting() =>
        new(
            "array sorting",
            "{a[$1] += $2} END{PROCINFO[\"sorted_in\"]=\"@ind_str_asc\"; for(k in a) print k, a[k]}",
            ["{a[$1] += $2} END{PROCINFO[\"sorted_in\"]=\"@ind_str_asc\"; for(k in a) print k, a[k]}", "/array-sort.txt"],
            [new BenchmarkFile("/array-sort.txt", FullInvocationBenchmarks.CreateArraySortingData())],
            recordCount: 8_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario HeavyConcatByteMode() =>
        new(
            "heavy concat byte mode",
            "{x = $1 \"|\" $2 \"|\" $3 \"|\" $4; total += length(x)} END{print total}",
            ["--characters-as-bytes", "{x = $1 \"|\" $2 \"|\" $3 \"|\" $4; total += length(x)} END{print total}", "/concat.txt"],
            [new BenchmarkFile("/concat.txt", FullInvocationBenchmarks.CreateHeavyConcatData())],
            recordCount: 12_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static BenchmarkScenario ExecutableFallbackDispatch() =>
        new(
            "executable fallback dispatch",
            "function inc(x){return x+1} {print inc($2)}",
            ["function inc(x){return x+1} {print inc($2)}", "/data.tsv"],
            [new BenchmarkFile("/data.tsv", BuildColumnData())],
            recordCount: 20_000,
            counterExecutionEngineKind: AwkExecutionEngineKind.Interpreter);

    public static bool TryResolveMetrics(string benchmarkTypeName, string benchmarkMethodName, out BenchmarkWorkloadMetrics metrics)
    {
        if (benchmarkTypeName == nameof(LegacyLayerBenchmarks))
        {
            metrics = ColumnExtraction().Metrics;
            return true;
        }

        if (benchmarkTypeName == nameof(FrontendLayerBenchmarks))
        {
            metrics = ColumnExtraction().Metrics;
            return true;
        }

        if (benchmarkTypeName == nameof(ExecutableLayerBenchmarks))
        {
            metrics = benchmarkMethodName == nameof(ExecutableLayerBenchmarks.ExecuteExecutableFallbackDispatch)
                ? ExecutableFallbackDispatch().Metrics
                : ColumnExtraction().Metrics;
            return true;
        }

        metrics = benchmarkMethodName switch
        {
            nameof(FullInvocationBenchmarks.ColumnExtraction) => ColumnExtraction().Metrics,
            nameof(FullInvocationBenchmarks.Utf8StringFunctions) => new BenchmarkWorkloadMetrics(
                new BenchmarkFile("/utf8.txt", FullInvocationBenchmarks.CreateLargeUtf8Data()).ContentBytes.Length,
                12_000),
            nameof(FullInvocationBenchmarks.SingleByteFieldSeparator) => new BenchmarkWorkloadMetrics(
                new BenchmarkFile("/pipe.txt", FullInvocationBenchmarks.CreatePipeFieldData()).ContentBytes.Length,
                18_000),
            nameof(FullInvocationBenchmarks.RegexFieldSeparator) => RegexFieldSeparator().Metrics,
            nameof(FullInvocationBenchmarks.RegexRecordSeparator) => new BenchmarkWorkloadMetrics(
                new BenchmarkFile("/rs.txt", FullInvocationBenchmarks.CreateRecordSeparatorData()).ContentBytes.Length,
                20_000),
            nameof(FullInvocationBenchmarks.SplitAndGsub) => new BenchmarkWorkloadMetrics(
                new BenchmarkFile("/split.csv", FullInvocationBenchmarks.CreateSplitSubstitutionData()).ContentBytes.Length,
                16_000),
            nameof(FullInvocationBenchmarks.DynamicRecordSeparator) => DynamicRecordSeparator().Metrics,
            nameof(FullInvocationBenchmarks.NonAsciiRecordSeparator) => NonAsciiRecordSeparator().Metrics,
            nameof(FullInvocationBenchmarks.FieldModes) => FieldModes().Metrics,
            nameof(FullInvocationBenchmarks.FieldWidthsUtf8) => FieldWidthsUtf8().Metrics,
            nameof(FullInvocationBenchmarks.LargeScalarGrowth) => LargeScalarGrowth().Metrics,
            nameof(FullInvocationBenchmarks.LargeFieldList) => LargeFieldList().Metrics,
            nameof(FullInvocationBenchmarks.SmallTextSplitArrays) => SmallTextSplitArrays().Metrics,
            nameof(FullInvocationBenchmarks.RegexReplacementExpansion) => RegexReplacementExpansion().Metrics,
            nameof(FullInvocationBenchmarks.ArraySorting) => ArraySorting().Metrics,
            nameof(FullInvocationBenchmarks.HeavyConcatByteMode) => HeavyConcatByteMode().Metrics,
            _ => default
        };

        return metrics.InputBytes > 0;
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
}

internal sealed class BenchmarkScenario(
    string name,
    string programSource,
    IReadOnlyList<string> arguments,
    IReadOnlyList<BenchmarkFile> files,
    long recordCount,
    AwkExecutionEngineKind counterExecutionEngineKind)
{
    public static BenchmarkScenario Empty { get; } = new(
        string.Empty,
        string.Empty,
        [],
        [],
        0,
        AwkExecutionEngineKind.Interpreter);

    public string Name { get; } = name;

    public string ProgramSource { get; } = programSource;

    public IReadOnlyList<string> Arguments { get; } = arguments;

    public IReadOnlyList<BenchmarkFile> Files { get; } = files;

    public BenchmarkWorkloadMetrics Metrics { get; } = new(SumInputBytes(files), recordCount);

    public AwkExecutionEngineKind CounterExecutionEngineKind { get; } = counterExecutionEngineKind;

    private static long SumInputBytes(IReadOnlyList<BenchmarkFile> files)
    {
        var total = 0L;
        for (var index = 0; index < files.Count; index++)
        {
            total += files[index].ContentBytes.Length;
        }

        return total;
    }
}

internal readonly record struct BenchmarkWorkloadMetrics(long InputBytes, long RecordCount);

internal readonly record struct BenchmarkFile
{
    public BenchmarkFile(string path, string content)
    {
        Path = path;
        ContentBytes = Encoding.UTF8.GetBytes(content);
    }

    public string Path { get; }

    public byte[] ContentBytes { get; }
}

internal readonly record struct BenchmarkRunResult(
    long StandardOutputBytes,
    ulong StandardOutputHash,
    long StandardErrorBytes,
    ulong StandardErrorHash);

internal sealed class BenchmarkHost : IAwkHost
{
    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    private readonly Dictionary<string, byte[]> _files;
    private readonly Dictionary<AwkFileDescriptor, ReadState> _openFiles = new();
    private long _standardOutputBytes;
    private long _standardErrorBytes;
    private ulong _standardOutputHash = FnvOffsetBasis;
    private ulong _standardErrorHash = FnvOffsetBasis;
    private int _nextDescriptor = 3;

    private BenchmarkHost(Dictionary<string, byte[]> files)
    {
        _files = files;
    }

    public static BenchmarkHost FromFiles(IReadOnlyList<BenchmarkFile> files)
    {
        var encoded = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        for (var index = 0; index < files.Count; index++)
        {
            encoded[files[index].Path] = files[index].ContentBytes;
        }

        return new BenchmarkHost(encoded);
    }

    public BenchmarkRunResult Result =>
        new(_standardOutputBytes, _standardOutputHash, _standardErrorBytes, _standardErrorHash);

    public Task<AwkExitCode> AppendAsync(
        AwkFileDescriptor fileDescriptor,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (fileDescriptor == AwkFileDescriptor.StdOut)
        {
            _standardOutputBytes += content.Length;
            _standardOutputHash = Hash(_standardOutputHash, content.Span);
        }
        else if (fileDescriptor == AwkFileDescriptor.StdErr)
        {
            _standardErrorBytes += content.Length;
            _standardErrorHash = Hash(_standardErrorHash, content.Span);
        }

        return Task.FromResult(AwkExitCode.Success);
    }

    public Task<AwkOpenedFile> OpenFileAsync(
        AwkPath path,
        AwkFileOpenMode mode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (mode != AwkFileOpenMode.Read || !_files.TryGetValue(path.Path, out var content))
            return Task.FromResult(AwkOpenedFile.MissingFile());

        var descriptor = new AwkFileDescriptor(_nextDescriptor++);
        _openFiles[descriptor] = new ReadState(content);
        return Task.FromResult(AwkOpenedFile.Opened(descriptor));
    }

    public Task<AwkReadResult> ReadLinesAsync(
        AwkFileDescriptor fileDescriptor,
        int limit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_openFiles.TryGetValue(fileDescriptor, out var state))
            return Task.FromResult(AwkReadResult.EndOfFile);

        return Task.FromResult(state.ReadLines(limit));
    }

    public Task<AwkOperationStatus> CloseDescriptorAsync(
        AwkFileDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _openFiles.Remove(descriptor)
                ? AwkOperationStatus.Success
                : AwkOperationStatus.Failure);
    }

    public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(
        IReadOnlyList<AwkCommandInvocation> commands,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("Benchmark host does not run external commands.");

    public Task<AwkPipeDescriptors> CreatePipeAsync(AwkPipeMode mode, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Benchmark host does not create pipes.");

    private static ulong Hash(ulong current, ReadOnlySpan<byte> content)
    {
        for (var index = 0; index < content.Length; index++)
        {
            current ^= content[index];
            current *= FnvPrime;
        }

        return current;
    }

    private sealed class ReadState(byte[] content)
    {
        private int _offset;

        public AwkReadResult ReadLines(int limit)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

            if (_offset >= content.Length)
                return AwkReadResult.EndOfFile;

            var start = _offset;
            var span = content.AsSpan(start);
            var offsets = limit == 1 ? [] : new int[Math.Min(limit - 1, span.Length)];
            var lineCount = 0;
            var consumed = 0;
            while (lineCount < limit)
            {
                var newline = span[consumed..].IndexOf((byte)'\n');
                if (newline < 0)
                    break;

                lineCount++;
                consumed += newline + 1;
                if (lineCount >= limit)
                    break;

                offsets[lineCount - 1] = consumed;
            }

            if (lineCount > 0)
            {
                _offset += consumed;
                var completeBuffer = new byte[consumed];
                Array.Copy(content, start, completeBuffer, 0, consumed);
                return AwkReadResult.FromContent(
                    completeBuffer,
                    lineCount > 1 ? offsets[..(lineCount - 1)] : [],
                    _offset >= content.Length
                        ? AwkReadCompletion.EndOfFile
                        : AwkReadCompletion.MoreData);
            }

            var length = content.Length - start;
            var buffer = new byte[length];
            Array.Copy(content, start, buffer, 0, length);
            _offset = content.Length;
            return AwkReadResult.FromContent(buffer, [], AwkReadCompletion.EndOfFile);
        }
    }
}
