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
            Assert.True(prepared.FunctionRuns > 0);
        }
    }

    [Theory]
    [InlineData("t=x;if(t)return t+1;return t-1", true)]
    [InlineData("t=x;if(t){x++;return t}return 0", true)]
    [InlineData("t=x;if(t)return;return 0", true)]
    [InlineData("t=x;if(t)return t;return", true)]
    [InlineData("t=x;if(t)return t;return 0;t++", true)]
    [InlineData("t=x;u=x;if(t)return t;return 0", true)]
    [InlineData("global=x;if(x)return x;return 0", true)]
    [InlineData("t+=x;if(t)return t;return 0", true)]
    [InlineData("t=x;if((getline line)>0)return t;return 0", true)]
    [InlineData("t=step(x);if(t)return t;return 0", true)]
    public void LocalConditionalReturnsPrepareCompleteComputationalBodies(string body, bool expected)
    {
        var function = Bind("function step(x,t,u){" + body + "} BEGIN{}").Functions[AwkFunctionName.FromText("step")];
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction> { [function.FunctionName] = function };
        Assert.Equal(expected, function.Region is not null);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("syntax")]
    [InlineData("lint")]
    [InlineData("pending")]
    [InlineData("captured-regex")]
    public async Task LocalConditionalReturnsKeepPolicyAndFieldDiagnosticGuards(string boundary)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        var program = Bind("function step(x,t){t=$1+x;if(t>8)return t;return 0}{total=step(7)}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var fs = AwkVariableName.FromText("FS");
        if (boundary == "captured-regex") state.AssignVariable(fs, "[,;]");
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("2"));
        state.AssignVariable(fs, " ");
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        try
        {
            await Assert.Single(program.MainActions).ExecuteAsync(state, io, output);
            var value = state.ResolveVariableValue(AwkVariableName.FromText("total"));
            Assert.Equal(9, value.AsNumber());
            Assert.Equal(1, counters.BoundFunctionRuns);
            Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally { await io.CloseAsync(); }
    }

    [Theory]
    [InlineData(4, 1, true)]
    [InlineData(5, 1, true)]
    [InlineData(65, 1, true)]
    [InlineData(2, 3, true)]
    public void LocalConditionalReturnCallsSupportOmittedLocalsAndExcessArguments(int slots, int arguments, bool expected)
    {
        var parameters = string.Join(",", Enumerable.Range(0, slots).Select(i => i == 0 ? "x" : "p" + i));
        var call = "step(" + string.Join(",", Enumerable.Repeat("1", arguments)) + ")";
        var parametersSource = "function step(" + parameters + "){x=1;if(x)return x;return 0}{total=" + call + "}";
        var program = Bind(parametersSource);
        Assert.Single(program.MainActions);
        Assert.Equal(expected, program.Functions[AwkFunctionName.FromText("step")].Region is not null);
    }

}
