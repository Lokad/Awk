namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
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

}
