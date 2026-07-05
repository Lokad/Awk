using System.Buffers;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkStatement
{
    private sealed class PrintStatement(
        IReadOnlyList<AwkExpressionPart> parts,
        AwkOutputTarget target) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            if (parts.Count == 0)
                return AwkStatementFlow.Continue;

            var state = context.State;
            var execution = RequireExecutionContext(context);
            var output = execution.Output;
            if (!target.IsRedirected)
            {
                var segments = ArrayPool<AwkText>.Shared.Rent((parts.Count * 2) + 1);
                var segmentCount = 0;
                try
                {
                    for (var i = 0; i < parts.Count; i++)
                    {
                        var part = parts[i];
                        if (part.LeadingSpace && i > 0)
                        {
                            segments[segmentCount++] = ResolveSpecialVariableText(
                                state,
                                AwkSpecialVariableId.OutputFieldSeparator);
                        }

                        segments[segmentCount++] =
                            state.FormatPrintByteText(await part.Expression.EvaluateAsync(context).ConfigureAwait(false));
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
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part.LeadingSpace && i > 0)
                    builder.Append(ResolveSpecialVariableText(
                        state,
                        AwkSpecialVariableId.OutputFieldSeparator));

                builder.Append(state.FormatPrintByteText(await part.Expression.EvaluateAsync(context).ConfigureAwait(false)));
            }

            builder.Append(ResolveSpecialVariableText(
                state,
                AwkSpecialVariableId.OutputRecordSeparator));
            var outputCopyLease = state.MemoryGovernor.ReserveTransient(
                AwkRuntimeMemory.EstimateByteArrayBytes(builder.Length));
            try
            {
                await EmitOutputAsync(
                        output,
                        execution.Io,
                        target,
                        builder.ToArray(),
                        context)
                    .ConfigureAwait(false);
            }
            finally
            {
                outputCopyLease.Dispose();
            }

            return AwkStatementFlow.Continue;
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            if (target.IsRedirected || parts.Count == 0)
                return false;

            for (var index = 0; index < parts.Count; index++)
            {
                if (parts[index].LeadingSpace != (index > 0))
                    return false;

                compiler.EmitExpressionOrFallback(parts[index].Expression);
            }

            compiler.Emit(AwkOpcode.Print, parts.Count);
            compiler.Pop(parts.Count);
            return true;
        }

        private static AwkText ResolveSpecialVariableText(AwkRuntimeState state, AwkSpecialVariableId id) =>
            state.ResolveSpecialVariableText(id);
    }

    private sealed class PrintfStatement(
        AwkExpression format,
        IReadOnlyList<AwkExpression> arguments,
        AwkOutputTarget target) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            var execution = RequireExecutionContext(context);
            var output = execution.Output;
            var formatText = await format.ResolveStringAsync(context).ConfigureAwait(false);
            var resolvedArguments = ArrayPool<AwkValue>.Shared.Rent(arguments.Count);
            AwkText formatted;
            try
            {
                for (var i = 0; i < arguments.Count; i++)
                    resolvedArguments[i] = await arguments[i].EvaluateAsync(context).ConfigureAwait(false);

                formatted = AwkFormattingRuntime.FormatPrintfStrictByteText(
                    formatText,
                    new AwkValueArrayPrintfArguments(resolvedArguments, arguments.Count, state.Counters),
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
