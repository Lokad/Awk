namespace Lokad.Awk;

// Both bindings refer to storage owned by this execution, never the prepared program.
internal readonly record struct AwkGlobalBinding(AwkVariables.AwkScalarCell? Scalar, AwkArrayIdentity? Array);
