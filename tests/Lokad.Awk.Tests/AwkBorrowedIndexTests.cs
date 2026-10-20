namespace Lokad.Awk.Tests;

public sealed class AwkBorrowedIndexTests
{
    private const long Limit = 8 << 20;

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task SearchDoesNotRetainFieldBytesOrPopulateValueCache(int mode)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using (var owner = AwkExecutionMemoryOwner.Create(mode != 0 ? budget : null, CancellationToken.None))
        {
            var state = State(mode, owner);
            var bytes = "id déjà-timeout"u8.ToArray();
            state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
            _ = state.FieldCount;
            var allocations = state.Counters.AwkTextAllocations;
            var committed = state.MemoryAccounting.CurrentCommittedBytes;
            Assert.Equal(6, (await Call().EvaluateAsync(new(state))).AsNumber());
            Assert.Equal(0, state.Counters.AwkTextAllocations - allocations);
            Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
            Array.Fill(bytes, (byte)'x', 3, bytes.Length - 3);
            Assert.Equal(0, (await Call().EvaluateAsync(new(state))).AsNumber());
            Assert.Equal(2, state.Counters.BorrowedFieldIndexSearches);
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("id déjà-timeout"));
            Assert.Equal(6, (await Call().EvaluateAsync(new(state))).AsNumber());
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("63", 1)]
    [InlineData("64", 1)]
    [InlineData("65", 0)]
    [InlineData("cached", 0)]
    [InlineData("owned", 0)]
    [InlineData("variable", 0)]
    [InlineData("lint", 0)]
    [InlineData("regex", 0)]
    public async Task ExistingFallbacksStayOrdinary(string shape, int expected)
    {
        var state = State(0, AwkExecutionMemoryOwner.Disabled, lint: shape == "lint", regex: shape == "regex");
        var ascii = int.TryParse(shape, out var size);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("id " + (ascii ? new string('a', size) : "déjà-timeout")));
        if (shape == "cached") _ = state.GetFieldValue(2);
        if (shape == "owned") state.AssignField(2, "déjà-timeout");
        var needle = AwkVariableName.FromText("needle");
        state.AssignVariableValue(needle, AwkValue.FromText("timeout"));
        var expression = shape == "variable" ? Call(AwkExpression.Variable(needle)) : Call();
        Assert.Equal(ascii ? 0 : 6, (await expression.EvaluateAsync(new(state))).AsNumber());
        Assert.Equal(expected, state.Counters.BorrowedFieldIndexSearches);
    }

    [Theory]
    [InlineData(false, false, 6)] [InlineData(true, false, 8)]
    [InlineData(false, true, 6)] [InlineData(true, true, 8)]
    public async Task SearchKeepsCaseAndCoordinateRules(bool bytes, bool ignoreCase, int expected)
    {
        var state = State(0, AwkExecutionMemoryOwner.Disabled, bytes: bytes);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(ignoreCase ? "id déjà-TIMEOUT😀" : "id déjà-timeout😀"));
        if (ignoreCase) state.AssignVariableValue(AwkVariableName.FromText("IGNORECASE"), AwkValue.FromNumber(1));
        Assert.Equal(expected, (await Call().EvaluateAsync(new(state))).AsNumber());
        Assert.Equal(bytes ? 15 : 13, (await Call(AwkExpression.Literal("😀")).EvaluateAsync(new(state))).AsNumber());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("id 007"));
        Assert.Equal(1, (await Call(AwkExpression.Literal("00")).EvaluateAsync(new(state))).AsNumber());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("id"));
        Assert.Equal(1, (await Call(AwkExpression.Literal("")).EvaluateAsync(new(state))).AsNumber());
        Assert.Equal(0, (await Call().EvaluateAsync(new(state))).AsNumber());
    }

    [Theory]
    [InlineData(1, false)] [InlineData(2, false)]
    [InlineData(1, true)] [InlineData(2, true)]
    public async Task ExistingFieldCapacityNeedsNoSearchAdmissionButUnparsedFieldsDo(int mode, bool parsed)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using (var owner = AwkExecutionMemoryOwner.Create(mode != 0 ? budget : null, CancellationToken.None))
        {
            var state = State(mode, owner);
            var record = AwkText.FromSourceLiteralString("id déjà-timeout");
            state.SetCurrentRecord(record);
            if (parsed) _ = state.FieldCount;
            using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes))
            {
                if (parsed)
                    Assert.Equal(6, (await Call().EvaluateAsync(new(state))).AsNumber());
                else
                {
                    var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await Call().EvaluateAsync(new(state)));
                    Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
                    Assert.Equal(0, state.Counters.BorrowedFieldIndexSearches);
                }
                Assert.Same(record, state.CurrentRecordByteText);
                Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            }
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task CancellationDoesNotStartSearch()
    {
        using var cancellation = new CancellationTokenSource();
        var state = State(0, AwkExecutionMemoryOwner.Disabled, cancellation.Token);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("id déjà-timeout"));
        cancellation.Cancel();
        var error = await Assert.ThrowsAsync<OperationCanceledException>(async () => await Call().EvaluateAsync(new(state)));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, state.Counters.BorrowedFieldIndexSearches);
    }

    private static AwkExpression Call(AwkExpression? needle = null)
    {
        Assert.True(AwkBuiltins.TryGetCallable("index", out var builtin));
        return AwkExpression.BuiltinCall(builtin,
            [AwkExpression.FieldIndex(AwkExpression.Number(2)), needle ?? AwkExpression.Literal("timeout")]);
    }

    private static AwkRuntimeState State(int mode,AwkExecutionMemoryOwner owner,CancellationToken token = default,bool bytes = false,bool lint = false,bool regex = false,AwkRuntimeLimits? limits = null)
    {
        limits ??= AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), limits)
        { MemoryOwner = owner };
        var profile = bytes ? AwkCompatibilityProfile.Default.WithCharactersAsBytes() : AwkCompatibilityProfile.Default;
        if (lint) profile = profile.WithLintMode(AwkLintMode.Normal);
        return new(regex ? AwkFieldSeparatorSpec.FromText("[ ]+") : AwkFieldSeparatorSpec.Default, token, options,
            new AwkRuntimeResourceMonitor(limits, options.Counters, token), profile);
    }
}
