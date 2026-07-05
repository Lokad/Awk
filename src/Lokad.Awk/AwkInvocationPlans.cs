using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed record AwkInvocationPlan(
    AwkSourcePlan Source,
    AwkInputPlan Input,
    AwkRuntimeArgumentPlan RuntimeArguments);

internal sealed class AwkSourcePlan
{
    public AwkSourcePlan(IReadOnlyList<AwkSourceReference> sources)
    {
        Sources = sources;
    }

    public IReadOnlyList<AwkSourceReference> Sources { get; }
}

internal sealed record AwkInputPlan(
    IReadOnlyList<AwkResolvedPath> Paths,
    IReadOnlyList<AwkInputArgument> InputArguments);

internal sealed record AwkRuntimeArgumentPlan(
    IReadOnlyList<string> RuntimeArguments,
    IReadOnlyList<AwkVariableAssignment> Variables,
    IReadOnlyList<AwkEnvironmentVariable> Environment,
    AwkPath CurrentDirectory);

internal readonly record struct AwkStdioPlan(
    AwkFileDescriptor StdIn,
    AwkFileDescriptor StdOut,
    AwkFileDescriptor StdErr);

internal sealed record AwkExecutionOptions(
    AwkFieldSeparatorSpec FieldSeparator,
    AwkCompatibilityProfile CompatibilityProfile);

internal enum AwkTerminalInvocationKind
{
    None,
    Help,
    Version,
    Error
}

internal readonly record struct AwkTerminalInvocationResult
{
    private readonly ReadOnlyMemory<byte> _errorMessage;
    private readonly AwkDiagnosticId _errorId;

    private AwkTerminalInvocationResult(
        AwkTerminalInvocationKind kind,
        ReadOnlyMemory<byte> errorMessage,
        AwkDiagnosticId errorId)
    {
        Kind = kind;
        _errorMessage = errorMessage;
        _errorId = errorId;
    }

    public static AwkTerminalInvocationResult None => default;

    public static AwkTerminalInvocationResult Help { get; } =
        new(AwkTerminalInvocationKind.Help, ReadOnlyMemory<byte>.Empty, AwkDiagnosticId.Unknown);

    public static AwkTerminalInvocationResult Version { get; } =
        new(AwkTerminalInvocationKind.Version, ReadOnlyMemory<byte>.Empty, AwkDiagnosticId.Unknown);

    public static AwkTerminalInvocationResult Error(ReadOnlyMemory<byte> message, AwkDiagnosticId id) =>
        new(AwkTerminalInvocationKind.Error, message, id);

    public AwkTerminalInvocationKind Kind { get; }

    public bool IsNone => Kind == AwkTerminalInvocationKind.None;

    public bool IsHelp => Kind == AwkTerminalInvocationKind.Help;

    public bool IsVersion => Kind == AwkTerminalInvocationKind.Version;

    public bool IsError => Kind == AwkTerminalInvocationKind.Error;

    public bool IsTerminal => Kind != AwkTerminalInvocationKind.None;

    public ReadOnlyMemory<byte> ErrorMessage =>
        IsError ? _errorMessage : ReadOnlyMemory<byte>.Empty;

    public AwkDiagnosticId ErrorId =>
        IsError ? _errorId : AwkDiagnosticId.Unknown;
}
