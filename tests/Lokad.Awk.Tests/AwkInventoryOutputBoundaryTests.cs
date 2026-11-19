using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkInventoryOutputBoundaryTests
{
    private const int Large = AwkRuntimeLimits.CancellationCheckBytes * 16;

    public static IEnumerable<object[]> OutputCopies()
    {
        foreach (var kind in new[] { "text", "parts", "bytes" })
            foreach (var rope in new[] { false, true })
                yield return [kind, rope];
    }

    [Theory]
    [MemberData(nameof(OutputCopies))]
    public void CancellationDuringOutputAdmissionPreservesEarlierBytesAndAllowsRetry(string kind, bool rope)
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        using var output = new AwkOutputSink(memory.Accounting, () => token.ThrowIfCancellationRequested());
        output.Append(AwkText.FromSourceLiteralString("kept:"));
        using var state = AwkTestHarness.CreateRuntimeStateWithCounters(AwkRuntimeCounters.Disabled);
        var text = rope
            ? state.ConcatText([AwkText.FromString(new string('a', Large / 2)), AwkText.FromString(new string('b', Large / 2))])
            : AwkText.FromString(new string('a', Large));
        var expected = rope ? new string('a', Large / 2) + new string('b', Large / 2) : new string('a', Large);
        var bytes = Encoding.UTF8.GetBytes(expected);
        memory.Budget.OnGrowth = _ => cancellation.Cancel();

        var error = Assert.Throws<OperationCanceledException>(() => Append(output, kind, text, bytes));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal("kept:", output.ToString());
        memory.Budget.OnGrowth = null;
        token = CancellationToken.None;
        Append(output, kind, text, bytes);
        Assert.Equal("kept:" + expected + (kind == "parts" ? "!" : ""), output.ToString());

        output.Dispose();
        Assert.Equal(0, memory.Accounting.LiveBytes);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("parts")]
    [InlineData("bytes")]
    public void OutputMemoryFailurePreservesEarlierBytes(string kind)
    {
        using var memoryForGovernor = new CooperativeTestMemory(1_024);
        var governor = memoryForGovernor.Accounting;
        using var output = new AwkOutputSink(governor, static () => { });
        output.Append(AwkText.FromSourceLiteralString("kept:"));
        var committed = governor.LiveBytes;
        var text = AwkText.FromString(new string('a', Large));
        var bytes = Encoding.UTF8.GetBytes(text.AsString());

        var error = Assert.Throws<AwkRuntimeException>(() => Append(output, kind, text, bytes));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("kept:", output.ToString());
        Assert.Equal(committed, governor.LiveBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActionExecutorOutputUsesTheRuntimeCancellationToken(bool observed)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = Counters(observed);
        using var memoryForState = State(counters, cancellation.Token);
        var state = memoryForState.State;
        var program = new AwkProgram([], null, [], [], [], new Dictionary<AwkFunctionName, AwkUserFunction>());
        using var executor = new AwkRuntimeActionExecutor(program, state);
        executor.Output.Append(AwkText.FromSourceLiteralString("kept:"));
        cancellation.Cancel();

        var error = Assert.Throws<OperationCanceledException>(() =>
            executor.Output.Append(AwkText.FromString(new string('a', Large))));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal("kept:", executor.Output.ToString());
    }

    public static IEnumerable<object[]> Inventories()
    {
        foreach (var source in new[] { "globals", "scalars", "arrays", "duplicate-scalars", "duplicate-arrays" })
            foreach (var operation in new[] { "count", "keys", "values" })
                yield return [source, operation];
    }

    [Theory]
    [MemberData(nameof(Inventories))]
    public void SymbolTableInventoryChecksEvenFilteredNamesAndAllowsRetry(string source, string operation)
    {
        var names = Enumerable.Range(0, 4_096).Select(index => AwkVariableName.FromText("v" + index)).ToArray();
        var duplicate = source.StartsWith("duplicate-", StringComparison.Ordinal);
        var globals = source == "globals" ? names.ToHashSet() : duplicate ? new HashSet<AwkVariableName> { names[0] } : [];
        globals.Add(AwkVariableName.FromText(AwkVariables.SymbolTableName));
        var scalarNames = source.EndsWith("scalars", StringComparison.Ordinal)
            ? duplicate ? Enumerable.Repeat(names[0], names.Length) : names : [];
        var arrayNames = source.EndsWith("arrays", StringComparison.Ordinal)
            ? duplicate ? Enumerable.Repeat(names[0], names.Length) : names : [];
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var checks = 0;
        void Check()
        {
            if (!armed) return;
            if (++checks == 5) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var table = Table(globals, scalarNames, arrayNames, Check);
        var count = table.Count;
        Assert.Equal(duplicate ? 1 : names.Length, count);
        armed = true;

        var error = Assert.Throws<OperationCanceledException>(() => ReadInventory(table, count, operation));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(5, checks);
        armed = false;
        Assert.Equal(count, table.Count);
        var keys = new AwkArrayKey[count];
        table.CopyKeys(keys);
        var values = table.SnapshotValues(count, 0, static (_, key, _) => key);
        var expected = (duplicate ? names.Take(1) : names).Select(static name => name.Text).Order(StringComparer.Ordinal);
        Assert.Equal(expected, keys.Select(static key => key.ToString()).Order(StringComparer.Ordinal));
        Assert.Equal(keys, values);
    }

    [Theory]
    [InlineData("keys")]
    [InlineData("values")]
    public void SymbolTableLongKeyEncodingUsesTheCheckpoint(string operation)
    {
        var name = AwkVariableName.FromText(new string('a', Large));
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = false;
        void Check()
        {
            if (!armed) return;
            if (++checks == 5) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var table = Table(new HashSet<AwkVariableName> { name }, [], [], Check);
        armed = true;

        var error = Assert.Throws<OperationCanceledException>(() => ReadInventory(table, 1, operation));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(5, checks);
        armed = false;
        var keys = new AwkArrayKey[1];
        table.CopyKeys(keys);
        Assert.Equal(name.Text, keys[0].ToString());
        Assert.Equal(1, table.Count);
    }

    [Theory]
    [InlineData(false, "keys")]
    [InlineData(false, "values")]
    [InlineData(true, "count")]
    [InlineData(true, "keys")]
    [InlineData(true, "values")]
    public void VariableSnapshotsPassTheirCheckpointAndPreserveStorage(bool symbolTable, string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters, cancellation.Token);
        var governor = memory.Accounting;
        var variables = memory.State.Variables;
        var names = Enumerable.Range(0, 512).Select(index => AwkVariableName.FromText("v" + index)).ToHashSet();
        var array = AwkArrayName.FromText(symbolTable ? AwkVariables.SymbolTableName : "kept");
        if (symbolTable) variables.InitializeGlobalNames(names);
        else
            foreach (var name in names)
                variables.SetArrayValue(array, new AwkArrayKey(name.Text), AwkValue.FromNumber(7));
        var count = variables.GetArraySnapshotCount(array);
        var committed = governor.LiveBytes;
        cancellation.Cancel();

        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            switch (operation)
            {
                case "count": _ = variables.GetArraySnapshotCount(array); break;
                case "keys": variables.CopyArrayKeys(array, new AwkArrayKey[count]); break;
                case "values": _ = variables.SnapshotArrayValues(array, count, 0, static (_, key, _) => key); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(committed, governor.LiveBytes);
        using var retry = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters);
        var retryVariables = retry.State.Variables;
        if (symbolTable) retryVariables.InitializeGlobalNames(names);
        else
            foreach (var name in names)
                retryVariables.SetArrayValue(array, new AwkArrayKey(name.Text), AwkValue.FromNumber(7));
        Assert.Equal(count, retryVariables.GetArraySnapshotCount(array));
        var keys = new AwkArrayKey[count];
        retryVariables.CopyArrayKeys(array, keys);
        var copiedNames = keys.Select(static key => key.ToString()).ToHashSet(StringComparer.Ordinal);
        Assert.True(names.All(name => copiedNames.Contains(name.Text)));
    }

    [Fact]
    public void AsciiTerminatorsReuseSharedTextAcrossAllByteValues()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var decodeRuntime = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        for (var value = 0; value < 128; value++)
        {
            var first = AwkRecordTerminator.FromAsciiByte((byte)value).ByteText;
            Assert.True(first.HasSameStorage(AwkRecordTerminator.FromAsciiByte((byte)value).ByteText));
            Assert.Equal(new byte[] { (byte)value }, first.AsSpan().ToArray());
            _ = decodeRuntime.DecodeText(first);
            var decodes = counters[AwkCounter.StringDecodes];
            _ = decodeRuntime.DecodeText(first);
            Assert.Equal(decodes, counters[AwkCounter.StringDecodes]);
        }
        Assert.True(AwkRecordTerminator.NewLine.ByteText.HasSameStorage(AwkRecordTerminator.FromAsciiByte(10).ByteText));
        Assert.Throws<ArgumentOutOfRangeException>(() => AwkRecordTerminator.FromAsciiByte(128));
    }

    [Theory]
    [InlineData(false, "|")]
    [InlineData(true, "|")]
    [InlineData(false, "[,|]")]
    [InlineData(true, "[,|]")]
    public async Task CachedAsciiSeparatorsCreateNoTerminatorTexts(bool observed, string separator)
    {
        var counters = Counters(observed);
        using var memoryForState = State(counters, CancellationToken.None);
        var state = memoryForState.State;
        var host = new TestOperationVisitor([]);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(Array.Empty<byte>(), AwkReadCompletion.EndOfFile, static _ => { }));
        var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
        state.AssignVariableValue(AwkVariableName.FromText("RS"), AwkValue.FromText(AwkText.FromSourceLiteralString(separator)));
        var allocations = counters[AwkCounter.TextAllocations];
        var payloadBytes = counters[AwkCounter.TextBytes];
        using var cursor = new AwkRecordCursor(state, reader);

        Assert.False((await cursor.ReadRecordAsync()).HasRecord);

        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        Assert.Equal(payloadBytes, counters[AwkCounter.TextBytes]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);

    }

    private static AwkSymbolTableArray Table(IReadOnlySet<AwkVariableName> globals,
        IEnumerable<AwkVariableName> scalars, IEnumerable<AwkVariableName> arrays, Action checkpoint) =>
        new(() => globals, () => scalars.Select(static name => new AwkScalarInventoryItem(new AwkGlobalSlotId(0), name)),
            Resolve, GetSlot, static _ => false, () => arrays.Select(static name => new AwkArrayName(name.Identifier)),
            static _ => AwkValue.Unassigned(), static (_, _) => { }, checkpoint);

    private static bool Resolve(AwkVariableName name, out AwkGlobalSlotId slot)
    {
        slot = default;
        return false;
    }

    private static bool GetSlot(AwkGlobalSlotId id, out AwkValue slot)
    {
        slot = default;
        return false;
    }

    private static void ReadInventory(AwkSymbolTableArray table, int count, string operation)
    {
        switch (operation)
        {
            case "count": _ = table.Count; break;
            case "keys": table.CopyKeys(new AwkArrayKey[count]); break;
            case "values": _ = table.SnapshotValues(count, 0, static (_, key, _) => key); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static void Append(AwkOutputSink output, string kind, AwkText text, byte[] bytes)
    {
        switch (kind)
        {
            case "text": output.Append(text); break;
            case "parts": output.Append(new AwkText[] { text, AwkText.FromSourceLiteralString("!") }.AsSpan()); break;
            case "bytes": output.Append(bytes.AsMemory()); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static AwkRuntimeCounters Counters(bool observed) =>
        observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;

    private static CooperativeTestMemory State(AwkRuntimeCounters counters, CancellationToken token)
    {

        var testMemory = new CooperativeTestMemory(8 << 20);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        return testMemory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, testMemory.Configure(options)));
    }
}
