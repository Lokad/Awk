namespace Lokad.Awk.Tests;

public sealed class AwkTextCancellationTests
{
    private const int LongText = 16_384;

    public static IEnumerable<object[]> Utf8IngressInvalidEdges()
    {
        foreach (var prefix in new[] { 0, 4092, 4093, 4094, 4095, 4096, 4097, 8191, 8192, 8193 })
            foreach (var suffix in new[] { "80", "C0AF", "EDA080", "F4908080", "E282", "F0288CBC" })
                yield return [prefix, suffix];
    }

    [Theory]
    [MemberData(nameof(Utf8IngressInvalidEdges))]
    public void Utf8IngressRejectsMalformedChunkEdgesBeforeAllocation(int prefix, string suffix)
    {
        var invalid = Convert.FromHexString(suffix);
        var bytes = new byte[prefix + invalid.Length];
        bytes.AsSpan(0, prefix).Fill((byte)'a');
        invalid.CopyTo(bytes.AsSpan(prefix));
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = Governor().InitializeRuntime(counters);
        var committed = memory.Accounting.LiveBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => memory.State.CopyHostText(bytes));
        Assert.Equal("awk: invalid UTF-8 input\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, counters[AwkCounter.TextBytes]);
        Assert.Equal(committed, memory.Accounting.LiveBytes);
    }

