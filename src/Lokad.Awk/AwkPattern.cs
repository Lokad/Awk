using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract class AwkPattern
{
    public static AwkPattern Regex(AwkRegex regex) => new RegexPattern(regex);

    public static AwkPattern Range(AwkRegex start, AwkRegex end) => new RangePattern(start, end);

    public static AwkPattern Range(AwkCondition start, AwkCondition end) => new ConditionRangePattern(start, end);

    /// <summary>
    /// Returns whether the current input record should execute the associated action and updates per-run pattern state.
    /// </summary>
    public abstract ValueTask<bool> MatchesAsync(AwkRuntimeState state);

    private sealed class RegexPattern(AwkRegex regex) : AwkPattern
    {
        public override ValueTask<bool> MatchesAsync(AwkRuntimeState state) =>
            ValueTask.FromResult(state.IsRegexMatch(regex, state.CurrentRecordByteText));
    }

    private sealed class RangePattern(AwkRegex start, AwkRegex end) : AwkPattern
    {
        public override ValueTask<bool> MatchesAsync(AwkRuntimeState state)
        {
            var line = state.CurrentRecordByteText;
            var active = state.IsRangePatternActive(this);
            if (!active && state.IsRegexMatch(start, line))
            {
                active = true;
                state.SetRangePatternActive(this, active: true);
            }

            var matches = active;
            if (active && state.IsRegexMatch(end, line))
            {
                state.SetRangePatternActive(this, active: false);
            }

            return ValueTask.FromResult(matches);
        }
    }

    private sealed class ConditionRangePattern(AwkCondition start, AwkCondition end) : AwkPattern
    {
        public override async ValueTask<bool> MatchesAsync(AwkRuntimeState state)
        {
            var context = new AwkEvaluationContext(state);
            var active = state.IsRangePatternActive(this);
            if (!active && await start.EvaluateAsync(context).ConfigureAwait(false))
            {
                active = true;
                state.SetRangePatternActive(this, active: true);
            }

            var matches = active;
            if (active && await end.EvaluateAsync(context).ConfigureAwait(false))
            {
                state.SetRangePatternActive(this, active: false);
            }

            return matches;
        }
    }
}
