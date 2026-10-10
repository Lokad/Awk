namespace Lokad.Awk;

// Untouched elements and arguments share only their initial typing decision.
// Assignment still replaces a scalar value; it does not write through this cell.
internal sealed class AwkUntypedScalar
{
    public bool IsScalar { get; private set; }

    public void Promote() => IsScalar = true;
}
