namespace Lokad.Awk.Tests;

public sealed partial class AwkArrayCompoundUpdateTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ReturnedUpdateChecksCancellationAfterCalleeRefund(bool numeric)
    {
        var source = "function rhs(v){getline v<\"/rhs\";return " + (numeric ? "v+0" : "v") +
            "}BEGIN{result=(values[\"k\"]+=rhs())}";
        await using var execution = new NativeArrayExecution(source, true, new InputGate("/rhs", "0003\n", true));
        var state = execution.State;
        var array = AwkArrayName.FromText("values");
        state.SetArrayAwkValue(array, Key, AwkValue.FromText("007"));
        var run = execution.Start();
        await execution.Inputs[0].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(run.IsCompleted);
        var retired = false;
        execution.Memory.Budget.OnRefund = () =>
        {
            if (state.IsInsideFunction) return;
            retired = true;
            execution.Cancel();
        };
        execution.Inputs[0].Release.TrySetResult();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));

        Assert.True(retired);
        Assert.Equal("007", state.GetArrayAwkValue(array, Key).AsString());
        Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
        Assert.False(state.IsInsideFunction);
        execution.Memory.Budget.OnRefund = null;
    }

    [Fact]
    public async Task FieldUpdateReadsItsNumericRegisterAfterAdmission()
    {
        await using var execution = new NativeArrayExecution("BEGIN{a[$1]+=1}", true);
        execution.State.SetCurrentRecord(AwkText.FromString("key"));
        _ = execution.State.FieldCount;
        using var storage = new AwkRegionStorage(execution.State);
        storage.Initialize(execution.Region);
        var update = Assert.Single(execution.Region.Updates);
        Assert.True(update.Right.IsRegister);
        var changed = false;
        execution.Memory.Budget.OnGrowth = _ =>
        {
            storage.Numbers[update.Right.RegisterIndex] = 5;
            changed = true;
        };

        Assert.Equal(AwkStatementFlow.Continue, await execution.ExecuteAsync(storage));

        Assert.True(changed);
        Assert.Equal(5, execution.State.GetArrayAwkValue(AwkArrayName.FromText("a"), new AwkArrayKey("key")).AsNumber());
    }

    [Fact]
    public async Task NestedNumericUpdatesReuseTheirFundedOwner()
    {
        await using var execution = new NativeArrayExecution("BEGIN{a[\"r\"][\"n\"]++}", true);
        using var storage = new AwkRegionStorage(execution.State);
        Assert.Equal(AwkStatementFlow.Continue, await execution.ExecuteAsync(storage));
        execution.Memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;

        Assert.Equal(AwkStatementFlow.Continue, await execution.ExecuteAsync(storage));

        var child = execution.State.GetSubarray(AwkArrayName.FromText("a"), new AwkArrayKey("r"), default);
        Assert.Equal(2, execution.State.GetArrayNumber(child, new AwkArrayKey("n")));
        Assert.Equal(0, execution.Memory.Budget.Refusals);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicRepeatedNestedUpdatesAdmitOnlyNewOwners(bool existing)
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new MockFileSystem();
        host.AfterAppend = () =>
        {
            var output = host.GetOutput(AwkFileDescriptor.StdOut);
            if (output == "ready\n") budget.RefuseNextAdmissionMinimumGrowth = 1;
            else if (output == "ready\nready\n") budget.RefuseNextAdmissionMinimumGrowth = null;
        };
        var source = "function step(a,k){a[k][\"n\"]++;return 0}" +
            "BEGIN{for(i=0;i<2;i++){step(a," + (existing ? "0" : "i") +
            ");print \"ready\"}print a[0][\"n\"]}";
        var invocation = AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []);
        Assert.True(Awk.TryParse(invocation, out var awk));
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget);

        var status = await awk.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(existing ? 0 : 2, status.Value);
        Assert.Equal(existing ? "ready\nready\n2\n" : "ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(existing ? "" : "awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(existing ? 0 : 1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
