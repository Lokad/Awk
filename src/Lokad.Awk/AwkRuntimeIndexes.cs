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
