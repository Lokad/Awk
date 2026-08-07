using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkStatement
{
    private sealed class UnsupportedCoprocessStatement : AwkStatement
    {
        public static readonly UnsupportedCoprocessStatement Instance = new();

        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorUnsupportedCoprocess, AwkDiagnosticId.UnsupportedPolicy);
        }
    }

    private sealed class GetlineStatement(AwkGetlineTarget target, AwkGetlineSource source) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var execution = RequireExecutionContext(context);
            _ = await AwkGetlineRuntime.ExecuteAsync(
                    target,
                    source,
                    context,
                    execution,
                    flushPendingDiagnostics: false)
                .ConfigureAwait(false);

            return AwkStatementFlow.Continue;
        }
    }

    private sealed class FflushStatement(AwkFflushTarget targetExpression) : AwkStatement
    {
        internal override bool RequiresExecutableFallbackFlush => true;

        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var execution = RequireExecutionContext(context);
            await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
            _ = await targetExpression.FlushAsync(context, execution.Io).ConfigureAwait(false);
            return AwkStatementFlow.Continue;
        }
    }

    private sealed class CloseStatement(AwkExpression targetExpression) : AwkStatement
    {
        internal override bool RequiresExecutableFallbackFlush => true;

        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var execution = RequireExecutionContext(context);
            await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
            var target = await targetExpression
                .ResolveStringAsync(context)
                .ConfigureAwait(false);
            await execution.Io.CloseAsync(target).ConfigureAwait(false);
            return AwkStatementFlow.Continue;
        }
    }
}
