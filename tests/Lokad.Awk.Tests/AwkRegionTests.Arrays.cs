namespace Lokad.Awk.Tests;

public sealed partial class AwkRegionTests
{
    [Theory]
    [InlineData("split", false)] [InlineData("split", true)]
    [InlineData("patsplit", false)] [InlineData("patsplit", true)]
    [InlineData("match", false)] [InlineData("match", true)]
    public void ArrayArgumentCaptureAdmissionPrecedesMissingReadAndLaterEffects(string builtin, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token, true);
        var state = fixture.State;
        var pattern = builtin == "patsplit" ? ",\".\"" : "";
        var call = builtin == "match" ? "match(\"\",\"\",a[\"missing\"])"
            : builtin + "(\"\",a[\"missing\"]" + pattern + ")";
        var region = Compile(call + ";later=9", state);
        var name = AwkArrayName.FromText("a");
        state.SetArrayAwkValue(name, new("existing"), AwkValue.FromNumber(7));
        var owner = state.ResolveOrdinaryArray(name, default);
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(region.Layout);
        var held = fixture.Budget.Reserved;
        if (cancel) fixture.Budget.OnGrowth = _ => cancellation.Cancel();
        else fixture.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => region.Execute(state, storage));
        else Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Single(owner.Slots);
        Assert.False(owner.Slots.ContainsKey(new("missing")));
        Assert.False(state.HasScalar(AwkVariableName.FromText("later")));
        Assert.Equal(held, fixture.Budget.Reserved);
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ArrayArgumentStorageKeepsBoundedFundingWhenAnOriginIsPromoted(bool governed)
    {
        using var fixture = State(CancellationToken.None, governed);
        var state = fixture.State;
        var region = Compile("split(\"left right\",a[1],(a[1][9]=1)?\" \":\"x\");delete a", state);
        using var storage = new AwkRegionStorage(state);
        region.Execute(state, storage);
        var held = fixture.Budget.Reserved;
        for (var iteration = 0; iteration < 32; iteration++)
        {
            region.Execute(state, storage);
            Assert.Equal(held, fixture.Budget.Reserved);
        }
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData("isarray", false)] [InlineData("isarray", true)]
    [InlineData("typeof", false)] [InlineData("typeof", true)]
    public void IntrospectionAdmissionFailureDoesNotInsertOrRunLaterEffects(string builtin, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token, true);
        var state = fixture.State;
        var region = Compile("kind=" + builtin + "(a[\"missing\"]);later=9", state);
        var name = AwkArrayName.FromText("a");
        state.SetArrayAwkValue(name, new("existing"), AwkValue.FromNumber(7));
        var owner = state.ResolveOrdinaryArray(name, default);
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(region.Layout);
        var held = fixture.Budget.Reserved;
        if (cancel) fixture.Budget.OnGrowth = _ => cancellation.Cancel();
        else fixture.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => region.Execute(state, storage));
        else Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Single(owner.Slots);
        Assert.False(owner.Slots.ContainsKey(new("missing")));
        Assert.False(state.HasScalar(AwkVariableName.FromText("kind")));
        Assert.False(state.HasScalar(AwkVariableName.FromText("later")));
        Assert.Equal(held, fixture.Budget.Reserved);
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedArrayUpdatesKeepCapturedPriorAndRefundScratch(bool governed)
    {
        using var fixture = State(CancellationToken.None, governed);
        var state = fixture.State;
        var region = Compile("a[1]+=(a[1]+=3);prior=a[1]++;after=a[1]", state);
        var array = AwkArrayName.FromText("a");
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayAwkValue(array, key, AwkValue.FromNumber(5));
        using var storage = new AwkRegionStorage(state);
        region.Execute(state, storage);
        Assert.Equal(13, state.ResolveVariableValue(AwkVariableName.FromText("prior")).AsNumber());
        Assert.Equal(14, state.ResolveVariableValue(AwkVariableName.FromText("after")).AsNumber());
        Assert.Equal(14, state.GetArrayAwkValue(array, key).AsNumber());
        Assert.All(storage.Targets, target => Assert.Null(target.Array));
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingTargetAdmissionFailurePrecedesRhsAndCommit(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token, true);
        var state = fixture.State;
        var region = Compile("a[1]+=(later=9)", state);
        var array = AwkArrayName.FromText("a");
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayAwkValue(array, key, AwkValue.FromNumber(5));
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(region.Layout);
        var before = fixture.Budget.Reserved;
        if (cancel) fixture.Budget.OnGrowth = _ => cancellation.Cancel();
        else fixture.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => region.Execute(state, storage));
        else Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.False(state.HasScalar(AwkVariableName.FromText("later")));
        Assert.Equal(5, state.GetArrayAwkValue(array, key).AsNumber());
        Assert.All(storage.Targets, target => Assert.Null(target.Array));
        Assert.Equal(before, fixture.Budget.Reserved);
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArrayCaptureRetiresBeforeNextInvocationAndOnRhsFailure(bool fail)
    {
        using var fixture = State(CancellationToken.None, true);
        var state = fixture.State;
        var region = Compile(fail ? "a[$1]+=(1/0)" : "a[$1]+=1", state);
        var array = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("0007");
        state.SetArrayAwkValue(array, key, AwkValue.FromNumber(5));
        state.SetCurrentRecord(AwkText.FromString("0007"));
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(region.Layout);
        for (var i = 0; i < 20; i++)
        {
            if (fail) Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
            else region.Execute(state, storage);
            Assert.All(storage.Targets, target => Assert.Null(target.Array));
        }
        Assert.Equal(fail ? 5 : 25, state.GetArrayAwkValue(array, key).AsNumber());
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }
}
