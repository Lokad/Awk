using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>Represents a parsed awk command that can be executed against an explicit host.</summary>
public sealed class Awk
{
    private readonly AwkInvocation _invocation;

    private Awk(AwkInvocation invocation)
    {
        _invocation = invocation;
    }

    /// <summary>
    /// Parses an invocation whose command base name is <c>awk</c>.
    /// </summary>
    /// <returns>The parsed command, or <see langword="null"/> when the command name is not <c>awk</c>.</returns>
    public static Awk? TryParse(AwkCommandInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        static bool IsAwkName(string name) =>
            AwkNameFacts.GetCommandBaseName(name).SequenceEqual("awk".AsSpan());

        if (!IsAwkName(invocation.CommandName))
            return null;

        return new Awk(AwkCommandParser.Parse(invocation));
    }

    /// <summary>Executes the parsed command, routing every external effect through <paramref name="host"/>.</summary>
    /// <returns>A task whose result is the awk process exit status.</returns>
    public Task<int> ExecuteAsync(IAwkHost host, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        return AwkExecutor.ExecuteAsync(host, _invocation, cancellationToken);
    }
}
