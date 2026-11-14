namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task FirstFunctionStatementDrainsInheritedOutputBeforeLaterEffects(bool governed, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters, enabled: governed);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        var program = Bind("function f(){first=1;later=2;later*=3;return later}BEGIN{total=f()}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var first = AwkVariableName.FromText("first");
        var later = AwkVariableName.FromText("later");
        state.AssignVariableValue(first, AwkValue.FromNumber(0));
        state.AssignVariableValue(later, AwkValue.FromNumber(0));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (_, bytes, _) =>
            {
                Assert.True(state.IsInsideFunction);
                Assert.Equal(1, state.ResolveVariableValue(first).AsNumber());
                Assert.Equal(0, state.ResolveVariableValue(later).AsNumber());
                Assert.True(bytes.Span.SequenceEqual("inherited\n"u8));
                entered.TrySetResult();
                await release.Task.ConfigureAwait(false);
                Assert.True(bytes.Span.SequenceEqual("inherited\n"u8));
                return AwkAppendResult.Accepted();
            }
        };
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, cancellation.Token, counters, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        output.Append(AwkText.FromSourceLiteralString("inherited\n"));
        var execution = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (governed) Assert.True(memory.Budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            else Assert.Equal(AwkStatementFlow.Continue, await execution);
            Assert.Equal(cancel ? 0 : 6, state.ResolveVariableValue(later).AsNumber());
            Assert.False(state.IsInsideFunction);
        }
        finally
        {
            release.TrySetResult();
            try { await execution; } catch (OperationCanceledException) when (cancel) { }
            await io.CloseForCleanupAsync();
        }
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData("function inc(x){return x+1} function f(x,i){for(i=0;inc(i)<4;i++){if(i==1)continue;x+=i};return x} BEGIN{total=f(7);print total}", "9\n")]
    [InlineData("function f(x,i,t){t=x++;t+=--x;t+=x--;t+=++x;return t*10+x} BEGIN{total=f(\"0007\");print total}", "287\n")]
    [InlineData("function f(x){if(!x&&++x)return x;return x?x++:-x} BEGIN{total=f(0)+f(7);print total}", "8\n")]
    [InlineData("function f(x){if(x){x++;return};x=99} BEGIN{total=f(7);print \"[\" total \"]\"}", "[]\n")]
    [InlineData("function f(x){x++;if(x<4)x++} BEGIN{total=f(7);print \"[\" total \"]\"}", "[]\n")]
    [InlineData("function f(x,i,j){for(i=0;i<3;i++){j=0;do{j++;if(j==2)continue;if(j==4)break;x+=i+j}while(j<8)};return x} BEGIN{total=f(7);print total}", "25\n")]
    public async Task CompleteComputationalBodiesPreserveOutputs(string source, string output)
    {
        var prepared = await RunAsync(source);

        Assert.Equal(output, prepared.Output);
        Assert.Equal("", prepared.Error);

        Assert.True(prepared.FunctionRuns > 0);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(257)]
    [InlineData(4097)]
    public async Task LinearFunctionLengthIsIndependentOfPreparationDepth(int count)
    {
        var source = "function f(x){" + string.Join(';', Enumerable.Repeat("x++", count)) +
            ";return x} BEGIN{total=f(7);print total}";
        var prepared = await RunAsync(source);

        Assert.Equal(7 + count, prepared.Total);

        Assert.Equal(1, prepared.FunctionRuns);

    }

    [Fact]
    public void CompleteFunctionPreparationObservesConstructionCancellation()
    {
        var source = "function f(x){" + string.Join(';', Enumerable.Repeat("x++", 4097)) + ";return x} BEGIN{}";
        var syntax = AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue), CancellationToken.None).Syntax;
        using var cancellation = new CancellationTokenSource();
        var visits = 0;
        var scope = AwkLoweringScope.Function(new([AwkVariableName.FromText("x")]), checkpoint: () =>
        {
            if (++visits == 100) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        });
        Assert.ThrowsAny<OperationCanceledException>(() => AwkRegionCompiler.TryCompile(syntax.Functions[0].BodyStatements, scope, out _));
        Assert.NotNull(Bind(source).Functions[AwkFunctionName.FromText("f")].Region);
    }
}
