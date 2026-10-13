namespace Lokad.Awk;

/// <summary>Immutable ordinals for ordinary global reads; runtime slots remain execution-owned.</summary>
internal sealed class AwkGlobalReadLayout
{
    private readonly Dictionary<AwkVariableName, int> _indices = new();

    public AwkGlobalReadLayout(IReadOnlySet<AwkVariableName> names, Action? checkpoint)
    {
        foreach (var name in names)
        {
            checkpoint?.Invoke();
            if (!AwkSpecialVariables.TryGet(name.Text, out _))
                _indices.Add(name, _indices.Count);
        }
    }

    public int Count => _indices.Count;

    public bool TryGetIndex(AwkVariableName name, out int index) => _indices.TryGetValue(name, out index);
}
