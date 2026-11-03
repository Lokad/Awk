namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    private void BeginIteration(AwkRegionArrayAccess access)
    {
        _state.ThrowIfCancellationRequested();
        if (access.Path >= 0)
        {
            var path = _cursor.Storage.Path(access.Path);
            var array = _state.GetSubarray(path.ResolveOwner(_state), path.Key);
            _cursor.Storage.ReleasePath(access.Path);
            _iterationKeys = _state.SnapshotArrayKeyValuesAsync(array, Context);
        }
        else
            _iterationKeys = _state.TryGetBoundArray(access.Name, access.Binding, out var array)
                ? _state.SnapshotArrayKeyValuesAsync(array, Context)
                : _state.SnapshotArrayKeyValuesAsync(access.Name, Context);
        _pending = Pending.Iteration;
    }

    private void CompleteIteration(AwkLeasedReadOnlyList<AwkArrayKey> keys)
    {
        _iterationKeys = default;
        _pending = Pending.None;
        try
        {
            _state.ThrowIfCancellationRequested();
            var site = _cursor.Region.Instructions[_cursor.Position - 1];
            _cursor.Storage.BeginIterator(site.Target, keys);
        }
        catch { keys.Dispose(); throw; }
    }

    private void AssignVariable(AwkRegionVariable variable, AwkValue value)
    {
        _state.ThrowIfCancellationRequested();
        if (variable.LocalIndex >= 0)
            _state.AssignFunctionLocalValue(new(variable.LocalIndex), variable.Name, value);
        else if (variable.GlobalIndex >= 0)
            _state.AssignPreparedGlobalValue(variable.Name, variable.GlobalIndex, value);
        else
            _state.AssignVariableValue(variable.Name, value);
    }
}
