using System;

namespace Lokad.Awk;

internal static class AwkDisposable
{
    public static void DisposeIfPresent(object value)
    {
        if (value is IDisposable disposable)
            disposable.Dispose();
    }
}
