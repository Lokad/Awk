namespace Lokad.Awk.Tests;

public sealed partial class AwkFunctionStorageTests
{
    [Theory]
    [InlineData("1", AwkNumberParsingMode.Decimal, AwkNumberParsingMode.Decimal, 1)]
    [InlineData("010", AwkNumberParsingMode.Decimal, AwkNumberParsingMode.NonDecimalData, 8)]
    [InlineData("0x10", AwkNumberParsingMode.HexadecimalInput, AwkNumberParsingMode.HexadecimalInput, 16)]
    [InlineData("+007", AwkNumberParsingMode.Decimal, AwkNumberParsingMode.Decimal, 7)]
    [InlineData("1e2", AwkNumberParsingMode.Decimal, AwkNumberParsingMode.NonDecimalData, 100)]
    [InlineData("-0", AwkNumberParsingMode.Decimal, AwkNumberParsingMode.Decimal, 0)]
    internal void ArithmeticInputUsesItsCachedNumberInAlreadyFundedIncomingStorage(
        string spelling, AwkNumberParsingMode sourceMode, AwkNumberParsingMode frameMode, double expected)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var stack = Stack(memory, numberParsingMode: frameMode);
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("x")]);
        layout.PrepareInputs([AwkParameterInput.Arithmetic]);
        var value = AwkValue.FromNumericString(spelling, sourceMode);
        var normalizedBits = BitConverter.DoubleToInt64Bits(value.ForFunctionStorage(frameMode, null).AsNumber());
        using (memory.Accounting.ReserveTransient(value.RetainedTextMemoryBytes))
        {
            var frame = stack.PrepareFrame(layout);
            Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
            using (pressure)
            {
                stack.StageScalarArgument(frame, 0, in value);
                Assert.Equal(AwkValueKind.Number, frame.GetSlot(0).Value.StoredKind);
                Assert.Equal(expected, frame.GetSlot(0).Value.AsNumber());
                Assert.Equal(normalizedBits, BitConverter.DoubleToInt64Bits(frame.GetSlot(0).Value.StoredNumber));
                Assert.Null(frame.GetSlot(0).Value.StoredReference);
                Assert.Equal(0, frame.RetainedMemoryBytes);
                Assert.Equal(0, memory.Budget.Refusals);
                Assert.Equal(spelling, value.ByteText.AsString());
            }
            stack.DiscardIncomingFrame(frame);
        }
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void InputsThatKeepSpellingStillRequireAdmission(bool ordinaryString)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var stack = Stack(memory);
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("x")]);
        layout.PrepareInputs([ordinaryString ? AwkParameterInput.Arithmetic : AwkParameterInput.Value]);
        var value = ordinaryString ? AwkValue.FromText("0007") : AwkValue.FromNumericString("0007");
        using (memory.Accounting.ReserveTransient(value.RetainedTextMemoryBytes))
        {
            var frame = stack.PrepareFrame(layout);
            Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
            using (pressure)
            {
                memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
                Assert.Throws<AwkRuntimeException>(() => stack.StageScalarArgument(frame, 0, in value));
                Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, frame.GetSlot(0).Kind);
                Assert.Equal(0, frame.RetainedMemoryBytes);
                Assert.Equal(1, memory.Budget.Refusals);
                Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
                Assert.Equal("0007", value.ByteText.AsString());
            }
            stack.DiscardIncomingFrame(frame);
        }
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }
}
