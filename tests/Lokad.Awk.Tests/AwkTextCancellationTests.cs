namespace Lokad.Awk.Tests;

public sealed class AwkTextCancellationTests
{
    private const int LongText = 16_384;

    public static IEnumerable<object[]> Utf8IngressInvalidEdges()
    {
        foreach (var prefix in new[] { 0, 4095, 4096, 4097, 8191 })
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
        foreach (var ingress in new[] { "host", "owned", "copy" })
        {
            var counters = AwkRuntimeCounters.CreateEnabled();
            var governor = Governor(counters);
            var error = Assert.Throws<AwkRuntimeException>(() =>
            {
                _ = ingress switch
                {
                    "host" => AwkText.FromHostBytes(bytes, counters),
                    "owned" => AwkText.FromOwnedBytes(bytes, counters),
                    _ => AwkText.CopyHostBytes(bytes, counters, governor)
                };
            });
            Assert.Equal("awk: invalid UTF-8 input\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(0, counters.AwkTextAllocations);
            Assert.Equal(0, counters.AwkTextPayloadBytes);
            Assert.Equal(0, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }
    }

    [Theory]
    [InlineData(4095, "FF", false)]
    [InlineData(4096, "FF", true)]
    [InlineData(4097, "FF", true)]
    [InlineData(4095, "E282", false)]
    [InlineData(4095, "E2FFAC", false)]
    [InlineData(4095, "F09F9880FF", true)]
    [InlineData(4096, "E282", true)]
    public void Utf8IngressKeepsCancellationAndInvalidByteFailureOrder(int prefix, string suffix, bool cancellationWins)
    {
        var tail = Convert.FromHexString(suffix);
        var bytes = new byte[prefix + tail.Length];
        bytes.AsSpan(0, prefix).Fill((byte)'a');
        tail.CopyTo(bytes.AsSpan(prefix));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        void Check()
        {
            if (++checks == 2) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        if (cancellationWins)
        {
            var error = Assert.Throws<OperationCanceledException>(() => AwkText.CopyHostBytes(bytes, counters, governor, Check));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(2, checks);
        }
        else
        {
            var error = Assert.Throws<AwkRuntimeException>(() => AwkText.CopyHostBytes(bytes, counters, governor, Check));
            Assert.Equal("awk: invalid UTF-8 input\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(1, checks);
        }
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.AwkTextPayloadBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(4095, false)]
    [InlineData(4096, false)]
    [InlineData(8193, false)]
    [InlineData(4095, true)]
    public void Utf8IngressKeepsOwnedCopyAndByteNativeCounters(int prefix, bool supplementary)
    {
        var input = AwkUtf8Codec.Encode(new string('a', prefix) + (supplementary ? "😀" : "")).ToArray();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        var text = AwkText.CopyHostBytes(input, counters, governor);
        Assert.True(text.AsSpan().SequenceEqual(input));
        Assert.Equal(input.Length == 0 ? 0 : 1, counters.AwkTextAllocations);
        Assert.Equal(input.Length, counters.AwkTextPayloadBytes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("ascii")]
    [InlineData("bmp")]
    [InlineData("supplementary")]
    [InlineData("deep")]
    public void InterruptedMaterializationReleasesMemoryAndRetainsCompleteParts(string scenario)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        var expected = scenario switch
        {
            "ascii" => new string('a', LongText) + "z",
            "bmp" => new string('\u754c', LongText) + "z",
            "supplementary" => string.Concat(Enumerable.Repeat("\ud83d\ude00", LongText)) + "z",
            "deep" => new string('a', 3_000) + "z",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var text = scenario == "deep" ? DeepText(3_000, counters, governor)
            : AwkText.FromString(expected[..^1]).Concat(AwkText.FromString("z"), counters, governor);
        using var cancellation = new CancellationTokenSource();
        var copyingChecks = 0;
        Action checkpoint = () =>
        {
            if (governor.CurrentCommittedBytes > 0 && ++copyingChecks == 4)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };

        var error = Assert.Throws<OperationCanceledException>(() => { _ = text.AsSpan(checkpoint); });

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(4, copyingChecks);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, counters.ConcatenationBytesCopied);
        Assert.Equal(expected, text.AsString());
        Assert.Equal(text.ByteLength, counters.ConcatenationBytesCopied);
        Assert.Equal(expected, text.AsString());
        Assert.Equal(text.ByteLength, counters.ConcatenationBytesCopied);
    }

    [Theory]
    [InlineData("runes")]
    [InlineData("ascii")]
    [InlineData("number")]
    [InlineData("parsed-value")]
    [InlineData("hash")]
    [InlineData("key")]
    [InlineData("compare")]
    [InlineData("search")]
    [InlineData("string")]
    [InlineData("memory")]
    public void CallersCheckCancellationBeforeHiddenMaterialization(string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var text = AwkText.FromString(new string('0', LongText))
            .Concat(AwkText.FromString("1"), counters, AwkMemoryAccounting.Disabled);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Action checkpoint = cancellation.Token.ThrowIfCancellationRequested;

        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            switch (operation)
            {
                case "runes": _ = text.GetRuneLength(checkpoint); break;
                case "ascii": _ = text.ContainsNonAscii(counters, checkpoint); break;
                case "number": _ = text.TryParseNumber(AwkNumberParsingMode.Decimal, AwkNumberTextExtent.EntireText, out _, checkpoint); break;
                case "parsed-value": _ = AwkValue.FromText(text, AwkNumberParsingMode.Decimal, checkpoint); break;
                case "hash": _ = text.GetHashCode(checkpoint); break;
                case "key": _ = new AwkArrayKey(text, checkpoint); break;
                case "compare": _ = text.CompareBytes(text, checkpoint); break;
                case "search": _ = text.IndexOf(AwkText.FromString("1"), checkpoint); break;
                case "string": _ = text.AsString(counters, checkpoint); break;
                case "memory": _ = text.AsMemory(checkpoint); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, counters.ConcatenationBytesCopied);
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
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();

        Assert.Throws<OperationCanceledException>(() => AwkText.CopyHostBytes(bytes, counters, governor, CancelOnCheck(cancellation, 2)));

        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(expected, AwkText.CopyHostBytes(bytes, counters, governor).AsString());
        bytes[^1] = 0xFF;
        var error = Assert.Throws<AwkRuntimeException>(() => AwkText.CopyHostBytes(bytes, counters, governor));
        Assert.Equal("awk: invalid UTF-8 input\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }

    [Theory]
    [InlineData("hash")]
    [InlineData("equality")]
    [InlineData("comparison")]
    [InlineData("ascii-equality")]
    public void OwnedByteScansObserveMidFlightCancellation(string operation)
    {
        var expected = new string('a', LongText);
        var text = AwkText.FromString(expected);
        var other = AwkText.FromString(expected);
        using var cancellation = new CancellationTokenSource();
        var checkpoint = CancelOnCheck(cancellation, operation == "ascii-equality" ? 5 : 6);
        Assert.Throws<OperationCanceledException>(() =>
        {
            switch (operation)
            {
                case "hash": _ = text.GetHashCode(checkpoint); break;
                case "equality": _ = text.Equals(other, checkpoint); break;
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
        var text = AwkText.FromString(new string('\u754c', LongText));
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        Action checkpoint = () =>
        {
            if (counters.StringDecodes > 0) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };
        Assert.Throws<OperationCanceledException>(() => text.AsString(counters, checkpoint));
        Assert.Equal(1, counters.StringDecodes);
        Assert.Equal(new string('\u754c', LongText), text.AsString(counters));
        Assert.Equal(2, counters.StringDecodes);
        Assert.Equal(new string('\u754c', LongText), text.AsString(counters));
        Assert.Equal(2, counters.StringDecodes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanceledFormatDecodePreservesThePreviousCacheAndAllowsRetry(bool output)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var current = new AwkFormatString(AwkText.FromString("%.1f"));
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        Action checkpoint = () =>
        {
            if (armed && counters.StringDecodes == 2) cancellation.Cancel();
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        };
        var formatter = new AwkFormattingRuntime(_ => current, limits, counters, AwkMemoryAccounting.Disabled, checkpoint: checkpoint);
        AwkText Format() => output ? formatter.FormatPrintByteText(AwkValue.FromNumber(1.25)) : formatter.FormatStringByteText(AwkValue.FromNumber(1.25));
        Assert.Equal("1.2", Format().AsString());
        current = new AwkFormatString(AwkText.FromString("%.3f"));
        armed = true;

        Assert.Throws<OperationCanceledException>(() => Format());

        armed = false;
        Assert.Equal("1.250", Format().AsString());
        Assert.Equal(3, counters.StringDecodes);
        Assert.Equal("1.250", Format().AsString());
        Assert.Equal(3, counters.StringDecodes);
    }

    [Fact]
    public void CanceledBuilderAppendDoesNotExposeAPartialSuffix()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        var governor = AwkMemoryAccounting.Create(limits, counters);
        using (var builder = new AwkUtf8Builder(limits, governor))
        {
            builder.Append("kept"u8);
            using var cancellation = new CancellationTokenSource();
            var bytes = AwkUtf8Codec.Encode(new string('a', LongText)).ToArray();
            Assert.Throws<OperationCanceledException>(() => builder.Append(bytes.AsSpan(), CancelOnCheck(cancellation, 3)));
            Assert.Equal("kept"u8.ToArray(), builder.AsSpan().ToArray());
            builder.Append("!"u8);
            Assert.Equal("kept!", builder.CopyToAwkText(counters).AsString());
        }
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Fact]
    public void BorrowedConcatenationPartsRemainDetachedAfterCancellationAndRetry()
    {
        var bytes = AwkUtf8Codec.Encode(new string('a', LongText)).ToArray();
        var borrowed = AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() => borrowed.Concat(AwkText.FromString("z"), counters, governor, CancelOnCheck(cancellation, 3)));
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        var completed = borrowed.Concat(AwkText.FromString("z"), counters, governor);
        bytes.AsSpan().Fill((byte)'b');
        Assert.Equal(new string('a', LongText) + "z", completed.AsString());
    }

    [Fact]
    public async Task WaitingForAnotherMaterializerObservesCancellation()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        var text = AwkText.FromString(new string('a', LongText)).Concat(AwkText.FromString("z"), counters, governor);
        using var copying = new ManualResetEventSlim();
        using var resume = new ManualResetEventSlim();
        // Both participants must run independently of the test runner's constrained
        // worker pool while the owner deliberately blocks at the copy checkpoint.
        var owner = Task.Factory.StartNew(() => text.AsMemory(() =>
        {
            if (governor.CurrentCommittedBytes > 0 && !copying.IsSet)
            {
                copying.Set();
                if (!resume.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Materializer watchdog elapsed.");
            }
        }).ToArray(), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            Assert.True(copying.Wait(TimeSpan.FromSeconds(5)));
            using var cancellation = new CancellationTokenSource();
            var waiter = Task.Factory.StartNew(
                () => text.AsMemory(CancelOnCheck(cancellation, 2)).ToArray(),
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            var error = await Assert.ThrowsAsync<OperationCanceledException>(async () => await waiter.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.False(owner.IsCompleted);
        }
        finally
        {
            resume.Set();
            await owner.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.Equal(new string('a', LongText) + "z", text.AsString());
        Assert.Equal(text.ByteLength, counters.ConcatenationBytesCopied);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
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
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        Action checkpoint = () =>
        {
            if (!armed) return;
            if (governor.CurrentReservedBytes > 0) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };
        var variables = new AwkVariables(100, 100, 1_024, governor, counters,
            AwkValue.FromText, AwkValue.FromNumericString,
            text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal),
            (_, _) => { }, _ => { }, checkpoint: checkpoint);
        var name = AwkArrayName.FromText("kept");
        var key = new AwkArrayKey(new string('k', LongText));
        variables.SetArrayValue(name, key, AwkValue.FromNumber(7));
        var retained = governor.CurrentCommittedBytes;
        var pending = AwkValue.FromDeferredText(AwkText.FromString(new string('a', LongText)), AwkNumberParsingMode.Decimal);
        armed = true;

        Assert.Throws<OperationCanceledException>(() => variables.SetArrayValue(name, key, pending));

        armed = false;
        Assert.Equal(7, variables.GetArrayValue(name, key).AsNumber());
        Assert.Equal(retained, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Fact]
    public void CanceledMultiPartCopyPublishesNoResultAndAllowsRetry()
    {
        AwkText[] parts = [AwkText.FromString(new string('a', LongText)), AwkText.FromString("b"), AwkText.FromString("c")];
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() => AwkText.Concat(parts.AsSpan(), counters, governor, CancelOnCheck(cancellation, 5)));
        Assert.Equal(0, counters.ConcatenationBytesCopied);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        var completed = AwkText.Concat(parts.AsSpan(), counters, governor);
        Assert.Equal(new string('a', LongText) + "bc", completed.AsString());
        Assert.Equal(completed.ByteLength, counters.ConcatenationBytesCopied);
    }

    [Fact]
    public void MaterializationMemoryFailureLeavesTheCacheRetryable()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(LongText + 1), counters);
        var text = AwkText.FromString(new string('a', LongText)).Concat(AwkText.FromString("z"), counters, governor);
        using (var existing = governor.ReserveTransient(1))
        {
            var error = Assert.Throws<AwkRuntimeException>(() => { _ = text.AsSpan(() => { }); });
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(0, counters.ConcatenationBytesCopied);
            Assert.Equal(0, governor.CurrentReservedBytes);
            Assert.Equal(1, governor.CurrentCommittedBytes);
        }
        Assert.Equal(new string('a', LongText) + "z", text.AsString());
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
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
    [InlineData(false)]
    [InlineData(true)]
    public void TextCheckpointsDoNotDuplicateExplicitFuelOrStringInterop(bool observed)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var text = AwkText.FromString(new string('a', LongText)).Concat(AwkText.FromString("z"), counters, AwkMemoryAccounting.Disabled);
        var chunks = AwkExecutionGovernor.ByteWorkSteps(text.ByteLength);
        var resources = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(chunks), counters);
        resources.RecordByteWork(text.ByteLength);
        Action checkpoint = () => resources.ThrowIfCancellationRequested(CancellationToken.None);
        Assert.Equal(text.ByteLength, text.GetRuneLength(checkpoint));
        var key = new AwkArrayKey(text, checkpoint);
        var comparer = AwkArrayKeyComparer.ForRuntime(checkpoint);
        Assert.True(comparer.Equals(key, key));
        Assert.Equal(observed ? chunks : 0, counters.ExecutionSteps);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        if (observed) Assert.True(counters.CancellationChecks > 4);
    }

    private static AwkMemoryAccounting Governor(AwkRuntimeCounters counters) =>
        AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20), counters);

    private static AwkText DeepText(int count, AwkRuntimeCounters counters, AwkMemoryAccounting governor)
    {
        var leaf = AwkText.FromString("a");
        var text = leaf;
        for (var index = 1; index < count; index++) text = text.Concat(leaf, counters, governor);
        return text.Concat(AwkText.FromString("z"), counters, governor);
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
