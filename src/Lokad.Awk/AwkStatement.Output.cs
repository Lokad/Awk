using System.Buffers;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkStatement
{
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
            var values = ArrayPool<AwkValue>.Shared.Rent(expressions.Length);
            try
            {
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
            }
            finally
            {
                ArrayPool<AwkValue>.Shared.Return(values, clearArray: true);
            }

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
                    ? state.ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator)
                    : AwkText.Empty;
                using var segments = AwkPrintRuntime.Stage(
                    values.AsSpan(0, valueCount),
                    outputFieldSeparator,
                    state.ResolveSpecialVariableText(AwkSpecialVariableId.OutputRecordSeparator),
                    state);
                if (!target.HasValue)
                {
                    output.Append(segments.Segments);
                    return;
                }

                using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor);
                builder.Append(segments.Segments);
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
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            var execution = RequireExecutionContext(context);
            var resolvedTarget = await ResolveOutputTargetAsync(target, context).ConfigureAwait(false);
            var formatText = await format.ResolveStringAsync(context).ConfigureAwait(false);
            var resolvedArguments = ArrayPool<AwkValue>.Shared.Rent(arguments.Length);
            AwkText formatted;
            try
            {
                for (var i = 0; i < arguments.Length; i++)
                    resolvedArguments[i] = await arguments[i].EvaluateAsync(context).ConfigureAwait(false);

                formatted = AwkFormattingRuntime.FormatPrintfStrictByteText(
                    formatText,
                    new AwkValueArrayPrintfArguments(resolvedArguments, arguments.Length, state),
                    state.Limits,
                    state.Counters,
                    state.MemoryGovernor);
            }
            finally
            {
                ArrayPool<AwkValue>.Shared.Return(resolvedArguments, clearArray: true);
            }

            await EmitByteTextOutputAsync(execution.Output, execution.Io, resolvedTarget, formatted).ConfigureAwait(false);

            return AwkStatementFlow.Continue;

            static async Task EmitByteTextOutputAsync(
                AwkOutputSink output,
                AwkIo io,
                AwkOptionalResolvedOutputRedirection target,
                AwkText content)
            {
                await EmitOutputAsync(output, io, target, content.AsMemory()).ConfigureAwait(false);
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
