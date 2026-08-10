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
            var output = execution.Output;
            if (!target.IsRedirected)
            {
                var segments = ArrayPool<AwkText>.Shared.Rent((expressions.Length * 2) + 1);
                var segmentCount = 0;
                try
                {
                    for (var i = 0; i < expressions.Length; i++)
                    {
                        if (i > 0)
                        {
                            segments[segmentCount++] = ResolveSpecialVariableText(
                                state,
                                AwkSpecialVariableId.OutputFieldSeparator);
                        }

                        segments[segmentCount++] =
                            state.FormatPrintByteText(await expressions[i].EvaluateAsync(context).ConfigureAwait(false));
                    }

                    segments[segmentCount++] = ResolveSpecialVariableText(
                        state,
                        AwkSpecialVariableId.OutputRecordSeparator);
                    for (var index = 0; index < segmentCount; index++)
                        output.Append(segments[index]);
                }
                finally
                {
                    ArrayPool<AwkText>.Shared.Return(segments, clearArray: true);
                }

                return AwkStatementFlow.Continue;
            }

            using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor);
            for (var i = 0; i < expressions.Length; i++)
            {
                if (i > 0)
                    builder.Append(ResolveSpecialVariableText(
                        state,
                        AwkSpecialVariableId.OutputFieldSeparator));

                builder.Append(state.FormatPrintByteText(
                    await expressions[i].EvaluateAsync(context).ConfigureAwait(false)));
            }

            builder.Append(ResolveSpecialVariableText(
                state,
                AwkSpecialVariableId.OutputRecordSeparator));
            await EmitOutputAsync(
                    output,
                    execution.Io,
                    target,
                    builder.AsMemory(),
                    context)
                .ConfigureAwait(false);

            return AwkStatementFlow.Continue;
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            if (target.IsRedirected || expressions.Length == 0)
                return false;

            for (var index = 0; index < expressions.Length; index++)
                compiler.EmitExpressionOrFallback(expressions[index]);

            compiler.EmitCounted(AwkOpcode.Print, expressions.Length);
            compiler.Pop(expressions.Length);
            return true;
        }

        private static AwkText ResolveSpecialVariableText(AwkRuntimeState state, AwkSpecialVariableId id) =>
            state.ResolveSpecialVariableText(id);
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
            var output = execution.Output;
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
                    state.Counters,
                    state.MemoryGovernor);
            }
            finally
            {
                ArrayPool<AwkValue>.Shared.Return(resolvedArguments, clearArray: true);
            }

            await EmitOutputAsync(output, execution.Io, target, formatted, context).ConfigureAwait(false);

            return AwkStatementFlow.Continue;
        }
    }

    private static async Task EmitOutputAsync(
        AwkOutputSink output,
        AwkIo io,
        AwkOutputTarget target,
        AwkText content,
        AwkEvaluationContext context)
    {
        await EmitOutputAsync(output, io, target, content.AsMemory(), context).ConfigureAwait(false);
    }

    private static async Task EmitOutputAsync(
        AwkOutputSink output,
        AwkIo io,
        AwkOutputTarget target,
        ReadOnlyMemory<byte> content,
        AwkEvaluationContext context)
    {
        if (!target.TryGetRedirection(out var redirection))
        {
            output.Append(content);
            return;
        }

        var path = new AwkRedirectionPath(await redirection.Target.ResolveStringAsync(context).ConfigureAwait(false));
        if (redirection.Kind == AwkOutputRedirectionKind.Coprocess)
            throw new AwkRuntimeException(AwkMessages.ErrorUnsupportedCoprocess, AwkDiagnosticId.UnsupportedPolicy);

        await io.WriteAsync(new AwkRedirectedOutput(path, redirection.Kind, content)).ConfigureAwait(false);
    }
}
