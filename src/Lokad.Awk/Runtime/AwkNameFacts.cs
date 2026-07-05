using System;

namespace Lokad.Awk;

internal static class AwkNameFacts
{
    public static ReadOnlySpan<char> GetCommandBaseName(ReadOnlySpan<char> word)
    {
        var slashIndex = word.LastIndexOf('/');
        var backslashIndex = word.LastIndexOf('\\');
        var separatorIndex = Math.Max(slashIndex, backslashIndex);

        return separatorIndex < 0 ? word : word[(separatorIndex + 1)..];
    }

    public static bool IsValidVariableName(ReadOnlySpan<char> name)
    {
        if (name.IsEmpty || !IsNameStart(name[0]))
        {
            return false;
        }

        for (var i = 1; i < name.Length; i++)
        {
            if (!IsNamePart(name[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsNameStart(char value) =>
        value is '_' or >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    public static bool IsNamePart(char value) =>
        IsNameStart(value) || value is >= '0' and <= '9';
}
