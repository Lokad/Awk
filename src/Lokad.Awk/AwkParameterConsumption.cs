namespace Lokad.Awk;

// Preparation metadata only: preserve full values unless all reads ultimately consume
// arithmetic, proved-number comparisons or complete array keys. Numeric keys format at use.
internal static class AwkParameterConsumption
{
    private const int Arithmetic = -1;
    private const int Observable = -2;
    private const int WriteOnly = -3;
    private const int Key = -4;
    private const int NumericComparison = -5;

    [Flags]
    private enum Use : byte { None = 0, Arithmetic = 1, Comparison = 2, Key = 4, Storage = 8, Observable = 16 }

    public static void Prepare(AwkSyntaxProgram syntax,
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions, AwkCompatibilityMode mode, Action? checkpoint)
    {
        if (functions.Count == 0) return;
        var ranges = new Dictionary<AwkFunctionName, ParameterRange>(functions.Count);
        var count = 0;
        foreach (var (name, function) in functions)
        {
            checkpoint?.Invoke();
            ranges.Add(name, new(count, function.Parameters.Count));
            count = checked(count + function.Parameters.Count);
        }
        if (count == 0) return;

        // One use lattice replaces independent mark arrays/passes. Queue membership
        // bounds pending propagation to one entry per parameter, including cycles.
        // Existing coarse frontend admission funds this token-bounded preparation.
        var uses = new Use[count];
        var heads = new int[count];
        Array.Fill(heads, -1);
        var edges = new List<ForwardingEdge>();
        var statements = new Stack<AwkStatementSyntax>();
        var expressions = new Stack<ParameterRead>();
        foreach (var function in syntax.Functions)
        {
            checkpoint?.Invoke();
            var name = AwkFunctionName.FromText(function.Name);
            var current = ranges[name];
            var layout = functions[name].LocalLayout;
            PushStatements(function.BodyStatements);
            while (statements.TryPop(out var statement))
            {
                checkpoint?.Invoke();
                foreach (var expression in statement.EnumerateChildExpressions())
                    expressions.Push(new(expression, statement.Kind == AwkStatementKind.Exit ? Arithmetic : Observable));
                PushStatements(statement.BodyStatements);
                PushStatements(statement.ElseStatements);
                foreach (var clause in statement.SwitchCases)
                {
                    if (clause.Expression.TryGetValue(out var expression)) expressions.Push(new(expression, Observable));
                    PushStatements(clause.BodyStatements);
                }
                while (expressions.TryPop(out var work))
                {
                    checkpoint?.Invoke();
                    var expression = work.Expression;
                    var use = work.Use;
                    if (AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var identifier))
                    {
                        if (!layout.TryGetSlot(new(identifier.Identifier), out var slot))
                            continue;
                        var node = current.Start + slot.Index;
                        if (work.NeedsScalarStorage) uses[node] |= Use.Storage;
                        uses[node] |= use switch
                        {
                            WriteOnly => Use.Storage, Arithmetic => Use.Arithmetic,
                            NumericComparison => Use.Comparison, Key => Use.Key,
                            Observable => Use.Observable, _ => Use.None
                        };
                        if (use >= 0)
                        {
                            edges.Add(new(node, heads[use]));
                            heads[use] = edges.Count - 1;
                        }
                        continue;
                    }
                    var children = expression.Children;
                    switch (expression.Kind)
                    {
                        case AwkExpressionKind.Group when children.Count == 1:
                            expressions.Push(new(children[0], use, work.NeedsScalarStorage));
                            break;
                        case AwkExpressionKind.Field:
                            foreach (var child in children) expressions.Push(new(child, Arithmetic));
                            break;
                        case AwkExpressionKind.Assignment when children.Count == 2 && expression.OperatorTokens.Count == 1:
                            var compound = AwkAssignmentOperators.TryGetCompoundOperation(expression.OperatorTokens[0].Kind, out _);
                            if (compound) expressions.Push(new(children[0], WriteOnly));
                            expressions.Push(new(children[0], compound ? Arithmetic : WriteOnly));
                            expressions.Push(new(children[1], compound ? Arithmetic : Observable));
                            break;
                        case AwkExpressionKind.Binary when expression.OperatorTokens.Count == 1:
                            var token = expression.OperatorTokens[0].Kind;
                            var arithmetic = token is AwkTokenKind.Plus or AwkTokenKind.Minus
                                or AwkTokenKind.Star or AwkTokenKind.Slash or AwkTokenKind.Percent or AwkTokenKind.Caret;
                            if (IsComparison(token) && children.Count == 2)
                            {
                                expressions.Push(new(children[0], ProducesNumber(children[1], layout) ? NumericComparison : Observable));
                                expressions.Push(new(children[1], ProducesNumber(children[0], layout) ? NumericComparison : Observable));
                            }
                            else foreach (var child in children) expressions.Push(new(child, arithmetic ? Arithmetic : Observable));
                            break;
                        case AwkExpressionKind.Unary or AwkExpressionKind.Postfix when expression.OperatorTokens.Count == 1:
                            var numericUnary = expression.OperatorTokens[0].Kind is AwkTokenKind.Plus or AwkTokenKind.Minus
                                or AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus;
                            if (expression.OperatorTokens[0].Kind is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus)
                                foreach (var child in children) expressions.Push(new(child, WriteOnly));
                            foreach (var child in children) expressions.Push(new(child, numericUnary ? Arithmetic : Observable));
                            break;
                        case AwkExpressionKind.Conditional when children.Count == 3:
                            expressions.Push(new(children[0], Observable));
                            // These branches publish general values, including when the
                            // conditional itself is an argument to a key-only callee.
                            expressions.Push(new(children[1], use, NeedsScalarStorage: true));
                            expressions.Push(new(children[2], use, NeedsScalarStorage: true));
                            break;
                        case AwkExpressionKind.ArrayElement when children.Count >= 2:
                            expressions.Push(new(children[0], Observable));
                            for (var i = 1; i < children.Count; i++)
                                expressions.Push(new(children[i], children.Count == 2 ? Key : Observable));
                            break;
                        case AwkExpressionKind.FunctionCall:
                            if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var target) &&
                                !layout.TryGetSlot(new(target.Identifier), out _))
                            {
                                if (ranges.TryGetValue(AwkFunctionName.FromText(target.Name), out var callee))
                                {
                                    for (var i = 1; i < children.Count; i++)
                                        expressions.Push(new(children[i], i - 1 < callee.Count ? callee.Start + i - 1 : Observable));
                                }
                                else
                                {
                                    if (AwkBuiltins.TryGetCallable(target.Name, out var builtin))
                                    {
                                        for (var i = 1; i < children.Count; i++)
                                            expressions.Push(new(children[i], builtin.ConsumesArithmeticInput(i - 1) ? Arithmetic : Observable));
                                    }
                                    else
                                        for (var i = 1; i < children.Count; i++)
                                            expressions.Push(new(children[i], Observable));
                                }
                            }
                            else foreach (var child in children) expressions.Push(new(child, Observable));
                            break;
                        default:
                            // Truth, unknown calls and
                            // every unrecognized future construct retain the original value.
                            foreach (var child in children) expressions.Push(new(child, Observable));
                            break;
                    }
                }
            }
        }

        var queue = new Queue<int>(count);
        var queued = new bool[count];
        for (var index = 0; index < count; index++)
        {
            if ((index & 63) == 0) checkpoint?.Invoke();
            if (uses[index] != Use.None)
            {
                queue.Enqueue(index);
                queued[index] = true;
            }
        }
        while (queue.TryDequeue(out var target))
        {
            checkpoint?.Invoke();
            queued[target] = false;
            var walked = 0;
            for (var index = heads[target]; index >= 0; index = edges[index].Next)
            {
                if ((walked++ & 63) == 0) checkpoint?.Invoke();
                var source = edges[index].Source;
                var merged = uses[source] | uses[target];
                if (merged == uses[source]) continue;
                uses[source] = merged;
                if (!queued[source])
                {
                    queue.Enqueue(source);
                    queued[source] = true;
                }
            }
        }
        foreach (var (name, range) in ranges)
        {
            checkpoint?.Invoke();
            AwkParameterInput[] inputs = [];
            for (var index = 0; index < range.Count; index++)
            {
                if ((index & 63) == 0) checkpoint?.Invoke();
                var node = range.Start + index;
                var use = uses[node];
                if ((use & Use.Observable) != 0) continue;
                // An unread parameter can also carry an owning key unchanged. This
                // matters when a key flows to a known callee that ignores its input.
                var input = (use & Use.Key) == 0 && (use & Use.Comparison) != 0 ? AwkParameterInput.NumericComparison :
                    (use & Use.Key) == 0 && (use & Use.Arithmetic) != 0 ? AwkParameterInput.Arithmetic :
                    (use & (Use.Arithmetic | Use.Comparison | Use.Storage)) == 0 ? AwkParameterInput.Key : AwkParameterInput.Value;
                if (input == AwkParameterInput.Value) continue;
                if (inputs.Length == 0) inputs = new AwkParameterInput[range.Count];
                inputs[index] = input;
            }
            functions[name].LocalLayout.PrepareInputs(inputs);
        }

        bool ProducesNumber(AwkExpressionSyntax expression, AwkFunctionLocalLayout layout)
        {
            while (expression.Kind == AwkExpressionKind.Group && expression.Children.Count == 1)
            {
                checkpoint?.Invoke();
                expression = expression.Children[0];
            }
            if (expression.Kind == AwkExpressionKind.Number) return true;
            if (expression.OperatorTokens.Count == 1)
            {
                var token = expression.OperatorTokens[0].Kind;
                if (expression.Kind == AwkExpressionKind.Binary && (IsComparison(token) || token is
                    AwkTokenKind.Plus or AwkTokenKind.Minus or AwkTokenKind.Star or AwkTokenKind.Slash or
                    AwkTokenKind.Percent or AwkTokenKind.Caret or AwkTokenKind.AmpersandAmpersand or AwkTokenKind.PipePipe)) return true;
                if (expression.Kind is AwkExpressionKind.Unary or AwkExpressionKind.Postfix && token is
                    AwkTokenKind.Plus or AwkTokenKind.Minus or AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus or AwkTokenKind.Bang) return true;
            }
            return expression.Kind == AwkExpressionKind.FunctionCall &&
                AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var called) &&
                !layout.TryGetSlot(new(called.Identifier), out _) &&
                AwkBuiltins.TryGetCallable(called.Name, out var builtin) &&
                AwkCompatibilityNames.EnablesBuiltin(builtin, mode) &&
                (builtin.LoweringKind is AwkBuiltinLoweringKind.Length or AwkBuiltinLoweringKind.IsArray or
                    AwkBuiltinLoweringKind.Match or AwkBuiltinLoweringKind.Split or AwkBuiltinLoweringKind.Patsplit or
                    AwkBuiltinLoweringKind.Asort or AwkBuiltinLoweringKind.Asorti or AwkBuiltinLoweringKind.Sub or AwkBuiltinLoweringKind.Gsub ||
                 builtin.TryGetScalarComputation(out var computation) && computation.ReturnsNumber);
        }

        void PushStatements(IReadOnlyList<AwkStatementSyntax> body)
        {
            for (var i = 0; i < body.Count; i++)
            {
                if ((i & 63) == 0) checkpoint?.Invoke();
                statements.Push(body[i]);
            }
        }
    }

    private static bool IsComparison(AwkTokenKind token) => token is AwkTokenKind.EqualEqual or AwkTokenKind.BangEqual or
        AwkTokenKind.Less or AwkTokenKind.LessEqual or AwkTokenKind.Greater or AwkTokenKind.GreaterEqual;

    private readonly record struct ParameterRead
    {
        private readonly AwkExpressionSyntax? _expression;
        public ParameterRead(AwkExpressionSyntax expression, int use, bool NeedsScalarStorage = false) =>
            (_expression, Use, this.NeedsScalarStorage) = (expression, use, NeedsScalarStorage);
        public AwkExpressionSyntax Expression => _expression ?? throw new InvalidOperationException("An empty parameter read has no expression.");
        public int Use { get; }
        public bool NeedsScalarStorage { get; }
    }
    private readonly record struct ParameterRange(int Start, int Count);
    private readonly record struct ForwardingEdge(int Source, int Next);
}
