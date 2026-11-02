namespace Lokad.Awk.Tests;

public sealed partial class AwkRegionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompositeKeysKeepOnlyReusableCapacityAfterEachExecution(bool governed)
    {
        using var fixture = State(CancellationToken.None, governed);
        var state = fixture.State;
        var region = Compile("sum[$1,$2]+=$3", state);
        state.SetCurrentRecord(AwkText.FromString("key part 7"));
        using var storage = new AwkRegionStorage(state);
        region.Execute(state, storage);
        var builder = Assert.Single(storage.Builders);
        var warm = fixture.Budget.Reserved;
        for (var index = 0; index < 20; index++)
        {
            region.Execute(state, storage);
            Assert.Same(builder, Assert.Single(storage.Builders));
            Assert.Equal(0, builder.Length);
            Assert.All(storage.Values, value => Assert.Equal(0, value.RetainedTextMemoryBytes));
            Assert.All(storage.Targets, target => Assert.Null(target.Array));
            Assert.Equal(warm, fixture.Budget.Reserved);
        }
        Assert.Equal(147, state.GetArrayAwkValue(AwkArrayName.FromText("sum"), new AwkArrayKey("key\u001cpart")).AsNumber());
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void BuilderStorageFollowsActiveNestingInsteadOfAccessCount()
    {
        using var fixture = State(CancellationToken.None, true);
        var state = fixture.State;
        var region = Compile("a[b[1,2],3]+=1;x=a[4,5];a[6,7]+=2;y=a[b[1,2],9]", state);
        using var storage = new AwkRegionStorage(state);
        region.Execute(state, storage);
        Assert.Equal(2, storage.Builders.Length);
        Assert.All(storage.Builders, builder => Assert.Equal(0, builder.Length));
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeparatorAdmissionFailureStopsBeforeComponentsAndRhs(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token, true);
        var state = fixture.State;
        var region = Compile("a[(component=1),2]+=(later=9)", state);
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(region.Layout);
        var before = fixture.Budget.Reserved;
        if (cancel) fixture.Budget.OnGrowth = _ => cancellation.Cancel();
        else fixture.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => region.Execute(state, storage));
        else Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.False(state.HasScalar(AwkVariableName.FromText("component")));
        Assert.False(state.HasScalar(AwkVariableName.FromText("later")));
        Assert.Equal(before, fixture.Budget.Reserved);
        Assert.All(storage.Builders, builder => Assert.Equal(0, builder.Length));
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void RefusedBuilderGrowthKeepsArrayAndStopsRhs()
    {
        using var fixture = State(CancellationToken.None, true);
        var state = fixture.State;
        var region = Compile("a[$1,$2]+=(later=9)", state);
        var array = AwkArrayName.FromText("a");
        state.SetArrayAwkValue(array, new AwkArrayKey("keep"), AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromString(new string('x', 4096) + " tail"));
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(region.Layout);
        fixture.Budget.RefuseNextAdmissionMinimumGrowth = 1024;
        Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Equal(7, state.GetArrayAwkValue(array, new AwkArrayKey("keep")).AsNumber());
        Assert.False(state.HasScalar(AwkVariableName.FromText("later")));
        Assert.Equal(0, fixture.Budget.GrowthAttemptsAfterRefusal);
        Assert.All(storage.Builders, builder => Assert.Equal(0, builder.Length));
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void CompletedKeyDoesNotHoldSeparatorWhileTargetAdmissionFails()
    {
        using var fixture = State(CancellationToken.None, true);
        var state = fixture.State;
        var region = Compile("a[$1,$2]+=(later+=1)", state);
        state.SetCurrentRecord(AwkText.FromString("key part"));
        using var storage = new AwkRegionStorage(state);
        region.Execute(state, storage);
        var array = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("key\u001cpart");
        var before = fixture.Budget.Reserved;
        fixture.Budget.RefuseNextAdmissionMinimumGrowth = state.ResolveSubscriptSeparatorText().EstimatedRetainedMemoryBytes + 1;
        Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("later")).AsNumber());
        Assert.Equal(1, state.GetArrayAwkValue(array, key).AsNumber());
        Assert.Equal(before, fixture.Budget.Reserved);
        Assert.All(storage.Builders, builder => Assert.Equal(0, builder.Length));
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }
}
