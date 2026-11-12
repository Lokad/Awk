namespace Lokad.Awk.Tests;

// Storage/migration checks. Ordinary behavior belongs to the public golden corpus.
public sealed partial class AwkRegionTests
{
    [Theory]
    [InlineData("BEGIN{print length(1,2)}", "Invalid number of arguments for length.")]
    [InlineData("BEGIN{{print length(1,2)}}", "Invalid number of arguments for length.")]
    [InlineData("BEGIN{while(1){print length(1,2)}}", "Invalid `while' statement.")]
    [InlineData("missing(){print length(1,2)}", "Invalid number of arguments for length.")]
    public void PreparationFailureKeepsTheEnclosingDiagnosticAndBodyPrecedence(string source, string expected)
    {
        var parsed = AwkParser.Parse(source);
        Assert.True(parsed.Success);
        Assert.False(AwkTestHarness.TryPrepareRule(Assert.Single(parsed.Syntax.Rules), AwkLoweringScope.Global,
            -1, out var region, out var diagnostic));
        Assert.Null(region);
        Assert.Equal(expected, diagnostic.Message);
    }

    [Theory]
    [InlineData(512)]
    [InlineData(2048)]
    public void DeepSyntacticBlocksPrepareAndExecuteWithoutRecursiveBlockEmission(int depth)
    {
        var source = "BEGIN{" + new string('{', depth) + "value=7" + new string('}', depth + 1);
        var parsed = AwkParser.Parse(source, depth + 8);
        Assert.True(parsed.Success);
        Assert.True(AwkTestHarness.TryPrepareRule(parsed.Syntax.Rules[0], AwkLoweringScope.Global, -1, out var region));
        using var fixture = State(CancellationToken.None, true);
        using var storage = new AwkRegionStorage(fixture.State);
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(fixture.State, storage));
        Assert.Equal(7, fixture.State.ResolveVariableValue(AwkVariableName.FromText("value")).AsNumber());
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    public static IEnumerable<object[]> ComputationCases()
    {
        foreach (var governed in new[] { false, true })
        {
            yield return ["x=1;a=x+(x=7);b=(x==(x=3));c=x++;d=++x;e=0&&(x=99);f=1||(x=99);g=(0?(x=77):x)",
                "record", new[] { "x", "a", "b", "c", "d", "e", "f", "g" }, new[] { "5", "8", "0", "3", "5", "0", "1", "5" }, governed];
            yield return ["literal=(text<3);input=($1<3);saved=$1;value=(NF=$2)",
                "0007 0001 tail", new[] { "literal", "input", "saved", "value", "NF" }, new[] { "1", "0", "0007", "0001", "0001" }, governed];
            yield return ["s=0;for(i=0;i<4;i++){if(i==1)continue;j=0;do{j++;if(j==2)break;s+=i+j}while(j<3)}while(s<10){s++;if(s==9)break}",
                "record", new[] { "s", "i", "j" }, new[] { "9", "4", "2" }, governed];
            yield return ["saved=$1;equal=($1==(NF=0));after=NF",
                "0007 tail", new[] { "saved", "equal", "after" }, new[] { "0007", "0", "0" }, governed];
            yield return ["sum+=$2;value=(sum*3+2)%101;result=-value+ +sum",
                "row 0007", new[] { "sum", "value", "result" }, new[] { "7", "23", "-16" }, governed];
            yield return ["for(i=0;i<5;i++)for(j=0;j<4;j++){if(j==1)continue;if(i==2)break;sum+=i+j}",
                "row", new[] { "sum", "i", "j" }, new[] { "44", "5", "4" }, governed];
        }
    }

