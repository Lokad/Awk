using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>Represents a recognized awk invocation that can be executed against an explicit host.</summary>
public sealed class Awk
{
    private readonly AwkCommandInvocation _command;

    private Awk(AwkCommandInvocation command)
    {
        _command = command;
    }

    /// <summary>
    /// Recognizes and wraps an invocation whose command base name is <c>awk</c>.
    /// </summary>
    /// <remarks>Checks the command name only. Options and source are validated during execution;
    /// source loading uses the host supplied to <c>ExecuteAsync</c>.</remarks>
    /// <param name="invocation">The command invocation to recognize.</param>
    /// <param name="awk">The recognized command when the command base name is <c>awk</c>.</param>
    /// <returns><see langword="true"/> when the command base name is <c>awk</c>.</returns>
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

    /// <summary>Prepares and executes the recognized command, routing every external effect through <paramref name="host"/>.</summary>
    /// <returns>A task whose result is the awk process exit status.</returns>
    public Task<AwkExitCode> ExecuteAsync(IAwkHost host, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        return AwkExecutor.ExecuteAsync(
            host,
            _command,
            cancellationToken,
            AwkRuntimeOptions.Create(AwkExecutionPolicy.CreateDefault(), _command.MemoryBudget));
    }

    /// <summary>Prepares and executes the recognized command under an explicit capability and resource policy.</summary>
    public Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkExecutionPolicy policy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(policy);
        return AwkExecutor.ExecuteAsync(host, _command,
            cancellationToken, AwkRuntimeOptions.Create(policy, _command.MemoryBudget));
    }
}
