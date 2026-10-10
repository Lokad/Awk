using System.Numerics;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{

    private static ulong ToBitOperand(AwkBuiltinContext context, int argumentIndex)
    {
        var value = context.GetNumber(argumentIndex);
        if (value < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeBitOperand);

        return ToUnsignedBits(value);
    }

    // Define the reference's otherwise platform-dependent out-of-range conversion:
    // nonnegative finite numbers outside uint64 and positive infinity become zero;
    // NaN becomes the unsigned high bit, as on the pinned GNU x86-64 reference.
    private static ulong ToUnsignedBits(double value) =>
        double.IsNaN(value) ? 1UL << 63 :
        value < 18_446_744_073_709_551_616d && value >= 0 ? (ulong)value : 0;

    private static AwkValue FromBitResult(ulong value)
    {
        // Preserve low bits exactly. Each trailing zero permits one more high bit in
        // binary64's 53-bit significand, up to the full unsigned 64-bit width.
        var shift = Math.Min(BitOperations.TrailingZeroCount(value), 11);
        const ulong significandMask = (1UL << 53) - 1;
        return AwkValue.FromNumber(value & (significandMask << shift));
    }

    private static AwkValue EvaluateBitShift(AwkBuiltinContext context, bool left)
    {
        var value = context.GetNumber(0);
        var shift = context.GetNumber(1);
        if (value < 0 || shift < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeBitOperand);

        // Test the original number before truncation; CLR shifts mask their count.
        // GNU 5.4 also returns zero for NaN instead of shifting by a cast sentinel.
        if (shift >= 64 || double.IsNaN(shift))
            return AwkValue.FromNumber(0);

        var operand = ToUnsignedBits(value);
        return FromBitResult(left ? operand << (int)shift : operand >> (int)shift);
    }
}
