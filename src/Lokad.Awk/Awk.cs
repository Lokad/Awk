using System;
using System.Diagnostics.CodeAnalysis;
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

        awk = new Awk(AwkCommandParser.Parse(invocation));
        return true;
    }

    /// <summary>Executes the parsed command, routing every external effect through <paramref name="host"/>.</summary>
    /// <returns>A task whose result is the awk process exit status.</returns>
    public Task<AwkExitCode> ExecuteAsync(IAwkHost host, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        return AwkExecutor.ExecuteAsync(host, _invocation, cancellationToken);
    }

    /// <summary>Executes the parsed command under an explicit capability and resource policy.</summary>
    public Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkExecutionPolicy policy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(policy);
        return policy.ExecutionTimeout is { } timeout
            ? ExecuteWithTimeoutAsync(host, policy, timeout, cancellationToken)
            : AwkExecutor.ExecuteAsync(
                host,
                _invocation,
                cancellationToken,
                AwkRuntimeOptions.Create(policy));
    }

    private async Task<AwkExitCode> ExecuteWithTimeoutAsync(
        IAwkHost host,
        AwkExecutionPolicy policy,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var executionSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        try
        {
            return await AwkExecutor.ExecuteAsync(
                    host,
                    _invocation,
                    executionSource.Token,
                    AwkRuntimeOptions.Create(policy))
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            try
            {
                _ = await host.AppendAsync(
                        _invocation.Stdio.StdErr,
                        AwkMessages.ErrorExecutionTimeout,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // A host failure while reporting the timeout cannot expose host exception details.
            }

            return AwkExitCode.FatalError;
        }
    }
}
