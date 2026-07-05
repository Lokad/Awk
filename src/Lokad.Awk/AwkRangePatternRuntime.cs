using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkRangePatternRuntime
{
    private readonly HashSet<AwkPattern> _activePatterns = [];

    public bool IsActive(AwkPattern pattern) =>
        _activePatterns.Contains(pattern);

    public void SetActive(AwkPattern pattern, bool active)
    {
        if (active)
        {
            _activePatterns.Add(pattern);
            return;
        }

        _activePatterns.Remove(pattern);
    }
}
