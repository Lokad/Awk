namespace Lokad.Awk.Tests;

public sealed class AwkArrayMemoryModeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericUpdatesRetainEquivalentKeysAndRefuseRealGrowth(bool storedAsText)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var variables = memory.InitializeRuntime(AwkRuntimeCounters.Disabled).State.Variables;
        var array = AwkArrayName.FromText("totals");
        var ordinal = AwkArrayKey.FromOrdinal(1);
        var text = new AwkArrayKey("1");
        variables.SetArrayValue(array, storedAsText ? text : ordinal, AwkValue.FromNumber(7));
        var committed = memory.Accounting.LiveBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        for (var value = 8; value <= 100; value++)
            variables.SetArrayValue(array, storedAsText ? ordinal : text, AwkValue.FromNumber(value));
        Assert.Equal(100, variables.GetArrayValue(array, text).AsNumber());
        Assert.Equal(committed, memory.Accounting.LiveBytes);
        var capacity = variables.ResolveOrdinaryArray(array).EnsureCapacity(0);
        for (var index = 2; index <= capacity; index++)
            variables.SetArrayValue(array, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(3));
        Assert.Equal(committed, memory.Accounting.LiveBytes);
        Assert.Throws<AwkRuntimeException>(() => variables.SetArrayValue(array, AwkArrayKey.FromOrdinal(capacity + 1), AwkValue.FromNumber(3)));
        Assert.Equal(100, variables.GetArrayValue(array, ordinal).AsNumber());
        Assert.True(variables.TryGetArrayCount(array, out var count));
        Assert.Equal(capacity, count);
        Assert.Equal(committed, memory.Accounting.LiveBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void LongKeyAccessCancelsBeforeMutation()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(1 << 20, enabled: false)
            .InitializeRuntime(AwkRuntimeCounters.Disabled, cancellation.Token);
        var variables = memory.State.Variables;
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey(new string('k', AwkRuntimeLimits.CancellationCheckBytes * 2));
        variables.SetArrayValue(name, key, AwkValue.FromNumber(42));
        variables.SetArrayValue(name, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(9));
        Assert.True(variables.ResolveOrdinaryArray(name).TryGetDictionaryCell(key, out var cell));
        Assert.NotNull(cell);
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => variables.SetArrayValue(name, key, AwkValue.FromNumber(7)));
        Assert.True(cell.IsAttached);
        Assert.Equal(42, cell.Slot.AsNumber());
        Assert.True(variables.TryGetArrayCount(name, out var count));
        Assert.Equal(2, count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UpdatesPreserveMixedKeysKindsAndRefunds(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(20_000, enabled: governed).InitializeRuntime(counters);
        var budget = memory.Budget;
        var accounting = memory.Accounting;
        Assert.Equal(governed, accounting.IsEnabled);
        var variables = memory.State.Variables;
        var name = AwkArrayName.FromText("a");
        variables.ClearArray(name);
        var emptyArrayBytes = accounting.LiveBytes;
        var numericKey = AwkArrayKey.FromOrdinal(1);
        var textKey = new AwkArrayKey("1");
        var paddedKey = new AwkArrayKey("01");
        for (var iteration = 0; iteration < 3; iteration++)
        {
            variables.SetArrayValue(name, numericKey, AwkValue.FromNumber(42));
            Assert.Equal(42, variables.GetArrayValue(name, textKey).AsNumber());
            variables.SetArrayValue(name, textKey, AwkValue.ForStoredText(new string('a', 4096)));
            Assert.Equal(new string('a', 4096), variables.GetArrayValue(name, numericKey).AsString());
            variables.SetArrayValue(name, numericKey, AwkTestHarness.NumericInput(AwkText.FromString("007")));
            var numericString = variables.GetArrayValue(name, textKey);
            Assert.Equal("007", numericString.AsString());
            Assert.Equal(7, numericString.AsNumber());
            variables.SetArrayValue(name, paddedKey, AwkValue.FromNumber(9));
            Assert.True(variables.TryGetArrayCount(name, out var count));
            Assert.Equal(2, count);
            Assert.Equal(9, variables.GetArrayValue(name, paddedKey).AsNumber());
            variables.DeleteArrayValue(name, textKey);
            variables.DeleteArrayValue(name, paddedKey);
            Assert.Equal(governed, accounting.LiveBytes > 0);
            variables.ClearArray(name);
            Assert.Equal(emptyArrayBytes, accounting.LiveBytes);
            Assert.Equal(emptyArrayBytes, budget.Reserved);
        }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeletionRefundsStoredKeyInBothRepresentationDirections(bool storedAsText)
    {
        using var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var budget = memory.Budget;
        budget.SetAvailableBytes(1024);
        var accounting = memory.Accounting;
        var variables = memory.State.Variables;
        var name = AwkArrayName.FromText("a");
        var textKey = new AwkArrayKey("1");
        variables.ClearArray(name);
        var emptyArrayBytes = accounting.LiveBytes;
        var ordinalKey = AwkArrayKey.FromOrdinal(1);
        for (var iteration = 0; iteration < 8; iteration++)
        {
            variables.SetArrayValue(name, storedAsText ? textKey : ordinalKey, AwkValue.FromNumber(42));
            Assert.True(accounting.LiveBytes > 0);
            variables.SetArrayValue(name, storedAsText ? ordinalKey : textKey, AwkValue.FromNumber(7));
            variables.DeleteArrayValue(name, storedAsText ? ordinalKey : textKey);
            Assert.True(variables.TryGetArrayCount(name, out var count));
            Assert.Equal(0, count);
            Assert.True(accounting.LiveBytes > 0);
            variables.ClearArray(name);
            Assert.Equal(emptyArrayBytes, accounting.LiveBytes);
            Assert.Equal(emptyArrayBytes, budget.Reserved);
        }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedAdmissionRefusesBeforeInsertOrUpdate(bool update)
    {
        using var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var budget = memory.Budget;
        budget.SetAvailableBytes(1024);
        var accounting = memory.Accounting;
        Assert.True(accounting.IsEnabled);
        var variables = memory.State.Variables;
        var name = AwkArrayName.FromText("a");
        var existingKey = AwkArrayKey.FromOrdinal(1);
        variables.ClearArray(name);
        var emptyArrayBytes = accounting.LiveBytes;
        variables.SetArrayValue(name, existingKey, AwkValue.FromNumber(42));
        var committed = accounting.LiveBytes;
        Assert.True(committed > 0);
        var target = update ? existingKey : AwkArrayKey.FromOrdinal(2);
        Assert.Throws<AwkRuntimeException>(() =>
            variables.SetArrayValue(name, target, AwkValue.ForStoredText(new string('a', 4096))));
        Assert.True(variables.TryGetArrayCount(name, out var count));
        Assert.Equal(1, count);
        Assert.Equal(42, variables.GetArrayValue(name, existingKey).AsNumber());
        Assert.Equal(committed, accounting.LiveBytes);
        variables.DeleteArrayValue(name, existingKey);
        Assert.True(accounting.LiveBytes > 0);
        variables.ClearArray(name);
        Assert.Equal(emptyArrayBytes, accounting.LiveBytes);
        Assert.Equal(emptyArrayBytes, budget.Reserved);
    }

}
