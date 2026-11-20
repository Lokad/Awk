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

    [Theory]
    [InlineData("t=x*3+y;return t", 161, 3, "161\n", "")]
    [InlineData("x=x*3+y;return x", 161, 3, "161\n", "")]
    [InlineData("t=x;return t+1", 8, 3, "8\n", "")]
    [InlineData("t=1/(x-5);return t", 5, 0, "", "awk: division by zero attempted\n")]
    [InlineData("t=x+1;return 1/(x-5)", 5, 0, "", "awk: division by zero attempted\n")]
    [InlineData("t=length(x);return t", 1, 3, "1\n", "")]
    public async Task LocalAssignmentsPreserveResultsAndFailureOrder(string body, double expectedTotal, double expectedIndex, string expectedOutput, string expectedError)
    {
        var source = "function step(x,y,t){" + body + "} BEGIN{total=5;for(i=0;i<3;i++)total=step(total,2);print total}";
        {
            var prepared = await RunAsync(source);
            AssertSemantics(prepared, expectedTotal, expectedIndex, expectedOutput, expectedError);
        }
    }

    [Theory]
    [InlineData("t=x+1;return t")]
    [InlineData("t=x+1;return")]
    [InlineData("t=x+1;return t;t++")]
    [InlineData("t+=x;return t")]
    [InlineData("t++;return t")]
    [InlineData("global=x;return global")]
    [InlineData("a[x]=1;return x")]
    [InlineData("a[x]=(getline y);return x")]
    [InlineData("$1=x;return x")]
    [InlineData("t=(getline line);return t")]
    [InlineData("t=step(x);return t")]
    public void LocalAssignmentReturnsBindBoundedBodies(string body)
    {
        _ = Bind("function step(x,t){" + body + "} BEGIN{}");
    }

    [Theory]
    [InlineData("default")]
    [InlineData("pending")]
    [InlineData("captured-regex")]
    public async Task LocalAssignmentReturnsKeepExistingGuardsAndFieldDiagnostics(string boundary)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = State(counters, limits);
        var program = Bind("function step(x,t){t=$1+x;return t} BEGIN{total=step(7)}");
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
        }
        finally { await io.CloseAsync(); }
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 1)]
    [InlineData(65, 1)]
    [InlineData(2, 3)]
    [InlineData(2, 2)]
    public async Task LocalAssignmentReturnCallsSupportOmittedLocalsAndExcessArguments(int slots, int arguments)
    {
        var parameters = string.Join(",", Enumerable.Range(0, slots).Select(i => i == 0 ? "x" : "p" + i));
        var call = "step(" + string.Join(",", Enumerable.Repeat("1", arguments)) + ")";
        var parametersSource = "function step(" + parameters + "){x=1;return x} BEGIN{total=" + call + ";print total}";
        var result = await RunAsync(parametersSource);
        AssertSemantics(result, 1, 0, "1\n", "");
    }

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
