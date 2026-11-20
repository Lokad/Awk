namespace Lokad.Awk;

internal static class AwkSpecialArrayPolicy
{
    public static bool IsElementReadOnly(string name,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault) =>
        AwkSpecialVariables.TryGetArray(name, out var definition) &&
        AwkCompatibilityNames.EnablesSpecial(definition.Id, mode) &&
        !definition.CanModifyArrayElements;

    public static void ThrowIfElementMutationForbidden(string name,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault)
    {
        if (IsElementReadOnly(name, mode))
            throw new AwkRuntimeException(AwkMessages.FormatCannotModifySpecialArray(name));
    }

    public static void ThrowIfReplacementForbidden(string name,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault)
    {
        if (!AwkSpecialVariables.TryGetArray(name, out var definition) ||
            !AwkCompatibilityNames.EnablesSpecial(definition.Id, mode) ||
            definition.CanReplaceArray)
        {
            return;
        }

        throw new AwkRuntimeException(definition.MutationPolicy == AwkSpecialArrayMutationPolicy.NonReplaceable
            ? AwkMessages.ErrorCannotDeleteSymtab
            : AwkMessages.FormatCannotModifySpecialArray(name));
    }
}
