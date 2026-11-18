using System.Collections.Concurrent;
using System.Reflection;
using Xunit.Sdk;

[assembly: Lokad.Awk.Tests.AwkInternalFailures]

namespace Lokad.Awk.Tests;

// The production observer ignores callback exceptions. Record instead, then assert
// outside the command boundary, including when the test itself fails or cancels.
public sealed class AwkInternalFailuresAttribute : BeforeAfterTestAttribute
{
    private static readonly AsyncLocal<ConcurrentQueue<Exception>?> Current = new();

    public override void Before(MethodInfo methodUnderTest) => Current.Value = new();

    internal static Action<Exception> CreateObserver()
    {
        var failures = Current.Value ??= new();
        return failures.Enqueue;
    }

    public override void After(MethodInfo methodUnderTest)
    {
        var failures = Current.Value;
        Current.Value = null;
        Assert.True(failures is null || failures.IsEmpty,
            "Unexpected library failure: " + string.Join("\n", failures ?? []));
    }
}
