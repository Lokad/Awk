namespace Lokad.Awk;

// Keep identity/key/value across evaluation, never a dictionary entry reference.
internal readonly record struct AwkArrayUpdateTarget(AwkArrayKey Key, double Previous, AwkArrayIdentity? Array);
