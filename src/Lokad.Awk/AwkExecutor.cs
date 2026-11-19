namespace Lokad.Awk;

internal static class AwkExecutor
{
    internal static async Task<AwkExitCode> ExecuteAsync(
        IAwkHost host, AwkCommandInvocation command, CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stderr = command.StdErr;
        var applicationHost = host;
        AwkExecutionMemoryOwner owner;
        try { owner = AwkExecutionMemoryOwner.Create(runtimeOptions.MemoryBudget, cancellationToken); }
        catch (AwkRuntimeException exception)
        {
            return await ReportRuntimeFailureAsync(
                new AwkExecutionHost(applicationHost, AwkExecutionMemoryOwner.Disabled),
                stderr, runtimeOptions, exception, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await ReportUnexpectedFailureAsync(
                new AwkExecutionHost(applicationHost, AwkExecutionMemoryOwner.Disabled),
                stderr, runtimeOptions, exception, cancellationToken)
                .ConfigureAwait(false);
        }
        host = new AwkExecutionHost(applicationHost, owner);
        var reported = false;
        var observedInternalFailure = false;
        try
        {
            await using (owner.ConfigureAwait(false))
            {
                try
                {
                    var token = owner.Token(cancellationToken);
                    var resolved = AwkCommandParser.Parse(command, owner.FrontendMemory, token);
                    var options = owner.IsEnabled ? runtimeOptions with { MemoryOwner = owner } : runtimeOptions;
                    var result = await ExecuteBodyAsync(host, resolved, token, options).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    return result;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                { cancellationToken.ThrowIfCancellationRequested(); throw; }
                catch (AwkRuntimeException exception)
                {
                    reported = true;
                    return await ReportRuntimeFailureAsync(host, stderr, runtimeOptions, exception, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    reported = true;
                    observedInternalFailure = exception is not AwkHostException;
                    return await ReportUnexpectedFailureAsync(host, stderr, runtimeOptions, exception, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { cancellationToken.ThrowIfCancellationRequested(); throw; }
        catch (Exception exception)
        {
            // A broken cleanup callback must not replay a diagnostic or notify twice.
            cancellationToken.ThrowIfCancellationRequested();
            if (reported)
            {
                if (!observedInternalFailure && exception is not AwkHostException)
                    ObserveInternalFailure(runtimeOptions, exception);
                return AwkExitCode.FatalError;
            }
            return await ReportUnexpectedFailureAsync(
                new AwkExecutionHost(applicationHost, AwkExecutionMemoryOwner.Disabled),
                stderr, runtimeOptions, exception, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<AwkExitCode> ExecuteBodyAsync(
        IAwkHost host,
        AwkInvocation invocation,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions)
    {
        var stringEscapeWarnings = new AwkStringEscapeWarnings();
        var regexEscapeWarnings = new AwkRegexEscapeWarnings();

        async Task<AwkExitCode> AppendResultAsync(
            AwkFileDescriptor descriptor,
            ReadOnlyMemory<byte> content,
            AwkExitCode successExitCode)
        {
            cancellationToken.ThrowIfCancellationRequested();
            runtimeOptions.Counters.Add(AwkCounter.HostCalls);
            runtimeOptions.Counters.Add(AwkCounter.OutputBytes, content.Length);
            var status = await AwkHostBoundaryPolicy.AwaitAsync(
                    host.AppendAsync(descriptor, content, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            return status.IsAccepted
                ? successExitCode
                : AwkExitCode.FatalError;
        }

        try
        {
            var optionWarnings = invocation.GetOptionWarnings(cancellationToken, stringEscapeWarnings);
            foreach (var warning in optionWarnings)
            {
                var status = await AppendResultAsync(invocation.Stdio.StdErr,
                    AwkDiagnostics.Warning(warning), AwkExitCode.Success).ConfigureAwait(false);
                if (status != AwkExitCode.Success)
                    return status;
            }
            if (invocation.TerminalResult.IsHelp)
            {
                return await AppendResultAsync(
                    invocation.Stdio.StdOut,
                    AwkMessages.HelpMessage,
                    AwkExitCode.Success)
                .ConfigureAwait(false);
            }

            if (invocation.TerminalResult.IsVersion)
            {
                return await AppendResultAsync(
                        invocation.Stdio.StdOut,
                        AwkMessages.VersionMessage,
                        AwkExitCode.Success)
                    .ConfigureAwait(false);
            }

            if (invocation.TerminalResult.IsError)
            {
                return await AppendResultAsync(invocation.Stdio.StdErr, invocation.TerminalResult.ErrorMessage,
                    AwkExitCode.FatalError).ConfigureAwait(false);
            }

            var resolved = await ResolveProgramAsync(
                    host,
                    invocation,
                    runtimeOptions,
                    stringEscapeWarnings,
                    regexEscapeWarnings,
                    cancellationToken)
                .ConfigureAwait(false);
            for (var index = 0; index < resolved.Warnings.Count; index++)
            {
                var warningExitCode = await AppendResultAsync(
                        invocation.Stdio.StdErr,
                        AwkDiagnostics.Warning(resolved.Warnings[index]),
                        AwkExitCode.Success)
                    .ConfigureAwait(false);
                if (warningExitCode != AwkExitCode.Success)
                    return warningExitCode;
                stringEscapeWarnings.Record(resolved.Warnings[index].StringEscapeWarning);
                regexEscapeWarnings.Record(resolved.Warnings[index].RegexEscapeWarning);
            }

            if (resolved is AwkProgramResolution.Failed failed)
            {
                return await AppendResultAsync(
                        invocation.Stdio.StdErr,
                        failed.Diagnostic.Message,
                        AwkExitCode.FatalError)
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
    }

    private static void ObserveInternalFailure(AwkRuntimeOptions options, Exception exception)
    {
        try { options.InternalFailureObserver?.Invoke(exception); }
        catch { /* Observability must not change command completion or cleanup. */ }
    }

    private static async Task<AwkExitCode> ReportUnexpectedFailureAsync(
        IAwkHost host, AwkFileDescriptor stderr, AwkRuntimeOptions options,
        Exception exception, CancellationToken cancellationToken)
    {
        var external = exception is AwkHostException;
        if (!external) ObserveInternalFailure(options, exception);
        var message = external ? AwkMessages.ErrorUnexpectedFailure : AwkMessages.ErrorInternalFailure;
        try
        {
            options.Counters.Add(AwkCounter.HostCalls, 1);
            options.Counters.Add(AwkCounter.OutputBytes, message.Length);
            _ = await AwkHostBoundaryPolicy.AwaitAsync(host.AppendAsync(stderr, message, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        catch { cancellationToken.ThrowIfCancellationRequested(); }
        return AwkExitCode.FatalError;
    }

    private static async Task<AwkExitCode> ReportRuntimeFailureAsync(
        IAwkHost host, AwkFileDescriptor stderr, AwkRuntimeOptions options,
        AwkRuntimeException exception, CancellationToken cancellationToken)
    {
        if (exception is AwkHostAppendException append && append.Descriptor == stderr)
            return AwkExitCode.FatalError;
        var message = exception.MessageBytes;
        options.Counters.Add(AwkCounter.HostCalls, 1);
        options.Counters.Add(AwkCounter.OutputBytes, message.Length);
        try
        {
            _ = await AwkHostBoundaryPolicy.AwaitAsync(
                host.AppendAsync(stderr, message, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        return AwkExitCode.FatalError;
    }

    private static async Task<AwkProgramResolution> ResolveProgramAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkRuntimeOptions options,
        AwkStringEscapeWarnings stringEscapeWarnings,
        AwkRegexEscapeWarnings regexEscapeWarnings,
        CancellationToken cancellationToken)
    {
        var sourceLoad = await AwkSourceLoader.LoadAsync(host, invocation, options, cancellationToken)
            .ConfigureAwait(false);
        if (sourceLoad is AwkSourceLoadResult.Failed failed)
        {
            return AwkProgramResolution.Fail(failed.Diagnostic);
        }

        if (sourceLoad is not AwkSourceLoadResult.Loaded loaded)
            throw new System.InvalidOperationException("Unexpected awk source load result.");

        var source = loaded.CompilationSource;
        cancellationToken.ThrowIfCancellationRequested();
        var parsed = AwkParser.Parse(
            source.Text, new AwkFrontendBudget(options.Limits.MaxSyntaxNestingDepth, stringEscapeWarnings, regexEscapeWarnings)
            {
                Memory = options.MemoryOwner.FrontendMemory,
                RegexEngine = options.RegexEngine,
                RegexCancellationToken = cancellationToken
            },
            cancellationToken,
            invocation.ExecutionOptions.CompatibilityProfile.Mode);
        var warnings = AwkSourceDiagnosticMap.MapDiagnostics(source, parsed.Warnings, cancellationToken.ThrowIfCancellationRequested);
        cancellationToken.ThrowIfCancellationRequested();
        var binding = AwkBinder.Bind(parsed, invocation.ExecutionOptions.CompatibilityProfile,
            cancellationToken.ThrowIfCancellationRequested, invocation.RuntimeArgumentsPlan, options.MemoryOwner.FrontendMemory);
        cancellationToken.ThrowIfCancellationRequested();
        if (!binding.Success)
        {
            return AwkProgramResolution.Fail(
                AwkDiagnostics.Parse(AwkSourceDiagnosticMap.MapDiagnostics(source, binding.Diagnostics, cancellationToken.ThrowIfCancellationRequested)),
                warnings);
        }

        return AwkProgramResolution.Success(binding.Program, warnings);
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
