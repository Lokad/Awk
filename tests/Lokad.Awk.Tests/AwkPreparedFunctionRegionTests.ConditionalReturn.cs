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
        }
    }

    [Theory]
    [InlineData("default")]
    [InlineData("pending")]
    public async Task ConditionalReturnsKeepExistingFunctionGuards(string boundary)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = State(counters, limits);
        var program = Bind("function choose(x){if(x<3)return x+2;return x-2} BEGIN{total=choose(7)}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        try
        {
            await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output);
            var value = state.ResolveVariableValue(AwkVariableName.FromText("total"));
            Assert.Equal(5, value.AsNumber());

            Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.False(state.IsInsideFunction);
        }
        finally { await io.CloseAsync(); }
    }

    [Theory]
    [InlineData("if(x)return 1;return 2")]
    [InlineData("if(x)return;return 2")]
    [InlineData("if(x){x++;return 1}return 2")]
    [InlineData("if(x)return 1;else return 2")]
    [InlineData("if(x){if(x)return 1}return 2")]
    [InlineData("if(x)return 1;return")]
    [InlineData("if(x)return 1;return 2;x++")]
    [InlineData("if((getline line)>0)return 1;return 2")]
    public void ConditionalReturnsBindComputationalBodies(string body)
    {
        _ = Bind("function choose(x){" + body + "} BEGIN{}");
    }

}
