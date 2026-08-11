using System.Collections.Generic;

namespace Lokad.Awk;

internal abstract class AwkBalancedTokenGroup
{
    protected AwkBalancedTokenGroup(
        AwkToken openToken,
        AwkToken closeToken,
        IReadOnlyList<AwkToken> innerTokens)
    {
        OpenToken = openToken;
        CloseToken = closeToken;
        InnerTokens = innerTokens;
        FullTokens = AwkTokenNavigation.Flatten([[openToken], innerTokens, [closeToken]]);
    }

    public AwkToken OpenToken { get; }

    public AwkToken CloseToken { get; }

    public IReadOnlyList<AwkToken> InnerTokens { get; }

    public IReadOnlyList<AwkToken> FullTokens { get; }
}
