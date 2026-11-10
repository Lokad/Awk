namespace Lokad.Awk;

// Preparation metadata only: preserve full values unless all reads ultimately consume
// arithmetic or complete array keys. Actual numeric keys still convert at their use.
internal static class AwkParameterConsumption
{
    private const int Arithmetic = -1;
    private const int Observable = -2;
    private const int WriteOnly = -3;
    private const int Key = -4;

    public static void Prepare(AwkSyntaxProgram syntax,
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions, Action? checkpoint)
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

        var opaque = new bool[count];
        var numeric = new bool[count];
        var keys = new bool[count];
        var scalarStorage = new bool[count];
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
                        if (work.NeedsScalarStorage) scalarStorage[node] = true;
                        if (use == WriteOnly) scalarStorage[node] = true;
                        else if (use == Arithmetic) numeric[node] = true;
                        else if (use == Key) keys[node] = true;
                        else if (use == Observable) opaque[node] = true;
                        else
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
                            var arithmetic = expression.OperatorTokens[0].Kind is AwkTokenKind.Plus or AwkTokenKind.Minus
                                or AwkTokenKind.Star or AwkTokenKind.Slash or AwkTokenKind.Percent or AwkTokenKind.Caret;
                            foreach (var child in children) expressions.Push(new(child, arithmetic ? Arithmetic : Observable));
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
                            // Comparisons, truth, unknown calls and
                            // every unrecognized future construct retain the original value.
                            foreach (var child in children) expressions.Push(new(child, Observable));
                            break;
                    }
                }
            }
        }

        var queue = new Queue<int>(count);
        Propagate(opaque);
        Propagate(numeric);
        Propagate(keys);
        Propagate(scalarStorage);
        foreach (var (name, range) in ranges)
        {
            checkpoint?.Invoke();
            AwkParameterInput[] inputs = [];
            for (var index = 0; index < range.Count; index++)
            {
                if ((index & 63) == 0) checkpoint?.Invoke();
                var node = range.Start + index;
                if (opaque[node]) continue;
                // An unread parameter can also carry an owning key unchanged. This
                // matters when a key flows to a known callee that ignores its input.
                var input = numeric[node] && !keys[node] ? AwkParameterInput.Arithmetic :
                    !numeric[node] && !scalarStorage[node] ? AwkParameterInput.Key : AwkParameterInput.Value;
                if (input == AwkParameterInput.Value) continue;
                if (inputs.Length == 0) inputs = new AwkParameterInput[range.Count];
                inputs[index] = input;
            }
            functions[name].LocalLayout.PrepareInputs(inputs);
        }

        void Propagate(bool[] marks)
        {
            for (var i = 0; i < count; i++)
            {
                checkpoint?.Invoke();
                if (marks[i]) queue.Enqueue(i);
            }
            while (queue.TryDequeue(out var target))
            {
                checkpoint?.Invoke();
                var walked = 0;
                for (var index = heads[target]; index >= 0; index = edges[index].Next)
                {
                    if ((walked++ & 63) == 0) checkpoint?.Invoke();
                    var source = edges[index].Source;
                    if (marks[source]) continue;
                    marks[source] = true;
                    queue.Enqueue(source);
                }
            }
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
