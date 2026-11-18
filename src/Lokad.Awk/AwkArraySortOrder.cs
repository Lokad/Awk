namespace Lokad.Awk;

internal readonly struct AwkArraySortOrder
{
    private readonly string? _unknownSelector;

    private AwkArraySortOrder(AwkArraySortMode mode, AwkFunctionName functionName = default,
        string? unknownSelector = null)
    {
        Mode = mode;
        FunctionName = functionName;
        _unknownSelector = unknownSelector;
    }

    public static AwkArraySortOrder DefaultIndexOrder => new(AwkArraySortMode.IndexStringAscending);
    public AwkArraySortMode Mode { get; }
    public AwkFunctionName FunctionName { get; }
    public string UnknownSelector => _unknownSelector ??
        throw new InvalidOperationException("No unknown sort selector is present.");

    public static AwkArraySortOrder Parse(AwkText text, AwkRuntimeState state)
    {
        state.ThrowIfCancellationRequested();
        if (text.ByteLength == 0) return default;
        var bytes = text.AsSpan(state.CancellationCheckpoint);
        if (bytes.SequenceEqual("@ind_str_asc"u8)) return new(AwkArraySortMode.IndexStringAscending);
        if (bytes.SequenceEqual("@ind_str_desc"u8)) return new(AwkArraySortMode.IndexStringDescending);
        if (bytes.SequenceEqual("@ind_num_asc"u8)) return new(AwkArraySortMode.IndexNumberAscending);
        if (bytes.SequenceEqual("@ind_num_desc"u8)) return new(AwkArraySortMode.IndexNumberDescending);
        if (bytes.SequenceEqual("@val_str_asc"u8)) return new(AwkArraySortMode.ValueStringAscending);
        if (bytes.SequenceEqual("@val_str_desc"u8)) return new(AwkArraySortMode.ValueStringDescending);
        if (bytes.SequenceEqual("@val_num_asc"u8)) return new(AwkArraySortMode.ValueNumberAscending);
        if (bytes.SequenceEqual("@val_num_desc"u8)) return new(AwkArraySortMode.ValueNumberDescending);
        if (bytes.SequenceEqual("@val_type_asc"u8)) return new(AwkArraySortMode.ValueTypeAscending);
        if (bytes.SequenceEqual("@val_type_desc"u8)) return new(AwkArraySortMode.ValueTypeDescending);
        if (bytes.SequenceEqual("@unsorted"u8)) return new(AwkArraySortMode.Unsorted);

        var selector = state.DecodeText(text);
        return AwkIdentifier.TryParse(selector, out var identifier)
            ? new(AwkArraySortMode.UserFunction, new AwkFunctionName(identifier))
            : new(AwkArraySortMode.Unknown, unknownSelector: selector);
    }
}

internal enum AwkArraySortMode
{
    Default,
    IndexStringAscending,
    IndexStringDescending,
    IndexNumberAscending,
    IndexNumberDescending,
    ValueStringAscending,
    ValueStringDescending,
    ValueNumberAscending,
    ValueNumberDescending,
    ValueTypeAscending,
    ValueTypeDescending,
    Unsorted,
    UserFunction,
    Unknown
}
