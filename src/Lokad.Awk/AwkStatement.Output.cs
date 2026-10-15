using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkStatement
{
    private sealed class FieldPrintStatement(AwkExpression indexExpression, AwkExpression fieldExpression) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var output = RequireExecutionContext(context).Output;
            // Keep the ordinary field-expression checkpoint before its index.
            context.State.RecordExecutionStep();
            var index = (int)await indexExpression.ResolveNumberAsync(context).ConfigureAwait(false);
            context.State.AppendFieldPrint(output, index);
            return AwkStatementFlow.Continue;
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            // The opt-in engine retains its existing field evaluation and print instruction.
            compiler.EmitExpressionOrFallback(fieldExpression);
            compiler.EmitPrint(1);
            compiler.Pop();
            return true;
        }
    }

    private sealed class PrintStatement(
        AwkExpression[] expressions,
        AwkOutputTarget target) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            if (expressions.Length == 0)
                return AwkStatementFlow.Continue;

            var state = context.State;
            var execution = RequireExecutionContext(context);
            var resolvedTarget = await ResolveOutputTargetAsync(target, context).ConfigureAwait(false);
            using var buffer = AwkValueBuffer.Rent(expressions.Length, state.MemoryAccounting);
            var values = buffer.Values;
            for (var index = 0; index < expressions.Length; index++)
                values[index] = await expressions[index].EvaluateAsync(context).ConfigureAwait(false);

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

                using var builder = new AwkUtf8Builder(state.Limits, state.MemoryAccounting);
                builder.Append(segments.Segments, state.CancellationCheckpoint);
                await EmitOutputAsync(output, io, target, builder.AsMemory()).ConfigureAwait(false);
            }
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            if (target.IsRedirected || expressions.Length == 0)
                return false;

            for (var index = 0; index < expressions.Length; index++)
                compiler.EmitExpressionOrFallback(expressions[index]);

            compiler.EmitPrint(expressions.Length);
            compiler.Pop(expressions.Length);
            return true;
        }

    }

    private sealed class PrintfStatement(
        AwkExpression format,
        AwkExpression[] arguments,
        AwkOutputTarget target) : AwkStatement
    {
        private readonly int[] _fieldIndices = AwkPrintfFieldArguments.Prepare(arguments);

        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            var execution = RequireExecutionContext(context);
            var resolvedTarget = await ResolveOutputTargetAsync(target, context).ConfigureAwait(false);
            var formatText = await format.ResolveStringAsync(context).ConfigureAwait(false);
            using var formatted = new AwkUtf8Builder(state.Limits, state.MemoryAccounting, capacity: 0);
            if (_fieldIndices.Length != 0)
            {
                for (var index = 0; index < arguments.Length; index++)
                {
                    AwkExpression.EvaluatePrintfFieldIndex(arguments[index], context);
                    state.PreflightPrintfField(_fieldIndices[index]);
                }
                AwkFormattingRuntime.FormatPrintfToBuilder(formatted, formatText,
                    new AwkPrintfFieldArguments(_fieldIndices, state), state);
            }
            else
            {
                using var buffer = AwkValueBuffer.Rent(arguments.Length, state.MemoryAccounting);
                var resolvedArguments = buffer.Values;
                for (var i = 0; i < arguments.Length; i++)
                    resolvedArguments[i] = await arguments[i].EvaluateAsync(context).ConfigureAwait(false);

                AwkFormattingRuntime.FormatPrintfToBuilder(formatted, formatText,
                    new AwkValueArrayPrintfArguments(resolvedArguments, arguments.Length, state), state);
            }

            state.ThrowIfCancellationRequested();
            await EmitOutputAsync(execution.Output, execution.Io, resolvedTarget, formatted.AsMemory())
                .ConfigureAwait(false);
            return AwkStatementFlow.Continue;
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

    private static async ValueTask<AwkOptionalResolvedOutputRedirection> ResolveOutputTargetAsync(
        AwkOutputTarget target,
        AwkEvaluationContext context)
    {
        if (!target.TryGetRedirection(out var redirection))
            return AwkOptionalResolvedOutputRedirection.None;

        var path = new AwkRedirectionPath(await redirection.Target.ResolveStringAsync(context).ConfigureAwait(false));
        if (redirection.Kind == AwkOutputRedirectionKind.Coprocess)
            throw new AwkRuntimeException(AwkMessages.ErrorUnsupportedCoprocess, AwkDiagnosticId.UnsupportedPolicy);

        return AwkOptionalResolvedOutputRedirection.Specified(new AwkResolvedOutputRedirection(path, redirection.Kind));
    }
}
