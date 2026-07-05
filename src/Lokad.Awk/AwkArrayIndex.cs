using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract record AwkArrayIndex
{
    public static AwkArrayIndex Empty { get; } = new AwkEmptyArrayIndex();

    public static AwkArrayIndex Simple(AwkExpression expression) =>
        new AwkSimpleArrayIndex(expression);

    public static AwkArrayIndex Composite(IReadOnlyList<AwkExpression> expressions) =>
        expressions.Count == 1
            ? Simple(expressions[0])
            : new AwkCompositeArrayIndex(AwkExecutableCollections.Copy(expressions));

    public abstract ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context);

    internal virtual bool TryEmitExecutableKey(AwkExecutableCompiler compiler) => false;
}

internal sealed record AwkEmptyArrayIndex : AwkArrayIndex
{
    public override ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(new AwkArrayKey(AwkText.Empty));
}

internal sealed record AwkSimpleArrayIndex(AwkExpression Expression) : AwkArrayIndex
{
    public override async ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context)
    {
        var value = await Expression.EvaluateAsync(context).ConfigureAwait(false);
        return context.State.ToArrayKey(value);
    }

    internal override bool TryEmitExecutableKey(AwkExecutableCompiler compiler) =>
        compiler.TryEmitExpression(Expression);
}

internal sealed record AwkCompositeArrayIndex(IReadOnlyList<AwkExpression> Expressions) : AwkArrayIndex
{
    public override async ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context)
    {
        var separator = context.State.ResolveSubscriptSeparatorText();
        using var builder = new AwkUtf8Builder(context.State.Limits, context.State.MemoryGovernor);
        for (var index = 0; index < Expressions.Count; index++)
        {
            if (index > 0)
                builder.Append(separator);

            var part = context.State.Formatting.FormatStringByteText(
                await Expressions[index].EvaluateAsync(context).ConfigureAwait(false));
            builder.Append(part);
        }

        return new AwkArrayKey(builder.CopyToAwkText(context.State.Counters));
    }
}
