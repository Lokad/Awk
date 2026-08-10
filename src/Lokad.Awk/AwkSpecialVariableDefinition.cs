namespace Lokad.Awk;

internal abstract class AwkSpecialVariableDefinition(
    AwkSpecialVariableId id,
    string name)
{
    public AwkSpecialVariableId Id { get; } = id;

    public string Name { get; } = name;

    protected AwkIdentifier Identifier { get; } = AwkIdentifier.FromTrustedName(name);

    /// <summary>Gets whether this special variable occupies scalar or array storage.</summary>
    public abstract AwkSpecialVariableKind Kind { get; }
}

internal sealed class AwkSpecialScalarVariableDefinition(
    AwkSpecialVariableId id,
    string name,
    AwkSpecialScalarInitialValue initialValue) : AwkSpecialVariableDefinition(id, name)
{
    public override AwkSpecialVariableKind Kind => AwkSpecialVariableKind.Scalar;

    public AwkVariableName VariableName => new(Identifier);

    public AwkSpecialScalarInitialValue InitialValue { get; } = initialValue;
}

internal sealed class AwkSpecialArrayVariableDefinition(
    AwkSpecialVariableId id,
    string name,
    AwkSpecialArrayMutationPolicy mutationPolicy) : AwkSpecialVariableDefinition(id, name)
{
    public override AwkSpecialVariableKind Kind => AwkSpecialVariableKind.Array;

    public AwkArrayName ArrayName => new(Identifier);

    public AwkSpecialArrayMutationPolicy MutationPolicy { get; } = mutationPolicy;

    public bool CanModifyArrayElements => MutationPolicy != AwkSpecialArrayMutationPolicy.ReadOnly;

    public bool CanReplaceArray => MutationPolicy == AwkSpecialArrayMutationPolicy.Mutable;
}

internal readonly struct AwkSpecialScalarInitialValue
{
    private readonly AwkValue _value;

    private AwkSpecialScalarInitialValue(AwkValue value)
    {
        _value = value;
        HasValue = true;
    }

    public bool HasValue { get; }

    public AwkValue Value =>
        HasValue ? _value : throw new System.InvalidOperationException("The special scalar has no initial value.");

    public static AwkSpecialScalarInitialValue None => default;

    public static AwkSpecialScalarInitialValue Some(AwkValue value) => new(value);
}

internal enum AwkSpecialVariableKind
{
    Scalar,
    Array
}

internal enum AwkSpecialArrayMutationPolicy
{
    Mutable,
    ReadOnly,
    NonReplaceable
}

internal enum AwkSpecialVariableId
{
    ArgumentCount,
    ArgumentIndex,
    BinMode,
    ConversionFormat,
    Errno,
    FieldWidths,
    FileName,
    FileRecordNumber,
    FieldPattern,
    FieldSeparator,
    IgnoreCase,
    Lint,
    FieldCount,
    RecordNumber,
    OutputFormat,
    OutputFieldSeparator,
    OutputRecordSeparator,
    Precision,
    RegexLength,
    RoundingMode,
    RecordSeparator,
    RegexStart,
    RecordTerminator,
    SubscriptSeparator,
    TextDomain,
    ArgumentVector,
    Environment,
    FunctionTable,
    ProcInfo,
    SymbolTable
}
