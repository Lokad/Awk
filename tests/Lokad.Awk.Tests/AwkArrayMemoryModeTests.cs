namespace Lokad.Awk.Tests;

public sealed class AwkArrayMemoryModeTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NumericUpdatesRetainEquivalentKeysAndRefuseRealGrowth(bool storedAsText)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var variables = CreateVariables(memory.Accounting, AwkRuntimeCounters.Disabled);
        var array = AwkArrayName.FromText("totals");
        var ordinal = AwkArrayKey.FromOrdinal(1);
        var text = new AwkArrayKey("1");
        variables.SetArrayValue(array, storedAsText ? text : ordinal, AwkValue.FromNumber(7));
        var committed = memory.Accounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        for (var value = 8; value <= 100; value++)
            variables.SetArrayValue(array, storedAsText ? ordinal : text, AwkValue.FromNumber(value));
        Assert.Equal(100, variables.GetArrayValue(array, text).AsNumber());
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Throws<AwkRuntimeException>(() => variables.SetArrayValue(array, AwkArrayKey.FromOrdinal(2), AwkValue.FromNumber(3)));
        Assert.Equal(100, variables.GetArrayValue(array, ordinal).AsNumber());
        Assert.True(variables.TryGetArrayCount(array, out var count));
        Assert.Equal(1, count);
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void LongKeyAccessCancelsBeforeMutation()
    {
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var variables = CreateVariables(AwkMemoryAccounting.Disabled, AwkRuntimeCounters.Disabled,
            () => { if (armed) cancellation.Token.ThrowIfCancellationRequested(); });
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey(new string('k', AwkRuntimeLimits.CancellationCheckBytes * 2));
        variables.SetArrayValue(name, key, AwkValue.FromNumber(42));
        variables.SetArrayValue(name, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(9));
        cancellation.Cancel();
        armed = true;
        Assert.Throws<OperationCanceledException>(() => variables.SetArrayValue(name, key, AwkValue.FromNumber(7)));
        armed = false;
        Assert.Equal(42, variables.GetArrayValue(name, key).AsNumber());
        Assert.True(variables.TryGetArrayCount(name, out var count));
        Assert.Equal(2, count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdatesPreserveMixedKeysKindsAndRefunds(bool governed)
    {
        var budget = new CooperativeTestBudget(20_000);
        await using var owner = AwkExecutionMemoryOwner.Create(
            governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        Assert.Equal(governed, accounting.IsEnabled);
        var variables = CreateVariables(accounting, counters);
        var name = AwkArrayName.FromText("a");
        variables.ClearArray(name);
        var emptyArrayBytes = accounting.CurrentCommittedBytes;
        var numericKey = AwkArrayKey.FromOrdinal(1);
        var textKey = new AwkArrayKey("1");
        var paddedKey = new AwkArrayKey("01");
        for (var iteration = 0; iteration < 3; iteration++)
        {
            variables.SetArrayValue(name, numericKey, AwkValue.FromNumber(42));
            Assert.Equal(42, variables.GetArrayValue(name, textKey).AsNumber());
            variables.SetArrayString(name, textKey, new string('a', 4096));
            Assert.Equal(new string('a', 4096), variables.GetArrayValue(name, numericKey).AsString());
            variables.SetArrayValue(name, numericKey, AwkValue.FromNumericString(AwkText.FromString("007")));
            var numericString = variables.GetArrayValue(name, textKey);
            Assert.Equal("007", numericString.AsString());
            Assert.Equal(7, numericString.AsNumber());
            variables.SetArrayValue(name, paddedKey, AwkValue.FromNumber(9));
            Assert.True(variables.TryGetArrayCount(name, out var count));
            Assert.Equal(2, count);
            Assert.Equal(9, variables.GetArrayValue(name, paddedKey).AsNumber());
            variables.DeleteArrayValue(name, textKey);
            variables.DeleteArrayValue(name, paddedKey);
            Assert.Equal(governed, accounting.CurrentCommittedBytes > 0);
            variables.ClearArray(name);
            Assert.Equal(emptyArrayBytes, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(emptyArrayBytes, budget.Reserved);
        }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeletionRefundsStoredKeyInBothRepresentationDirections(bool storedAsText)
    {
        var budget = new CooperativeTestBudget(1024);
        await using var owner = AwkExecutionMemoryOwner.Create(
            budget, CancellationToken.None);
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeCounters.Disabled, owner);
        var variables = CreateVariables(accounting, AwkRuntimeCounters.Disabled);
        var name = AwkArrayName.FromText("a");
        var textKey = new AwkArrayKey("1");
        variables.ClearArray(name);
        var emptyArrayBytes = accounting.CurrentCommittedBytes;
        var ordinalKey = AwkArrayKey.FromOrdinal(1);
        for (var iteration = 0; iteration < 8; iteration++)
        {
            variables.SetArrayValue(name, storedAsText ? textKey : ordinalKey, AwkValue.FromNumber(42));
            Assert.True(accounting.CurrentCommittedBytes > 0);
            variables.SetArrayValue(name, storedAsText ? ordinalKey : textKey, AwkValue.FromNumber(7));
            variables.DeleteArrayValue(name, storedAsText ? ordinalKey : textKey);
            Assert.True(variables.TryGetArrayCount(name, out var count));
            Assert.Equal(0, count);
            Assert.True(accounting.CurrentCommittedBytes > 0);
            variables.ClearArray(name);
            Assert.Equal(emptyArrayBytes, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(emptyArrayBytes, budget.Reserved);
        }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedAdmissionRefusesBeforeInsertOrUpdate(bool update)
    {
        var budget = new CooperativeTestBudget(1024);
        await using var owner = AwkExecutionMemoryOwner.Create(
            budget, CancellationToken.None);
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeCounters.Disabled, owner);
        Assert.True(accounting.IsEnabled);
        var variables = CreateVariables(accounting, AwkRuntimeCounters.Disabled);
        var name = AwkArrayName.FromText("a");
        var existingKey = AwkArrayKey.FromOrdinal(1);
        variables.ClearArray(name);
        var emptyArrayBytes = accounting.CurrentCommittedBytes;
        variables.SetArrayValue(name, existingKey, AwkValue.FromNumber(42));
        var committed = accounting.CurrentCommittedBytes;
        Assert.True(committed > 0);
        var target = update ? existingKey : AwkArrayKey.FromOrdinal(2);
        Assert.Throws<AwkRuntimeException>(() => variables.SetArrayString(name, target, new string('a', 4096)));
        Assert.True(variables.TryGetArrayCount(name, out var count));
        Assert.Equal(1, count);
        Assert.Equal(42, variables.GetArrayValue(name, existingKey).AsNumber());
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        variables.DeleteArrayValue(name, existingKey);
        Assert.True(accounting.CurrentCommittedBytes > 0);
        variables.ClearArray(name);
        Assert.Equal(emptyArrayBytes, accounting.CurrentCommittedBytes);
        Assert.Equal(emptyArrayBytes, budget.Reserved);
    }

    private static AwkVariables CreateVariables(AwkMemoryAccounting accounting, AwkRuntimeCounters counters,
        Action? checkpoint = null) =>
        new(accounting, counters, static text => AwkValue.FromText(text), static text => AwkValue.FromNumericString(text), static text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), static (_, _) => { }, checkpoint: checkpoint);
}
