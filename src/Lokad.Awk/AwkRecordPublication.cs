namespace Lokad.Awk;

// Prepared once before header admission, then consumed by the owning frame.
internal readonly record struct AwkRecordPublication(
    AwkRecordSplitPlan SplitPlan, long RetainedBytes, long GrowthBytes);
