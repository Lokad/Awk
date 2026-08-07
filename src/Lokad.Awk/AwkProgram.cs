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

    public static AwkProgram Concat(IReadOnlyList<AwkProgram> programs)
    {
        if (programs.Count == 0)
            return Empty;

        if (programs.Count == 1)
            return programs[0];

        var begin = new List<AwkAction>();
        var main = new List<AwkAction>();
        var end = new List<AwkAction>();
        var beginFile = new List<AwkAction>();
        var endFile = new List<AwkAction>();
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction>();
        for (var i = 0; i < programs.Count; i++)
        {
            begin.AddRange(programs[i].BeginActions);
            main.AddRange(programs[i].MainActions);
            end.AddRange(programs[i].EndActions);
            beginFile.AddRange(programs[i].BeginFileActions);
            endFile.AddRange(programs[i].EndFileActions);
            foreach (var function in programs[i].Functions.Values)
            {
                functions[function.FunctionName] = function;
            }
        }

        return new AwkProgram(begin, main, end, beginFile, endFile, functions);
    }

    public AwkProgram(
        IReadOnlyList<AwkAction> beginActions,
        IReadOnlyList<AwkAction> mainActions,
        IReadOnlyList<AwkAction> endActions,
        IReadOnlyList<AwkAction> beginFileActions,
        IReadOnlyList<AwkAction> endFileActions,
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions)
    {
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

    private static IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> CopyFunctions(
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions)
    {
        var copy = new Dictionary<AwkFunctionName, AwkUserFunction>();
        foreach (var function in functions.Values)
        {
            copy[function.FunctionName] = function;
        }

        return new ReadOnlyDictionary<AwkFunctionName, AwkUserFunction>(copy);
    }
}
