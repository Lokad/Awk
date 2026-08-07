using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal abstract class AwkLoweringOutcome<T>
{
    /// <summary>Gets whether lowering produced a usable value.</summary>
    public abstract bool Succeeded { get; }

    /// <summary>Gets the lowered value; failed implementations must reject access.</summary>
    public abstract T Value { get; }

    /// <summary>Gets diagnostics produced by failed lowering, or an empty collection on success.</summary>
    public abstract IReadOnlyList<AwkDiagnostic> Diagnostics { get; }

    public static AwkLoweringOutcome<T> Success(T value)
    {
        return new Successful(value);
    }

    public static AwkLoweringOutcome<T> Failure(IReadOnlyList<AwkDiagnostic> diagnostics)
    {
        return new Failed(diagnostics);
    }

    public static AwkLoweringOutcome<T> Failure(AwkDiagnostic diagnostic) =>
        Failure([diagnostic]);

    private sealed class Successful(T value) : AwkLoweringOutcome<T>
    {
        public override bool Succeeded => true;

        public override T Value { get; } = value;

        public override IReadOnlyList<AwkDiagnostic> Diagnostics { get; } = [];
    }

    private sealed class Failed(IReadOnlyList<AwkDiagnostic> diagnostics) : AwkLoweringOutcome<T>
    {
        public override bool Succeeded => false;

        public override T Value => throw new InvalidOperationException("No lowered value is present.");

        public override IReadOnlyList<AwkDiagnostic> Diagnostics { get; } = diagnostics;
    }
}
