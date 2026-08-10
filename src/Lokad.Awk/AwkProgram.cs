using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lokad.Awk;

internal readonly struct AwkProgram
{
    public static AwkProgram Empty => new(
        [],
        [],
        [],
        [],
        [],
        new Dictionary<AwkFunctionName, AwkUserFunction>());

    public AwkProgram(
        IReadOnlyList<AwkAction> beginActions,
        IReadOnlyList<AwkAction> mainActions,
        IReadOnlyList<AwkAction> endActions,
        IReadOnlyList<AwkAction> beginFileActions,
        IReadOnlyList<AwkAction> endFileActions,
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions)
    {
        static ReadOnlyDictionary<AwkFunctionName, AwkUserFunction> CopyFunctions(
            IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions)
        {
            var copy = new Dictionary<AwkFunctionName, AwkUserFunction>(functions.Count);
            foreach (var function in functions.Values)
                copy[function.FunctionName] = function;

            return new ReadOnlyDictionary<AwkFunctionName, AwkUserFunction>(copy);
        }

        BeginActions = AwkCollectionSnapshots.Copy(beginActions);
        MainActions = AwkCollectionSnapshots.Copy(mainActions);
        EndActions = AwkCollectionSnapshots.Copy(endActions);
        BeginFileActions = AwkCollectionSnapshots.Copy(beginFileActions);
        EndFileActions = AwkCollectionSnapshots.Copy(endFileActions);
        Functions = CopyFunctions(functions);
    }

    public AwkAction[] BeginActions { get; }

    public AwkAction[] MainActions { get; }

    public AwkAction[] EndActions { get; }

    public AwkAction[] BeginFileActions { get; }

    public AwkAction[] EndFileActions { get; }

    public IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> Functions { get; }

}
