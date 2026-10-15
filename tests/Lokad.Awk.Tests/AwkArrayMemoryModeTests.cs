namespace Lokad.Awk.Tests;

public sealed class AwkArrayMemoryModeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task UpdatesPreserveMixedKeysKindsLimitsAndRefunds(int mode)
    {
        var budget = new CooperativeTestBudget(20_000);
        await using var owner = AwkExecutionMemoryOwner.Create(
            mode == 2 ? budget : null, CancellationToken.None);
        var limits = mode == 1 ? AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(20_000)
            : AwkRuntimeLimits.CreateDefault();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(limits, counters, owner);
        Assert.Equal(mode != 0, accounting.IsEnabled);
        var variables = CreateVariables(accounting, counters);
        var name = AwkArrayName.FromText("a");
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
            Assert.Throws<AwkRuntimeException>(() => variables.SetArrayValue(
                name, AwkArrayKey.FromOrdinal(2), AwkValue.FromNumber(10)));
            Assert.True(variables.TryGetArrayCount(name, out var count));
            Assert.Equal(2, count);
            Assert.Equal(9, variables.GetArrayValue(name, paddedKey).AsNumber());
            variables.DeleteArrayValue(name, textKey);
            variables.DeleteArrayValue(name, paddedKey);
            Assert.Equal(0, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(0, budget.Reserved);
        }
        Assert.Equal(counters.MemoryCommittedBytes, counters.MemoryReleasedCommittedBytes);
        Assert.Equal(mode != 0, counters.MemoryCommittedBytes > 0);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DeletionRefundsStoredKeyInBothRepresentationDirections(bool sharedOnly, bool storedAsText)
    {
        var budget = new CooperativeTestBudget(1024);
        await using var owner = AwkExecutionMemoryOwner.Create(
            sharedOnly ? budget : null, CancellationToken.None);
        var limits = sharedOnly ? AwkRuntimeLimits.CreateDefault()
            : AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1024);
        var accounting = AwkMemoryAccounting.Create(limits, AwkRuntimeCounters.Disabled, owner);
        var variables = CreateVariables(accounting, AwkRuntimeCounters.Disabled);
        var name = AwkArrayName.FromText("a");
        var textKey = new AwkArrayKey("1");
        var ordinalKey = AwkArrayKey.FromOrdinal(1);
        for (var iteration = 0; iteration < 8; iteration++)
        {
            variables.SetArrayValue(name, storedAsText ? textKey : ordinalKey, AwkValue.FromNumber(42));
            Assert.True(accounting.CurrentCommittedBytes > 0);
            variables.DeleteArrayValue(name, storedAsText ? ordinalKey : textKey);
            Assert.True(variables.TryGetArrayCount(name, out var count));
            Assert.Equal(0, count);
            Assert.Equal(0, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SharedOnlyOrFiniteQuotaRefusesBeforeInsertOrUpdate(bool sharedOnly, bool update)
    {
        var budget = new CooperativeTestBudget(1024);
        await using var owner = AwkExecutionMemoryOwner.Create(
            sharedOnly ? budget : null, CancellationToken.None);
        var limits = sharedOnly ? AwkRuntimeLimits.CreateDefault()
            : AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1024);
        var accounting = AwkMemoryAccounting.Create(limits, AwkRuntimeCounters.Disabled, owner);
        Assert.True(accounting.IsEnabled);
        var variables = CreateVariables(accounting, AwkRuntimeCounters.Disabled);
        var name = AwkArrayName.FromText("a");
        var existingKey = AwkArrayKey.FromOrdinal(1);
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
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    private static AwkVariables CreateVariables(AwkMemoryAccounting accounting, AwkRuntimeCounters counters) =>
        new(2, 100, 100, accounting, counters,
            static text => AwkValue.FromText(text),
            static text => AwkValue.FromNumericString(text),
            static text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal),
            static (_, _) => { }, static _ => { });
}
