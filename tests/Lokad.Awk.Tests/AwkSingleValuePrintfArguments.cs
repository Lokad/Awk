namespace Lokad.Awk.Tests;

internal readonly struct AwkSingleValuePrintfArguments(
    AwkValue value,
    Action? checkpoint = null) : IPrintfArguments
{
    public int Count => 1;

    public bool TryGetBorrowedBytes(int index, out ReadOnlySpan<byte> bytes) { bytes = default; return false; }

    public AwkText GetByteText(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);

        // Raw fixture arguments use round-trip spelling; execution format policy stays at its owner.
        return value.IsNumberValue
            ? AwkText.FromString(value.AsString(), checkpoint)
            : value.ByteText;
    }

    public double GetNumber(int index) =>
        index == 0 ? value.AsNumber(checkpoint) : throw new ArgumentOutOfRangeException(nameof(index));

    public bool IsNumeric(int index) =>
        index == 0
            ? value.IsNumberValue || value.IsNumericStringValue
            : throw new ArgumentOutOfRangeException(nameof(index));
}

