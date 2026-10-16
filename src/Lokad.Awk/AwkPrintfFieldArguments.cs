namespace Lokad.Awk;

// These arguments borrow the current fields only during synchronous formatting.
// The caller has evaluated every index and preflighted every field before constructing this view.
internal readonly struct AwkPrintfFieldArguments(int[] fields, AwkRuntimeState state) : IPrintfArguments
{
    public int Count => fields.Length;

    public AwkText GetByteText(int index) => state.FormatStringByteText(state.GetFieldValue(fields[index]));
    public double GetNumber(int index) => state.GetFieldNumber(fields[index]);
    public bool IsNumeric(int index)
    {
        var value = state.GetFieldValue(fields[index]);
        return value.IsNumberValue || value.IsNumericStringValue;
    }

    public bool TryGetBorrowedBytes(int index, out ReadOnlySpan<byte> bytes) =>
        state.TryGetPrintfFieldBytes(fields[index], out bytes);

}
