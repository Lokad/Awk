namespace Lokad.Awk.Tests;

public sealed class AwkBorrowedMembershipTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("café 😀", false)]
    [InlineData("007", false)]
    [InlineData("0", true)]
    [InlineData("7", true)]
    [InlineData("-2147483648", true)]
    [InlineData("2147483647", true)]
    public void BorrowedDictionaryLookupsMatchOwnedKeysWithoutRetainingOrCopyingTheInput(string text, bool ordinal)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var comparer = AwkArrayKeyComparer.ForRuntime(null, counters, AwkMemoryAccounting.Disabled);
        var key = ordinal ? AwkArrayKey.FromOrdinal(int.Parse(text, System.Globalization.CultureInfo.InvariantCulture))
            : new AwkArrayKey(AwkText.FromSourceLiteralString(text));
        var dictionary = new Dictionary<AwkArrayKey, int>(comparer) { [key] = 17 };
        var bytes = AwkUtf8Codec.Encode(text).ToArray();
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<byte>>();
        Assert.True(lookup.ContainsKey(bytes));
        Assert.Equal(17, lookup[bytes]);
        Assert.False(lookup.ContainsKey("?missing"u8));
        Assert.Single(dictionary);
        Assert.Equal(0, counters.AwkTextAllocations);
        Array.Fill(bytes, (byte)'x');
        Assert.True(lookup.ContainsKey(AwkUtf8Codec.Encode(text).Span));
        Assert.Equal(17, dictionary[key]);
        Assert.Equal(0, counters.AwkTextAllocations);
        if (ordinal && text == "7") Assert.False(lookup.ContainsKey("007"u8));
    }

    [Fact]
    public void AlternateCreationOwnsItsBytesAndChecksAdmissionBeforeCopying()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForAccounting = new CooperativeTestMemory(128, counters);
        var accounting = memoryForAccounting.Accounting;
        IAlternateEqualityComparer<ReadOnlySpan<byte>, AwkArrayKey> comparer = AwkArrayKeyComparer.ForRuntime(null, counters, accounting);
        var bytes = "keep"u8.ToArray();
        var key = comparer.Create(bytes);
        Array.Fill(bytes, (byte)'x');
        Assert.Equal("keep", key.AsString(AwkRuntimeCounters.Disabled));
        var allocations = counters.AwkTextAllocations;
        var error = Assert.Throws<AwkRuntimeException>(() => comparer.Create(new byte[512]));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(0, accounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongBorrowedHashAndComparisonObserveCancellation(bool equality)
    {
        using var cancellation = new CancellationTokenSource();
        var comparer = AwkArrayKeyComparer.ForRuntime(cancellation.Token.ThrowIfCancellationRequested);
        var bytes = new byte[16_384];
        Array.Fill(bytes, (byte)'x');
        var key = new AwkArrayKey(AwkText.FromHostBytes(bytes));
        cancellation.Cancel();
        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            if (equality) _ = comparer.Equals(bytes, key);
            else _ = comparer.GetHashCode(bytes);
        });
        Assert.Equal(cancellation.Token, error.CancellationToken);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task MembershipDoesNotAddElementsOrChargesAndRetainedKeysSurviveRecordChanges(int mode)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(mode != 0 ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            limits, regexEngine: AwkUtf8RegexEngine.Instance)
        { MemoryOwner = owner };
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var array = AwkArrayName.FromText("lookup");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(array, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromString("hit tail"));
        _ = state.FieldCount;
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var allocations = counters.AwkTextAllocations;
        Assert.True(state.ContainsArrayField(array, 1));
        Assert.False(state.ContainsArrayField(array, 2));
        Assert.False(state.ContainsArrayField(array, 9));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        state.SetCurrentRecord(AwkText.FromString("new other"));
        Assert.False(state.ContainsArrayField(array, 1));
        Assert.Equal(7, state.GetArrayAwkValue(array, key).AsNumber());
    }

    [Fact]
    public async Task PendingIndexCompletesBeforeBorrowingTheSelectedRecord()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var name = AwkArrayName.FromText("lookup");
        state.SetArrayAwkValue(name, new AwkArrayKey("hit"), AwkValue.FromNumber(1));
        state.SetCurrentRecord(AwkText.FromString("miss old"));
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var expression = AwkExpression.ArrayMembership(AwkArrayIndex.Simple(AwkExpression.FieldIndex(new PendingIndex(completion.Task))), name);
        var pending = expression.EvaluateAsync(new AwkEvaluationContext(state));
        Assert.False(pending.IsCompleted);
        state.SetCurrentRecord(AwkText.FromString("hit after"));
        completion.SetResult(AwkValue.FromNumber(1));
        Assert.Equal(1, (await pending).AsNumber());
    }

    private sealed record ForwardingIndex(AwkArrayIndex Inner) : AwkArrayIndex
    {
        public override ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context) => Inner.ResolveKeyAsync(context);
    }

    private sealed class PendingIndex(Task<AwkValue> result) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => new(result);
    }
}
