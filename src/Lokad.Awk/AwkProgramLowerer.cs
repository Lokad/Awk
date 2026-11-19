namespace Lokad.Awk;

internal static class AwkProgramLowerer
{
    private static AwkUnobservedRecordHeaders PrepareUnobservedRecordHeaders(
        IReadOnlyList<AwkSymbolReference> symbols, AwkRuntimeArgumentPlan? arguments, Action? checkpoint)
    {
        // Standalone preparation can have observers outside the source program.
        if (arguments is null) return AwkUnobservedRecordHeaders.None;
        var unobserved = AwkUnobservedRecordHeaders.All;
        foreach (var symbol in symbols)
        {
            checkpoint?.Invoke();
            if (symbol.Name is "SYMTAB" or "ARGV") return AwkUnobservedRecordHeaders.None;
            if (symbol.Name == "NR") unobserved &= ~AwkUnobservedRecordHeaders.NR;
            if (symbol.Name == "FNR") unobserved &= ~AwkUnobservedRecordHeaders.FNR;
            if (symbol.Name == "RT") unobserved &= ~AwkUnobservedRecordHeaders.RT;
            if (unobserved == AwkUnobservedRecordHeaders.None) return unobserved;
        }
        return unobserved;
    }

    public static AwkBindingResult LowerValidated(AwkSyntaxProgram syntax, AwkCollectedSymbols symbols,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null,
        AwkRuntimeArgumentPlan? externalArguments = null, AwkFrontendMemory? memory = null)
    {
        checkpoint?.Invoke();
        if (syntax.Rules.Count == 0 && syntax.Functions.Count == 0)
        {
            return syntax.Directives.Count == 0
                ? AwkBindingResult.FromProgram(AwkProgram.Empty)
                : AwkBindingResult.Failure([new AwkDiagnostic(
                    new AwkTextSpan(0, Math.Min(1, syntax.SourceText.Length)),
                    "Expected awk pattern or action.", AwkDiagnosticId.BindInvalidProgram)]);
        }

        var beginActions = new List<AwkRegion>();
        var endActions = new List<AwkRegion>();
        var beginFileActions = new List<AwkRegion>();
        var endFileActions = new List<AwkRegion>();
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction>(syntax.Functions.Count);
        var globalReads = new AwkGlobalReadLayout(symbols.Globals, checkpoint);

        for (var functionIndex = 0; functionIndex < syntax.Functions.Count; functionIndex++)
        {
            checkpoint?.Invoke();
            var functionSyntax = syntax.Functions[functionIndex];
            var name = new AwkFunctionName(functionSyntax.Identifier.Value);
            var layout = symbols.FunctionLayouts[functionIndex];
            functions.Add(name, new AwkUserFunction(name, layout));
        }

        AwkValueConsumption.Prepare(syntax, functions, mode, checkpoint, globalReads,
            externalArguments is null ? null : symbols.References);

        // All named targets have one identity, including forward and recursive calls.
        for (var functionIndex = 0; functionIndex < syntax.Functions.Count; functionIndex++)
        {
            checkpoint?.Invoke();
            var functionSyntax = syntax.Functions[functionIndex];
            var function = functions[new AwkFunctionName(functionSyntax.Identifier.Value)];
            var scope = AwkLoweringScope.Function(function.LocalLayout, mode, checkpoint, globalReads, functions, memory);
            if (!AwkRegionCompiler.TryCompile(functionSyntax.BodyStatements, scope, out var region, out var diagnostic))
                return AwkBindingResult.Failure([diagnostic]);
            function.PrepareRegion(region);
        }

        AwkFunctionHostEffects.Prepare(functions, checkpoint);

        var globalScope = AwkLoweringScope.GlobalFor(mode, checkpoint, globalReads, functions, memory);
        var main = AwkRegionCompiler.CreateMain(globalScope);
        var rangeCount = 0;
        for (var ruleIndex = 0; ruleIndex < syntax.Rules.Count; ruleIndex++)
        {
            checkpoint?.Invoke();
            var rule = syntax.Rules[ruleIndex];
            var rangeIndex = rule.PatternSyntax is AwkRangePatternSyntax ? rangeCount++ : -1;
            if (rule.Kind == AwkRuleKind.Main)
            {
                if (!main.AppendMainRule(rule, rangeIndex, out var diagnostic))
                    return AwkBindingResult.Failure([diagnostic]);
                continue;
            }
            if (!AwkRegionCompiler.TryCompilePhase(rule, globalScope, out var action, out var phaseDiagnostic))
                return AwkBindingResult.Failure([phaseDiagnostic]);
            switch (rule.Kind)
            {
                case AwkRuleKind.Begin:
                    beginActions.Add(action);
                    break;
                case AwkRuleKind.End:
                    endActions.Add(action);
                    break;
                case AwkRuleKind.BeginFile:
                    beginFileActions.Add(action);
                    break;
                case AwkRuleKind.EndFile:
                    endFileActions.Add(action);
                    break;
            }
        }

        checkpoint?.Invoke();
        var program = new AwkProgram(beginActions, main.BuildMain(), endActions, beginFileActions, endFileActions, functions,
            symbols.Globals, globalReads.Count, rangeCount, main.HasMainRules,
            PrepareUnobservedRecordHeaders(symbols.References, externalArguments, checkpoint));
        checkpoint?.Invoke();
        return AwkBindingResult.FromProgram(program);
    }
}
