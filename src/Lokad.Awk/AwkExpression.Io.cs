using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class GetlineExpression(AwkGetlineTarget target, AwkGetlineSource source) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (!context.TryGetExecutionContext(out var execution))
                return AwkValue.FromNumber(-1);

            var result = await AwkGetlineRuntime.ExecuteAsync(
                    target,
                    source,
                    context,
                    execution,
                    flushRedirectedIoEvents: true)
                .ConfigureAwait(false);
            return AwkValue.FromNumber(result.StatusCode);
        }
    }
}
