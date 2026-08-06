using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

public sealed class Awk
{
    private readonly AwkInvocation _invocation;

    private Awk(AwkInvocation invocation)
    {
        _invocation = invocation;
    }

    public static Awk? TryParse(AwkCommandInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        if (!IsAwkName(invocation.CommandName))
            return null;

        return new Awk(AwkCommandParser.Parse(invocation));
    }

    public Task<int> ExecuteAsync(IAwkHost host, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        return AwkExecutor.ExecuteAsync(host, _invocation, cancellationToken);
    }

    private static bool IsAwkName(string name) =>
        AwkNameFacts.GetCommandBaseName(name).SequenceEqual("awk".AsSpan());
}
