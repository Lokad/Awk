using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lokad.Awk;

/// <summary>Width facts before character atoms expand into UTF-16 scalar expressions.</summary>
internal readonly record struct AwkRegexSelection(bool RequiresLongest, int MaximumScalars,
    bool RequiresCaptureSelection = false)
{
    public static AwkRegexSelection Analyze(string pattern)
    {
        var frames = new Stack<Frame>();
        var frame = new Frame();
        for (var index = 0; index < pattern.Length; index++)
        {
            switch (pattern[index])
            {
                case '[':
                    var end = BracketEnd(pattern, index);
                    if (end < 0)
                        return default;
                    index = end;
                    frame.Add(Width.Character);
                    break;
                case '\\':
                    if (++index >= pattern.Length)
                        return default;
                    var escaped = pattern[index];
                    // Capture-dependent and search-offset extensions are not awk EREs.
                    if (escaped is 'k' or 'G' || char.IsAsciiDigit(escaped))
                        return default;
                    if (escaped is 'p' or 'P' && index + 1 < pattern.Length && pattern[index + 1] == '{')
                    {
                        var close = pattern.IndexOf('}', index + 2);
                        if (close < 0)
                            return default;
                        index = close;
                    }
                    else if (escaped is 'u' or 'x')
                    {
                        var digits = escaped == 'u' ? 4 : 2;
                        index = Math.Min(pattern.Length - 1, index + digits);
                    }
                    frame.Add(escaped is 'A' or 'z' or 'Z' or 'b' or 'B' ? Width.Empty : Width.Character);
                    break;
                case '(':
                    var captures = index + 1 >= pattern.Length || pattern[index + 1] != '?';
                    var zeroWidth = false;
                    if (index + 1 < pattern.Length && pattern[index + 1] == '?')
                    {
                        if (!TryReadGroupHeader(pattern, ref index, out zeroWidth))
                            return default;
                        if (pattern[index] == ')')
                        {
                            frame.Add(Width.Empty);
                            break;
                        }
                    }
                    frames.Push(frame);
                    frame = new Frame { ZeroWidth = zeroWidth, Captures = captures };
                    break;
                case ')':
                    if (frames.Count == 0)
                        return default;
                    var group = frame.Finish();
                    if (frame.ZeroWidth)
                        group = Width.Empty;
                    else if (frame.Captures)
                        group = group with { HasCapture = true, NestedCapture = group.HasCapture || group.NestedCapture };
                    frame = frames.Pop();
                    frame.Add(group);
                    break;
                case '|':
                    frame.NextAlternative();
                    break;
                case '^':
                case '$':
                    frame.Add(Width.Empty);
                    break;
                case '*':
                case '+':
                case '?':
                    var minimum = pattern[index] == '+' ? 1 : 0;
                    var maximum = pattern[index] == '?' ? 1 : int.MaxValue;
                    var lazy = index + 1 < pattern.Length && pattern[index + 1] == '?';
                    if (lazy)
                        index++;
                    frame.Repeat(minimum, maximum, lazy);
                    break;
                case '{':
                    if (TryReadInterval(pattern, ref index, out minimum, out maximum))
                    {
                        lazy = index + 1 < pattern.Length && pattern[index + 1] == '?';
                        if (lazy)
                            index++;
                        frame.Repeat(minimum, maximum, lazy);
                    }
                    else
                        frame.Add(Width.Character);
                    break;
                default:
                    if (char.IsHighSurrogate(pattern[index]) && index + 1 < pattern.Length &&
                        char.IsLowSurrogate(pattern[index + 1]))
                        index++;
                    frame.Add(Width.Character);
                    break;
            }
        }

        if (frames.Count != 0)
            return default;
        var result = frame.Finish();
        var longest = result.Ambiguous && result.Variable;
        return new AwkRegexSelection(longest, result.Maximum,
            result.HasCapture && (longest || result.CaptureAmbiguous));
    }

    internal static int BracketEnd(string pattern, int opening)
    {
        var index = opening + 1;
        if (index < pattern.Length && pattern[index] == '^')
            index++;
        if (index < pattern.Length && pattern[index] == ']')
            index++;
        for (; index < pattern.Length; index++)
        {
            if (pattern[index] == '\\')
                index++;
            else if (pattern[index] == ']')
                return index;
        }
        return -1;
    }

    private static bool TryReadGroupHeader(string pattern, ref int index, out bool zeroWidth)
    {
        zeroWidth = false;
        var header = index + 2;
        if (header >= pattern.Length)
            return false;
        switch (pattern[header])
        {
            case ':':
                index = header;
                return true;
            case '=':
            case '!':
                zeroWidth = true;
                index = header;
                return true;
            case '<' when header + 1 < pattern.Length && pattern[header + 1] is '=' or '!':
                zeroWidth = true;
                index = header + 1;
                return true;
            case '<':
            case '\'':
                var close = pattern.IndexOf(pattern[header] == '<' ? '>' : '\'', header + 1);
                if (close < 0 || pattern.AsSpan(header + 1, close - header - 1).Contains('-'))
                    return false;
                index = close;
                return true;
            default:
                // Whitespace/comment mode, atomic groups and conditionals retain their
                // inherited backend semantics; they are outside the awk ERE contract.
                for (var option = header; option < pattern.Length; option++)
                {
                    if (pattern[option] is ':' or ')')
                    {
                        index = option;
                        return true;
                    }
                    if (pattern[option] is not ('i' or 'm' or 's' or 'n' or '-'))
                        return false;
                }
                return false;
        }
    }

    private static bool TryReadInterval(string pattern, ref int index, out int minimum, out int maximum)
    {
        minimum = maximum = 0;
        var close = index + 1;
        // Scan only interval syntax so repeated literal opening braces stay linear.
        while (close < pattern.Length && close - index <= 23 &&
            (char.IsAsciiDigit(pattern[close]) || pattern[close] == ','))
            close++;
        if (close >= pattern.Length || pattern[close] != '}')
            return false;
        var content = pattern.AsSpan(index + 1, close - index - 1);
        var comma = content.IndexOf(',');
        var lower = comma < 0 ? content : content[..comma];
        if (!int.TryParse(lower, NumberStyles.None, CultureInfo.InvariantCulture, out minimum))
            return false;
        maximum = minimum;
        if (comma >= 0)
        {
            var upper = content[(comma + 1)..];
            if (upper.IsEmpty)
                maximum = int.MaxValue;
            else if (!int.TryParse(upper, NumberStyles.None, CultureInfo.InvariantCulture, out maximum))
                return false;
        }
        index = close;
        return true;
    }

    private readonly record struct Width(int Minimum, int Maximum, bool Ambiguous,
        bool HasCapture = false, bool NestedCapture = false, bool CaptureAmbiguous = false)
    {
        public static Width Empty => new(0, 0, false);
        public static Width Character => new(1, 1, false);
        public bool Variable => Minimum != Maximum;

        public Width Append(Width other) => new(
            Add(Minimum, other.Minimum), Add(Maximum, other.Maximum),
            Ambiguous || other.Ambiguous || (Variable && other.Variable),
            HasCapture || other.HasCapture, NestedCapture || other.NestedCapture,
            CaptureAmbiguous || other.CaptureAmbiguous);

        public Width Alternative(Width other)
        {
            var minimum = Math.Min(Minimum, other.Minimum);
            var maximum = Math.Max(Maximum, other.Maximum);
            return new Width(minimum, maximum, Ambiguous || other.Ambiguous || minimum != maximum,
                HasCapture || other.HasCapture, NestedCapture || other.NestedCapture,
                CaptureAmbiguous || other.CaptureAmbiguous);
        }

        public Width Repeat(int minimum, int maximum, bool lazy) => new(
            Multiply(Minimum, minimum), Multiply(Maximum, maximum),
            Ambiguous || lazy || (Variable && maximum > 1), HasCapture, NestedCapture,
            CaptureAmbiguous || (HasCapture && maximum > 1 && (Minimum == 0 || NestedCapture)));

        private static int Add(int left, int right) => (int)Math.Min(int.MaxValue, (long)left + right);
        private static int Multiply(int left, int right) => (int)Math.Min(int.MaxValue, (long)left * right);
    }

    private sealed class Frame
    {
        private Width _sequence;
        private Width _atom;
        private Width _alternatives;
        private bool _hasAlternative;

        public bool ZeroWidth { get; init; }
        public bool Captures { get; init; }

        public void Add(Width atom)
        {
            _sequence = _sequence.Append(_atom);
            _atom = atom;
        }

        public void Repeat(int minimum, int maximum, bool lazy) =>
            _atom = _atom.Repeat(minimum, maximum, lazy);

        public void NextAlternative()
        {
            var branch = _sequence.Append(_atom);
            _alternatives = _hasAlternative ? _alternatives.Alternative(branch) : branch;
            _hasAlternative = true;
            _sequence = _atom = Width.Empty;
        }

        public Width Finish()
        {
            var branch = _sequence.Append(_atom);
            return _hasAlternative ? _alternatives.Alternative(branch) : branch;
        }
    }
}

