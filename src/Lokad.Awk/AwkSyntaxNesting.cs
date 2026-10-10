using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkSyntaxNesting
{
    public static bool TryFindExcessiveDelimiterDepth(
        IReadOnlyList<AwkToken> tokens,
        int maxDepth,
        out AwkTextSpan span,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var depth = 0;
        for (var index = 0; index < tokens.Count; index++)
        {
            checkpoint?.Invoke();
            var token = tokens[index];
            if (token.Kind is AwkTokenKind.LeftBrace or AwkTokenKind.LeftBracket or AwkTokenKind.LeftParen)
            {
                depth++;
                if (depth > maxDepth)
                {
                    span = token.Span;
                    return true;
                }

                continue;
            }

            if (token.Kind is AwkTokenKind.RightBrace or AwkTokenKind.RightBracket or AwkTokenKind.RightParen)
                depth = System.Math.Max(0, depth - 1);
        }

        span = default;
        return false;
    }

    public static bool TryFindExcessiveTreeDepth(
        AwkSyntaxProgram program,
        int maxDepth,
        out AwkTextSpan span,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var expressions = new Stack<ExpressionFrame>();
        var statements = new Stack<StatementFrame>();

        for (var ruleIndex = 0; ruleIndex < program.Rules.Count; ruleIndex++)
        {
            checkpoint?.Invoke();
            var rule = program.Rules[ruleIndex];
            foreach (var expression in rule.PatternSyntax.EnumerateChildExpressions())
                expressions.Push(new ExpressionFrame(expression, 1));

            PushStatements(rule.ActionStatements, 1);
        }

        for (var functionIndex = 0; functionIndex < program.Functions.Count; functionIndex++)
        {
            checkpoint?.Invoke();
            PushStatements(program.Functions[functionIndex].BodyStatements, 1);
        }

        while (statements.Count > 0)
        {
            checkpoint?.Invoke();
            var frame = statements.Pop();
            if (frame.Depth > maxDepth)
            {
                span = frame.Statement.Span;
                return true;
            }

            foreach (var expression in frame.Statement.EnumerateChildExpressions())
                expressions.Push(new ExpressionFrame(expression, frame.Depth + 1));

            PushStatements(frame.Statement.BodyStatements, frame.Depth + 1);
            PushStatements(frame.Statement.ElseStatements, frame.Depth + 1);
            for (var caseIndex = 0; caseIndex < frame.Statement.SwitchCases.Count; caseIndex++)
            {
                checkpoint?.Invoke();
                var switchCase = frame.Statement.SwitchCases[caseIndex];
                if (switchCase.Expression.TryGetValue(out var caseExpression))
                    expressions.Push(new ExpressionFrame(caseExpression, frame.Depth + 1));

                PushStatements(switchCase.BodyStatements, frame.Depth + 1);
            }
        }

        while (expressions.Count > 0)
        {
            checkpoint?.Invoke();
            var frame = expressions.Pop();
            if (frame.Depth > maxDepth)
            {
                span = frame.Expression.Span;
                return true;
            }

            for (var childIndex = 0; childIndex < frame.Expression.Children.Count; childIndex++)
            {
                checkpoint?.Invoke();
                expressions.Push(new ExpressionFrame(
                    frame.Expression.Children[childIndex],
                    frame.Depth + 1));
            }
        }

        span = default;
        return false;

        void PushStatements(IReadOnlyList<AwkStatementSyntax> source, int depth)
        {
            for (var index = 0; index < source.Count; index++)
            {
                checkpoint?.Invoke();
                statements.Push(new StatementFrame(source[index], depth));
            }
        }
    }

    private readonly struct ExpressionFrame
    {
        private readonly AwkExpressionSyntax? _expression;

        public ExpressionFrame(AwkExpressionSyntax expression, int depth)
        {
            _expression = expression;
            Depth = depth;
        }

        public AwkExpressionSyntax Expression =>
            _expression ?? throw new System.InvalidOperationException("The default expression frame is empty.");

        public int Depth { get; }
    }

    private readonly struct StatementFrame
    {
        private readonly AwkStatementSyntax? _statement;

        public StatementFrame(AwkStatementSyntax statement, int depth)
        {
            _statement = statement;
            Depth = depth;
        }

        public AwkStatementSyntax Statement =>
            _statement ?? throw new System.InvalidOperationException("The default statement frame is empty.");

        public int Depth { get; }
    }
}
