using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Frozen;

namespace Lokad.Awk;

internal readonly struct AwkProgram
{
    private static readonly IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> EmptyFunctions =
        new ReadOnlyDictionary<AwkFunctionName, AwkUserFunction>(
            new Dictionary<AwkFunctionName, AwkUserFunction>());

    private readonly AwkAction[]? _beginActions;
    private readonly AwkAction[]? _mainActions;
    private readonly AwkAction[]? _endActions;
    private readonly AwkAction[]? _beginFileActions;
    private readonly AwkAction[]? _endFileActions;
    private readonly IReadOnlyDictionary<AwkFunctionName, AwkUserFunction>? _functions;
    private readonly FrozenSet<AwkVariableName>? _globalNames;
    private readonly AwkGlobalReadLayout? _globalReads;

    public static AwkProgram Empty => default;

    public AwkProgram(
        IReadOnlyList<AwkAction> beginActions,
        IReadOnlyList<AwkAction> mainActions,
        IReadOnlyList<AwkAction> endActions,
        IReadOnlyList<AwkAction> beginFileActions,
        IReadOnlyList<AwkAction> endFileActions,
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions,
        IReadOnlySet<AwkVariableName>? globalNames = null,
        AwkGlobalReadLayout? globalReads = null)
    {
        static ReadOnlyDictionary<AwkFunctionName, AwkUserFunction> CopyFunctions(
            IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions)
        {
            var copy = new Dictionary<AwkFunctionName, AwkUserFunction>(functions.Count);
            foreach (var function in functions.Values)
                copy[function.FunctionName] = function;

            return new ReadOnlyDictionary<AwkFunctionName, AwkUserFunction>(copy);
        }

        _beginActions = AwkCollectionSnapshots.Copy(beginActions);
        _mainActions = AwkCollectionSnapshots.Copy(mainActions);
        _endActions = AwkCollectionSnapshots.Copy(endActions);
        _beginFileActions = AwkCollectionSnapshots.Copy(beginFileActions);
        _endFileActions = AwkCollectionSnapshots.Copy(endFileActions);
        _functions = CopyFunctions(functions);
        _globalNames = globalNames?.ToFrozenSet();
        _globalReads = globalReads;
    }

    public AwkAction[] BeginActions => _beginActions ?? [];

    public AwkAction[] MainActions => _mainActions ?? [];

    public AwkAction[] EndActions => _endActions ?? [];

    public AwkAction[] BeginFileActions => _beginFileActions ?? [];

    public AwkAction[] EndFileActions => _endFileActions ?? [];

    public IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> Functions =>
        _functions ?? EmptyFunctions;

    public IReadOnlySet<AwkVariableName> GlobalNames => _globalNames ?? FrozenSet<AwkVariableName>.Empty;

    public int GlobalReadCount => _globalReads?.Count ?? 0;

}
