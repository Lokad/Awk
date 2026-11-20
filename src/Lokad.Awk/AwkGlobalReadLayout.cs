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

    private AwkParameterInput[] _assignmentInputs = [];

    internal void PrepareAssignmentInputs(AwkParameterInput[] inputs) => _assignmentInputs = inputs;

    internal AwkParameterInput AssignmentInput(AwkVariableName name) =>
        _assignmentInputs.Length != 0 && TryGetIndex(name, out var index)
            ? _assignmentInputs[index] : AwkParameterInput.Value;

    public int Count => _indices.Count;

    public bool TryGetIndex(AwkVariableName name, out int index) => _indices.TryGetValue(name, out index);
}
