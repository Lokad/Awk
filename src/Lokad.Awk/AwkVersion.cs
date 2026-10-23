using System.Reflection;

namespace Lokad.Awk;

internal static class AwkVersion
{
    public static string Text { get; } = Read();

    private static string Read()
    {
        var version = typeof(Awk).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";
        var metadata = version.IndexOf('+');
        return metadata < 0 ? version : version[..metadata];
    }
}
