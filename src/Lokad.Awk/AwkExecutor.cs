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

    internal static Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions) =>
        ExecuteCoreAsync(host, invocation, cancellationToken, runtimeOptions, null);

    internal static Task<AwkExitCode> ExecuteCompiledAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkProgram program,
        IReadOnlyList<AwkDiagnostic> warnings,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions) =>
        ExecuteCoreAsync(
            host,
            invocation,
            cancellationToken,
            runtimeOptions,
            AwkProgramResolution.Success(program, warnings));

    private static async Task<AwkExitCode> ExecuteCoreAsync(
        IAwkHost host, AwkInvocation invocation, CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions, AwkProgramResolution? compiledResolution)
    {
        if (runtimeOptions.MemoryBudget is null)
            return await ExecuteBodyAsync(host, invocation, cancellationToken, runtimeOptions,
                compiledResolution).ConfigureAwait(false);

        AwkExecutionMemoryOwner owner;
        try
        {
            owner = AwkExecutionMemoryOwner.Create(runtimeOptions.MemoryBudget, cancellationToken);
        }
        catch (AwkRuntimeException exception)
        {
            return await ReportRuntimeFailureAsync(host, invocation, runtimeOptions, exception, cancellationToken)
                .ConfigureAwait(false);
        }
        await using (owner.ConfigureAwait(false))
            return await ExecuteBodyAsync(new AwkBudgetHost(host, owner), invocation,
                owner.Token(cancellationToken), runtimeOptions with { MemoryOwner = owner },
                compiledResolution).ConfigureAwait(false);
    }

    private static async Task<AwkExitCode> ExecuteBodyAsync(
        IAwkHost host,
        AwkInvocation invocation,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkProgramResolution? compiledResolution)
    {
        var resources = new AwkRuntimeResourceMonitor(runtimeOptions.Limits, runtimeOptions.Counters, cancellationToken);
        var stringEscapeWarnings = new AwkStringEscapeWarnings();
        var regexEscapeWarnings = new AwkRegexEscapeWarnings();

        async Task<AwkExitCode> AppendResultAsync(
            AwkFileDescriptor descriptor,
            ReadOnlyMemory<byte> content,
            AwkExitCode successExitCode)
        {
            resources.RecordHostCall();
            resources.RecordOutputBytes(content.Length);
            var status = await AwkHostBoundaryPolicy.AwaitAsync(
                    host.AppendAsync(descriptor, content, cancellationToken),
                    runtimeOptions.HostOperationTimeout,
                    cancellationToken)
                .ConfigureAwait(false);
            return status.Succeeded
                ? successExitCode
                : AwkDiagnostics.FatalExitCode;
        }

        try
        {
            var optionDiagnosticCount = 0;
            if (compiledResolution is null)
            {
                var optionWarnings = invocation.GetOptionWarnings(resources.Limits.MaxFrontendDiagnostics,
                    cancellationToken, stringEscapeWarnings);
                optionDiagnosticCount = optionWarnings.Count;
                foreach (var warning in optionWarnings)
                {
                    var status = await AppendResultAsync(invocation.Stdio.StdErr,
                        AwkDiagnostics.Warning(warning), AwkDiagnostics.SuccessExitCode).ConfigureAwait(false);
                    if (status != AwkDiagnostics.SuccessExitCode)
                        return status;
                }
            }
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

            var resolved = compiledResolution ?? await ResolveProgramAsync(
                    host,
                    invocation,
                    resources,
                    optionDiagnosticCount,
                    stringEscapeWarnings,
                    regexEscapeWarnings,
                    runtimeOptions.AccessiblePathRoot,
                    runtimeOptions.CleanupTimeout,
                    runtimeOptions.AllowSourceIncludes,
                    runtimeOptions.ExposeEnvironment,
                    runtimeOptions.HostOperationTimeout,
                    runtimeOptions.ExposeHostErrorDetails,
                    runtimeOptions.MaxHostErrorBytes,
                    cancellationToken,
                    runtimeOptions.MemoryOwner.FrontendMemory)
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
                stringEscapeWarnings.Record(resolved.Warnings[index].StringEscapeWarning);
                regexEscapeWarnings.Record(resolved.Warnings[index].RegexEscapeWarning);
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
                    resources,
                    stringEscapeWarnings,
                    regexEscapeWarnings)
                .ConfigureAwait(false);
        }
        catch (AwkRuntimeException exception)
        {
            return await ReportRuntimeFailureAsync(host, invocation, runtimeOptions, exception, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            try
            {
                _ = await AwkHostBoundaryPolicy.AwaitAsync(
                        host.AppendAsync(
                            invocation.Stdio.StdErr,
                            AwkMessages.ErrorUnexpectedFailure,
                            cancellationToken),
                        runtimeOptions.HostOperationTimeout,
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

    private static async Task<AwkExitCode> ReportRuntimeFailureAsync(
        IAwkHost host, AwkInvocation invocation, AwkRuntimeOptions options,
        AwkRuntimeException exception, CancellationToken cancellationToken)
    {
        var failure = AwkDiagnostics.Runtime(exception);
        options.Counters.RecordHostCall();
        options.Counters.RecordOutputBytes(failure.Message.Length);
        try
        {
            var status = await AwkHostBoundaryPolicy.AwaitAsync(
                    host.AppendAsync(invocation.Stdio.StdErr, failure.Message, cancellationToken),
                    options.HostOperationTimeout, cancellationToken)
                .ConfigureAwait(false);
            return status.Succeeded ? failure.ExitCode : AwkDiagnostics.FatalExitCode;
        }
        catch (Exception)
        {
            return AwkDiagnostics.FatalExitCode;
        }
    }

    internal static Task<AwkCompiledProgram> CompileAsync(
        IAwkHost host,
        AwkCommandInvocation command,
        CancellationToken cancellationToken) =>
        CompileAsync(host, AwkCommandParser.Parse(command), cancellationToken, command.MemoryBudget);

    internal static async Task<AwkCompiledProgram> CompileAsync(
        IAwkHost host,
        AwkInvocation invocation,
        CancellationToken cancellationToken,
        IMemoryBudget? memoryBudget = null)
    {
        var owner = AwkExecutionMemoryOwner.Create(memoryBudget, cancellationToken);
        try
        {
            return await CompileBodyAsync(owner.IsEnabled ? new AwkBudgetHost(host, owner) : host,
                invocation, owner.Token(cancellationToken), owner).ConfigureAwait(false);
        }
        catch
        {
            await owner.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<AwkCompiledProgram> CompileBodyAsync(
        IAwkHost host, AwkInvocation invocation, CancellationToken cancellationToken,
        AwkExecutionMemoryOwner owner)
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
        var resources = new AwkRuntimeResourceMonitor(runtimeOptions.Limits, runtimeOptions.Counters, cancellationToken);
        var stringEscapeWarnings = new AwkStringEscapeWarnings();
        var regexEscapeWarnings = new AwkRegexEscapeWarnings();
        var optionWarnings = invocation.GetOptionWarnings(resources.Limits.MaxFrontendDiagnostics,
            cancellationToken, stringEscapeWarnings);
        var resolved = await ResolveProgramAsync(
                host,
                invocation,
                resources,
                optionWarnings.Count,
                stringEscapeWarnings,
                regexEscapeWarnings,
                runtimeOptions.AccessiblePathRoot,
                runtimeOptions.CleanupTimeout,
                runtimeOptions.AllowSourceIncludes,
                runtimeOptions.ExposeEnvironment,
                runtimeOptions.HostOperationTimeout,
                runtimeOptions.ExposeHostErrorDetails,
                runtimeOptions.MaxHostErrorBytes,
                cancellationToken,
                owner.FrontendMemory)
            .ConfigureAwait(false);
        if (resolved is AwkProgramResolution.Failed failed)
            throw new System.InvalidOperationException(AwkUtf8Codec.Decode(failed.Diagnostic.Message));

        if (resolved is not AwkProgramResolution.ResolvedProgram resolvedProgram)
            throw new System.InvalidOperationException("Unexpected awk program resolution.");

        return new AwkCompiledProgram(
            invocation,
            resolvedProgram.Program,
            [.. optionWarnings, .. resolved.Warnings], owner);
    }

    private static async Task<AwkProgramResolution> ResolveProgramAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkRuntimeResourceMonitor resources,
        int previousDiagnostics,
        AwkStringEscapeWarnings stringEscapeWarnings,
        AwkRegexEscapeWarnings regexEscapeWarnings,
        AwkPath? accessiblePathRoot,
        TimeSpan cleanupTimeout,
        bool allowSourceIncludes,
        bool exposeEnvironment,
        TimeSpan hostOperationTimeout,
        bool exposeHostErrorDetails,
        int maxHostErrorBytes,
        CancellationToken cancellationToken,
        AwkFrontendMemory? memory = null)
    {
        var sourceLoad = await AwkSourceLoader.LoadAsync(
                host,
                invocation,
                resources,
                cancellationToken,
                accessiblePathRoot,
                cleanupTimeout,
                allowSourceIncludes,
                exposeEnvironment,
                hostOperationTimeout,
                exposeHostErrorDetails,
                maxHostErrorBytes,
                memory)
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
            AwkFrontendBudget.FromResources(resources, previousDiagnostics, stringEscapeWarnings, regexEscapeWarnings, memory),
            cancellationToken,
            invocation.ExecutionOptions.CompatibilityProfile.Mode);
        var warnings = AwkSourceDiagnosticMap.MapDiagnostics(source, parsed.Warnings, cancellationToken.ThrowIfCancellationRequested);
        cancellationToken.ThrowIfCancellationRequested();
        var binding = AwkBinder.Bind(parsed, invocation.ExecutionOptions.CompatibilityProfile,
            () => resources.ThrowIfCancellationRequested(cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
        var phase = binding.PhaseResult;
        if (!phase.Success)
        {
            return AwkProgramResolution.Fail(
                AwkDiagnostics.Parse(AwkSourceDiagnosticMap.MapDiagnostics(source, phase.Diagnostics, cancellationToken.ThrowIfCancellationRequested)),
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
