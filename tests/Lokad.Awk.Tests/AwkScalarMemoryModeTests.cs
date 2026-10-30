namespace Lokad.Awk.Tests;

public sealed class AwkScalarMemoryModeTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void NumericUpdatesKeepTheirOwnerFundedAndRealGrowthStillFails(bool beforeBaseline, bool indexed)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var variables = CreateVariables(memory.Accounting, AwkRuntimeCounters.Disabled,
            static (_, _) => { }, static () => 0, static () => { });
        var name = AwkVariableName.FromText("total");
        if (beforeBaseline) variables.SetScalarValue(name, AwkValue.FromNumber(7));
        variables.CaptureScalarMemoryBaseline();
        if (!beforeBaseline) variables.SetScalarValue(name, AwkValue.FromNumber(7));
        var id = variables.GetOrCreateScalarSlot(name);
        var committed = memory.Accounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        for (var value = 8; value <= 100; value++)
        {
            if (indexed) variables.SetScalarValue(id, AwkValue.FromNumber(value));
            else variables.SetScalarValue(name, AwkValue.FromNumber(value));
        }
        Assert.Equal(100, variables.GetScalarValue(id).AsNumber());
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Throws<AwkRuntimeException>(() => variables.SetScalarText(name, new string('x', 4096)));
        Assert.Equal(100, variables.GetOrCreateScalarValue(name).AsNumber());
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NumericOfsPublicationReacquiresStorageAfterPreparation(bool fail)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        Action prepare = static () => { };
        var variables = CreateVariables(memory.Accounting, AwkRuntimeCounters.Disabled,
            static (_, _) => { }, static () => 0, () => prepare());
        var ofs = AwkVariableName.FromText("OFS");
        variables.SetScalarValue(ofs, AwkValue.FromNumber(42));
        variables.CaptureScalarMemoryBaseline();
        var failure = new AwkRuntimeException("preparation rejected"u8.ToArray());
        prepare = () =>
        {
            for (var index = 0; index < 256; index++)
                variables.SetScalarValue(AwkVariableName.FromText("added" + index), AwkValue.FromNumber(index));
            if (fail) throw failure;
        };
        if (fail)
            Assert.Same(failure, Assert.Throws<AwkRuntimeException>(() => variables.SetScalarValue(ofs, AwkValue.FromNumber(7))));
        else variables.SetScalarValue(ofs, AwkValue.FromNumber(7));
        Assert.Equal(fail ? 42 : 7, variables.GetOrCreateScalarValue(ofs).AsNumber());
        Assert.Equal(255, variables.GetOrCreateScalarValue(AwkVariableName.FromText("added255")).AsNumber());
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdatesPreserveComputedNfOfsPreparationAndRefunds(bool governed)
    {
        var budget = new CooperativeTestBudget(20_000);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        Assert.Equal(governed, accounting.IsEnabled);
        var fieldCount = 3;
        var assignments = 0;
        var preparations = 0;
        var variables = CreateVariables(accounting, counters, (name, value) =>
        {
            Assert.Equal("NF", name);
            fieldCount = (int)value.AsNumber();
            assignments++;
        }, () => fieldCount, () => preparations++);
        var name = AwkVariableName.FromText("x");
        var ofs = AwkVariableName.FromText("OFS");
        variables.SetScalarValue(name, AwkValue.FromNumber(42));
        variables.SetScalarText(ofs, " ");
        var nf = variables.GetOrCreateScalarSlot(AwkVariableName.FromText("NF"));
        Assert.Equal(3, variables.GetScalarValue(nf).AsNumber());
        Assert.Equal(0, preparations);
        variables.CaptureScalarMemoryBaseline();
        variables.SetScalarText(ofs, "|");
        Assert.Equal(1, preparations);
        variables.SetScalarValue(AwkVariableName.FromText("NF"), AwkValue.FromNumber(2));
        Assert.Equal(1, assignments);
        Assert.Equal(2, variables.GetOrCreateScalarValue(AwkVariableName.FromText("NF")).AsNumber());
        Assert.Equal(2, variables.GetScalarValue(nf).AsNumber());
        for (var iteration = 0; iteration < 3; iteration++)
        {
            variables.SetScalarText(name, new string('a', 4096));
            Assert.Equal(new string('a', 4096), variables.GetOrCreateScalarValue(name).AsString());
            variables.SetScalarValue(name, AwkValue.FromNumber(7));
            Assert.Equal(7, variables.GetOrCreateScalarValue(name).AsNumber());
            Assert.Equal(0, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOfsPreparationPreservesValueAndAdmission(bool governed)
    {
        var budget = new CooperativeTestBudget(20_000);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeCounters.Disabled, owner);
        var error = new AwkRuntimeException("preparation rejected"u8.ToArray());
        var preparations = 0;
        var variables = CreateVariables(accounting, AwkRuntimeCounters.Disabled, static (_, _) => { },
            static () => 0, () => { preparations++; throw error; });
        var ofs = AwkVariableName.FromText("OFS");
        variables.SetScalarText(ofs, " ");
        Assert.Equal(0, preparations);
        variables.CaptureScalarMemoryBaseline();
        Assert.Same(error, Assert.Throws<AwkRuntimeException>(() => variables.SetScalarText(ofs, new string('|', 4096))));
        Assert.Equal(1, preparations);
        Assert.Equal(" ", variables.GetOrCreateScalarValue(ofs).AsString());
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(0, budget.Reserved);
    }
    [Fact]
    public async Task SharedAdmissionRefusalPrecedesOfsPreparation()
    {
        var budget = new CooperativeTestBudget(1024);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeCounters.Disabled, owner);
        Assert.True(accounting.IsEnabled);
        var preparations = 0;
        var variables = CreateVariables(accounting, AwkRuntimeCounters.Disabled, static (_, _) => { },
            static () => 0, () => preparations++);
        var ofs = AwkVariableName.FromText("OFS");
        variables.SetScalarText(ofs, " ");
        variables.CaptureScalarMemoryBaseline();
        Assert.Throws<AwkRuntimeException>(() => variables.SetScalarText(ofs, new string('|', 4096)));
        Assert.Equal(0, preparations);
        Assert.Equal(" ", variables.GetOrCreateScalarValue(ofs).AsString());
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    private static AwkVariables CreateVariables(AwkMemoryAccounting accounting, AwkRuntimeCounters counters,
        Action<string, AwkValue> assign, Func<int> fieldCount, Action prepareOfs) =>
        new(accounting, counters, static text => AwkValue.FromText(text), static text => AwkValue.FromNumericString(text), static text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), assign, () => AwkValue.FromNumber(fieldCount()), prepareOfs);
}
