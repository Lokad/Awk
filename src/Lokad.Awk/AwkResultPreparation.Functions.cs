namespace Lokad.Awk;

internal static partial class AwkResultPreparation
{
    private const int GeneralResult = -1;
    private const int LengthResult = -2;
    private const byte LengthObservation = 1;
    private const byte GeneralObservation = 2;

    private static void PrepareFunctions(AwkSyntaxProgram syntax,
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions,
        Dictionary<string, Definition> definitions, IReadOnlyList<AwkSymbolReference> symbols,
        AwkCompatibilityMode mode, Action? checkpoint)
    {
        // Comparator names can come from runtime strings. Their numeric observations
        // are not represented by ordinary call symbols; retain full returns here.
        foreach (var symbol in symbols)
        {
            checkpoint?.Invoke();
            if (symbol.Name is "PROCINFO" or "asort" or "asorti") return;
        }
        var items = syntax.Functions;
        var indices = new Dictionary<AwkFunctionName, int>(items.Count);
        for (var i = 0; i < items.Count; i++) indices.Add(AwkFunctionName.FromText(items[i].Name), i);
        var observations = new byte[items.Count];
        var unsafeText = new bool[items.Count];
        var textCallers = new List<int>?[items.Count];
        var forwarding = new List<int>?[items.Count];
        var expressions = new Stack<(AwkExpressionSyntax Expression, int Use)>();
        for (var index = 0; index < items.Count; index++)
        {
            checkpoint?.Invoke();
            var function = functions[AwkFunctionName.FromText(items[index].Name)];
            var layout = function.LocalLayout;
            var writes = new int[function.Parameters.Count];
            var locals = new LocalTextDefinition?[writes.Length];
            var inspectedLocals = new bool[writes.Length];
            var returns = new List<AwkExpressionSyntax>();
            foreach (var (statement, unconditional) in Statements(items[index].BodyStatements, checkpoint))
            {
                if (statement.Kind == AwkStatementKind.Return && statement.Expression.TryGetValue(out var returned))
                    returns.Add(returned);
                if (unconditional && statement.Kind == AwkStatementKind.Expression &&
                    statement.Expression.TryGetValue(out var expression))
                {
                    expression = Ungroup(expression, checkpoint);
                    if (expression.Kind == AwkExpressionKind.Assignment && expression.Children.Count == 2 &&
                        expression.OperatorTokens.Count == 1 && expression.OperatorTokens[0].Kind == AwkTokenKind.Equal &&
                        AwkExpressionSyntaxFacts.TryGetIdentifier(expression.Children[0], out var name) &&
                        layout.TryGetSlot(new(name.Identifier), out var slot))
                        locals[slot.Index] ??= new(expression.Children[1], expression.Span.End);
                }
                if (statement.Payload is AwkGetlineStatementSyntaxPayload getline &&
                    AwkGetlineSyntaxFacts.TryResolveStatementParts(getline.Getline, out var parts) &&
                    parts.Target.TryGetValue(out var target)) MarkWrite(target);
                if (statement.Payload is AwkInputPipeExpressionStatementSyntaxPayload pipe &&
                    pipe.InputPipeGetline.Target.Expression.TryGetValue(out target)) MarkWrite(target);
                if (statement.Payload is AwkForStatementSyntaxPayload { Header.IsForIn: true } loop &&
                    loop.Header.Parts.Count > 0 && loop.Header.Parts[0].Expression.TryGetValue(out target)) MarkWrite(target);
                if (statement.Kind == AwkStatementKind.Delete && statement.Expression.TryGetValue(out target)) MarkWrite(target);
                AddExpressions(statement, statement.Kind == AwkStatementKind.Return ? index : GeneralResult);
                VisitExpressions(index, MarkWrite);
            }
            // Reaching text definitions remain real scalar owners. Reject any
            // other write or an incoming value read before its defining assignment.
            foreach (var returned in returns)
            {
                expressions.Push((returned, GeneralResult));
                while (expressions.TryPop(out var work))
                {
                    checkpoint?.Invoke();
                    var expression = Ungroup(work.Expression, checkpoint);
                    // Ready text can be counted without moving materialization
                    // admission ahead of the return's diagnostic/output flush.
                    if (expression.Kind == AwkExpressionKind.String) continue;
                    if (expression.Kind == AwkExpressionKind.Conditional && expression.Children.Count == 3)
                    {
                        expressions.Push((expression.Children[1], GeneralResult));
                        expressions.Push((expression.Children[2], GeneralResult));
                        continue;
                    }
                    if (expression.Kind == AwkExpressionKind.Assignment && expression.Children.Count == 2 &&
                        expression.OperatorTokens.Count == 1 && expression.OperatorTokens[0].Kind == AwkTokenKind.Equal)
                    {
                        expressions.Push((expression.Children[1], GeneralResult));
                        continue;
                    }
                    if (AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var name) &&
                        layout.TryGetSlot(new(name.Identifier), out var slot) && writes[slot.Index] == 1 &&
                        locals[slot.Index] is { } local && local.End <= expression.Span.Start)
                    {
                        // Its defining RHS and function dependencies need only one
                        // inspection, even with many later returns through this cell.
                        if (!inspectedLocals[slot.Index])
                        {
                            inspectedLocals[slot.Index] = true;
                            expressions.Push((local.Value, GeneralResult));
                        }
                        continue;
                    }
                    if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var call) &&
                        !layout.TryGetSlot(new(call.Identifier), out _))
                    {
                        if (indices.TryGetValue(new(call.Identifier), out var callee))
                        {
                            (textCallers[callee] ??= []).Add(index);
                            continue;
                        }
                        if (AwkBuiltins.TryGetCallable(call.Name, out var builtin) &&
                            AwkCompatibilityNames.EnablesBuiltin(builtin, mode) &&
                            builtin.Id is AwkBuiltinId.ToLower or AwkBuiltinId.ToUpper or AwkBuiltinId.Substr or AwkBuiltinId.Sprintf)
                            continue;
                    }
                    unsafeText[index] = true;
                }
            }

            void MarkWrite(AwkExpressionSyntax target)
            {
                target = Ungroup(target, checkpoint);
                if (AwkExpressionSyntaxFacts.TryGetIdentifier(target, out var name) &&
                    layout.TryGetSlot(new(name.Identifier), out var slot)) writes[slot.Index]++;
            }
        }
        foreach (var rule in syntax.Rules)
        {
            checkpoint?.Invoke();
            foreach (var expression in rule.PatternSyntax.EnumerateChildExpressions()) expressions.Push((expression, GeneralResult));
            VisitExpressions(-1, null);
            foreach (var (statement, _) in Statements(rule.ActionStatements, checkpoint))
            {
                AddExpressions(statement, GeneralResult);
                VisitExpressions(-1, null);
            }
        }

        var pending = new Queue<int>();
        for (var i = 0; i < items.Count; i++) if (unsafeText[i]) pending.Enqueue(i);
        while (pending.TryDequeue(out var callee))
        {
            checkpoint?.Invoke();
            if (textCallers[callee] is not { } callers) continue;
            foreach (var caller in callers)
            {
                checkpoint?.Invoke();
                if (unsafeText[caller]) continue;
                unsafeText[caller] = true;
                pending.Enqueue(caller);
            }
        }
        for (var i = 0; i < items.Count; i++)
        {
            if (unsafeText[i]) observations[i] |= GeneralObservation;
            if (observations[i] != 0) pending.Enqueue(i);
        }
        var queued = new bool[items.Count];
        Propagate();
        // An unobserved function still has one ordinary prepared body. Its own
        // forwarding calls cannot acquire a different return meaning by accident.
        for (var i = 0; i < items.Count; i++)
            if (observations[i] == 0) { observations[i] = GeneralObservation; pending.Enqueue(i); }
        Propagate();
        for (var i = 0; i < items.Count; i++)
        {
            checkpoint?.Invoke();
            if (observations[i] == LengthObservation)
                functions[AwkFunctionName.FromText(items[i].Name)].PrepareStringLengthReturn();
        }

        void Propagate()
        {
            foreach (var index in pending) queued[index] = true;
            while (pending.TryDequeue(out var caller))
            {
                checkpoint?.Invoke();
                queued[caller] = false;
                if (forwarding[caller] is not { } callees) continue;
                foreach (var callee in callees)
                {
                    checkpoint?.Invoke();
                    var merged = (byte)(observations[callee] | observations[caller]);
                    if (merged == observations[callee]) continue;
                    observations[callee] = merged;
                    if (!queued[callee]) { pending.Enqueue(callee); queued[callee] = true; }
                }
            }
        }

        void AddExpressions(AwkStatementSyntax statement, int use)
        {
            foreach (var expression in statement.EnumerateChildExpressions()) expressions.Push((expression, use));
            foreach (var arm in statement.SwitchCases)
                if (arm.Expression.TryGetValue(out var expression)) expressions.Push((expression, GeneralResult));
        }

        void VisitExpressions(int functionIndex, Action<AwkExpressionSyntax>? write)
        {
            var layout = functionIndex >= 0 ? functions[AwkFunctionName.FromText(items[functionIndex].Name)].LocalLayout : null;
            while (expressions.TryPop(out var work))
            {
                checkpoint?.Invoke();
                var expression = Ungroup(work.Expression, checkpoint);
                var children = expression.Children;
                if (expression.Kind == AwkExpressionKind.Assignment && children.Count == 2)
                {
                    write?.Invoke(children[0]);
                    var closed = functionIndex < 0 && AwkExpressionSyntaxFacts.TryGetIdentifier(children[0], out var target) &&
                        definitions.TryGetValue(target.Name, out var definition) && definition.Reads > 0 && definition.Target.Equals(target.Span);
                    expressions.Push((children[0], GeneralResult));
                    expressions.Push((children[1], closed ? LengthResult : GeneralResult));
                    continue;
                }
                if (AwkExpressionSyntaxFacts.IsIncrementOrDecrementExpression(expression) && children.Count == 1)
                    write?.Invoke(children[0]);
                if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var call) &&
                    (layout is null || !layout.TryGetSlot(new(call.Identifier), out _)))
                {
                    if (AwkBuiltins.IsName(call.Name, AwkBuiltinId.Getline) &&
                        AwkGetlineSyntaxFacts.TryResolveExpressionCallParts(expression, out var input) &&
                        input.Target.TryGetValue(out var inputTarget)) write?.Invoke(inputTarget);
                    if (indices.TryGetValue(new(call.Identifier), out var callee))
                    {
                        if (work.Use >= 0) (forwarding[work.Use] ??= []).Add(callee);
                        else observations[callee] |= work.Use == LengthResult ? LengthObservation : GeneralObservation;
                        for (var i = 1; i < children.Count; i++) expressions.Push((children[i], GeneralResult));
                        continue;
                    }
                    if (AwkBuiltins.TryGetCallable(call.Name, out var builtin) && AwkCompatibilityNames.EnablesBuiltin(builtin, mode))
                    {
                        for (var i = 1; i < children.Count; i++)
                        {
                            if (builtin.GetArgumentRole(i - 1) is AwkBuiltinArgumentRole.LValue or AwkBuiltinArgumentRole.ArrayName)
                                write?.Invoke(children[i]);
                            expressions.Push((children[i], builtin.Id == AwkBuiltinId.Length && children.Count == 2 ? LengthResult : GeneralResult));
                        }
                        continue;
                    }
                }
                // A general value operation needs a real value even if its final
                // consumer is length. Only complete direct returns forward demand.
                foreach (var child in children) expressions.Push((child, GeneralResult));
            }
        }
    }

    private static AwkExpressionSyntax Ungroup(AwkExpressionSyntax expression, Action? checkpoint)
    {
        while (expression.Kind == AwkExpressionKind.Group && expression.Children.Count == 1)
        {
            checkpoint?.Invoke();
            expression = expression.Children[0];
        }
        return expression;
    }

    private static IEnumerable<(AwkStatementSyntax Statement, bool Unconditional)> Statements(
        IReadOnlyList<AwkStatementSyntax> statements, Action? checkpoint)
    {
        var pending = new Stack<(AwkStatementSyntax Statement, bool Unconditional)>();
        foreach (var statement in statements) pending.Push((statement, true));
        while (pending.TryPop(out var item))
        {
            checkpoint?.Invoke();
            yield return item;
            var unconditional = item.Unconditional && item.Statement.Kind == AwkStatementKind.Block;
            foreach (var child in item.Statement.BodyStatements) pending.Push((child, unconditional));
            foreach (var child in item.Statement.ElseStatements) pending.Push((child, false));
            foreach (var arm in item.Statement.SwitchCases)
                foreach (var child in arm.BodyStatements) pending.Push((child, false));
        }
    }

    private sealed record LocalTextDefinition(AwkExpressionSyntax Value, int End);
}
