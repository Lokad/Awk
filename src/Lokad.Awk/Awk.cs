using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>Represents a parsed awk command that can be executed against an explicit host.</summary>
public sealed class Awk
{
    private readonly AwkCommandInvocation _command;

    private Awk(AwkCommandInvocation command)
    {
        _command = command;
    }

    /// <summary>
    /// Parses an invocation whose command base name is <c>awk</c>.
    /// </summary>
    /// <param name="invocation">The command invocation to inspect and parse.</param>
    /// <param name="awk">The parsed command when the command base name is <c>awk</c>.</param>
    /// <returns><see langword="true"/> when the invocation represents an awk command.</returns>
    public static bool TryParse(
        AwkCommandInvocation invocation,
        [NotNullWhen(true)] out Awk? awk)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        static bool IsAwkName(string name) =>
            AwkNameFacts.GetCommandBaseName(name).SequenceEqual("awk".AsSpan());

        if (!IsAwkName(invocation.CommandName))
        {
            awk = null;
            return false;
        }

        awk = new Awk(invocation);
        return true;
    }

    /// <summary>Executes the parsed command, routing every external effect through <paramref name="host"/>.</summary>
    /// <returns>A task whose result is the awk process exit status.</returns>
    public Task<AwkExitCode> ExecuteAsync(IAwkHost host, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        return AwkExecutor.ExecuteAsync(
            host,
            AwkCommandParser.Parse(_command),
            cancellationToken,
            AwkRuntimeOptions.Create(AwkExecutionPolicy.CreateDefault(), _command.MemoryBudget));
    }

    /// <summary>Executes the parsed command under an explicit capability and resource policy.</summary>
    public Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkExecutionPolicy policy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(policy);
        return AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(_command, policy),
            cancellationToken, AwkRuntimeOptions.Create(policy, _command.MemoryBudget));
    }
}
