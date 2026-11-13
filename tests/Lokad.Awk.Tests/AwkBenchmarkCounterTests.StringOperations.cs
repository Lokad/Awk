namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData("literal", false)] [InlineData("literal", true)]
    [InlineData("global", false)] [InlineData("global", true)]
    [InlineData("field", false)] [InlineData("field", true)]
    [InlineData("local", false)] [InlineData("local", true)]
    public async Task ImmediateCaseLengthsCountFormattedInputsWithoutProducingText(string source, bool governed)
    {
        const int records = 32;
        foreach (var upper in new[] { false, true })
        {
            var operation = upper ? "toupper" : "tolower";
            var operand = source switch { "literal" => "\"beta\"", "global" => "stored", "local" => "s", _ => "$1" };
            var expression = "length(" + operation + "(" + operand + "))";
            var script = (source == "local" ? "function size(s){return " + expression + "}" : "") +
                "BEGIN{stored=\"beta\"}{total+=" + (source == "local" ? "size($1)" : expression) + "}END{print total}";
            var host = new MockFileSystem();
            host.SetStandardInput(string.Concat(Enumerable.Repeat("beta tail\n", records)));
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            var exit = await AwkExecutor.ExecuteAsync(host,
                AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", script)), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("128\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, counters.AsciiCasePreparationBytes);
            Assert.Equal(0, counters.AsciiCaseConversionBytes);
            Assert.Equal(0, counters.StringDecodes);
            Assert.Equal(0, counters.Utf8Encodes);
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LocalTextProducersKeepByteNativeValuesAcrossRepeatedAssignments(bool governed)
    {
        const int records = 128;
        const string script = "function clip(s,n,t){t=tolower(s);if(length(t)>n)return substr(t,1,n);return t}" +
            "{v=clip($1,$2);total+=length(v)}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("ID:beta:tail 4\nID:beta:tail 32\n", records / 2)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", script)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal((records * 8) + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.InRange(counters.AwkTextAllocations, 0, records * 2 + 32);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task StringOperationsPreserveOwnedFieldsThroughRecordReconstruction(bool governed)
    {
        const int records = 128;
        const string source = """
            {old=$2;a["kept"]=$2;$1="0007"
             found=index($2,$0);from_array=index(a["kept"],$0)
             slice=substr($2,1,2);lower=tolower($2);upper=toupper($2)}
            END{print found,from_array,slice,lower,upper,old,$0}
            """;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("left ABC\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("0 0 AB abc ABC ABC 0007 ABC\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.AwkTextAllocations, 0, records * 5 + 32);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
