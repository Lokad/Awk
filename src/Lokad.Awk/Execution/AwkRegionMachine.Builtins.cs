namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    // General builtins keep their argument staging and actual host boundary
    // together, outside the common arithmetic/record driver. True resumes the
    // existing pending operation; the borrowed computation view is never retained.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private bool ExecuteBuiltin(in AwkRegionComputation view, in AwkRegionInstruction instruction, int position)
    {
        var builtin = view.Region.Builtins[instruction.Left];
        if (builtin.Host is { } host)
        {
            view.State.ThrowIfCancellationRequested();
            _position = position + 1;
            _pending = Pending.BuiltinValue;
            _hostValue = host.EvaluateBound(view.Storage.Values, builtin.Start, builtin.Count, Context);
            return true;
        }

        var computation = builtin.Computation ?? throw new InvalidOperationException("A computational builtin requires its implementation.");
        var pendingArguments = builtin.PendingArguments;
        var firstArgument = builtin.Count - pendingArguments.Length;
        var firstPending = builtin.Start + firstArgument;
        for (var index = 0; index < pendingArguments.Length; index++)
        {
            if ((index & 63) == 63) view.State.ThrowIfCancellationRequested();
            view.Storage.StoreValue(firstPending + index,
                view.ArgumentValue(pendingArguments[index], computation.InputConsumption(firstArgument + index)),
                preserveOrigin: true);
        }
        view.State.ThrowIfCancellationRequested();
        var result = computation.EvaluateBound(view.Storage.Values, builtin.Start, builtin.Count, Context);
        if (computation.ReturnsNumber) view.Numbers[instruction.Target] = result.StoredNumber;
        else view.Storage.StoreValue(instruction.Target, result);
        view.Storage.ReleaseValues(builtin.Start, builtin.Count);
        return false;
    }
}
