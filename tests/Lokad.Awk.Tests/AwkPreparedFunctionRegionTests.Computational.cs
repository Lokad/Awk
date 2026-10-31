namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData("function inc(x){return x+1} function f(x,i){for(i=0;inc(i)<4;i++){if(i==1)continue;x+=i};return x} BEGIN{total=f(7);print total}", "9\n")]
    [InlineData("function f(x,i,t){t=x++;t+=--x;t+=x--;t+=++x;return t*10+x} BEGIN{total=f(\"0007\");print total}", "287\n")]
    [InlineData("function f(x){if(!x&&++x)return x;return x?x++:-x} BEGIN{total=f(0)+f(7);print total}", "8\n")]
    [InlineData("function f(x){if(x){x++;return};x=99} BEGIN{total=f(7);print \"[\" total \"]\"}", "[]\n")]
    [InlineData("function f(x){x++;if(x<4)x++} BEGIN{total=f(7);print \"[\" total \"]\"}", "[]\n")]
    [InlineData("function f(x,i,j){for(i=0;i<3;i++){j=0;do{j++;if(j==2)continue;if(j==4)break;x+=i+j}while(j<8)};return x} BEGIN{total=f(7);print total}", "25\n")]
    public async Task CompleteComputationalBodiesMatchOrdinaryExecution(string source, string output)
    {
        var prepared = await RunAsync(source, false);
        var ordinary = await RunAsync(source, true);
        Assert.Equal(output, prepared.Output);
        Assert.Equal("", prepared.Error);
        Assert.Equal(ordinary, prepared with { PreparedFunctions = ordinary.PreparedFunctions });
        Assert.Equal(0, ordinary.PreparedFunctions);
        Assert.True(prepared.PreparedFunctions > 0);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(257)]
    [InlineData(4097)]
    public async Task LinearFunctionLengthIsIndependentOfPreparationDepth(int count)
    {
        var source = "function f(x){" + string.Join(';', Enumerable.Repeat("x++", count)) +
            ";return x} BEGIN{total=f(7);print total}";
        var prepared = await RunAsync(source, false);
        var ordinary = await RunAsync(source, true);
        Assert.Equal(7 + count, prepared.Total);
        Assert.Equal(ordinary, prepared with { PreparedFunctions = ordinary.PreparedFunctions });
        Assert.Equal(1, prepared.PreparedFunctions);
        Assert.Equal(0, ordinary.PreparedFunctions);
    }

    [Fact]
    public void CompleteFunctionPreparationObservesConstructionCancellation()
    {
        var program = Bind("function f(x){" + string.Join(';', Enumerable.Repeat("x++", 4097)) +
            ";return x} BEGIN{}");
        using var cancellation = new CancellationTokenSource();
        var functions = program.Functions.ToDictionary(pair => pair.Key, pair => pair.Value);
        var visits = 0;
        Assert.ThrowsAny<OperationCanceledException>(() => AwkSynchronousFunction.Prepare(functions, () =>
        {
            if (++visits == 100) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }));
        Assert.Single(AwkSynchronousFunction.Prepare(functions, null) ??
            throw new InvalidOperationException("Expected a prepared function."));
    }

    [Theory]
    [InlineData("for")]
    [InlineData("while")]
    [InlineData("do")]
    public async Task PreparedComputationalLoopsObserveCancellationAndUnwind(string kind)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token);
        var iterations = 0;
        var test = new CancelingOperand(() =>
        {
            if (++iterations == 20) cancellation.Cancel();
        });
        var loop = kind switch
        {
            "for" => AwkStatement.For(AwkStatement.NoOp(), new(test), AwkStatement.NoOp(), []),
            "while" => AwkStatement.While(new(test), []),
            _ => AwkStatement.DoWhile([], new(test))
        };
        var name = AwkFunctionName.FromText("f");
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction>
            { [name] = new(name, AwkFunctionLocalLayout.Empty, [loop]) };
        Assert.Single(AwkSynchronousFunction.Prepare(functions, null) ??
            throw new InvalidOperationException("Expected a prepared function."));
        state.InitializeFunctions(functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await functions[name].InvokeAsync([], new(state, output, io)));
            Assert.Equal(20, iterations);
            Assert.Equal(1, counters.SynchronousFunctionRuns);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally { await io.CloseAsync(); }
    }
}
