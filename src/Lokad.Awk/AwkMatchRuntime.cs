namespace Lokad.Awk;

// AWK result publication; the supplied provider owns matching and selection.
internal static class AwkMatchRuntime
{
    internal static int Execute(AwkRuntimeState state, AwkText input, AwkRegexPattern pattern, AwkArrayTarget? capture)
    {
        state.ThrowIfCancellationRequested();
        var regex = state.CreateRegex(pattern);
        if (capture is { } target)
        {
            using var leased = state.MatchRegexCaptureValue(regex, input);
            var match = leased.Match;
            state.ReplaceMatchCaptureArray(target, leased);
            return PublishPosition(state, match.Success, match.Index, match.Length);
        }
        var position = state.MatchRegexPosition(regex, input);
        return PublishPosition(state, position.Success, position.Index, position.Length);
    }

    private static int PublishPosition(AwkRuntimeState state, bool success, int index, int length)
    {
        var start = success ? index + 1 : 0;
        state.AssignVariableValue(new AwkSpecialVariableName(AwkSpecialVariableId.RegexStart), AwkValue.FromNumber(start));
        state.AssignVariableValue(new AwkSpecialVariableName(AwkSpecialVariableId.RegexLength), AwkValue.FromNumber(success ? length : -1));
        return start;
    }
}
