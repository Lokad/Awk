namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    private void BeginIteration(AwkRegionIteration site, AwkRegionArrayAccess access, AwkArrayKey key)
    {
        _state.ThrowIfCancellationRequested();
        AwkArrayTarget target;
        if (access.Path >= 0)
        {
            var path = _storage.Path(access.Path);
            target = new(AwkValue.FromArrayReference(_state.GetSubarray(path.ResolveOwner(_state), path.Key)));
            _storage.ReleasePath(access.Path);
        }
        else if (access.Key >= 0)
            target = new(AwkValue.FromArrayReference(_state.GetSubarray(access.Name, key, access.Binding)));
        else
            target = _state.TryGetBoundArray(access.Name, access.Binding, out var array)
                ? new(AwkValue.FromArrayReference(array)) : new(access.Name);
        var start = _state.BeginArrayKeySnapshot(target);
        if (start.NeedsComparisons) _storage.StoreSort(site.Sort, start.Sort);
        else
        {
            try
            {
                _state.ThrowIfCancellationRequested();
                _storage.BeginIterator(site.Iterator, start.Keys);
            }
            catch { start.Keys.Dispose(); throw; }
        }
    }

    private bool AdvanceIterationSort(AwkRegionIteration site, int position)
    {
        var storage = _storage;
        if (!storage.TryGetSort(site.Sort, out var operation)) return false;
        if (ScheduleSortComparison(operation, site.CallSlot, site.Comparison, position)) return true;
        storage.BeginIterator(site.Iterator, operation.TakeKeys());
        storage.ReleaseSort(site.Sort);
        return false;
    }

    private void AssignVariable(AwkRegionVariable variable, AwkValue value)
    {
        _state.ThrowIfCancellationRequested();
        if (variable.Kind == AwkRegionOperandKind.LocalVariable)
            _state.AssignFunctionLocalValue(new(variable.Index), variable.Name, value);
        else if (variable.Kind == AwkRegionOperandKind.GlobalVariable)
            _state.AssignPreparedGlobalValue(variable.Name, variable.Index, value);
        else
            _state.AssignVariableValue(variable.Name, value);
    }

    private void AssignVariableNumber(AwkRegionVariable variable, double number)
    {
        _state.ThrowIfCancellationRequested();
        if (variable.Kind == AwkRegionOperandKind.LocalVariable)
        {
            if (_frame is { } frame && frame.TryWriteNumber(variable.Index, number))
                _state.Counters.RecordBoundFrameNumberWrite();
            else _state.AssignFunctionLocalValue(new(variable.Index), variable.Name, AwkValue.FromNumber(number));
        }
        else if (variable.Kind == AwkRegionOperandKind.GlobalVariable)
        {
            if (!_state.TryAssignPreparedGlobalNumber(variable.Index, number))
                _state.AssignPreparedGlobalValue(variable.Name, variable.Index, AwkValue.FromNumber(number));
        }
        else _state.AssignVariableValue(variable.Name, AwkValue.FromNumber(number));
    }
}
