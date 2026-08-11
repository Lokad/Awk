using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static class AwkExecutor
{
    public static Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        CancellationToken cancellationToken) =>
        ExecuteAsync(host, invocation, cancellationToken, AwkRuntimeOptions.CreateSystemDefault());

    internal static async Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions)
    {
        var resources = new AwkRuntimeResourceMonitor(runtimeOptions.Limits, runtimeOptions.Counters);

        async Task<AwkExitCode> AppendResultAsync(
            AwkFileDescriptor descriptor,
            ReadOnlyMemory<byte> content,
            AwkExitCode successExitCode)
        {
            resources.RecordHostCall();
            resources.RecordOutputBytes(content.Length);
            var status = await host.AppendAsync(
                    descriptor,
                    content,
                    cancellationToken)
                .ConfigureAwait(false);
            return status.Succeeded
                ? successExitCode
                : AwkDiagnostics.FatalExitCode;
        }

        try
        {
            if (invocation.TerminalResult.IsHelp)
            {
                return await AppendResultAsync(
                    invocation.Stdio.StdOut,
                    AwkMessages.HelpMessage,
                    AwkDiagnostics.SuccessExitCode)
                .ConfigureAwait(false);
            }

            if (invocation.TerminalResult.IsVersion)
            {
                return await AppendResultAsync(
                        invocation.Stdio.StdOut,
                        AwkMessages.VersionMessage,
                        AwkDiagnostics.SuccessExitCode)
                    .ConfigureAwait(false);
            }

            if (invocation.TerminalResult.IsError)
            {
                var failure = AwkDiagnostics.Invocation(
                    invocation.TerminalResult.ErrorMessage,
                    invocation.TerminalResult.ErrorId);
                return await AppendResultAsync(invocation.Stdio.StdErr, failure.Message, failure.ExitCode)
                    .ConfigureAwait(false);
            }

            var resolved = await ResolveProgramAsync(
                    host,
                    invocation,
                    resources,
                    runtimeOptions.Limits.MaxSyntaxNestingDepth,
                    runtimeOptions.AccessiblePathRoot,
                    runtimeOptions.CleanupTimeout,
                    runtimeOptions.AllowSourceIncludes,
                    cancellationToken)
                .ConfigureAwait(false);
            for (var index = 0; index < resolved.Warnings.Count; index++)
            {
                var warningExitCode = await AppendResultAsync(
                        invocation.Stdio.StdErr,
                        AwkDiagnostics.Warning(resolved.Warnings[index]),
                        AwkDiagnostics.SuccessExitCode)
                    .ConfigureAwait(false);
                if (warningExitCode != AwkDiagnostics.SuccessExitCode)
                    return warningExitCode;
            }

            if (resolved is AwkProgramResolution.Failed failed)
            {
                return await AppendResultAsync(
                        invocation.Stdio.StdErr,
                        failed.Diagnostic.Message,
                        failed.Diagnostic.ExitCode)
                    .ConfigureAwait(false);
            }

            if (resolved is not AwkProgramResolution.ResolvedProgram resolvedProgram)
                throw new System.InvalidOperationException("Unexpected awk program resolution.");

            return await AwkRuntime.ExecuteAsync(
                    host,
                    invocation,
                    resolvedProgram.Program,
                    cancellationToken,
                    runtimeOptions,
                    executableProgram: null,
                    resources)
                .ConfigureAwait(false);
        }
        catch (AwkRuntimeException exception)
        {
            var failure = AwkDiagnostics.Runtime(exception);
            runtimeOptions.Counters.RecordHostCall();
            runtimeOptions.Counters.RecordOutputBytes(failure.Message.Length);
            try
            {
                var appendStatus = await host.AppendAsync(
                        invocation.Stdio.StdErr,
                        failure.Message,
                        cancellationToken)
                    .ConfigureAwait(false);
                return appendStatus.Succeeded
                    ? failure.ExitCode
                    : AwkDiagnostics.FatalExitCode;
            }
            catch (Exception)
            {
                return AwkDiagnostics.FatalExitCode;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            try
            {
                _ = await host.AppendAsync(
                        invocation.Stdio.StdErr,
                        AwkMessages.ErrorUnexpectedFailure,
                        cancellationToken)
                    .ConfigureAwait(false);
                return AwkDiagnostics.FatalExitCode;
            }
            catch (Exception)
            {
                return AwkDiagnostics.FatalExitCode;
            }
        }
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
        if (invocation.TerminalResult.IsTerminal)
        {
            if (invocation.TerminalResult.IsError)
            {
                throw new System.InvalidOperationException(
                    AwkUtf8Codec.Decode(invocation.TerminalResult.ErrorMessage));
            }

            throw new System.InvalidOperationException("Terminal awk invocations cannot be precompiled.");
        }

        var runtimeOptions = AwkRuntimeOptions.CreateSystemDefault();
        var resources = new AwkRuntimeResourceMonitor(runtimeOptions.Limits, runtimeOptions.Counters);
        var resolved = await ResolveProgramAsync(
                host,
                invocation,
                resources,
                runtimeOptions.Limits.MaxSyntaxNestingDepth,
                runtimeOptions.AccessiblePathRoot,
                runtimeOptions.CleanupTimeout,
                runtimeOptions.AllowSourceIncludes,
                cancellationToken)
            .ConfigureAwait(false);
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
        AwkRuntimeResourceMonitor resources,
        int maxSyntaxNestingDepth,
        AwkPath? accessiblePathRoot,
        TimeSpan cleanupTimeout,
        bool allowSourceIncludes,
        CancellationToken cancellationToken)
    {
        var sourceLoad = await AwkSourceLoader.LoadAsync(
                host,
                invocation,
                resources,
                cancellationToken,
                accessiblePathRoot,
                cleanupTimeout,
                allowSourceIncludes)
            .ConfigureAwait(false);
        if (sourceLoad is AwkSourceLoadResult.Failed failed)
        {
            return AwkProgramResolution.Fail(AwkDiagnostics.SourceLoad(failed.ErrorMessage, failed.Id));
        }

        if (sourceLoad is not AwkSourceLoadResult.Loaded loaded)
            throw new System.InvalidOperationException("Unexpected awk source load result.");

        var source = loaded.CompilationSource;
        resources.ThrowIfFrontendCharactersExceeded(source.Text.Length);
        cancellationToken.ThrowIfCancellationRequested();
        var parsed = AwkParser.Parse(
            source.Text,
            maxSyntaxNestingDepth,
            resources.Limits.MaxFrontendTokens,
            resources.Limits.MaxFrontendDiagnostics,
            cancellationToken);
        var warnings = AwkSourceDiagnosticMap.MapDiagnostics(source, parsed.Warnings);
        cancellationToken.ThrowIfCancellationRequested();
        var binding = AwkBinder.Bind(parsed, invocation.ExecutionOptions.CompatibilityProfile);
        cancellationToken.ThrowIfCancellationRequested();
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

        return AwkProgramResolution.Success(boundProgram.Program, warnings);
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
