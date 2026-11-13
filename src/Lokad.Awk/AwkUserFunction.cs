using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkUserFunction
{
    // Preparation finishes before the program is published to any execution.
    internal AwkRegion? Region { get; private set; }
    internal bool MayInvokeHost { get; private set; } = true;
    internal bool ReturnsStringLength { get; private set; }
    internal void PrepareStringLengthReturn() => ReturnsStringLength = true;
    internal void PrepareHostEffects(bool mayInvokeHost) => MayInvokeHost = mayInvokeHost;

    internal void PrepareRegion(AwkRegion region)
    {
        Region = region;
    }

    internal AwkUserFunction(AwkFunctionName name, AwkFunctionLocalLayout localLayout)
    {
        FunctionName = name;
        LocalLayout = localLayout;
    }

    public AwkFunctionName FunctionName { get; }

    public string Name => FunctionName.Text;

    public AwkFunctionLocalLayout LocalLayout { get; }

    public IReadOnlyList<AwkVariableName> Parameters => LocalLayout.Parameters;
}
