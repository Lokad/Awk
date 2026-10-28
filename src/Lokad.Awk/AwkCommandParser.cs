using System;
using System.Threading;

namespace Lokad.Awk;

internal static class AwkCommandParser
{
    public static AwkInvocation Parse(AwkCommandInvocation invocation,
        AwkFrontendMemory? memory = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        (memory ?? AwkFrontendMemory.Disabled).AdmitInvocationCopies(invocation, cancellationToken);
        try { return new Parser(invocation, cancellationToken).Parse(); }
        catch (Exception exception) when (exception is FormatException or AwkPathException)
        {
            return new AwkInvocationBuilder(invocation)
                .WithErrorMessage(AwkMessages.ErrorInvalidPath, AwkDiagnosticId.OptionInvalid)
                .Build();
        }
    }

    private sealed class Parser(AwkCommandInvocation invocation, CancellationToken cancellationToken)
    {
        private readonly AwkSourceInputPlanner _sourceInputPlanner =
            new(invocation.CommandName, invocation.CurrentDirectory, cancellationToken);
        private AwkCompatibilityProfile _profile = AwkCompatibilityProfile.Default;
        private bool _sandbox;
        private bool _csv;

        public AwkInvocation Parse()
        {
            var parser = new CommandLineOptionParser(invocation.Arguments);
            var parsingInputArguments = false;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (parsingInputArguments)
                {
                    AddRemainingInputArguments(parser);
                    break;
                }

                if (!parser.TryRead(out var item))
                    break;

                switch (item)
                {
                    case CommandLineOptionItem.ShortOption shortOption:
                        {
                            var result = ParseShortOption(parser, shortOption.Name);
                            if (result.Status == AwkCommandParseStatus.Proceed)
                            {
                                parsingInputArguments = result.ParseInputArguments;
                                continue;
                            }

                            if (result.Status == AwkCommandParseStatus.Terminal)
                                return BuildTerminalInvocation(result.Terminal);

                            return BuildErrorInvocation(result.Message, result.Id);
                        }
                    case CommandLineOptionItem.Argument argument:
                        AddScriptOrInputArgument(argument.Value);
                        parsingInputArguments = true;
                        continue;
                    case CommandLineOptionItem.LongOption longOption:
                        {
                            var result = ParseLongOption(parser, longOption);
                            if (result.Status == AwkCommandParseStatus.Proceed)
                            {
                                parsingInputArguments = result.ParseInputArguments;
                                continue;
                            }

                            if (result.Status == AwkCommandParseStatus.Terminal)
                                return BuildTerminalInvocation(result.Terminal);

                            return BuildErrorInvocation(result.Message, result.Id);
                        }
                }
            }

            if (!_sourceInputPlanner.HasScript)
                return BuildErrorInvocation(AwkMessages.ErrorMissingScript, AwkDiagnosticId.OptionMissingScript);

            if (_csv && _profile.Mode == AwkCompatibilityMode.Posix)
                return BuildErrorInvocation(AwkMessages.ErrorCsvPosixConflict, AwkDiagnosticId.OptionInvalid);
            _sourceInputPlanner.PrepareCompatibility(_profile.Mode);
            return CreateInvocationBuilder()
                .WithPlan(_sourceInputPlanner.BuildPlan(invocation))
                .WithCompatibilityProfile(_profile)
                .Build();

            void AddRemainingInputArguments(CommandLineOptionParser parser)
            {
                while (parser.TryReadRaw(out var argument))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _sourceInputPlanner.AddInputArgument(argument);
                }
            }

            void AddScriptOrInputArgument(string argument)
            {
                if (!_sourceInputPlanner.HasScript)
                    _sourceInputPlanner.AddInlineScript(argument);
                else
                    _sourceInputPlanner.AddInputArgument(argument);
            }

            AwkInvocation BuildErrorInvocation(ReadOnlyMemory<byte> message, AwkDiagnosticId id)
            {
                return CreateInvocationBuilder()
                    .WithErrorMessage(message, id)
                    .Build();
            }

            AwkInvocation BuildTerminalInvocation(AwkCommandTerminal terminal)
            {
                var builder = CreateInvocationBuilder();
                return terminal switch
                {
                    AwkCommandTerminal.Help => builder.WithHelp().Build(),
                    AwkCommandTerminal.Version => builder.WithVersion().Build(),
                    _ => throw new InvalidOperationException("Unknown terminal awk invocation.")
                };
            }