    [Theory]
    [InlineData(4095, "FF", false)]
    [InlineData(4096, "FF", true)]
    [InlineData(4097, "FF", true)]
    [InlineData(4095, "E282", false)]
    [InlineData(4095, "E2FFAC", false)]
    [InlineData(4095, "F09F9880FF", true)]
    [InlineData(4096, "E282", true)]
    [InlineData(4093, "F09F988080", true)]
    [InlineData(4094, "E282AC80", true)]
    [InlineData(4095, "C2A080", true)]
    [InlineData(4095, "F09F98FF", false)]
    [InlineData(4094, "F09FFF80", false)]
    public void Utf8IngressKeepsCancellationAndInvalidByteFailureOrder(int prefix, string suffix, bool cancellationWins)
    {
        var tail = Convert.FromHexString(suffix);
        var bytes = new byte[prefix + tail.Length];
        bytes.AsSpan(0, prefix).Fill((byte)'a');
        tail.CopyTo(bytes.AsSpan(prefix));
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        void Check()
        {
            if (++checks == 2) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        if (cancellationWins)
        {
            var error = Assert.Throws<OperationCanceledException>(() => AwkText.ThrowIfInvalidUtf8(bytes, Check));
            Assert.Equal(cancellation.Token, error.CancellationToken);
        }
        else
        {
            var error = Assert.Throws<AwkRuntimeException>(() => AwkText.ThrowIfInvalidUtf8(bytes, Check));
            Assert.Equal("awk: invalid UTF-8 input\n", AwkUtf8Codec.Decode(error.MessageBytes));
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(4095, false)]
    [InlineData(4096, false)]
    [InlineData(8193, false)]
    [InlineData(4095, true)]
    [InlineData(4092, true)]
    [InlineData(4093, true)]
    [InlineData(4094, true)]
    [InlineData(8191, true)]
    public void Utf8IngressKeepsOwnedCopyAndByteNativeCounters(int prefix, bool supplementary)
    {
        var input = AwkUtf8Codec.Encode(new string('a', prefix) + (supplementary ? "😀" : "")).ToArray();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = Governor().InitializeRuntime(counters);
        var committed = memory.Accounting.LiveBytes;
        var text = memory.State.CopyHostText(input);
        Assert.True(text.AsSpan().SequenceEqual(input));
        if (input.Length > 0)
        {
            input[0] = 0xFF;
            Assert.NotEqual(input[0], text.AsSpan()[0]);
        }
        Assert.Equal(input.Length == 0 ? 0 : 1, counters[AwkCounter.TextAllocations]);
        Assert.Equal(input.Length, counters[AwkCounter.TextBytes]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(committed, memory.Accounting.LiveBytes);
    }

    [Theory]
    [InlineData("ascii")]
    [InlineData("bmp")]
    [InlineData("supplementary")]
    [InlineData("deep")]
    public void InterruptedMaterializationReleasesMemoryAndRetainsCompleteParts(string scenario)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor().InitializeRuntime(counters);
        var governor = memoryForGovernor.Accounting;
        var committed = governor.LiveBytes;
        var expected = scenario switch
        {
            "ascii" => new string('a', LongText) + "z",
            "bmp" => new string('\u754c', LongText) + "z",
            "supplementary" => string.Concat(Enumerable.Repeat("\ud83d\ude00", LongText)) + "z",
            "deep" => new string('a', 3_000) + "z",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var text = scenario == "deep" ? DeepText(3_000, memoryForGovernor.State)
            : memoryForGovernor.State.ConcatText([AwkText.FromString(expected[..^1]), AwkText.FromString("z")]);
        using var cancellation = new CancellationTokenSource();
        var copyingChecks = 0;
        Action checkpoint = () =>
        {
            if (governor.LiveBytes > committed && ++copyingChecks == 4)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };

        var error = Assert.Throws<OperationCanceledException>(() => { _ = text.AsSpan(checkpoint); });

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(committed, governor.LiveBytes);
        Assert.Equal(0, counters[AwkCounter.ConcatenationBytesCopied]);
        Assert.Equal(expected, text.AsString());
        Assert.Equal(text.ByteLength, counters[AwkCounter.ConcatenationBytesCopied]);
        Assert.Equal(expected, text.AsString());
        Assert.Equal(text.ByteLength, counters[AwkCounter.ConcatenationBytesCopied]);
    }

    [Theory]
    [InlineData("runes")]
    [InlineData("ascii")]
    [InlineData("number")]
    [InlineData("parsed-value")]
    [InlineData("hash")]
    [InlineData("key")]
    [InlineData("compare")]
    [InlineData("text-equality")]
    [InlineData("search")]
    [InlineData("string")]
    [InlineData("memory")]
    public void CallersCheckCancellationBeforeHiddenMaterialization(string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        using var runtime = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token,
            AwkTestHarness.CreateRuntimeOptions(counters));
        var text = runtime.ConcatText([AwkText.FromString(new string('0', LongText)), AwkText.FromString("1")]);
        cancellation.Cancel();
        Action checkpoint = cancellation.Token.ThrowIfCancellationRequested;

        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            switch (operation)
            {
                case "runes": _ = text.GetRuneLength(checkpoint); break;
                case "ascii": _ = text.IsAscii(out _, checkpoint); break;
                case "number": _ = text.TryParseAwkNumber(AwkNumberParsingMode.Decimal, out _, out _, checkpoint); break;
                case "parsed-value": _ = AwkValue.FromText(text, AwkNumberParsingMode.Decimal, checkpoint); break;
                case "hash": _ = text.GetHashCode(checkpoint); break;
                case "key": _ = new AwkArrayKey(text, checkpoint); break;
                case "compare": _ = text.CompareBytes(text, checkpoint); break;
                case "text-equality": _ = text.Equals(text, checkpoint); break;
                case "search": _ = runtime.IndexOfStringUnits(new(text), new(AwkText.FromSourceLiteralString("1"))); break;
                case "string": _ = text.AsString(checkpoint); break;
                case "memory": _ = text.AsMemory(checkpoint); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, counters[AwkCounter.ConcatenationBytesCopied]);
        Assert.Equal(new string('0', LongText) + "1", text.AsString());
    }

    [Theory]
    [InlineData("ascii")]
    [InlineData("bmp")]
    [InlineData("supplementary")]
    public void Utf8ValidationChecksAfterScalarBoundaryCrossings(string scenario)
    {
        var unit = scenario switch { "ascii" => "a", "bmp" => "\u754c", "supplementary" => "\ud83d\ude00", _ => throw new ArgumentOutOfRangeException(nameof(scenario)) };
        var expected = new string('a', 4_095) + string.Concat(Enumerable.Repeat(unit, LongText));
        var bytes = AwkUtf8Codec.Encode(expected).ToArray();
        using var cancellation = new CancellationTokenSource();

        Assert.Throws<OperationCanceledException>(() => AwkText.ThrowIfInvalidUtf8(bytes, CancelOnCheck(cancellation, 2)));

        using var memory = Governor().InitializeRuntime(AwkRuntimeCounters.Disabled);
        Assert.Equal(expected, memory.State.CopyHostText(bytes).AsString());
        bytes[^1] = 0xFF;
        var error = Assert.Throws<AwkRuntimeException>(() => memory.State.CopyHostText(bytes));
        Assert.Equal("awk: invalid UTF-8 input\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }

    [Theory]
    [InlineData("hash")]
    [InlineData("equality")]
    [InlineData("owned-key-equality")]
    [InlineData("borrowed-key-equality")]
    [InlineData("comparison")]
    [InlineData("ascii-equality")]
    public void OwnedByteScansObserveMidFlightCancellation(string operation)
    {
        // Leave ample scanning after entry/materialization checks; cancellation
        // must not depend on a final checkpoint after a short comparison.
        var expected = new string('a', LongText * 4);
        var text = AwkText.FromString(expected);
        var other = AwkText.FromString(expected);
        var key = new AwkArrayKey(text);
        var otherKey = new AwkArrayKey(other);
        using var cancellation = new CancellationTokenSource();
        var checkpoint = CancelOnCheck(cancellation, operation == "ascii-equality" ? 5 : 6);
        Assert.Throws<OperationCanceledException>(() =>
        {
            switch (operation)
            {
                case "hash": _ = text.GetHashCode(checkpoint); break;
                case "equality": _ = text.Equals(other, checkpoint); break;
                case "owned-key-equality": _ = key.Equals(otherKey, checkpoint); break;
                case "borrowed-key-equality": _ = key.Equals(other.AsSpan(), checkpoint); break;
                case "comparison": _ = text.CompareBytes(other, checkpoint); break;
                case "ascii-equality": _ = text.EqualsAscii(expected, checkpoint); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });
        Assert.True(text.Equals(other));
        Assert.Equal(text.GetHashCode(), other.GetHashCode());
        Assert.True(text.EqualsAscii(expected));
    }

    [Fact]
    public void CanceledDecodeDoesNotPublishAStringCache()
    {
        var expected = new string('\u754c', LongText);
        var text = AwkText.FromString(expected);
        var prepared = text.PrepareDecodedString(out var decoded, checkpoint: null);
        Assert.True(decoded);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var error = Assert.Throws<OperationCanceledException>(() =>
            text.PublishDecodedString(prepared, cancellation.Token.ThrowIfCancellationRequested));
        Assert.Equal(cancellation.Token, error.CancellationToken);

        var counters = AwkRuntimeCounters.CreateEnabled();
        using var runtime = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var retried = runtime.DecodeText(text);
        Assert.Equal(expected, retried);
        Assert.NotSame(prepared, retried);
        Assert.Equal(1, counters[AwkCounter.StringDecodes]);
        Assert.Same(retried, runtime.DecodeText(text));
        Assert.Equal(1, counters[AwkCounter.StringDecodes]);
    }

    [Fact]
    public void CanceledBuilderAppendDoesNotExposeAPartialSuffix()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20);
        var governor = memoryForGovernor.Accounting;
        using (var builder = new AwkUtf8Builder(governor))
        {
            builder.Append("kept"u8);
            using var cancellation = new CancellationTokenSource();
            var bytes = AwkUtf8Codec.Encode(new string('a', LongText)).ToArray();
            Assert.Throws<OperationCanceledException>(() => builder.Append(bytes.AsSpan(), CancelOnCheck(cancellation, 3)));
            Assert.Equal("kept"u8.ToArray(), builder.AsSpan().ToArray());
            builder.Append("!"u8);
            Assert.Equal("kept!", AwkUtf8Codec.DecodeRuntimeText(builder.CopyToOwnedBytes()));
        }
        Assert.Equal(0, governor.LiveBytes);
    }

    [Fact]
    public void OwnedConcatenationRetainsIngressThroughCanceledMaterializationAndRetry()
    {
        var bytes = AwkUtf8Codec.Encode(new string('a', LongText)).ToArray();
        var owned = AwkTestHarness.CopyHostText(bytes);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor().InitializeRuntime(counters);
        var governor = memoryForGovernor.Accounting;
        var committed = governor.LiveBytes;
        using var cancellation = new CancellationTokenSource();
        var completed = memoryForGovernor.State.ConcatText([owned, AwkText.FromString("z")]);
        Assert.Throws<OperationCanceledException>(() => completed.AsMemory(CancelOnCheck(cancellation, 3)));
        Assert.Equal(committed, governor.LiveBytes);
        bytes.AsSpan().Fill((byte)'b');
        Assert.Equal(new string('a', LongText) + "z", completed.AsString());
    }

    [Fact]
    public void RuntimeComparerCancelsBeforeDictionaryMutationWithoutAffectingTheSharedComparer()
    {
        using var cancellation = new CancellationTokenSource();
        var comparer = AwkArrayKeyComparer.ForRuntime(cancellation.Token.ThrowIfCancellationRequested);
        var key = new AwkArrayKey(new string('a', LongText));
        var dictionary = new Dictionary<AwkArrayKey, int>(comparer) { [key] = 7 };
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => dictionary[key] = 8);
        Assert.Single(dictionary);
        Assert.Equal(7, dictionary.Single().Value);
        var independent = new Dictionary<AwkArrayKey, int>(AwkArrayKeyComparer.Instance) { [key] = 9 };
        Assert.Equal(9, independent[key]);
    }

    [Fact]
    public void CanceledArrayReplacementReleasesItsMutationReservation()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        using var memory = Governor().InitializeRuntime(counters, cancellation.Token);
        var governor = memory.Accounting;
        var variables = memory.State.Variables;
        var name = AwkArrayName.FromText("kept");
        var key = new AwkArrayKey(new string('k', LongText));
        variables.SetArrayValue(name, key, AwkValue.FromNumber(7));
        Assert.True(variables.ResolveOrdinaryArray(name).TryGetDictionaryCell(key, out var cell));
        Assert.NotNull(cell);
        var retained = governor.LiveBytes;
        var pending = AwkValue.FromDeferredText(AwkText.FromString(new string('a', LongText)), AwkNumberParsingMode.Decimal);
        memory.Budget.OnGrowth = _ => cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => variables.SetArrayValue(name, key, pending));

        memory.Budget.OnGrowth = null;
        Assert.True(cell.IsAttached);
        Assert.Equal(7, cell.Slot.AsNumber());
        Assert.Equal(retained, governor.LiveBytes);
    }

