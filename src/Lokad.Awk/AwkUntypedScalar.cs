namespace Lokad.Awk;

// Untouched elements and arguments share their initial typing decision.
// Assignment still replaces a scalar value; it does not write through this cell.
internal sealed class AwkUntypedScalar(AwkArrayIdentity? parent = null)
{
    private static readonly object Scalar = new();
    private static readonly object Detached = new();
    private object? _state;

    public AwkArrayIdentity? Parent { get; private set; } = parent;

    public bool IsScalar => ReferenceEquals(_state, Scalar);

    public bool IsDetached => ReferenceEquals(_state, Detached);

    public AwkArrayIdentity? Array => _state as AwkArrayIdentity;

    public void Promote()
    {
        if (Array is not null)
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar("array element"));
        _state = Scalar;
    }

    // The containing array admits the owner before publishing this typing decision.
    public void PromoteArray(AwkArrayIdentity array)
    {
        if (IsScalar)
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray("array element"));
        _state = array;
    }

    public void Detach()
    {
        Parent = null;
        if (!IsScalar) _state = Detached;
    }
}
