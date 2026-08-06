using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal abstract class AwkLoweringOutcome<T>
{
    public abstract bool Succeeded { get; }

    public abstract T Value { get; }

    public abstract IReadOnlyList<AwkDiagnostic> Diagnostics { get; }

    public static AwkLoweringOutcome<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Successful(value);
    }

    public static AwkLoweringOutcome<T> Failure(IReadOnlyList<AwkDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
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