    [Fact]
    public void CanceledMultiPartCopyPublishesNoResultAndAllowsRetry()
    {
        AwkText[] parts = [AwkText.FromString(new string('a', LongText)), AwkText.FromString("b"), AwkText.FromString("c")];
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        using (var memory = Governor().InitializeRuntime(counters, cancellation.Token))
        {
            var committed = memory.Accounting.LiveBytes;
            memory.Budget.OnGrowth = _ => cancellation.Cancel();
            var error = Assert.Throws<OperationCanceledException>(() => memory.State.ConcatText(parts));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(0, counters[AwkCounter.ConcatenationBytesCopied]);
            Assert.Equal(committed, memory.Accounting.LiveBytes);
        }

        using var retry = Governor().InitializeRuntime(counters);
        var completed = retry.State.ConcatText(parts);
        Assert.Equal(new string('a', LongText) + "bc", completed.AsString());
        Assert.Equal(completed.ByteLength, counters[AwkCounter.ConcatenationBytesCopied]);
    }

    [Fact]
    public void MaterializationMemoryFailureLeavesTheCacheRetryable()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor().InitializeRuntime(counters);
        memoryForGovernor.Budget.SetAvailableBytes(LongText + 1);
        var governor = memoryForGovernor.Accounting;
        var committed = governor.LiveBytes;
        var text = memoryForGovernor.State.ConcatText([AwkText.FromString(new string('a', LongText)), AwkText.FromString("z")]);
        using (var existing = governor.ReserveTransient(1))
        {
            var error = Assert.Throws<AwkRuntimeException>(() => { _ = text.AsSpan(() => { }); });
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(0, counters[AwkCounter.ConcatenationBytesCopied]);
            Assert.Equal(committed + 1, governor.LiveBytes);
        }
        Assert.Equal(new string('a', LongText) + "z", text.AsString());
        Assert.Equal(committed, governor.LiveBytes);
    }

    [Theory]
    [InlineData(-2147483648)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(2147483647)]
    public void CallbackAwareKeysPreserveOrdinalAndTextHashDomains(int ordinal)
    {
        var text = new AwkArrayKey(ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture), () => { });
        var compact = AwkArrayKey.FromOrdinal(ordinal);
        var comparer = AwkArrayKeyComparer.ForRuntime(() => { });
        Assert.True(comparer.Equals(text, compact));
        Assert.Equal(text.GetHashCode(), compact.GetHashCode());
        Assert.Equal(comparer.GetHashCode(text), comparer.GetHashCode(compact));
        var dictionary = new Dictionary<AwkArrayKey, int>(comparer) { [text] = 7 };
        Assert.Equal(7, dictionary[compact]);
    }

    [Theory]
    [InlineData("fields", true)]
    [InlineData("long-field", true)]
    [InlineData("long-field", false)]
    [InlineData("separator", true)]
    [InlineData("terminator", true)]
    [InlineData("empty", true)]
    public void CanceledRecordCopyKeepsThePublishedPrefixAndFundedCapacity(string mode, bool governed)
    {
        using var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        using var builder = new AwkUtf8Builder(memory.Accounting, capacity: 16);
        var shortChecks = 0;
        Append(LongText / 2, () => shortChecks++);
        builder.Clear();
        builder.Append("kept:"u8);
        using var cancellation = new CancellationTokenSource();
        var failure = Assert.Throws<OperationCanceledException>(() =>
            Append(LongText, CancelOnCheck(cancellation, shortChecks + 1)));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.True(builder.AsSpan().SequenceEqual("kept:"u8));
        if (governed) Assert.True(memory.Accounting.LiveBytes > 0);

        // A separate attempt can reuse the still-funded destination, including when
        // responsiveness was required for metadata that emitted no bytes at all.
        Append(LongText);
        var expected = mode switch
        {
            "fields" => string.Join('|', Enumerable.Repeat(new string('a', 16), LongText)) + "!",
            "long-field" => new string('x', LongText) + "|É!",
            "separator" => "a" + new string('|', LongText) + "b!",
            "terminator" => "a|b" + new string('!', LongText),
            _ => ""
        };
        Assert.Equal("kept:" + expected, AwkUtf8Codec.Decode(builder.AsSpan()));
        builder.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);

        void Append(int size, Action? checkpoint = null)
        {
            var record = AwkUtf8Codec.Encode(mode == "fields" ? new string('a', size * 16) :
                mode == "long-field" ? new string('x', size) : "ab");
            var fields = mode switch
            {
                "fields" => Enumerable.Range(0, size).Select(index => AwkFieldText.FromRecordSlice(index * 16, 16)).ToArray(),
                "long-field" => [AwkFieldText.FromRecordSlice(0, size), AwkFieldText.FromOwned(AwkText.FromSourceLiteralString("É"))],
                "empty" => new AwkFieldText[size],
                _ => [AwkFieldText.FromRecordSlice(0, 1), AwkFieldText.FromRecordSlice(1, 1)]
            };
            var separator = AwkText.FromSourceLiteralString(mode == "separator" ? new string('|', size) : mode == "empty" ? "" : "|");
            var terminator = AwkText.FromSourceLiteralString(mode == "terminator" ? new string('!', size) : mode == "empty" ? "" : "!");
            var byteLength = fields.Sum(field => field.ByteLength()) + (fields.Length - 1) * separator.ByteLength;
            builder.AppendRecord(new AwkRecordFields(fields), new AwkCopySource(record.Span), separator, terminator, byteLength, checkpoint);
        }
    }

    [Theory]
    [InlineData(100, 1)]
    [InlineData(256, 128)]
    public void RecordSliceCopyPreservesFragmentedFields(int count, int width)
    {
        using var builder = new AwkUtf8Builder(AwkMemoryAccounting.Disabled, capacity: 512);
        var value = new string('a', width);
        var record = AwkUtf8Codec.Encode(value).ToArray();
        var fields = Enumerable.Repeat(AwkFieldText.FromRecordSlice(0, width), count).ToArray();
        var byteLength = count * width + count - 1;
        builder.AppendRecord(new AwkRecordFields(fields), new AwkCopySource(record), AwkText.FromSourceLiteralString("|"),
            AwkText.FromSourceLiteralString("!"), byteLength, static () => { });
        Assert.Equal(string.Join('|', Enumerable.Repeat(value, count)) + "!", AwkUtf8Codec.Decode(builder.AsSpan()));

    }

    private static CooperativeTestMemory Governor() =>
        new CooperativeTestMemory(1 << 20);

    private static AwkText DeepText(int count, AwkRuntimeState state)
    {
        var leaf = AwkText.FromString("a");
        var text = leaf;
        for (var index = 1; index < count; index++) text = state.ConcatText([text, leaf]);
        return state.ConcatText([text, AwkText.FromString("z")]);
    }

    private static Action CancelOnCheck(CancellationTokenSource cancellation, int check)
    {
        var calls = 0;
        return () =>
        {
            if (++calls == check) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };
    }
}
