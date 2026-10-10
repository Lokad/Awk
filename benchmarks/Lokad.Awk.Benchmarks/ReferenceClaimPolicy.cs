namespace Lokad.Awk.Benchmarking;

internal static class ReferenceClaimPolicy
{
    public static bool CanPublish(string mode, bool complete, bool clean, bool quiet, bool buildMatches,
        bool hasReference, int requestedCases, int completedCases, bool allCorrect) =>
        mode == "qualify" && complete && clean && quiet && buildMatches && hasReference &&
        requestedCases > 0 && completedCases == requestedCases && allCorrect;
}
