using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkStatement
{
    private sealed class FieldPrintStatement(AwkExpression indexExpression) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var output = RequireExecutionContext(context).Output;
            // Keep the ordinary field-expression checkpoint before its index.
            context.State.Checkpoint();
            var index = AwkFieldIndex.FromNumber(await indexExpression.ResolveNumberAsync(context).ConfigureAwait(false)).Value;
            context.State.AppendFieldPrint(output, index);
            return AwkStatementFlow.Continue;
        }
    }

    private sealed class PrintStatement(
        AwkExpression[] expressions,
        AwkOutputTarget target) : AwkStatement
    {
        private readonly int[] _fieldIndices = !target.IsRedirected && expressions.Length >= 2
            ? AwkExpression.PrepareLiteralFieldIndices(expressions, maximum: 4) : [];

        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            if (expressions.Length == 0)
                return AwkStatementFlow.Continue;

            var state = context.State;
            var execution = RequireExecutionContext(context);
            if (_fieldIndices.Length != 0)
            {
                AwkPrintRuntime.AppendFields(execution.Output, expressions, _fieldIndices, context);
                return AwkStatementFlow.Continue;
            }
            if (!target.IsRedirected && expressions.Length == 1)
            {
                var value = await expressions[0].EvaluateAsync(context).ConfigureAwait(false);
                AwkPrintRuntime.AppendSingle(execution.Output, value, state);
                return AwkStatementFlow.Continue;
            }
            if (!target.IsRedirected && expressions.Length is >= 2 and <= 4)
            {
                var first = await expressions[0].EvaluateAsync(context).ConfigureAwait(false);
                using var heldFirst = expressions[0].IsTextLiteral ? AwkMemoryLease.Disabled : state.MemoryAccounting.RetainValue(first);
                var second = await expressions[1].EvaluateAsync(context).ConfigureAwait(false);
                using var heldSecond = expressions[1].IsTextLiteral ? AwkMemoryLease.Disabled : state.MemoryAccounting.RetainValue(second);
                var third = expressions.Length >= 3
                    ? await expressions[2].EvaluateAsync(context).ConfigureAwait(false) : default;
                using var heldThird = expressions.Length < 3 || expressions[2].IsTextLiteral
                    ? AwkMemoryLease.Disabled : state.MemoryAccounting.RetainValue(third);
                var fourth = expressions.Length == 4
                    ? await expressions[3].EvaluateAsync(context).ConfigureAwait(false) : default;
                AwkPrintRuntime.AppendSmall(execution.Output, [first, second, third, fourth], expressions.Length, state);
                return AwkStatementFlow.Continue;
            }
            var targetValue = await EvaluateOutputTargetAsync(target, context).ConfigureAwait(false);
            using var heldTargetValue = state.MemoryAccounting.RetainValue(targetValue);
            using var heldTarget = target.IsRedirected ? state.MemoryAccounting.CreateTransientLease() : AwkMemoryLease.Disabled;
            using var buffer = AwkValueBuffer.Rent(expressions.Length, state);
            var values = buffer.Values;
            for (var index = 0; index < expressions.Length; index++)
                buffer.Set(index, await expressions[index].EvaluateAsync(context).ConfigureAwait(false));

            var resolvedTarget = ResolveOutputTarget(target, targetValue, state, heldTarget);
            await EmitPrintAsync(
                    execution.Output,
                    execution.Io,
                    resolvedTarget,
                    values,
                    expressions.Length,
                    state)
                .ConfigureAwait(false);

            return AwkStatementFlow.Continue;

            static async Task EmitPrintAsync(
                AwkOutputSink output,
                AwkIo io,
                AwkOptionalResolvedOutputRedirection target,
                AwkValue[] values,
                int valueCount,
                AwkRuntimeState state)
            {
                var outputFieldSeparator = valueCount > 1
                    ? state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator)
                    : AwkValue.FromText(AwkText.Empty);
                using var segments = AwkPrintRuntime.Stage(
                    values.AsSpan(0, valueCount),
                    outputFieldSeparator,
                    state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator),
                    state);
                if (!target.HasValue)
                {
                    output.Append(segments.Segments);
                    return;
                }

                using var builder = new AwkUtf8Builder(state.MemoryAccounting);
                builder.Append(segments.Segments, state.CancellationCheckpoint);
                await EmitOutputAsync(output, io, target, builder.AsMemory()).ConfigureAwait(false);
            }
        }

    }

    private sealed class PrintfStatement(
        AwkExpression format,
        AwkExpression[] arguments,
        AwkOutputTarget target) : AwkStatement
    {
        private readonly int[] _fieldIndices = AwkExpression.PrepareLiteralFieldIndices(arguments, maximum: 16);

        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            var execution = RequireExecutionContext(context);
            var targetValue = await EvaluateOutputTargetAsync(target, context).ConfigureAwait(false);
            using var heldTargetValue = state.MemoryAccounting.RetainValue(targetValue);
            using var heldTarget = target.IsRedirected ? state.MemoryAccounting.CreateTransientLease() : AwkMemoryLease.Disabled;
            var resolvedTarget = AwkOptionalResolvedOutputRedirection.None;
            var formatValue = await format.EvaluateAsync(context).ConfigureAwait(false);
            using var heldFormatValue = format.IsTextLiteral ? AwkMemoryLease.Disabled : state.MemoryAccounting.RetainValue(formatValue);
            AwkUtf8Builder? formatted = null;
            try
            {
                if (_fieldIndices.Length != 0)
                {
                    for (var index = 0; index < arguments.Length; index++)
                    {
                        AwkExpression.EvaluateLiteralFieldIndex(arguments[index], context);
                        state.PreflightPrintfField(_fieldIndices[index]);
                    }
                    var fields = new AwkPrintfFieldArguments(_fieldIndices, state);
                    resolvedTarget = ResolveOutputTarget(target, targetValue, state, heldTarget);
                    using var heldFormat = state.MemoryAccounting.CreateTransientLease();
                    var formatText = AwkFormattingRuntime.PrepareStringValue(formatValue, state, heldFormat, out _);
                    if (!resolvedTarget.HasValue && execution.Output.TryFormatPrintf(formatText, fields, state))
                        return AwkStatementFlow.Continue;
                    formatted = new AwkUtf8Builder(state.MemoryAccounting, capacity: 0);
                    AwkFormattingRuntime.FormatPrintfToBuilder(formatted, formatText, fields, state);
                }
                else
                {
                    using var buffer = AwkValueBuffer.Rent(arguments.Length, state);
                    var resolvedArguments = buffer.Values;
                    for (var i = 0; i < arguments.Length; i++)
                        buffer.Set(i, await arguments[i].EvaluateAsync(context).ConfigureAwait(false));

                    var values = new AwkValueArrayPrintfArguments(resolvedArguments, arguments.Length, state);
                    resolvedTarget = ResolveOutputTarget(target, targetValue, state, heldTarget);
                    using var heldFormat = state.MemoryAccounting.CreateTransientLease();
                    var formatText = AwkFormattingRuntime.PrepareStringValue(formatValue, state, heldFormat, out _);
                    if (!resolvedTarget.HasValue && execution.Output.TryFormatPrintf(formatText, values, state))
                        return AwkStatementFlow.Continue;
                    formatted = new AwkUtf8Builder(state.MemoryAccounting, capacity: 0);
                    AwkFormattingRuntime.FormatPrintfToBuilder(formatted, formatText, values, state);
                }
                state.ThrowIfCancellationRequested();
                await EmitOutputAsync(execution.Output, execution.Io, resolvedTarget, formatted.AsMemory())
                    .ConfigureAwait(false);
                return AwkStatementFlow.Continue;
            }
            finally
            {
                formatted?.Dispose();
            }
        }
    }

    private static async Task EmitOutputAsync(
        AwkOutputSink output,
        AwkIo io,
        AwkOptionalResolvedOutputRedirection target,
        ReadOnlyMemory<byte> content)
    {
        if (!target.TryGetValue(out var redirection))
        {
            output.Append(content);
            return;
        }

        await io.WriteAsync(new AwkRedirectedOutput(redirection.Path, redirection.Kind, content)).ConfigureAwait(false);
    }

    private static async ValueTask<AwkValue> EvaluateOutputTargetAsync(
        AwkOutputTarget target,
        AwkEvaluationContext context)
    {
        if (!target.TryGetRedirection(out var redirection))
            return AwkValue.Unassigned();

        var value = await redirection.Target.EvaluateAsync(context).ConfigureAwait(false);
        if (redirection.Kind == AwkOutputRedirectionKind.Coprocess)
        {
            using var held = context.State.MemoryAccounting.CreateTransientLease();
            _ = AwkFormattingRuntime.PrepareStringValue(value, context.State, held, out _);
            throw new AwkRuntimeException(AwkMessages.ErrorUnsupportedCoprocess, AwkDiagnosticId.UnsupportedPolicy);
        }
        return value;
    }

    private static AwkOptionalResolvedOutputRedirection ResolveOutputTarget(
        AwkOutputTarget target, AwkValue captured, AwkRuntimeState state, AwkMemoryLease held)
    {
        if (!target.TryGetRedirection(out var redirection)) return AwkOptionalResolvedOutputRedirection.None;
        var path = new AwkRedirectionPath(AwkFormattingRuntime.PrepareStringValue(captured, state, held, out _));
        return AwkOptionalResolvedOutputRedirection.Specified(new(path, redirection.Kind));
    }
}
