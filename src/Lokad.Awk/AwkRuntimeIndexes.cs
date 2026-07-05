using System;

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

    public override string ToString() => Value.ToString();
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

    public override string ToString() => Value.ToString();
}

internal readonly record struct AwkRecordNumber
{
    public AwkRecordNumber(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Record numbers must be zero or positive.");

        Value = value;
    }

    public int Value { get; }

    public AwkRecordNumber Next() => new(Value + 1);

    public override string ToString() => Value.ToString();
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

    public override string ToString() => Value.ToString();
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

    public override string ToString() => Value.ToString();
}

internal readonly record struct AwkInputPlanIndex
{
    private readonly int _value;

    private AwkInputPlanIndex(int value)
    {
        _value = value;
    }

    public static AwkInputPlanIndex None { get; } = new(-1);

    public static AwkInputPlanIndex FromZeroBased(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Input plan indexes must be zero or positive.");

        return new AwkInputPlanIndex(value);
    }

    public bool HasValue => _value >= 0;

    public int Value =>
        HasValue ? _value : throw new InvalidOperationException("The input plan index has no value.");

    public override string ToString() => HasValue ? _value.ToString() : "<none>";
}
