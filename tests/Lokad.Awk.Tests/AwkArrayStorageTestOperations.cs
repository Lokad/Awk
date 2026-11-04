namespace Lokad.Awk.Tests;

// Storage admission/publication tests use the production owners directly.
// User-comparison suspension is exercised through source in the continuation tests.
internal static class AwkArrayStorageTestOperations
{
    internal static int Sort(AwkRuntimeState state, AwkArrayName source, AwkArrayName destination,
        AwkArraySortOrder order, bool keys)
    {
        using var operation = state.BeginArraySort(new(source), order, keys);
        Assert.False(operation.TryNextComparison());
        return operation.Publish(new(destination), keys);
    }

    internal static AwkLeasedReadOnlyList<AwkArrayKey> Snapshot(AwkRuntimeState state, AwkArrayName name)
    {
        var start = state.BeginArrayKeySnapshot(new(name));
        Assert.False(start.NeedsComparisons);
        return start.Keys;
    }
}
