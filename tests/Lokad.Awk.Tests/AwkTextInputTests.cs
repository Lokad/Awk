using System.Buffers;
using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

public sealed class AwkTextInputTests
{
    [Fact]
    public void TextCaptureKeepsTheCanonicalThreeWordWidth() =>
        Assert.Equal(24, Unsafe.SizeOf<AwkTextInput>());

    [Theory]
    [InlineData("number", "1.3")]
    [InlineData("numeric-string", "0007")]
    [InlineData("string", "0007")]
    public void ScalarCaptureKeepsSpellingAndFormatsNumbersAtUse(string kind, string expected)
    {
        using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var value = kind switch
        {
            "number" => AwkValue.FromNumber(1.26),
            "numeric-string" => AwkValue.FromNumericString("0007"),
            _ => AwkValue.FromText("0007")
        };
        var captured = new AwkTextInput(in value);
        value = AwkValue.FromNumber(9);
        state.AssignVariable(AwkVariableName.FromText("CONVFMT"), "%.1f");
        Assert.Equal(expected, AwkUtf8Codec.Decode(captured.Text(state).AsSpan(null)));
        Assert.Equal(9, value.AsNumber());
    }

    [Fact]
    public void FieldCaptureRetainsTheActualRecordBackingAcrossRebinding()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var context = new AwkFieldTestContext(counters, AwkMemoryAccounting.Disabled);
        context.Frame.SetRecordText(AwkText.FromSourceLiteralString("0007 tail"));
        Assert.True(context.Frame.TryCaptureTextInput(new(1), out var captured));
        context.Frame.SetRecordText(AwkText.FromSourceLiteralString("changed other"));
        Assert.Equal("0007", AwkUtf8Codec.Decode(captured.ReadyText.AsSpan(null)));
        Assert.Equal(1, captured.TextField);
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.TextValueClassifications);
    }

    [Fact]
    public void FieldCapturePrecedesItsOwnedSourceCallback()
    {
        var armed = false;
        AwkFieldFrame? frame = null;
        using var context = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled,
            checkpoint: () =>
            {
                if (!armed) return;
                armed = false;
                Assert.IsType<AwkFieldFrame>(frame).SetRecordText(AwkText.FromSourceLiteralString("changed next"));
            });
        frame = context.Frame;
        var original = "old" + new string('x', 4096);
        frame.SetRecordText(AwkText.FromSourceLiteralString(original + " tail"));
        Assert.Equal(2, frame.GetFieldCount());
        armed = true;
        Assert.True(frame.TryCaptureTextInput(new(1), out var captured));
        Assert.False(armed);
        Assert.Equal(original, AwkUtf8Codec.Decode(captured.ReadyText.AsSpan(null)));
        Assert.Equal("changed", frame.GetFieldValue(new(1)).AsString());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void MemoryCaptureRetainsItsActualOwnerAndRange(bool managerBacked)
    {
        var original = "_0007_"u8.ToArray();
        using var manager = new TestMemory(original);
        ReadOnlyMemory<byte> memory = managerBacked ? manager.Memory : original;
        var captured = AwkTextInput.FromMemory(memory.Slice(1, 4), 3);
        memory = "changed"u8.ToArray();
        Assert.Equal("0007", AwkUtf8Codec.Decode(captured.ReadyText.AsSpan(null)));
        Assert.Equal(3, captured.TextField);
        Assert.Equal("changed", AwkUtf8Codec.Decode(memory.Span));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReaderCaptureKeepsItsRangeAfterTheSourceCellIsReplaced(bool managerBacked)
    {
        var bytes = "_0007_"u8.ToArray();
        using var manager = new TestMemory(bytes);
        ReadOnlyMemory<byte> memory = managerBacked ? manager.Memory : bytes;
        var retired = false;
        using var reader = AwkReadBuffer.FromContent(memory, AwkReadCompletion.EndOfFile, _ => retired = true);
        var cell = AwkFunctionLocalSlot.MissingScalar();
        cell.InitializeReaderText(reader, 1, 4);
        Assert.True(cell.TryGetTextInput(out var captured, null));
        cell = AwkFunctionLocalSlot.Scalar(AwkValue.FromText("changed"));
        Assert.False(retired);
        Assert.Equal("0007", AwkUtf8Codec.Decode(captured.ReadyText.AsSpan(null)));
        Assert.Equal(-1, captured.TextField);
        Assert.Equal("changed", cell.Value.AsString());
    }

    private sealed class TestMemory(byte[] bytes) : MemoryManager<byte>
    {
        private bool _disposed;
        public override Span<byte> GetSpan()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return bytes;
        }
        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();
        public override void Unpin() { }
        protected override void Dispose(bool disposing) => _disposed = true;
    }
}