    [Theory]
    [MemberData(nameof(ComputationCases))]
    public void ComputationPreservesValuesAndOrder(string body, string record, string[] names, string[] expected, bool governed)
    {
        using var fixture = State(CancellationToken.None, governed);
        var state = fixture.State;
        var region = Compile(body, state);
        state.SetCurrentRecord(AwkText.FromString(record));
        state.AssignVariableValue(AwkVariableName.FromText("text"), AwkValue.FromText("20"));
        using var storage = new AwkRegionStorage(state);
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        for (var i = 0; i < names.Length; i++)
            Assert.Equal(expected[i], state.ResolveVariableValue(AwkVariableName.FromText(names[i])).AsString());
        Assert.All(storage.Values, value => Assert.Equal(0, value.RetainedTextMemoryBytes));
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WarmCapturesReleasePayloadAndKeepBoundedScratch(bool governed)
    {
        using var fixture = State(CancellationToken.None, governed);
        var state = fixture.State;
        var region = Compile("result=(source<3)", state);
        state.AssignVariableValue(AwkVariableName.FromText("source"), AwkValue.FromText(new string('x', 4096)));
        using var storage = new AwkRegionStorage(state);
        region.Execute(state, storage);
        var warm = fixture.Budget.Reserved;
        for (var i = 0; i < 50; i++)
        {
            region.Execute(state, storage);
            Assert.Equal(warm, fixture.Budget.Reserved);
        }
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void RefusedScratchAdmissionPrecedesEffects()
    {
        using var fixture = State(CancellationToken.None, true);
        var state = fixture.State;
        var region = Compile(string.Concat(Enumerable.Repeat("effect+=1;", 512)), state);
        using var storage = new AwkRegionStorage(state);
        var before = fixture.Budget.Reserved;
        fixture.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.False(state.HasScalar(AwkVariableName.FromText("effect")));
        Assert.Equal(before, fixture.Budget.Reserved);
    }

    [Fact]
    public void CancellationRequestedByScratchAdmissionPrecedesEffects()
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token, true);
        var state = fixture.State;
        var region = Compile("effect+=1", state);
        using var storage = new AwkRegionStorage(state);
        fixture.Budget.OnGrowth = _ => cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => region.Execute(state, storage));
        Assert.False(state.HasScalar(AwkVariableName.FromText("effect")));
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, "while(1){}")] [InlineData(true, "while(1){}")]
    [InlineData(false, "for(;;){}")] [InlineData(true, "for(;;){}")]
    [InlineData(false, "do{}while(1)")] [InlineData(true, "do{}while(1)")]
    [InlineData(false, "while(1){if(0)break;if(1)continue}")]
    [InlineData(true, "while(1){if(0)break;if(1)continue}")]
    public async Task FunctionLoopRespondsToCancellationAndUnwindsWithoutCheckpointCounts(bool governed, string body)
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token, governed);
        var binding = AwkBinder.Bind(AwkParser.Parse("function repeat(){" + body + "}BEGIN{repeat()}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        fixture.State.InitializeGlobalNames(program.GlobalNames);
        fixture.State.InitializeGlobalReadSlots(program.GlobalReadCount);
        fixture.State.InitializeFunctions(program.Functions);
        var region = Assert.Single(program.BeginActions);
        using var storage = new AwkRegionStorage(fixture.State);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // This intentionally CPU-bound loop must not occupy a worker needed to
        // schedule its cancellation test on a small, busy CI runner.
        var execution = Task.Factory.StartNew(
            () => { started.TrySetResult(); return region.Execute(fixture.State, storage); },
            CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(20);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            cancellation.Cancel();
            try { await execution.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (OperationCanceledException) { }
        }
        Assert.False(fixture.State.IsInsideFunction);
        Assert.Equal(0, fixture.State.MemoryAccounting.CurrentReservedBytes);
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void DisposedScratchCannotAllocateUnfundedStorage()
    {
        using var fixture = State(CancellationToken.None, true);
        var storage = new AwkRegionStorage(fixture.State);
        storage.Dispose();
        Assert.Throws<ObjectDisposedException>(() => storage.Prepare(new(128, 128)));
    }

    [Fact]
    public void UnaryNegationPreservesSignedZero()
    {
        using var fixture = State(CancellationToken.None, false);
        var region = Compile("negative=-0;positive=-negative", fixture.State);
        using var storage = new AwkRegionStorage(fixture.State);
        region.Execute(fixture.State, storage);
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(fixture.State.ResolveVariableValue(AwkVariableName.FromText("negative")).AsNumber()));
        Assert.Equal(0, BitConverter.DoubleToInt64Bits(fixture.State.ResolveVariableValue(AwkVariableName.FromText("positive")).AsNumber()));
    }

    private static AwkRegion Compile(string body, AwkRuntimeState state)
    {
        var parsed = AwkParser.Parse("{" + body + "}");
        Assert.Empty(parsed.Errors);
        AwkSymbolCollector.Collect(parsed.Syntax, out var names);
        var reads = new AwkGlobalReadLayout(names, null);
        state.InitializeGlobalNames(names);
        state.InitializeGlobalReadSlots(reads.Count);
        var scope = AwkLoweringScope.GlobalFor(AwkCompatibilityMode.GnuDefault, globalReads: reads);
        Assert.True(AwkRegionCompiler.TryCompile(parsed.Syntax.Rules[0].ActionStatements, scope, out var region));
        return region;
    }

    private static CooperativeTestMemory State(CancellationToken token, bool governed)
    {
        var fixture = new CooperativeTestMemory(4 << 20, enabled: governed);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault());
        return fixture.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            token, fixture.Configure(options)));
    }
}
