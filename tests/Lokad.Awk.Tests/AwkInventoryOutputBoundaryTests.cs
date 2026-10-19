using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkInventoryOutputBoundaryTests
{
    private const int Large = AwkRuntimeLimits.CancellationCheckBytes * 16;

    public static IEnumerable<object[]> OutputCopies()
    {
        foreach (var observed in new[] { false, true })
        foreach (var kind in new[] { "text", "parts", "bytes" })
        foreach (var rope in new[] { false, true })
            yield return [observed, kind, rope];
    }

    [Theory]
    [MemberData(nameof(OutputCopies))]
    public void InterruptedOutputAppendPreservesEarlierBytesAndAllowsRetry(bool observed, string kind, bool rope)
    {
        var counters = Counters(observed);
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(8 << 20);
        var governor = AwkMemoryAccounting.Create(limits, counters);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var checks = 0;
        void Check()
        {
            if (!armed) return;
            if (++checks == 6) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        using var output = new AwkOutputSink(limits, governor, counters, Check);
        output.Append(AwkText.FromSourceLiteralString("kept:"));
        var text = rope
            ? AwkText.FromString(new string('a', Large / 2)).Concat(AwkText.FromString(new string('b', Large / 2)), AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled)
            : AwkText.FromString(new string('a', Large));
        var expected = rope ? new string('a', Large / 2) + new string('b', Large / 2) : new string('a', Large);
        var bytes = Encoding.UTF8.GetBytes(expected);
        armed = true;

        var error = Assert.Throws<OperationCanceledException>(() => Append(output, kind, text, bytes));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(6, checks);
        Assert.Equal("kept:", output.ToString());
        Assert.Equal(0, governor.CurrentReservedBytes);
        armed = false;
        Append(output, kind, text, bytes);
        Assert.Equal("kept:" + expected + (kind == "parts" ? "!" : ""), output.ToString());
        Assert.Equal(observed ? 1 : 0, counters.OutputSinkCreations);
        output.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, "text")]
    [InlineData(true, "text")]
    [InlineData(false, "parts")]
    [InlineData(true, "parts")]
    [InlineData(false, "bytes")]
    [InlineData(true, "bytes")]
    public void OutputMemoryFailurePreservesEarlierBytes(bool observed, string kind)
    {
        var counters = Counters(observed);
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1_024);
        var governor = AwkMemoryAccounting.Create(limits, counters);
        using var output = new AwkOutputSink(limits, governor, counters, static () => { });
        output.Append(AwkText.FromSourceLiteralString("kept:"));
        var committed = governor.CurrentCommittedBytes;
        var text = AwkText.FromString(new string('a', Large));
        var bytes = Encoding.UTF8.GetBytes(text.AsString());

        var error = Assert.Throws<AwkRuntimeException>(() => Append(output, kind, text, bytes));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("kept:", output.ToString());
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActionExecutorOutputUsesTheRuntimeCancellationToken(bool observed)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = Counters(observed);
        var state = State(counters, cancellation.Token);
        var program = new AwkProgram([], [], [], [], [], new Dictionary<AwkFunctionName, AwkUserFunction>());
        using var executor = new AwkRuntimeActionExecutor(program, state);
        executor.Output.Append(AwkText.FromSourceLiteralString("kept:"));
        cancellation.Cancel();

        var error = Assert.Throws<OperationCanceledException>(() =>
            executor.Output.Append(AwkText.FromString(new string('a', Large))));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal("kept:", executor.Output.ToString());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(8 << 20), counters);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var checks = 0;
        void Check()
        {
            if (!armed) return;
            if (++checks == 3) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var variables = new AwkVariables(4_096, 4_096, 1_024, governor, counters, AwkValue.FromText, AwkValue.FromNumericString, text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), (_, _) => { }, _ => { }, checkpoint: Check);
        var names = Enumerable.Range(0, 512).Select(index => AwkVariableName.FromText("v" + index)).ToHashSet();
        var array = AwkArrayName.FromText(symbolTable ? AwkVariables.SymbolTableName : "kept");
        if (symbolTable) variables.InitializeGlobalNames(names);
        else
            foreach (var name in names)
                variables.SetArrayValue(array, new AwkArrayKey(name.Text), AwkValue.FromNumber(7));
        var count = variables.GetArraySnapshotCount(array);
        var committed = governor.CurrentCommittedBytes;
        armed = true;

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
        Assert.Equal(3, checks);
        armed = false;
        Assert.Equal(count, variables.GetArraySnapshotCount(array));
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        var keys = new AwkArrayKey[count];
        variables.CopyArrayKeys(array, keys);
        Assert.Equal(names.Select(static name => name.Text).Order(StringComparer.Ordinal), keys.Select(static key => key.ToString()).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AsciiTerminatorsReuseSharedTextAcrossAllByteValues()
    {
        for (var value = 0; value < 128; value++)
        {
            var first = AwkRecordTerminator.FromAsciiByte((byte)value).ByteText;
            Assert.Same(first, AwkRecordTerminator.FromAsciiByte((byte)value).ByteText);
            Assert.Equal(new byte[] { (byte)value }, first.AsSpan().ToArray());
        }
        Assert.Same(AwkRecordTerminator.NewLine.ByteText, AwkRecordTerminator.FromAsciiByte(10).ByteText);
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
        var state = State(counters, CancellationToken.None);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(Array.Empty<byte>(), [], AwkReadCompletion.EndOfFile));
        var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        state.AssignVariableValue(AwkVariableName.FromText("RS"), AwkValue.FromText(AwkText.FromSourceLiteralString(separator)));
        var allocations = counters.AwkTextAllocations;
        var payloadBytes = counters.AwkTextPayloadBytes;
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn));

        Assert.False((await cursor.ReadAsync()).HasRecord);

        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(payloadBytes, counters.AwkTextPayloadBytes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, counters.RegexCompiles);
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

    private static bool GetSlot(AwkGlobalSlotId id, out AwkVariableSlot slot)
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

    private static AwkRuntimeState State(AwkRuntimeCounters counters, CancellationToken token)
    {
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(8 << 20),
            AwkRegexEngineKind.Utf8Adapter, regexEngine: AwkUtf8RegexEngine.Instance);
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, options);
    }
}
