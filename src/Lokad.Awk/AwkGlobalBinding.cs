namespace Lokad.Awk;

// Zero scalar slot is unbound; positive is slot+1. Arrays bind their stable owner.
internal readonly record struct AwkGlobalBinding(int ScalarSlot, AwkArrayIdentity? Array);
