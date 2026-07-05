using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal readonly struct AwkLoweringOutcome<T>
{
    private AwkLoweringOutcome(
        bool success,
        object value,
        IReadOnlyList<AwkDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(value);
        Succeeded = success;
        _value = value;
        Diagnostics = diagnostics;
    }

    private readonly object _value;

    public bool Succeeded { get; }

    public T Value =>
        Succeeded
            ? (T)_value
            : throw new InvalidOperationException("No lowered value is present.");

    public IReadOnlyList<AwkDiagnostic> Diagnostics { get; }

    public static AwkLoweringOutcome<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new AwkLoweringOutcome<T>(true, value, []);
    }

    public static AwkLoweringOutcome<T> Failure(IReadOnlyList<AwkDiagnostic> diagnostics) =>
        new(false, NoLoweredValue.Instance, diagnostics);

    public static AwkLoweringOutcome<T> Failure(AwkDiagnostic diagnostic) =>
        Failure([diagnostic]);

    private sealed class NoLoweredValue
    {
        public static readonly NoLoweredValue Instance = new();
    }
}
