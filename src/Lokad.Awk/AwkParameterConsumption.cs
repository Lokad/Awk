namespace Lokad.Awk;

// Preparation metadata only: an incoming value may lose its text only when every read
// ultimately consumes arithmetic. This neither declares a type nor promotes aliases.
internal static class AwkParameterConsumption
{
    private const int Arithmetic = -1;
    private const int Observable = -2;
    private const int WriteOnly = -3;

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
        var heads = new int[count];
        Array.Fill(heads, -1);
        var edges = new List<ForwardingEdge>();
        var statements = new Stack<AwkStatementSyntax>();
        var expressions = new Stack<(AwkExpressionSyntax Expression, int Use)>();
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
                    expressions.Push((expression, statement.Kind == AwkStatementKind.Exit ? Arithmetic : Observable));
                PushStatements(statement.BodyStatements);
                PushStatements(statement.ElseStatements);
                foreach (var clause in statement.SwitchCases)
                {
                    if (clause.Expression.TryGetValue(out var expression)) expressions.Push((expression, Observable));
                    PushStatements(clause.BodyStatements);
                }
                while (expressions.TryPop(out var work))
                {
                    checkpoint?.Invoke();
                    var expression = work.Expression;
                    var use = work.Use;
                    if (AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var identifier))
                    {
                        if (use == WriteOnly || !layout.TryGetSlot(new(identifier.Identifier), out var slot))
                            continue;
                        var node = current.Start + slot.Index;
                        if (use == Arithmetic) numeric[node] = true;
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
                            expressions.Push((children[0], use));
                            break;
                        case AwkExpressionKind.Field:
                            foreach (var child in children) expressions.Push((child, Arithmetic));
                            break;
                        case AwkExpressionKind.Assignment when children.Count == 2 && expression.OperatorTokens.Count == 1:
                            var compound = AwkAssignmentOperators.TryGetCompoundOperation(expression.OperatorTokens[0].Kind, out _);
                            expressions.Push((children[0], compound ? Arithmetic : WriteOnly));
                            expressions.Push((children[1], compound ? Arithmetic : Observable));
                            break;
                        case AwkExpressionKind.Binary when expression.OperatorTokens.Count == 1:
                            var arithmetic = expression.OperatorTokens[0].Kind is AwkTokenKind.Plus or AwkTokenKind.Minus
                                or AwkTokenKind.Star or AwkTokenKind.Slash or AwkTokenKind.Percent or AwkTokenKind.Caret;
                            foreach (var child in children) expressions.Push((child, arithmetic ? Arithmetic : Observable));
                            break;
                        case AwkExpressionKind.Unary or AwkExpressionKind.Postfix when expression.OperatorTokens.Count == 1:
                            var numericUnary = expression.OperatorTokens[0].Kind is AwkTokenKind.Plus or AwkTokenKind.Minus
                                or AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus;
                            foreach (var child in children) expressions.Push((child, numericUnary ? Arithmetic : Observable));
                            break;
                        case AwkExpressionKind.Conditional when children.Count == 3:
                            expressions.Push((children[0], Observable));
                            expressions.Push((children[1], use));
                            expressions.Push((children[2], use));
                            break;
                        case AwkExpressionKind.FunctionCall:
                            if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var target) &&
                                !layout.TryGetSlot(new(target.Identifier), out _) &&
                                ranges.TryGetValue(AwkFunctionName.FromText(target.Name), out var callee))
                            {
                                for (var i = 1; i < children.Count; i++)
                                    expressions.Push((children[i], i - 1 < callee.Count ? callee.Start + i - 1 : Observable));
                            }
                            else foreach (var child in children) expressions.Push((child, Observable));
                            break;
                        default:
                            // Array roots/keys, comparisons, truth, unknown/builtin calls and
                            // every unrecognized future construct retain the original value.
                            foreach (var child in children) expressions.Push((child, Observable));
                            break;
                    }
                }
            }
        }

        var queue = new Queue<int>(count);
        Propagate(opaque);
        Propagate(numeric);
        foreach (var (name, range) in ranges)
        {
            checkpoint?.Invoke();
            bool[] inputs = [];
            for (var index = 0; index < range.Count; index++)
            {
                if ((index & 63) == 0) checkpoint?.Invoke();
                if (!numeric[range.Start + index] || opaque[range.Start + index]) continue;
                if (inputs.Length == 0) inputs = new bool[range.Count];
                inputs[index] = true;
            }
            functions[name].LocalLayout.PrepareArithmeticInputs(inputs);
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

    private readonly record struct ParameterRange(int Start, int Count);
    private readonly record struct ForwardingEdge(int Source, int Next);
}
