namespace Lokad.Awk.Tests;

public sealed class AwkBorrowedSubstringTests
{
    private const long Limit = 8 << 20;

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task PristineSmallFieldProducesOwnedResult(bool governed, bool whole)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using (var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None))
        {
            using var state = State(owner);
            Assert.Equal(governed, state.MemoryAccounting.IsEnabled);
            var bytes = "key résumé"u8.ToArray();
            state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
            var allocations = state.Counters.AwkTextAllocations;
            var result = await Call(whole: whole).EvaluateAsync(new(state));
            Assert.Equal(1, state.Counters.BorrowedFieldSubstrings);
            Assert.Equal(1, state.Counters.AwkTextAllocations - allocations);
            Array.Fill(bytes, (byte)'x');
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("other record"));
            Assert.Equal(whole ? "résumé" : "ré", result.AsString());
            Assert.Equal(0, state.Counters.StringDecodes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
    public async Task OnlyPristineShortLiteralInputsBorrow(string shape, int expected)
    {
        using var state = State(AwkExecutionMemoryOwner.Disabled);
        var ascii = int.TryParse(shape, out var size);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("key " + (ascii ? new string('a', size) : "résumé")));
        if (shape == "cached") _ = state.GetFieldValue(2);
        if (shape == "owned") state.AssignField(2, "résumé");
        var start = AwkVariableName.FromText("start");
        state.AssignVariableValue(start, AwkValue.FromNumber(1));
        var expression = shape == "variable" ? Call(start: AwkExpression.Variable(start)) : Call();
        var result = await expression.EvaluateAsync(new(state));
        Assert.Equal(ascii ? "aa" : "ré", result.AsString());
        Assert.Equal(expected, state.Counters.BorrowedFieldSubstrings);
    }

    [Theory]
    [InlineData(false, 1, "é")]
    [InlineData(true, 2, "é")]
    [InlineData(true, 1, null)]
    public async Task ByteSlicesKeepStrictUtf8AndScalarBoundaries(bool bytes, int length, string? expected)
    {
        using var state = State(AwkExecutionMemoryOwner.Disabled, bytes: bytes);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("key é😀z"));
        if (expected is null)
        {
            using var baseline = State(AwkExecutionMemoryOwner.Disabled, bytes: bytes);
            baseline.SetCurrentRecord(AwkText.FromSourceLiteralString("key é😀z"));
            _ = baseline.GetFieldValue(2);
            var ordinary = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await Call(length: length).EvaluateAsync(new(baseline)));
            Assert.Equal(0, baseline.Counters.BorrowedFieldSubstrings);
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await Call(length: length).EvaluateAsync(new(state)));
            Assert.Equal(ordinary.MessageBytes.ToArray(), error.MessageBytes.ToArray());
            Assert.Equal("key é😀z", state.CurrentRecordByteText.AsString());
        }
        else
            Assert.Equal(expected, (await Call(length: length).EvaluateAsync(new(state))).AsString());
        Assert.Equal(1, state.Counters.BorrowedFieldSubstrings);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task ResourceFailureKeepsAssignmentAndInput()
    {
        var budget = new CooperativeTestBudget(Limit);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            using var state = State(owner);
            var record = AwkText.FromSourceLiteralString("key résumé");
            state.SetCurrentRecord(record);
            _ = state.FieldCount;
            var target = AwkVariableName.FromText("saved");
            state.AssignVariableValue(target, AwkValue.FromText("keep"));
            var expression = AwkExpression.Assignment(AwkReference.Scalar(target), Call());
            using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes))
            {
                var committed = state.MemoryAccounting.CurrentCommittedBytes;
                var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await expression.EvaluateAsync(new(state)));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
                Assert.Equal("keep", state.ResolveVariableValue(target).AsString());
                Assert.Same(record, state.CurrentRecordByteText);
                Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
                Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            }
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task CancellationDoesNotPublishResult()
    {
        using var cancellation = new CancellationTokenSource();
        using var state = State(AwkExecutionMemoryOwner.Disabled, cancellation.Token);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("key résumé"));
        var target = AwkVariableName.FromText("saved");
        state.AssignVariableValue(target, AwkValue.FromText("keep"));
        cancellation.Cancel();
        var expression = AwkExpression.Assignment(AwkReference.Scalar(target), Call());
        var error = await Assert.ThrowsAsync<OperationCanceledException>(async () => await expression.EvaluateAsync(new(state)));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, state.Counters.BorrowedFieldSubstrings);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkExpression Call(bool whole = false, int length = 2, AwkExpression? start = null)
    {
        Assert.True(AwkBuiltins.TryGetCallable("substr", out var builtin));
        var field = AwkExpression.FieldIndex(AwkExpression.Number(2));
        var first = start ?? AwkExpression.Number(1);
        return AwkExpression.BuiltinCall(builtin, whole ? [field, first]
            : [field, first, AwkExpression.Number(length)]);
    }

    private static AwkRuntimeState State(AwkExecutionMemoryOwner owner, CancellationToken token = default,
        bool bytes = false, AwkRuntimeLimits? limits = null)
    {
        limits ??= AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), limits)
        { MemoryOwner = owner };
        var profile = bytes ? AwkCompatibilityProfile.Default.WithCharactersAsBytes() : AwkCompatibilityProfile.Default;
        return new(AwkFieldSeparatorSpec.Default, token, options,
            new AwkRuntimeResourceMonitor(limits, options.Counters, token), profile);
    }
}
