namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PreparedStoredTextConsumersKeepSpellingWithoutGeneralValueTransport(bool governed)
    {
        const int records = 1024;
        const string source = "function score(s,t){t=tolower(s);return length(t)+index(t,\":\")+length(substr(t,1,2))}" +
            "{v=tolower($1);total+=score(v)+length(v)}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("ID:beta:tail 4\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(16 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("29696\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.BoundScalarLengths >= 2 * records);
        Assert.InRange(counters.ValuesCreated, 0, 4 * records + 32);
        Assert.InRange(counters.AwkTextAllocations, 0, 3 * records + 32);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> DirectTextInputCases()
    {
        foreach (var operation in new[] { "lower", "upper", "substr", "index" })
        foreach (var field in new[] { "$0", "$1", "$(column)" })
        foreach (var capture in new[] { false, true })
        foreach (var governed in new[] { false, true })
            if (!capture || operation is "substr" or "index") yield return [operation, field, capture, governed];
    }

    [Theory]
    [MemberData(nameof(DirectTextInputCases))]
    public async Task DirectTextInputsAvoidUnobservedNumericClassification(
        string operation, string field, bool capture, bool governed)
    {
        const int records = 64;
        var expression = operation switch
        {
            "substr" => "substr(" + field + "," + (capture ? "position()" : "2") + ",4)",
            "index" => "index(" + field + "," + (capture ? "needle()" : "\"Cd\"") + ")",
            "upper" => "toupper(" + field + ")",
            _ => "tolower(" + field + ")"
        };
        var source = "function position(){return 2}function needle(){return \"Cd\"}BEGIN{column=1}{total+=" +
            (operation == "index" ? expression : "length(" + expression + ")") + "}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("AbCdEfghij\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(operation == "index" ? "192\n" : operation == "substr" ? "256\n" : "640\n",
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.TextValueClassifications);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task SubstringInputsUseTheirTextAndArithmeticContracts(bool governed, bool capture)
    {
        const int records = 64;
        var source = "function size(){return 4}{total+=length(substr($1,$2," +
            (capture ? "size()" : "$3") + "))}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("abcdefghij 0002 0004\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("256\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.TextValueClassifications);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, 1)] [InlineData(true, false, 1)]
    [InlineData(false, true, 1)] [InlineData(true, true, 1)]
    [InlineData(false, false, 2)] [InlineData(true, false, 2)]
    [InlineData(false, true, 2)] [InlineData(true, true, 2)]
    [InlineData(false, false, 65)] [InlineData(true, false, 65)]
    [InlineData(false, true, 65)] [InlineData(true, true, 65)]
    public async Task NumericBuiltinsKeepFieldInputsByteNative(bool governed, bool forwarding, int arity)
    {
        const int records = 128;
        var binary = arity > 1;
        var arguments = "$1" + string.Concat(Enumerable.Repeat(",$2", arity - 1));
        var expression = (binary ? "and(" : "int(") + arguments + ")";
        var definitions = "";
        if (forwarding)
        {
            var parameters = "x" + string.Concat(Enumerable.Range(1, arity - 1).Select(i => ",y" + i));
            definitions = "function use(" + parameters + "){return " + (binary ? "and(" : "int(") + parameters + ")}";
            expression = "use(" + arguments + ")";
        }
        var source = definitions + "{sum+=" + expression + "}END{print sum}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007 0003\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal((records * (binary ? 3 : 7)) + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.True(counters.NumericFieldByteParses >= records * (binary ? 2 : 1));
        if (forwarding) Assert.Equal(records, counters.BoundFunctionRuns);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("sqrt", false)] [InlineData("sqrt", true)]
    [InlineData("exp", false)] [InlineData("exp", true)]
    [InlineData("log", false)] [InlineData("log", true)]
    public async Task MathCallsKeepNativeNumericInputs(string builtin, bool governed)
    {
        const int records = 128;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0001\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var source = "function score(x){return " + builtin + "(x)}{sum+=(score($1)>=0)}END{print sum}";
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(records + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
