using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static class AwkExecutor
{
    public static async Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        CancellationToken cancellationToken)
    {
        if (invocation.TerminalResult.IsHelp)
        {
            var status = await host.AppendAsync(
                    invocation.Stdio.StdOut,
                    AwkMessages.HelpMessage,
                    cancellationToken)
                .ConfigureAwait(false);
            return status.Succeeded
                ? AwkDiagnostics.SuccessExitCode
                : AwkDiagnostics.FatalExitCode;
        }

        if (invocation.TerminalResult.IsVersion)
        {
            var status = await host.AppendAsync(
                    invocation.Stdio.StdOut,
                    AwkMessages.VersionMessage,
                    cancellationToken)
                .ConfigureAwait(false);
            return status.Succeeded
                ? AwkDiagnostics.SuccessExitCode
                : AwkDiagnostics.FatalExitCode;
        }

        if (invocation.TerminalResult.IsError)
        {
            var failure = AwkDiagnostics.Invocation(
                invocation.TerminalResult.ErrorMessage,
                invocation.TerminalResult.ErrorId);
            var status = await host.AppendAsync(
                    invocation.Stdio.StdErr,
                    failure.Message,
                    cancellationToken)
                .ConfigureAwait(false);
            return status.Succeeded
                ? failure.ExitCode
                : AwkDiagnostics.FatalExitCode;
        }

        var resolved = await ResolveProgramAsync(host, invocation, cancellationToken).ConfigureAwait(false);
        for (var index = 0; index < resolved.Warnings.Count; index++)
        {
            var status = await host.AppendAsync(
                    invocation.Stdio.StdErr,
                    AwkDiagnostics.Warning(resolved.Warnings[index]),
                    cancellationToken)
                .ConfigureAwait(false);
            if (status.Failed)
                return AwkDiagnostics.FatalExitCode;
        }

        if (resolved is AwkProgramResolution.Failed failed)
        {
            var status = await host.AppendAsync(
                    invocation.Stdio.StdErr,
                    failed.Diagnostic.Message,
                    cancellationToken)
                .ConfigureAwait(false);
            return status.Succeeded
                ? failed.Diagnostic.ExitCode
                : AwkDiagnostics.FatalExitCode;
        }

        if (resolved is not AwkProgramResolution.ResolvedProgram resolvedProgram)
            throw new System.InvalidOperationException("Unexpected awk program resolution.");

        return await AwkRuntime.ExecuteAsync(host, invocation, resolvedProgram.Program, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static Task<AwkCompiledProgram> CompileAsync(
        IAwkHost host,
        AwkCommandInvocation command,
        CancellationToken cancellationToken) =>
        CompileAsync(host, AwkCommandParser.Parse(command), cancellationToken);

    internal static async Task<AwkCompiledProgram> CompileAsync(
        IAwkHost host,
        AwkInvocation invocation,
        CancellationToken cancellationToken)
    {
        if (invocation.TerminalResult.IsHelp || invocation.TerminalResult.IsVersion)
            throw new System.InvalidOperationException("Terminal awk invocations cannot be precompiled.");

        if (invocation.TerminalResult.IsError)
            throw new System.InvalidOperationException(AwkUtf8Codec.Decode(invocation.TerminalResult.ErrorMessage));

        var resolved = await ResolveProgramAsync(host, invocation, cancellationToken).ConfigureAwait(false);
        if (resolved is AwkProgramResolution.Failed failed)
            throw new System.InvalidOperationException(AwkUtf8Codec.Decode(failed.Diagnostic.Message));

        if (resolved is not AwkProgramResolution.ResolvedProgram resolvedProgram)
            throw new System.InvalidOperationException("Unexpected awk program resolution.");

        return new AwkCompiledProgram(
            invocation,
            resolvedProgram.Program,
            AwkExecutableProgram.Compile(resolvedProgram.Program),
            resolved.Warnings);
    }

    private static async Task<AwkProgramResolution> ResolveProgramAsync(
        IAwkHost host,
        AwkInvocation invocation,
        CancellationToken cancellationToken)
    {
        var sourceLoad = await AwkSourceLoader.LoadAsync(host, invocation, cancellationToken).ConfigureAwait(false);
        if (sourceLoad is AwkSourceLoadResult.Failed failed)
        {
            return AwkProgramResolution.Fail(AwkDiagnostics.SourceLoad(failed.ErrorMessage, failed.Id));
        }

        if (sourceLoad is not AwkSourceLoadResult.Loaded loaded)
            throw new System.InvalidOperationException("Unexpected awk source load result.");

        var sources = loaded.Sources;
        var programs = new List<AwkProgram>(sources.Count);
        var warnings = new List<AwkDiagnostic>();
        for (var index = 0; index < sources.Count; index++)
        {
            var source = sources[index];
            var parsed = AwkParser.Parse(source.Text);
            warnings.AddRange(AwkSourceDiagnosticMap.MapDiagnostics(source, parsed.Warnings));

            var binding = AwkBinder.Bind(parsed, invocation.ExecutionOptions.CompatibilityProfile);
            var phase = binding.PhaseResult;
            if (!phase.Success)
            {
                return AwkProgramResolution.Fail(
                    AwkDiagnostics.Parse(AwkSourceDiagnosticMap.MapDiagnostics(source, phase.Diagnostics)),
                    warnings);
            }

            var boundProgram = phase.Value;
            if (!boundProgram.HasProgram)
            {
                return AwkProgramResolution.Fail(
                    new AwkFailureDiagnostic(
                        AwkMessages.ErrorInvalidScript,
                        AwkDiagnostics.FatalExitCode,
                        AwkDiagnosticId.ParseSyntax),
                    warnings);
            }

            programs.Add(boundProgram.Program);
        }

        return AwkProgramResolution.Success(AwkProgram.Concat(programs), warnings);
    }

    private abstract class AwkProgramResolution
    {
        private AwkProgramResolution(IReadOnlyList<AwkDiagnostic> warnings)
        {
            Warnings = warnings;
        }

        public IReadOnlyList<AwkDiagnostic> Warnings { get; }

        public static ResolvedProgram Success(
            AwkProgram program,
            IReadOnlyList<AwkDiagnostic> warnings) =>
            new ResolvedProgram(program, warnings);

        public static Failed Fail(AwkFailureDiagnostic failure) =>
            new Failed(failure, []);

        public static Failed Fail(
            AwkFailureDiagnostic failure,
            IReadOnlyList<AwkDiagnostic> warnings) =>
            new Failed(failure, warnings);

        public sealed class ResolvedProgram(
            AwkProgram program,
            IReadOnlyList<AwkDiagnostic> warnings) : AwkProgramResolution(warnings)
        {
            public AwkProgram Program { get; } = program;
        }

        public sealed class Failed(
            AwkFailureDiagnostic failure,
            IReadOnlyList<AwkDiagnostic> warnings) : AwkProgramResolution(warnings)
        {
            public AwkFailureDiagnostic Diagnostic { get; } = failure;
        }
    }
}