/// <summary>One timeout-bounded search chooses the start/end before capturing left-to-right.</summary>
internal sealed class AwkLongestRegex
{
    private AwkLongestRegex(Regex regex, int[] groups)
    {
        Regex = regex;
        Groups = groups;
    }

    public Regex Regex { get; }
    public int[] Groups { get; }

    public static AwkLongestRegex Create(AwkRegex source, AwkRegexCaseSensitivity caseSensitivity)
    {
        var names = source.Backend switch
        {
            AwkDotNetRegexBackend dotNet => dotNet.Value.GetGroupNames(),
            AwkUtf8RegexBackend utf8 => utf8.Value.GetGroupNames(),
            _ => throw new InvalidOperationException("Unknown regex backend.")
        };
        var prefixName = UniqueName(names, "LokadPrefix");
        var wholeName = UniqueName(names, "LokadWhole");
        var maximum = source.Selection.MaximumScalars;
        var length = maximum == int.MaxValue ? "*" : "{0," + maximum.ToString(CultureInfo.InvariantCulture) + "}";
        var prefix = @"\k<" + prefixName + ">";
        var whole = @"\k<" + wholeName + ">";
        // Lazy prefix establishes the earliest scalar boundary. The greedy lookahead
        // selects the longest endpoint using a capture-free right-to-left probe.
        // The original expression then captures left-to-right at that exact endpoint.
        // Every assertion sees the complete input, including before startAt.
        var stripped = StripCaptures(source.Pattern);
        var pattern = @"\G(?<" + prefixName + ">" + AwkRegexScalarPattern.AnyScalar + @"*?)(?=(?:" +
            stripped + @"))(?=(?<" +
            wholeName + ">" + AwkRegexScalarPattern.AnyScalar + length + @")(?<=\G" + prefix + "(?:" +
            stripped + ")))(?:" + source.Pattern + @")(?<=\G" + prefix + whole + ")";
        var regex = new Regex(pattern, AwkRegex.ResolveOptions(caseSensitivity), AwkRegex.MatchTimeout);
        var groups = new int[names.Length];
        for (var index = 0; index < groups.Length; index++)
            groups[index] = regex.GroupNumberFromName(index == 0 ? wholeName : names[index]);
        return new AwkLongestRegex(regex, groups);
    }

