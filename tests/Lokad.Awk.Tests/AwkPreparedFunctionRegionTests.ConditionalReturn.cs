namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData("if(x<3)return x+2;return x-2", 1, 3, 0, "3\n", "")]
    [InlineData("if(x<3)return x+2;return x-2", 7, 5, 0, "5\n", "")]
    [InlineData("if(1/x)return x+2;return x-2", 0, 5, 0, "", "awk: division by zero attempted\n")]
    [InlineData("if(x)return 1/0;return 9", 1, 5, 0, "", "awk: division by zero attempted\n")]
    [InlineData("if(x)return 9;return 1/0", 0, 5, 0, "", "awk: division by zero attempted\n")]
    [InlineData("if(x)return x;return 1/0", 1, 1, 0, "1\n", "")]
    public async Task ConditionalReturnsPreserveResultsAndFailureOrder(string body, int argument, double expectedTotal, double expectedIndex, string expectedOutput, string expectedError)
    {
        var source = "function choose(x){" + body + "} BEGIN{total=5;total=choose(" + argument + ");print total}";
        {
            var prepared = await RunAsync(source);
            AssertSemantics(prepared, expectedTotal, expectedIndex, expectedOutput, expectedError);
            Assert.Equal(1, prepared.FunctionRuns);
        }
    }

    [Theory]
    [InlineData("default")]
    [InlineData("syntax")]
    [InlineData("lint")]
    [InlineData("pending")]
    public async Task ConditionalReturnsKeepExistingFunctionGuards(string boundary)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        var program = Bind("function choose(x){if(x<3)return x+2;return x-2}{total=choose(7)}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        try
        {
            await Assert.Single(program.MainActions).ExecuteAsync(state, io, output);
            var value = state.ResolveVariableValue(AwkVariableName.FromText("total"));
            Assert.Equal(5, value.AsNumber());
            Assert.Equal(1, counters.BoundFunctionRuns);
            Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally { await io.CloseAsync(); }
    }

    [Theory]
    [InlineData("if(x)return 1;return 2", true)]
    [InlineData("if(x)return;return 2", true)]
    [InlineData("if(x){x++;return 1}return 2", true)]
    [InlineData("if(x)return 1;else return 2", true)]
    [InlineData("if(x){if(x)return 1}return 2", true)]
    [InlineData("if(x)return 1;return", true)]
    [InlineData("if(x)return 1;return 2;x++", true)]
    [InlineData("if((getline line)>0)return 1;return 2", true)]
    public void ConditionalReturnsPrepareCompleteComputationalBodies(string body, bool expected)
    {
        var function = Bind("function choose(x){" + body + "} BEGIN{}").Functions[AwkFunctionName.FromText("choose")];
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction> { [function.FunctionName] = function };
        Assert.Equal(expected, function.Region is not null);
    }

}
