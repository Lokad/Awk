using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Frozen;

namespace Lokad.Awk;

internal readonly struct AwkProgram
{
    private static readonly IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> EmptyFunctions =
        new ReadOnlyDictionary<AwkFunctionName, AwkUserFunction>(
            new Dictionary<AwkFunctionName, AwkUserFunction>());

    private readonly AwkRegion[]? _beginActions;
    private readonly AwkRegion? _mainProgram;
    private readonly AwkRegion[]? _endActions;
    private readonly AwkRegion[]? _beginFileActions;
    private readonly AwkRegion[]? _endFileActions;
    private readonly IReadOnlyDictionary<AwkFunctionName, AwkUserFunction>? _functions;
    private readonly FrozenSet<AwkVariableName>? _globalNames;

    public static AwkProgram Empty => default;

    public AwkProgram(
        IReadOnlyList<AwkRegion> beginActions,
        AwkRegion? mainProgram,
        IReadOnlyList<AwkRegion> endActions,
        IReadOnlyList<AwkRegion> beginFileActions,
        IReadOnlyList<AwkRegion> endFileActions,
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions,
        IReadOnlySet<AwkVariableName>? globalNames = null,
        int globalReadCount = 0, int rangeCount = 0, bool hasMainRules = false)
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
        _mainProgram = mainProgram;
        HasMainRules = hasMainRules;
        _endActions = AwkCollectionSnapshots.Copy(endActions);
        _beginFileActions = AwkCollectionSnapshots.Copy(beginFileActions);
        _endFileActions = AwkCollectionSnapshots.Copy(endFileActions);
        _functions = CopyFunctions(functions);
        _globalNames = globalNames?.ToFrozenSet();
        GlobalReadCount = globalReadCount;
        RangeCount = rangeCount;
    }

    public AwkRegion[] BeginActions => _beginActions ?? [];

    public AwkRegion MainProgram => _mainProgram ?? throw new InvalidOperationException("The program has no prepared main input.");
    public bool HasMainRules { get; }

    public AwkRegion[] EndActions => _endActions ?? [];

    public AwkRegion[] BeginFileActions => _beginFileActions ?? [];

    public AwkRegion[] EndFileActions => _endFileActions ?? [];

    public IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> Functions =>
        _functions ?? EmptyFunctions;

    public IReadOnlySet<AwkVariableName> GlobalNames => _globalNames ?? FrozenSet<AwkVariableName>.Empty;

    public int GlobalReadCount { get; }

    public int RangeCount { get; }

}