            AwkCommandParseResult ParseShortOption(CommandLineOptionParser parser, char option)
            {
                var spelling = AwkOptionSpelling.Short(option);
                if (!AwkOptions.TryResolveShortOption(option, out var spec))
                    return AwkCommandParseResult.Fail(
                        AwkMessages.FormatInvalidOption(spelling.Display),
                        AwkDiagnosticId.OptionInvalid);

                var valueResult = ReadShortOptionValue(parser, spec);
                if (valueResult.Status == AwkOptionValueStatus.Error)
                    return AwkCommandParseResult.Fail(valueResult.Message, valueResult.Id);

                if (spec.Action == AwkOptionAction.ExpandWOption)
                    return ApplyWOption(parser, valueResult.Value);

                return ApplyOption(spec, valueResult.Value, spelling);

                static AwkOptionValueResult ReadShortOptionValue(
                    CommandLineOptionParser optionParser,
                    AwkOptionSpec optionSpec)
                {
                    switch (optionSpec.ValueCardinality)
                    {
                        case AwkOptionValueCardinality.None:
                            return AwkOptionValueResult.Empty;
                        case AwkOptionValueCardinality.Optional:
                            optionParser.TryReadShortGroupRemainder(out var optionalValue);
                            return AwkOptionValueResult.Success(optionalValue);
                        case AwkOptionValueCardinality.Required:
                            if (AwkOptionValueReader.TryReadShortAttachedOrRaw(optionParser, out var value))
                                return AwkOptionValueResult.Success(value);

                            return AwkOptionValueResult.Fail(
                                AwkMessages.ErrorMissingValue,
                                AwkDiagnosticId.OptionMissingValue);
                        default:
                            return AwkOptionValueResult.Empty;
                    }
                }

                AwkCommandParseResult ApplyWOption(CommandLineOptionParser parser, string value)
                {
                    if (value.Length == 0)
                        return AwkCommandParseResult.Fail(
                            AwkMessages.ErrorMissingValue,
                            AwkDiagnosticId.OptionMissingValue);

                    var equalsIndex = value.IndexOf('=', StringComparison.Ordinal);
                    var name = equalsIndex < 0 ? value : value[..equalsIndex];
                    var optionValue = equalsIndex < 0
                        ? CommandLineOptionValue.Absent
                        : CommandLineOptionValue.Inline(value[(equalsIndex + 1)..]);
                    if (!AwkOptions.TryResolveWOptionName(name, out var spec))
                        return RejectInvalidWOption(name);

                    var spelling = AwkOptionSpelling.W(name);
                    var valueResult = ReadWOptionValue(parser, spec, optionValue, spelling.Display);
                    if (valueResult.Status == AwkOptionValueStatus.Error)
                        return AwkCommandParseResult.Fail(valueResult.Message, valueResult.Id);

                    return ApplyOption(spec, valueResult.Value, spelling);

                    static AwkOptionValueResult ReadWOptionValue(
                        CommandLineOptionParser parser,
                        AwkOptionSpec spec,
                        CommandLineOptionValue inlineValue,
                        string optionName)
                    {
                        return ReadNamedOptionValue(
                            spec,
                            inlineValue,
                            optionName,
                            (out string requiredValue) => AwkOptionValueReader.TryReadW(
                                parser,
                                inlineValue,
                                out requiredValue));
                    }

                    static AwkCommandParseResult RejectInvalidWOption(string name) =>
                        AwkCommandParseResult.Fail(
                            AwkMessages.FormatInvalidOption(AwkOptionSpelling.W(name).Display),
                            AwkDiagnosticId.OptionInvalid);
                }
            }

