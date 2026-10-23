using System;
using System.Globalization;

namespace Lokad.Awk;

internal readonly record struct AwkFieldIndex
{
    public AwkFieldIndex(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Field indexes must be zero or positive.");

        Value = value;
    }

    public static AwkFieldIndex FromNumber(double number)
    {
        if (!double.IsFinite(number))
            throw new AwkRuntimeException(AwkMessages.ErrorNonfiniteFieldReference);
        if (number <= -1)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);

        // Every finite positive out-of-range read is an absent field. Growth still
        // preflights representable storage; saturation never aliases an existing field.
        return new(number >= int.MaxValue ? int.MaxValue : (int)number);
    }

    public int Value { get; }

    public bool IsCurrentRecord => Value == 0;

    public bool IsNamedField => Value > 0;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

internal readonly record struct AwkFieldCount
{
    public AwkFieldCount(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Field counts must be zero or positive.");

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

internal readonly record struct AwkArgvIndex
{
    public AwkArgvIndex(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "ARGV indexes must be zero or positive.");

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

internal readonly record struct AwkGlobalSlotId
{
    public AwkGlobalSlotId(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Global slot indexes must be zero or positive.");

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
