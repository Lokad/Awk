using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class CommandLineOptionParser
{
    private readonly IReadOnlyList<string> _arguments;
    private readonly bool _allowSingleDashLongOptions;
    private bool _parsingOptions = true;
    private int _argumentIndex;
    private string _shortGroup = string.Empty;
    private int _shortIndex;

    public CommandLineOptionParser(IReadOnlyList<string> arguments)
        : this(arguments, allowSingleDashLongOptions: false)
    {
    }

    public CommandLineOptionParser(IReadOnlyList<string> arguments, bool allowSingleDashLongOptions)
    {
        _arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        _allowSingleDashLongOptions = allowSingleDashLongOptions;
    }

    public bool TryRead(out CommandLineOptionItem item)
    {
        if (TryReadShortGroup(out item))
        {
            return true;
        }

        while (_argumentIndex < _arguments.Count)
        {
            var argument = _arguments[_argumentIndex];
            _argumentIndex++;

            if (_parsingOptions && IsDoubleDash(argument))
            {
                _parsingOptions = false;
                continue;
            }

            if (_parsingOptions && IsLongOption(argument))
            {
                item = ParseLongOption(argument);
                return true;
            }

            if (_parsingOptions && IsSingleDashLongOption(argument))
            {
                item = ParseSingleDashLongOption(argument);
                return true;
            }

            if (_parsingOptions && IsShortOptionGroup(argument))
            {
                _shortGroup = argument;
                _shortIndex = 1;
                return TryReadShortGroup(out item);
            }

            item = new CommandLineOptionItem.Argument(argument);
            return true;
        }

        item = CommandLineOptionItem.End;
        return false;
    }

    public bool TryReadRaw(out string argument)
    {
        _shortGroup = string.Empty;
        _shortIndex = 0;

        if (_argumentIndex >= _arguments.Count)
        {
            argument = string.Empty;
            return false;
        }

        argument = _arguments[_argumentIndex];
        _argumentIndex++;
        return true;
    }

    public bool TryReadShortGroupRemainder(out string remainder)
    {
        if (string.IsNullOrEmpty(_shortGroup) || _shortIndex <= 0 || _shortIndex >= _shortGroup.Length)
        {
            remainder = string.Empty;
            return false;
        }

        remainder = _shortGroup[_shortIndex..];
        _shortGroup = string.Empty;
        _shortIndex = 0;
        return true;
    }

    private bool TryReadShortGroup(out CommandLineOptionItem item)
    {
        if (string.IsNullOrEmpty(_shortGroup))
        {
            item = CommandLineOptionItem.End;
            return false;
        }

        if (_shortIndex >= _shortGroup.Length)
        {
            _shortGroup = string.Empty;
            item = CommandLineOptionItem.End;
            return false;
        }

        var option = _shortGroup[_shortIndex];
        _shortIndex++;
        if (_shortIndex >= _shortGroup.Length)
        {
            _shortGroup = string.Empty;
        }

        item = new CommandLineOptionItem.ShortOption(option);
        return true;
    }

    private bool IsShortOptionGroup(string argument)
    {
        if (argument.Length <= 1 || argument[0] != '-' || argument[1] == '-')
        {
            return false;
        }

        if (_allowSingleDashLongOptions && argument.Length > 2)
        {
            return false;
        }

        return true;
    }

    private static bool IsLongOption(string argument) =>
        argument.Length > 2 && argument[0] == '-' && argument[1] == '-';

    private bool IsSingleDashLongOption(string argument) =>
        _allowSingleDashLongOptions && argument.Length > 2 && argument[0] == '-' && argument[1] != '-';

    private static bool IsDoubleDash(string argument) => argument == "--";

    private static CommandLineOptionItem ParseLongOption(string argument)
    {
        var equalsIndex = argument.IndexOf('=', 2);

        if (equalsIndex < 0)
        {
            return new CommandLineOptionItem.LongOption(argument[2..], CommandLineOptionValue.Absent);
        }

        var name = argument[2..equalsIndex];
        var value = argument[(equalsIndex + 1)..];
        return new CommandLineOptionItem.LongOption(name, CommandLineOptionValue.Inline(value));
    }

    private static CommandLineOptionItem ParseSingleDashLongOption(string argument)
    {
        var equalsIndex = argument.IndexOf('=', 1);

        if (equalsIndex < 0)
        {
            return new CommandLineOptionItem.LongOption(argument[1..], CommandLineOptionValue.Absent);
        }

        var name = argument[1..equalsIndex];
        var value = argument[(equalsIndex + 1)..];
        return new CommandLineOptionItem.LongOption(name, CommandLineOptionValue.Inline(value));
    }
}

internal abstract record CommandLineOptionItem
{
    public static CommandLineOptionItem End { get; } = new EndItem();

    private CommandLineOptionItem()
    {
    }

    public sealed record ShortOption(char Name) : CommandLineOptionItem;

    public sealed record LongOption(string Name, CommandLineOptionValue Value) : CommandLineOptionItem;

    public sealed record Argument(string Value) : CommandLineOptionItem;

    private sealed record EndItem : CommandLineOptionItem;
}

internal readonly record struct CommandLineOptionValue
{
    private readonly string _text;

    private CommandLineOptionValue(CommandLineOptionValueKind kind, string text)
    {
        Kind = kind;
        _text = text;
    }

    public static CommandLineOptionValue Absent => default;

    public static CommandLineOptionValue Inline(string text) =>
        new(CommandLineOptionValueKind.Inline, text ?? string.Empty);

    public CommandLineOptionValueKind Kind { get; }

    public bool HasInlineValue => Kind == CommandLineOptionValueKind.Inline;

    public string Text => HasInlineValue ? _text : string.Empty;
}

internal enum CommandLineOptionValueKind
{
    Absent,
    Inline
}
