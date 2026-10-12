namespace Lokad.Awk.Tests;

// Forced GC and shared-pool trimming from unrelated tests can allocate inside an
// otherwise allocation-free measured loop, even when that loop owns its thread.
// Short cancellation watchdogs also need to run without competing test collections.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AwkAllocationMeasurementCollection
{
    public const string Name = "Awk allocation measurements";
}
