namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData("t=x+1;if(t>3)return t;return x", 8, 3, "8\n", "")]
    [InlineData("t=x;if(1/(t-5))return t;return 0", 5, 0, "", "awk: division by zero attempted\n")]
    [InlineData("t=1/(x-5);if(t)return t;return 0", 5, 0, "", "awk: division by zero attempted\n")]
    [InlineData("t=x;if(t)return 1/(x-5);return 0", 5, 0, "", "awk: division by zero attempted\n")]
    [InlineData("t=x;if(t<0)return 0;return 1/(x-5)", 5, 0, "", "awk: division by zero attempted\n")]
    [InlineData("t=length(x);if(t)return t;return 0", 1, 3, "1\n", "")]
    public async Task LocalConditionsPreserveResultsAndFailureOrder(string body, double expectedTotal, double expectedIndex, string expectedOutput, string expectedError)
    {
        var source = "function step(x,t){" + body + "} BEGIN{total=5;for(i=0;i<3;i++)total=step(total);print total}";
        {
            var prepared = await RunAsync(source);
            AssertSemantics(prepared, expectedTotal, expectedIndex, expectedOutput, expectedError);
        }
    }

    [Theory]
    [InlineData("t=x;if(t)return t+1;return t-1")]
    [InlineData("t=x;if(t){x++;return t}return 0")]
    [InlineData("t=x;if(t)return;return 0")]
    [InlineData("t=x;if(t)return t;return")]
    [InlineData("t=x;if(t)return t;return 0;t++")]
    [InlineData("t=x;u=x;if(t)return t;return 0")]
    [InlineData("global=x;if(x)return x;return 0")]
    [InlineData("t+=x;if(t)return t;return 0")]
    [InlineData("t=x;if((getline line)>0)return t;return 0")]
    [InlineData("t=step(x);if(t)return t;return 0")]
    public void LocalConditionalReturnsBindComputationalBodies(string body)
    {
        _ = Bind("function step(x,t,u){" + body + "} BEGIN{}");
    }

    [Theory]
    [InlineData("default")]
    [InlineData("pending")]
    [InlineData("captured-regex")]
    public async Task LocalConditionalReturnsKeepPolicyAndFieldDiagnosticGuards(string boundary)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = State(counters, limits);
        var program = Bind("function step(x,t){t=$1+x;if(t>8)return t;return 0} BEGIN{total=step(7)}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var fs = AwkVariableName.FromText("FS");
        if (boundary == "captured-regex") state.AssignVariable(fs, "[,;]");
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("2"));
        state.AssignVariable(fs, " ");
        var host = new MockFileSystem();
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        try
        {
            await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output);
            var value = state.ResolveVariableValue(AwkVariableName.FromText("total"));
            Assert.Equal(9, value.AsNumber());

            Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally { await io.CloseAsync(); }
    }

    [Theory]
    [InlineData(4, 1)]
    [InlineData(5, 1)]
    [InlineData(65, 1)]
    [InlineData(2, 3)]
    public async Task LocalConditionalReturnCallsSupportOmittedLocalsAndExcessArguments(int slots, int arguments)
    {
        var parameters = string.Join(",", Enumerable.Range(0, slots).Select(i => i == 0 ? "x" : "p" + i));
        var call = "step(" + string.Join(",", Enumerable.Repeat("1", arguments)) + ")";
        var parametersSource = "function step(" + parameters + "){x=1;if(x)return x;return 0} BEGIN{total=" + call + ";print total}";
        var result = await RunAsync(parametersSource);
        AssertSemantics(result, 1, 0, "1\n", "");
    }

}
