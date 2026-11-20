namespace Lokad.Awk;

// One function-level fact for record-backed arguments. Unknown operations are
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
                    var target = indices[region.Calls[instruction.CallSite].Function];
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
        // Remember the first instruction that can capture a record field.
        var firstRecordText = new int[region.Calls.Length];
        var lastHost = 0;
        for (var index = 0; index < region.Instructions.Length; index++)
        {
            if ((index & 127) == 0) checkpoint?.Invoke();
            ref readonly var instruction = ref region.Instructions[index];
            if (instruction.Kind == AwkRegionInstructionKind.Call)
            {
                ref var call = ref region.Calls[instruction.CallSite];
                var start = firstRecordText[instruction.CallSite];
                var host = call.Function.MayInvokeHost;
                var range = !host && (start == 0 || lastHost < start) && call.Function.LocalLayout.HasTextInput
                    ? IncludeRecordText(call.Arguments, instruction.FirstPendingArgument, call.Arguments.Length,
                        start == 0 ? default : call.RecordTextArguments, checkpoint)
                    : default;
                call = call with { RecordTextArguments = range };
                if (host) lastHost = index + 1;
                continue;
            }
            if (instruction.Kind == AwkRegionInstructionKind.Arguments)
            {
                ref var call = ref region.Calls[instruction.CallSite];
                if (call.Function.MayInvokeHost || !call.Function.LocalLayout.HasTextInput) continue;
                ref var start = ref firstRecordText[instruction.CallSite];
                var range = IncludeRecordText(call.Arguments, instruction.FirstArgument, instruction.ArgumentEnd,
                    start == 0 ? default : call.RecordTextArguments, checkpoint);
                if (range.IsEmpty) continue;
                if (start == 0) start = index + 1;
                call = call with { RecordTextArguments = range };
                continue;
            }
            if (DirectHost(in instruction)) lastHost = index + 1;
        }
    }

    private static AwkRegionRecordTextRange IncludeRecordText(ReadOnlySpan<AwkRegionArgument> arguments,
        int first, int end, AwkRegionRecordTextRange range, Action? checkpoint)
    {
        for (var index = first; index < end; index++)
        {
            if ((index & 127) == 127) checkpoint?.Invoke();
            ref readonly var argument = ref arguments[index];
            if (argument.Consumption == AwkParameterInput.Text &&
                argument.Kind is AwkRegionArgumentKind.Field or AwkRegionArgumentKind.DynamicField)
                range = new(range.IsEmpty ? index + 1 : range.First, index + 1);
        }
        return range;
    }

    private static bool DirectHost(in AwkRegionInstruction instruction) =>
        (instruction.Effects & AwkRegionEffects.MayInvokeHost) != 0;
}
