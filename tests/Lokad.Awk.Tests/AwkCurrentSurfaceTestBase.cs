
namespace Lokad.Awk.Tests;

public abstract class AwkCurrentSurfaceTestBase
{
    protected static AwkCommandInvocation BuildInvocation(string command, params string[] arguments)
    {
        return AwkTestHarness.BuildInvocation(command, arguments);
    }
}