    private static string UniqueName(string[] names, string candidate)
    {
        while (Array.IndexOf(names, candidate) >= 0)
            candidate += "_";
        return candidate;
    }

    private static string StripCaptures(string pattern)
    {
        var builder = new StringBuilder(pattern.Length);
        for (var index = 0; index < pattern.Length; index++)
        {
            if (pattern[index] == '\\' && index + 1 < pattern.Length)
                builder.Append(pattern[index]).Append(pattern[++index]);
            else if (pattern[index] == '[')
            {
                var end = AwkRegexSelection.BracketEnd(pattern, index);
                if (end < 0)
                    throw new ArgumentException("Invalid regex bracket.");
                builder.Append(pattern, index, end + 1 - index);
                index = end;
            }
            else if (pattern[index] == '(' && (index + 1 >= pattern.Length || pattern[index + 1] != '?'))
                builder.Append("(?:");
            else if (pattern[index] == '(' && index + 2 < pattern.Length &&
                (pattern[index + 2] == '\'' || (pattern[index + 2] == '<' &&
                    index + 3 < pattern.Length && pattern[index + 3] is not ('=' or '!'))))
            {
                index = pattern.IndexOf(pattern[index + 2] == '<' ? '>' : '\'', index + 3);
                builder.Append("(?:");
            }
            else if (pattern[index] == '|')
            {
                // Keep nullable alternative arms after removing captures. The backend's
                // right-to-left optimizer can otherwise discard an empty arm following
                // a positive repetition, e.g. (a+|())+, in a lookbehind with a backreference.
                builder.Append(@"|(?=[\s\S]|\z)");
            }
            else
                builder.Append(pattern[index]);
        }
        return builder.ToString();
    }
}

internal readonly struct AwkDotNetMatch(Match match, int[]? groups)
{
    private readonly Match? _match = match;
    public Match Match => _match ?? Match.Empty;
    public int[]? Groups { get; } = groups;
    public bool Success => Match.Success;
    public Group Whole => Match.Groups[Groups is null ? 0 : Groups[0]];
}
