namespace Lokad.Awk;

// One function-level fact for reader-backed arguments. Unknown operations are
// conservative; parameter consumption remains its separate use lattice.
internal static class AwkFunctionHostEffects
{
    internal static void Prepare(IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions, Action? checkpoint)
    {
        if (!functions.Values.Any(static function => function.LocalLayout.HasTextInput)) return;
        var items = functions.Values.ToArray();
        var indices = new Dictionary<AwkUserFunction, int>(items.Length);
        for (var index = 0; index < items.Length; index++) indices.Add(items[index], index);
        var callers = new List<int>[items.Length];
        var unsafeHost = new bool[items.Length];
        var pending = new Queue<int>();
        for (var index = 0; index < items.Length; index++)
        {
            checkpoint?.Invoke();
            var region = items[index].Region ?? throw new InvalidOperationException("Function preparation is incomplete.");
            foreach (var instruction in region.Instructions)
            {
                checkpoint?.Invoke();
                if (instruction.Kind == AwkRegionInstructionKind.Call)
                {
                    var target = indices[region.Calls[instruction.Left].Function];
                    (callers[target] ??= []).Add(index);
                }
                else unsafeHost[index] |= DirectHost(in instruction);
            }
            if (unsafeHost[index]) pending.Enqueue(index);
        }
        while (pending.TryDequeue(out var target))
        {
            checkpoint?.Invoke();
            if (callers[target] is not { } incoming) continue;
            foreach (var caller in incoming)
            {
                checkpoint?.Invoke();
                if (unsafeHost[caller]) continue;
                unsafeHost[caller] = true;
                pending.Enqueue(caller);
            }
        }
        for (var index = 0; index < items.Length; index++) items[index].PrepareHostEffects(unsafeHost[index]);
        foreach (var function in items)
            PrepareCalls(function.Region ?? throw new InvalidOperationException("Function preparation is incomplete."), checkpoint);
    }

    internal static void PrepareCalls(AwkRegion region, Action? checkpoint)
    {
        if (!region.Calls.Any(static call => call.Function.LocalLayout.HasTextInput)) return;
        var prefix = new int[region.Instructions.Length + 1];
        var first = new int[region.Calls.Length];
        Array.Fill(first, -1);
        for (var index = 0; index < region.Instructions.Length; index++)
        {
            if ((index & 127) == 0) checkpoint?.Invoke();
            var instruction = region.Instructions[index];
            var host = instruction.Kind == AwkRegionInstructionKind.Call
                ? region.Calls[instruction.Left].Function.MayInvokeHost : DirectHost(in instruction);
            prefix[index + 1] = prefix[index] + (host ? 1 : 0);
            if (instruction.Kind == AwkRegionInstructionKind.Arguments && first[instruction.Target] < 0)
                first[instruction.Target] = index;
            if (instruction.Kind != AwkRegionInstructionKind.Call) continue;
            var site = instruction.Left;
            var call = region.Calls[site];
            var start = first[site] < 0 ? index : first[site];
            region.Calls[site] = call with { CanBorrowReaderText = !call.Function.MayInvokeHost && prefix[index] == prefix[start] };
        }
    }

    private static bool DirectHost(in AwkRegionInstruction instruction) => instruction.Kind switch
    {
        // General builtins and regex/provider operations may publish diagnostics,
        // even without an explicit host builtin. Keep their ordinary owned capture.
        AwkRegionInstructionKind.Builtin => true,
        AwkRegionInstructionKind.Poll or AwkRegionInstructionKind.StatementBoundary or
        AwkRegionInstructionKind.MoveNumber or AwkRegionInstructionKind.FieldIndex or AwkRegionInstructionKind.FieldNumber or
        AwkRegionInstructionKind.DynamicFieldNumber or AwkRegionInstructionKind.LiteralNumber or AwkRegionInstructionKind.ValueNumber or
        AwkRegionInstructionKind.TextNumber or AwkRegionInstructionKind.ArrayNumber or AwkRegionInstructionKind.LocalNumber or
        AwkRegionInstructionKind.GlobalNumber or AwkRegionInstructionKind.VariableNumber or AwkRegionInstructionKind.Value or
        AwkRegionInstructionKind.Length or AwkRegionInstructionKind.InspectVariable or AwkRegionInstructionKind.InspectArray or
        AwkRegionInstructionKind.InspectValue or AwkRegionInstructionKind.Index or AwkRegionInstructionKind.Substring or
        AwkRegionInstructionKind.Case or AwkRegionInstructionKind.Arithmetic or AwkRegionInstructionKind.Compound or
        AwkRegionInstructionKind.Compare or AwkRegionInstructionKind.CompareText or AwkRegionInstructionKind.Negate or
        AwkRegionInstructionKind.Not or AwkRegionInstructionKind.Truth or AwkRegionInstructionKind.Discard or
        AwkRegionInstructionKind.Assign or AwkRegionInstructionKind.AssignNumber or AwkRegionInstructionKind.ScalarUpdate or
        AwkRegionInstructionKind.AssignField or AwkRegionInstructionKind.CopyField or AwkRegionInstructionKind.ArrayPrepare or
        AwkRegionInstructionKind.ArrayUpdate or AwkRegionInstructionKind.ArrayAssign or AwkRegionInstructionKind.ArrayRead or
        AwkRegionInstructionKind.ArrayContains or AwkRegionInstructionKind.ArrayDelete or AwkRegionInstructionKind.ArrayClear or
        AwkRegionInstructionKind.ArrayTarget or
        AwkRegionInstructionKind.NestedContains or AwkRegionInstructionKind.PathDelete or AwkRegionInstructionKind.PathBegin or
        AwkRegionInstructionKind.PathKey or AwkRegionInstructionKind.PathEnd or AwkRegionInstructionKind.KeyBegin or
        AwkRegionInstructionKind.KeySeparator or AwkRegionInstructionKind.KeyAppend or AwkRegionInstructionKind.KeyReady or
        AwkRegionInstructionKind.KeyEnd or AwkRegionInstructionKind.ReleaseValue or AwkRegionInstructionKind.ReleaseValues or
        AwkRegionInstructionKind.BeginCall or AwkRegionInstructionKind.Arguments or AwkRegionInstructionKind.BuiltinArgument or
        AwkRegionInstructionKind.ConcatArgument or
        AwkRegionInstructionKind.ConcatValue or AwkRegionInstructionKind.Return or AwkRegionInstructionKind.ReturnNumber or
        AwkRegionInstructionKind.PrepareString or AwkRegionInstructionKind.PrepareByteText or
        AwkRegionInstructionKind.Jump or AwkRegionInstructionKind.JumpIfFalse or AwkRegionInstructionKind.JumpIfTrue or
        AwkRegionInstructionKind.JumpIfZero or AwkRegionInstructionKind.JumpIfNonzero or AwkRegionInstructionKind.JumpIfCase => false,
        _ => true
    };
}
