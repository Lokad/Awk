using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lokad.Awk;

/// <summary>
/// Well-formed but not yet canonicalized awk path.
/// </summary>
public sealed class AwkRawPath : IParsable<AwkRawPath>
{
    private AwkRawPath(string raw)
    {
        Raw = raw;
    }

    public string Raw { get; }

    public static bool IsValid(string? s) =>
        !string.IsNullOrEmpty(s) && s.All(c => !AwkPath.IsForbiddenChar(c) && c is not ('*' or '\\' or ':'));

    public static AwkRawPath Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        if (!TryParse(s, provider, out var result))
        {
            throw new FormatException($"'{s}' is not a valid awk path.");
        }

        return result;
    }

    public static bool TryParse(string? s, IFormatProvider? provider, [NotNullWhen(true)] out AwkRawPath? result)
    {
        if (s is not null && IsValid(s))
        {
            result = new AwkRawPath(s);
            return true;
        }

        result = null;
        return false;
    }

    public static bool TryParse(string? s, [NotNullWhen(true)] out AwkRawPath? result) =>
        TryParse(s, null, out result);

    public AwkPath ToAbsolute(AwkPath current) =>
        ResolveAbsolute(current, confineToCurrent: false);

    public AwkPath ToAbsoluteWithin(AwkPath current) =>
        ResolveAbsolute(current, confineToCurrent: true);

    private AwkPath ResolveAbsolute(AwkPath current, bool confineToCurrent)
    {
        ArgumentNullException.ThrowIfNull(current);
        var combined = Raw.StartsWith('/')
            ? Raw
            : current.Path.Length == 1
                ? "/" + Raw
                : current.Path + "/" + Raw;

        var span = combined.AsSpan(1);
        List<(int start, int len)>? segments = null;

        foreach (var range in span.Split('/'))
        {
            var start = range.Start.Value;
            var len = range.End.Value - start;

            var isDot = len == 1 && span[start] == '.';
            var isDotDot = len == 2 && span[start] == '.' && span[start + 1] == '.';

            if (!isDot && !isDotDot && len > 0)
            {
                segments?.Add((start + 1, len));
                continue;
            }

            if (segments is null)
            {
                segments = [];
                foreach (var r in span[..start].Split('/'))
                {
                    var s = r.Start.Value;
                    var l = r.End.Value - s;
                    if (l > 0)
                    {
                        segments.Add((s + 1, l));
                    }
                }
            }

            if (len == 0 || isDot)
            {
                continue;
            }

            if (segments.Count == 0)
            {
                throw new AwkPathException($"Cannot resolve '{Raw}': '..' goes above the root.");
            }

            segments.RemoveAt(segments.Count - 1);
        }

        string resultPath;
        if (segments is null)
        {
            resultPath = combined;
        }
        else
        {
            var totalLen = Math.Max(1, segments.Count);
            foreach (var (_, len) in segments)
            {
                totalLen += len;
            }

            resultPath = string.Create(totalLen, (combined, segments), static (dst, state) =>
            {
                var (src, segs) = state;
                dst[0] = '/';
                var pos = 1;
                for (var i = 0; i < segs.Count; i++)
                {
                    if (i > 0)
                    {
                        dst[pos++] = '/';
                    }

                    var (segStart, segLen) = segs[i];
                    src.AsSpan(segStart, segLen).CopyTo(dst[pos..]);
                    pos += segLen;
                }
            });
        }

        var result = new AwkPath(resultPath);

        if (confineToCurrent && !current.IsEqualToOrParentOf(result))
        {
            throw new AwkPathException(
                $"Cannot resolve '{Raw}': result '{resultPath}' escapes jail '{current.Path}'.");
        }

        return result;
    }

    public override string ToString() => Raw;
}
