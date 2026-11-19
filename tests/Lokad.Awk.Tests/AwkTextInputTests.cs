using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

public sealed class AwkTextInputTests
{
    [Fact]
    public void TextCapturesKeepTheCanonicalThreeWordWidth()
    {
        Assert.Equal(24, Unsafe.SizeOf<AwkTextInput>());
        Assert.Equal(24, Unsafe.SizeOf<AwkByteTextOperand>());
        Assert.Equal(48, Unsafe.SizeOf<AwkScalarOperand>());
    }

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
            "numeric-string" => AwkTestHarness.NumericInput("0007"),
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
        using var context = new AwkFieldTestContext(counters);
        context.Frame.SetRecordText(AwkText.FromSourceLiteralString("0007 tail"));
        Assert.True(context.Frame.TryCaptureTextInput(new(1), out var captured));
        context.Frame.SetRecordText(AwkText.FromSourceLiteralString("changed other"));
        Assert.Equal("0007", AwkUtf8Codec.Decode(captured.ReadyText.AsSpan(null)));
        Assert.Equal(1, captured.TextField);
        Assert.Equal(0, counters[AwkCounter.TextAllocations]);

    }


    [Fact]
    public void RecordCaptureKeepsItsRangeAfterTheSourceCellIsReplaced()
    {
        using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var record = state.InputRecordStorage;
        record.StageValidated("_0007_"u8);
        var cell = AwkFunctionLocalSlot.MissingScalar();
        cell.InitializeRecordText(record, record.CurrentSlot, 1, 4);
        Assert.True(cell.TryGetTextInput(out var captured, null));
        cell = AwkFunctionLocalSlot.Scalar(AwkValue.FromText("changed"));
        Assert.Equal("0007", AwkUtf8Codec.Decode(captured.ReadyText.AsSpan(null)));
        Assert.Equal(-1, captured.TextField);
        Assert.Equal("changed", cell.Value.AsString());
    }
}
