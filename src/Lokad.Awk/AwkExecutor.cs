namespace Lokad.Awk;

internal static class AwkExecutor
{
    internal static async Task<AwkExitCode> ExecuteAsync(
        IAwkHost host, AwkCommandInvocation command, CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stderr = command.StdErr;
        AwkExecutionMemoryOwner owner;
        try { owner = AwkExecutionMemoryOwner.Create(runtimeOptions.MemoryBudget, cancellationToken); }
        catch (AwkRuntimeException exception)
        {
            return await ReportRuntimeFailureAsync(host, stderr, runtimeOptions, exception, cancellationToken)
                .ConfigureAwait(false);
        }
        await using (owner.ConfigureAwait(false))
        {
            try
            {
                var token = owner.Token(cancellationToken);
                var resolved = AwkCommandParser.Parse(command, owner.FrontendMemory, token);
                var options = owner.IsEnabled ? runtimeOptions with { MemoryOwner = owner } : runtimeOptions;
                var result = await ExecuteBodyAsync(owner.IsEnabled ? new AwkBudgetHost(host, owner) : host,
                    resolved, token, options).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            catch (AwkRuntimeException exception)
            {
                return await ReportRuntimeFailureAsync(host, stderr, runtimeOptions, exception, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private static async Task<AwkExitCode> ExecuteBodyAsync(
        IAwkHost host,
        AwkInvocation invocation,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions)
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
                    cancellationToken)
                .ConfigureAwait(false);
            return status.IsAccepted
                ? successExitCode
                : AwkDiagnostics.FatalExitCode;
        }

        try
        {
            var optionWarnings = invocation.GetOptionWarnings(cancellationToken, stringEscapeWarnings);
            foreach (var warning in optionWarnings)
            {
                var status = await AppendResultAsync(invocation.Stdio.StdErr,
                    AwkDiagnostics.Warning(warning), AwkDiagnostics.SuccessExitCode).ConfigureAwait(false);
                if (status != AwkDiagnostics.SuccessExitCode)
                    return status;
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

            var resolved = await ResolveProgramAsync(
                    host,
                    invocation,
                    resources,
                    stringEscapeWarnings,
                    regexEscapeWarnings,
                    runtimeOptions.AccessiblePathRoot,
                    runtimeOptions.AllowSourceIncludes,
                    runtimeOptions.ExposeEnvironment,
                    runtimeOptions.ExposeHostErrorDetails,
                    runtimeOptions.MaxHostErrorBytes,
                    cancellationToken,
                    runtimeOptions.MemoryOwner.FrontendMemory, runtimeOptions.RegexEngine)
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
            return await ReportRuntimeFailureAsync(host, invocation.Stdio.StdErr, runtimeOptions, exception, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        IAwkHost host, AwkFileDescriptor stderr, AwkRuntimeOptions options,
        AwkRuntimeException exception, CancellationToken cancellationToken)
    {
        if (exception is AwkHostAppendException append && append.Descriptor == stderr)
            return AwkDiagnostics.FatalExitCode;
        var failure = AwkDiagnostics.Runtime(exception);
        options.Counters.RecordHostCall();
        options.Counters.RecordOutputBytes(failure.Message.Length);
        try
        {
            var status = await AwkHostBoundaryPolicy.AwaitAsync(
                    host.AppendAsync(stderr, failure.Message, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
            return status.IsAccepted ? failure.ExitCode : AwkDiagnostics.FatalExitCode;
        }
        catch (Exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return AwkDiagnostics.FatalExitCode;
        }
    }

    private static async Task<AwkProgramResolution> ResolveProgramAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkRuntimeResourceMonitor resources,
        AwkStringEscapeWarnings stringEscapeWarnings,
        AwkRegexEscapeWarnings regexEscapeWarnings,
        AwkPath? accessiblePathRoot,
        bool allowSourceIncludes,
        bool exposeEnvironment,
        bool exposeHostErrorDetails,
        int maxHostErrorBytes,
        CancellationToken cancellationToken,
        AwkFrontendMemory? memory = null, IAwkRegexEngine? regexEngine = null)
    {
        var sourceLoad = await AwkSourceLoader.LoadAsync(
                host,
                invocation,
                resources,
                cancellationToken,
                accessiblePathRoot,
                allowSourceIncludes,
                exposeEnvironment,
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
        cancellationToken.ThrowIfCancellationRequested();
        var parsed = AwkParser.Parse(
            source.Text, new AwkFrontendBudget(resources.Limits.MaxSyntaxNestingDepth, stringEscapeWarnings, regexEscapeWarnings)
            { Memory = memory ?? AwkFrontendMemory.Disabled, RegexEngine = regexEngine, RegexCancellationToken = cancellationToken },
            cancellationToken,
            invocation.ExecutionOptions.CompatibilityProfile.Mode);
        var warnings = AwkSourceDiagnosticMap.MapDiagnostics(source, parsed.Warnings, cancellationToken.ThrowIfCancellationRequested);
        cancellationToken.ThrowIfCancellationRequested();
        var binding = AwkBinder.Bind(parsed, invocation.ExecutionOptions.CompatibilityProfile,
            () => resources.ThrowIfCancellationRequested(cancellationToken), invocation.RuntimeArgumentsPlan);
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