            AwkCommandParseResult ParseLongOption(CommandLineOptionParser parser, CommandLineOptionItem.LongOption item)
            {
                if (!AwkOptions.TryResolveLongOptionName(item.Name, out var spec))
                    return AwkCommandParseResult.Fail(
                        AwkMessages.FormatInvalidOption(AwkOptionSpelling.Long(item.Name).Display),
                        AwkDiagnosticId.OptionInvalid);

                var spelling = AwkOptionSpelling.Long(item.Name);
                var valueResult = ReadLongOptionValue(parser, item, spec, spelling);
                if (valueResult.Status == AwkOptionValueStatus.Error)
                    return AwkCommandParseResult.Fail(valueResult.Message, valueResult.Id);

                return ApplyOption(spec, valueResult.Value, spelling);

                static AwkOptionValueResult ReadLongOptionValue(
                    CommandLineOptionParser parser,
                    CommandLineOptionItem.LongOption item,
                    AwkOptionSpec spec,
                    AwkOptionSpelling spelling)
                {
                    return ReadNamedOptionValue(
                        spec,
                        item.Value,
                        spelling.Display,
                        (out string requiredValue) => AwkOptionValueReader.TryReadLong(parser, item, out requiredValue));
                }
            }
        }

        private AwkInvocationBuilder CreateInvocationBuilder()
        {
            var builder = new AwkInvocationBuilder(invocation);
            if (_csv) builder.WithCsv();
            return _sandbox ? builder.WithSandbox() : builder;
        }

        private AwkCommandParseResult ApplyOption(
            AwkOptionSpec spec,
            string value,
            AwkOptionSpelling spelling)
        {
            if (spec.Action == AwkOptionAction.RejectDynamicLoad)
                return AwkCommandParseResult.Fail(
                    AwkMessages.ErrorUnsupportedDynamicLoad,
                    AwkDiagnosticId.UnsupportedPolicy);

            if (spec.IsUnsupportedGnuOption)
                return RejectUnsupportedGnuOption(spelling.Display);

            switch (spec.Action)
            {
                case AwkOptionAction.Csv:
                    _csv = true;
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.AddVariableAssignment:
                    return TryAddVariableAssignment(value);
                case AwkOptionAction.CharactersAsBytes:
                    _profile = _profile.WithCharactersAsBytes();
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.Traditional:
                    _profile = _profile.WithMode(AwkCompatibilityMode.Traditional);
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.EnterInputArguments:
                    _sourceInputPlanner.AddSourceFile(value);
                    return AwkCommandParseResult.ReadInputArguments;
                case AwkOptionAction.FieldSeparator:
                    _sourceInputPlanner.AddFieldSeparatorOption(value);
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.AddSourceFile:
                    _sourceInputPlanner.AddSourceFile(value);
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.PrintHelp:
                    return AwkCommandParseResult.Terminate(AwkCommandTerminal.Help);
                case AwkOptionAction.AddIncludeFile:
                    _sourceInputPlanner.AddIncludeFile(value);
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.Lint:
                    return TryApplyLintOption(value, spelling.LintDisplay);
                case AwkOptionAction.LintOld:
                    _profile = _profile.WithLintMode(AwkLintMode.Normal);
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.NoOptimize:
                case AwkOptionAction.Optimize:
                case AwkOptionAction.ReInterval:
                case AwkOptionAction.UseLcNumeric:
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.Sandbox:
                    _sandbox = true;
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.NonDecimalData:
                    _profile = _profile.WithNonDecimalData();
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.Posix:
                    _profile = _profile.WithMode(AwkCompatibilityMode.Posix);
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.AddInlineScript:
                    _sourceInputPlanner.AddInlineScript(value);
                    return AwkCommandParseResult.KeepParsing;
                case AwkOptionAction.PrintVersion:
                    return AwkCommandParseResult.Terminate(AwkCommandTerminal.Version);
                default:
                    return AwkCommandParseResult.Fail(
                        AwkMessages.FormatInvalidOption(spelling.Display),
                        AwkDiagnosticId.OptionInvalid);
            }

            AwkCommandParseResult TryAddVariableAssignment(string assignment)
            {
                if (_sourceInputPlanner.TryAddOptionVariableAssignment(assignment, out var error))
                    return AwkCommandParseResult.KeepParsing;

                return AwkCommandParseResult.Fail(
                    error.IsEmpty ? AwkMessages.ErrorInvalidVariableAssignment : error,
                    AwkDiagnosticId.OptionInvalidVariableAssignment);
            }

            AwkCommandParseResult TryApplyLintOption(string value, string optionName)
            {
                var lintMode = value switch
                {
                    "" => AwkLintMode.Normal,
                    "fatal" => AwkLintMode.Fatal,
                    "invalid" => AwkLintMode.Normal,
                    "no-ext" => AwkLintMode.Normal,
                    _ => AwkLintMode.Off
                };
                if (lintMode == AwkLintMode.Off && value.Length > 0)
                    return AwkCommandParseResult.Fail(
                        AwkMessages.FormatInvalidOption(optionName + "=" + value),
                        AwkDiagnosticId.OptionInvalid);

                _profile = _profile.WithLintMode(lintMode);
                return AwkCommandParseResult.KeepParsing;
            }

            static AwkCommandParseResult RejectUnsupportedGnuOption(string option) =>
                AwkCommandParseResult.Fail(
                    AwkMessages.FormatUnsupportedGnuOption(option),
                    AwkDiagnosticId.UnsupportedPolicy);
        }

        private static AwkOptionValueResult ReadNamedOptionValue(
            AwkOptionSpec spec,
            CommandLineOptionValue inlineValue,
            string optionName,
            TryReadRequiredOptionValue tryReadRequired)
        {
            if (spec.IsUnsupportedGnuOption &&
                spec.ValueCardinality != AwkOptionValueCardinality.Required)
                return AwkOptionValueResult.Empty;

            switch (spec.ValueCardinality)
            {
                case AwkOptionValueCardinality.None:
                    if (inlineValue.HasInlineValue)
                        return AwkOptionValueResult.Fail(
                            AwkMessages.FormatInvalidOption(optionName),
                            AwkDiagnosticId.OptionInvalid);

                    return AwkOptionValueResult.Empty;
                case AwkOptionValueCardinality.Required:
                    if (tryReadRequired(out var requiredValue))
                        return AwkOptionValueResult.Success(requiredValue);

                    return AwkOptionValueResult.Fail(
                        AwkMessages.ErrorMissingValue,
                        AwkDiagnosticId.OptionMissingValue);
                case AwkOptionValueCardinality.Optional:
                    return AwkOptionValueResult.Success(inlineValue.Text);
                default:
                    return AwkOptionValueResult.Empty;
            }
        }

        private readonly struct AwkCommandParseResult
        {
            private AwkCommandParseResult(
                AwkCommandParseStatus status,
                bool parseInputArguments,
                AwkCommandTerminal terminal,
                ReadOnlyMemory<byte> message,
                AwkDiagnosticId id)
            {
                Status = status;
                ParseInputArguments = parseInputArguments;
                Terminal = terminal;
                Message = message;
                Id = id;
            }

            public AwkCommandParseStatus Status { get; }

            public bool ParseInputArguments { get; }

            public AwkCommandTerminal Terminal { get; }

            public ReadOnlyMemory<byte> Message { get; }

            public AwkDiagnosticId Id { get; }

            public static AwkCommandParseResult KeepParsing { get; } =
                new(AwkCommandParseStatus.Proceed, parseInputArguments: false, default, default, default);

            public static AwkCommandParseResult ReadInputArguments { get; } =
                new(AwkCommandParseStatus.Proceed, parseInputArguments: true, default, default, default);

            public static AwkCommandParseResult Terminate(AwkCommandTerminal terminal) =>
                new(AwkCommandParseStatus.Terminal, parseInputArguments: false, terminal, default, default);

            public static AwkCommandParseResult Fail(ReadOnlyMemory<byte> message, AwkDiagnosticId id) =>
                new(AwkCommandParseStatus.Error, parseInputArguments: false, default, message, id);
        }

        private enum AwkCommandParseStatus
        {
            Proceed,
            Terminal,
            Error
        }

        private readonly struct AwkOptionValueResult
        {
            private readonly string? _value;

            private AwkOptionValueResult(
                AwkOptionValueStatus status,
                string value,
                ReadOnlyMemory<byte> message,
                AwkDiagnosticId id)
            {
                Status = status;
                _value = value;
                Message = message;
                Id = id;
            }

            public AwkOptionValueStatus Status { get; }

            public string Value => _value ?? string.Empty;

            public ReadOnlyMemory<byte> Message { get; }

            public AwkDiagnosticId Id { get; }

            public static AwkOptionValueResult Empty { get; } =
                new(AwkOptionValueStatus.Read, string.Empty, default, default);

            public static AwkOptionValueResult Success(string value) =>
                new(AwkOptionValueStatus.Read, value, default, default);

            public static AwkOptionValueResult Fail(ReadOnlyMemory<byte> message, AwkDiagnosticId id) =>
                new(AwkOptionValueStatus.Error, string.Empty, message, id);
        }

        private enum AwkOptionValueStatus
        {
            Read,
            Error
        }

        private readonly struct AwkOptionSpelling
        {
            private readonly string? _display;

            private AwkOptionSpelling(AwkOptionSource source, string display)
            {
                Source = source;
                _display = display;
            }

            public AwkOptionSource Source { get; }

            public string Display => _display ?? string.Empty;

            public string LintDisplay =>
                Source switch
                {
                    AwkOptionSource.Short => "-L",
                    AwkOptionSource.Long => "--lint",
                    AwkOptionSource.WOption => "-W lint",
                    _ => "--lint"
                };

            public static AwkOptionSpelling Short(char name) =>
                new(AwkOptionSource.Short, "-" + name);

            public static AwkOptionSpelling Long(string name) =>
                new(AwkOptionSource.Long, "--" + name);

            public static AwkOptionSpelling W(string name) =>
                new(AwkOptionSource.WOption, "-W " + name);
        }

        private enum AwkCommandTerminal
        {
            Help,
            Version
        }

        private enum AwkOptionSource
        {
            Short,
            Long,
            WOption
        }

        private delegate bool TryReadRequiredOptionValue(out string value);
    }
}
