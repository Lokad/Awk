using System;

namespace Lokad.Awk;

internal static class AwkMessages
{
    internal static readonly ReadOnlyMemory<byte> ErrorHexadecimalFloatExponent =
        "awk: hexadecimal floating point constants require an exponent\n"u8.ToArray();

    internal static readonly ReadOnlyMemory<byte> ErrorInvalidOptionPrefix = AwkUtf8Codec.Encode("awk: invalid option -- ");
    internal static readonly ReadOnlyMemory<byte> ErrorInvalidScript = AwkUtf8Codec.Encode("awk: invalid script\n");
    internal static readonly ReadOnlyMemory<byte> ErrorInvalidVariableAssignment =
        AwkUtf8Codec.Encode("awk: invalid variable assignment\n");
    internal static readonly ReadOnlyMemory<byte> ErrorMissingScript = AwkUtf8Codec.Encode("awk: missing script\n");
    internal static readonly ReadOnlyMemory<byte> ErrorMissingValue = AwkUtf8Codec.Encode("awk: missing option value\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedDynamicLoad =
        AwkUtf8Codec.Encode("awk: dynamic extension loading is unsupported in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedSystem =
        AwkUtf8Codec.Encode("awk: system() is unsupported in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> ErrorSandboxRedirection =
        AwkUtf8Codec.Encode("awk: redirections are disabled in sandbox mode\n");
    internal static readonly ReadOnlyMemory<byte> ErrorNegativeFieldReference =
        AwkUtf8Codec.Encode("awk: negative field index\n");
    internal static readonly ReadOnlyMemory<byte> ErrorNegativeBitOperand =
        AwkUtf8Codec.Encode("awk: negative bitwise operand\n");
    internal static readonly ReadOnlyMemory<byte> ErrorInvalidRegex =
        AwkUtf8Codec.Encode("awk: invalid regular expression\n");
    internal static readonly ReadOnlyMemory<byte> ErrorRegexScalarBoundary =
        AwkUtf8Codec.Encode("awk: regular expression splits a Unicode character\n");
    internal static readonly ReadOnlyMemory<byte> ErrorInvalidUtf8Input =
        AwkUtf8Codec.Encode("awk: invalid UTF-8 input\n");
    internal static readonly ReadOnlyMemory<byte> ErrorRegexTimeout =
        AwkUtf8Codec.Encode("awk: regular expression match timed out\n");
    internal static readonly ReadOnlyMemory<byte> ErrorRegexPatternLimit =
        AwkUtf8Codec.Encode("awk: regular expression pattern limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorRegexGroupLimit =
        AwkUtf8Codec.Encode("awk: regular expression group limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorRegexCompilationLimit =
        AwkUtf8Codec.Encode("awk: regular expression compilation limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorRegexRecordSeparatorScanLimit =
        AwkUtf8Codec.Encode("awk: regular expression record separator scan limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorNotEnoughPrintfArguments =
        AwkUtf8Codec.Encode("awk: not enough arguments to satisfy format string\n");
    internal static readonly ReadOnlyMemory<byte> ErrorPrintfMixedArguments =
        AwkUtf8Codec.Encode("awk: positional and sequential printf arguments cannot be mixed\n");
    internal static readonly ReadOnlyMemory<byte> ErrorPrintfArgumentPosition =
        AwkUtf8Codec.Encode("awk: printf argument positions must be positive\n");
    internal static readonly ReadOnlyMemory<byte> ErrorPrintfPositionSuffix =
        AwkUtf8Codec.Encode("awk: positional printf width or precision requires `$'\n");
    internal static readonly ReadOnlyMemory<byte> ErrorPrintfPositionalProfile =
        AwkUtf8Codec.Encode("awk: positional formats are not permitted in this awk profile\n");

    internal static ReadOnlyMemory<byte> FormatPrintfPosixModifier(char modifier) =>
        AwkUtf8Codec.Encode("awk: `" + modifier + "' is not permitted in POSIX awk formats\n");
    internal static readonly ReadOnlyMemory<byte> ErrorDivisionByZero =
        AwkUtf8Codec.Encode("awk: division by zero attempted\n");
    internal static readonly ReadOnlyMemory<byte> ErrorDivisionByZeroInDivide =
        AwkUtf8Codec.Encode("awk: division by zero attempted in `/='\n");
    internal static readonly ReadOnlyMemory<byte> ErrorDivisionByZeroInModulo =
        AwkUtf8Codec.Encode("awk: division by zero attempted in `%'\n");
    internal static readonly ReadOnlyMemory<byte> ErrorDivisionByZeroInModuloAssignment =
        AwkUtf8Codec.Encode("awk: division by zero attempted in `%='\n");
    internal static readonly ReadOnlyMemory<byte> ErrorBreakOutsideLoop =
        AwkUtf8Codec.Encode("awk: break used outside loop or switch\n");
    internal static readonly ReadOnlyMemory<byte> ErrorContinueOutsideLoop =
        AwkUtf8Codec.Encode("awk: continue used outside loop\n");
    internal static readonly ReadOnlyMemory<byte> ErrorNextOutsideInput =
        AwkUtf8Codec.Encode("awk: next used outside input action\n");
    internal static readonly ReadOnlyMemory<byte> ErrorNextFileOutsideInput =
        AwkUtf8Codec.Encode("awk: nextfile used outside input action\n");
    internal static readonly ReadOnlyMemory<byte> ErrorExitInsideSortComparison =
        AwkUtf8Codec.Encode("awk: exit used inside sort comparison function\n");
    internal static readonly ReadOnlyMemory<byte> ErrorMainInputGetlineInBeginFile =
        AwkUtf8Codec.Encode("awk: non-redirected `getline' invalid inside `BEGINFILE' rule\n");
    internal static readonly ReadOnlyMemory<byte> ErrorMainInputGetlineInEndFile =
        AwkUtf8Codec.Encode("awk: non-redirected `getline' invalid inside `ENDFILE' rule\n");
    internal static readonly ReadOnlyMemory<byte> ErrorReturnOutsideFunction =
        AwkUtf8Codec.Encode("awk: return used outside function\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedCoprocess =
        AwkUtf8Codec.Encode("awk: coprocesses are unsupported in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedInetSpecialFile =
        AwkUtf8Codec.Encode("awk: /inet special files are unsupported in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedAwkFileDescriptorSpecialFile =
        AwkUtf8Codec.Encode("awk: unsupported /dev/fd descriptor in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedInternationalization =
        AwkUtf8Codec.Encode("awk: internationalization functions are unsupported in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedNamespace =
        AwkUtf8Codec.Encode("awk: namespaces are unsupported in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedStronglyTypedRegex =
        AwkUtf8Codec.Encode("awk: strongly typed regexp constants are unsupported in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedByteModeEmptyFieldSeparator =
        AwkUtf8Codec.Encode("awk: byte-mode FS=\"\" over non-ASCII text is unsupported in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedByteModeFieldWidths =
        AwkUtf8Codec.Encode("awk: byte-mode FIELDWIDTHS cannot split a UTF-8 character in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> WarningPosixOverridesCharactersAsBytes =
        AwkUtf8Codec.Encode("awk: warning: `--posix' overrides `--characters-as-bytes'\n");
    internal static readonly ReadOnlyMemory<byte> ErrorInvalidFieldWidths =
        AwkUtf8Codec.Encode("awk: invalid FIELDWIDTHS value\n");
    internal static readonly ReadOnlyMemory<byte> ErrorFieldWidthsStarNotLast =
        AwkUtf8Codec.Encode("awk: `*' must be the last designator in FIELDWIDTHS\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedIndirectFunctionCall =
        AwkUtf8Codec.Encode("awk: indirect function calls are unsupported in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedArrayOfArrays =
        AwkUtf8Codec.Encode("awk: arrays of arrays are unsupported in Lokad awk\n");
    internal static readonly ReadOnlyMemory<byte> ErrorSplitSameDestinationAndSeparatorArray =
        AwkUtf8Codec.Encode("awk: split separator array must be distinct from the destination array\n");
    internal static readonly ReadOnlyMemory<byte> ErrorCannotDeleteSymtab =
        AwkUtf8Codec.Encode("awk: `delete' is not allowed with SYMTAB\n");
    internal static readonly ReadOnlyMemory<byte> ErrorInvalidSymtabReference =
        AwkUtf8Codec.Encode("awk: invalid SYMTAB reference\n");
    internal static readonly ReadOnlyMemory<byte> ErrorFunctionRecursionLimit =
        AwkUtf8Codec.Encode("awk: function recursion limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorArrayElementLimit =
        AwkUtf8Codec.Encode("awk: array element limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorSymbolLimit =
        AwkUtf8Codec.Encode("awk: symbol limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorIdentifierLimit =
        AwkUtf8Codec.Encode("awk: identifier byte limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorOutputFileLimit =
        AwkUtf8Codec.Encode("awk: output file limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorHostCallLimit =
        AwkUtf8Codec.Encode("awk: host call limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorMemoryLimit =
        AwkUtf8Codec.Encode("awk: memory limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorExecutionStepLimit =
        AwkUtf8Codec.Encode("awk: execution step limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorInputByteLimit =
        AwkUtf8Codec.Encode("awk: input byte limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorRecordLimit =
        AwkUtf8Codec.Encode("awk: input record limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorRegexOperationLimit =
        AwkUtf8Codec.Encode("awk: regular expression operation limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorSortComparisonLimit =
        AwkUtf8Codec.Encode("awk: array sort comparison limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorInputFileLimit =
        AwkUtf8Codec.Encode("awk: input file limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorPipeLimit =
        AwkUtf8Codec.Encode("awk: pipe limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorSourceLimit =
        AwkUtf8Codec.Encode("awk: source byte limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorSourceUnitLimit =
        AwkUtf8Codec.Encode("awk: source unit limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorExpandedSourceLimit =
        AwkUtf8Codec.Encode("awk: expanded source byte limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorFrontendCharacterLimit =
        AwkUtf8Codec.Encode("awk: frontend character limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorFrontendTokenLimit =
        AwkUtf8Codec.Encode("awk: frontend token limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorFrontendDiagnosticLimit =
        AwkUtf8Codec.Encode("awk: frontend diagnostic limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorIncludeDepthLimit =
        AwkUtf8Codec.Encode("awk: include depth limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorIncludeFileLimit =
        AwkUtf8Codec.Encode("awk: include file limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorFileRedirectionsDisabled =
        AwkUtf8Codec.Encode("awk: file redirections are disabled by execution policy\n");
    internal static readonly ReadOnlyMemory<byte> ErrorCommandPipesDisabled =
        AwkUtf8Codec.Encode("awk: command pipes are disabled by execution policy\n");
    internal static readonly ReadOnlyMemory<byte> ErrorSourceIncludesDisabled =
        AwkUtf8Codec.Encode("awk: source includes are disabled by execution policy\n");
    internal static readonly ReadOnlyMemory<byte> ErrorDynamicMainInputPathsDisabled =
        AwkUtf8Codec.Encode("awk: dynamic main-input paths are disabled by execution policy\n");
    internal static readonly ReadOnlyMemory<byte> ErrorPathOutsideExecutionRoot =
        AwkUtf8Codec.Encode("awk: path is outside the execution policy root\n");
    internal static readonly ReadOnlyMemory<byte> ErrorCommandTimeout =
        AwkUtf8Codec.Encode("awk: command pipe timed out\n");
    internal static readonly ReadOnlyMemory<byte> ErrorExecutionTimeout =
        AwkUtf8Codec.Encode("awk: execution timed out\n");
    internal static readonly ReadOnlyMemory<byte> ErrorHostOperationTimeout =
        AwkUtf8Codec.Encode("awk: host operation timed out\n");
    internal static readonly ReadOnlyMemory<byte> ErrorArgumentLimit =
        AwkUtf8Codec.Encode("awk: command argument limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorEnvironmentLimit =
        AwkUtf8Codec.Encode("awk: command environment limit exceeded\n");
    internal static readonly ReadOnlyMemory<byte> ErrorInvalidPath =
        AwkUtf8Codec.Encode("awk: invalid path\n");
    internal static readonly ReadOnlyMemory<byte> ErrorUnexpectedFailure =
        AwkUtf8Codec.Encode("awk: execution failed without exposing host exception details\n");
    internal static readonly ReadOnlyMemory<byte> ErrorSplitSeparatorArrayExtension =
        AwkUtf8Codec.Encode("awk: split: fourth argument is a GNU extension\n");

    internal static readonly ReadOnlyMemory<byte> ErrorLengthArrayArgument =
        AwkUtf8Codec.Encode("awk: length: received array argument\n");
    internal static readonly ReadOnlyMemory<byte> ErrorSwitchIsGnuExtension =
        AwkUtf8Codec.Encode("awk: switch is a GNU extension\n");
    internal static readonly ReadOnlyMemory<byte> ErrorInvalidCloseMode =
        AwkUtf8Codec.Encode("awk: close second argument must be `to' or `from'\n");
    internal const string ErrnoNoSuchFileOrDirectory = "No such file or directory";
    internal const string ErrnoRedirectionNeverOpened = "close of redirection that was never opened";
    internal static readonly ReadOnlyMemory<byte> ErrorUnsupportedGnuOptionPrefix =
        AwkUtf8Codec.Encode("awk: GNU option is unsupported in Lokad awk: ");
    internal static readonly ReadOnlyMemory<byte> HelpMessage = AwkUtf8Codec.Encode(
        "Usage: awk [OPTION]... 'program' [file...]\n" +
        "Process text files using awk patterns and actions.\n" +
        "\n" +
        "  -F fs, --field-separator=fs   use fs for the input field separator\n" +
        "  -f file, --file=file          read program source from file\n" +
        "  -e program, --source=program  use program as awk source text\n" +
        "  -v var=value, --assign=var=value\n" +
        "                                assign a variable before program execution\n" +
        "  -h, --help                    display this help and exit\n" +
        "  -V, --version                 display Lokad awk version and exit\n");
    internal static readonly ReadOnlyMemory<byte> VersionMessage = AwkUtf8Codec.Encode("Lokad awk\n");
    private static readonly ReadOnlyMemory<byte> ErrorNoSuchFilePrefix = AwkUtf8Codec.Encode("awk: ");
    private static readonly ReadOnlyMemory<byte> ErrorNoSuchFileSuffix =
        AwkUtf8Codec.Encode(": No such file or directory\n");
    private static readonly ReadOnlyMemory<byte> ErrorInvalidBuiltinArgumentCountPrefix =
        AwkUtf8Codec.Encode("awk: invalid number of arguments for ");
    private static readonly ReadOnlyMemory<byte> ErrorUndefinedSortComparisonPrefix =
        AwkUtf8Codec.Encode("awk: sort comparison function is not defined: ");
    private static readonly ReadOnlyMemory<byte> ErrorScalarAsArrayPrefix =
        AwkUtf8Codec.Encode("awk: attempt to use scalar `");
    private static readonly ReadOnlyMemory<byte> ErrorScalarAsArraySuffix =
        AwkUtf8Codec.Encode("' as an array\n");
    private static readonly ReadOnlyMemory<byte> ErrorArrayAsScalarPrefix =
        AwkUtf8Codec.Encode("awk: attempt to use array `");
    private static readonly ReadOnlyMemory<byte> ErrorArrayAsScalarSuffix =
        AwkUtf8Codec.Encode("' in a scalar context\n");
    private static readonly ReadOnlyMemory<byte> ErrorCannotModifySpecialArrayPrefix =
        AwkUtf8Codec.Encode("awk: cannot modify special array `");
    private static readonly ReadOnlyMemory<byte> ErrorCannotModifySpecialArraySuffix =
        AwkUtf8Codec.Encode("'\n");

    internal static ReadOnlyMemory<byte> FormatPathError(ReadOnlyMemory<byte> path)
    {
        var buffer = new byte[ErrorNoSuchFilePrefix.Length + path.Length + ErrorNoSuchFileSuffix.Length];
        var offset = 0;
        ErrorNoSuchFilePrefix.CopyTo(buffer.AsMemory(offset));
        offset += ErrorNoSuchFilePrefix.Length;
        path.CopyTo(buffer.AsMemory(offset));
        offset += path.Length;
        ErrorNoSuchFileSuffix.CopyTo(buffer.AsMemory(offset));
        return buffer;
    }

    internal static ReadOnlyMemory<byte> FormatPathError(ReadOnlyMemory<byte> path, string message)
    {
        var messageBytes = string.IsNullOrEmpty(message) ? AwkUtf8Codec.Encode("Operation failed.") : AwkUtf8Codec.Encode(message);
        var buffer = new byte[ErrorNoSuchFilePrefix.Length + path.Length + 2 + messageBytes.Length + 1];
        var offset = 0;
        ErrorNoSuchFilePrefix.CopyTo(buffer.AsMemory(offset));
        offset += ErrorNoSuchFilePrefix.Length;
        path.CopyTo(buffer.AsMemory(offset));
        offset += path.Length;
        buffer[offset++] = (byte)':';
        buffer[offset++] = (byte)' ';
        messageBytes.CopyTo(buffer.AsMemory(offset));
        offset += messageBytes.Length;
        buffer[offset] = (byte)'\n';
        return buffer;
    }

    internal static ReadOnlyMemory<byte> FormatAppendError(AwkFileDescriptor descriptor, int status) =>
        AwkUtf8Codec.Encode(
            "awk: file descriptor " +
            descriptor.Id +
            ": append failed with status " +
            status +
            "\n");

    internal static ReadOnlyMemory<byte> FormatPipeCreationError(
        AwkPipeCreationStatus status,
        string message) =>
        AwkUtf8Codec.Encode(
            status == AwkPipeCreationStatus.Unsupported
                ? "awk: pipe creation is unsupported: " + message + "\n"
                : "awk: pipe creation failed: " + message + "\n");

    internal static ReadOnlyMemory<byte> FormatDescriptorReadError(
        AwkFileDescriptor descriptor,
        string message) =>
        AwkUtf8Codec.Encode(
            "awk: file descriptor " +
            descriptor.Id +
            ": read failed: " +
            (string.IsNullOrEmpty(message) ? "Operation failed." : message) +
            "\n");

    internal static ReadOnlyMemory<byte> FormatInvalidCommandResultCount(int count) =>
        AwkUtf8Codec.Encode(
            "awk: host returned " +
            count +
            " command results for one command\n");

    internal static ReadOnlyMemory<byte> FormatDescriptorCloseError(AwkFileDescriptor descriptor) =>
        AwkUtf8Codec.Encode(
            "awk: file descriptor " +
            descriptor.Id +
            ": close failed\n");

    internal static ReadOnlyMemory<byte> FormatInvalidOption(string option)
    {
        var optionBytes = string.IsNullOrEmpty(option) ? ReadOnlyMemory<byte>.Empty : AwkUtf8Codec.Encode(option);
        var buffer = new byte[ErrorInvalidOptionPrefix.Length + optionBytes.Length + 1];
        AwkMessages.ErrorInvalidOptionPrefix.CopyTo(buffer.AsMemory());
        optionBytes.CopyTo(buffer.AsMemory(ErrorInvalidOptionPrefix.Length));
        buffer[^1] = (byte)'\n';
        return buffer;
    }

    internal static ReadOnlyMemory<byte> FormatUnsupportedGnuOption(string option)
    {
        var optionBytes = string.IsNullOrEmpty(option) ? ReadOnlyMemory<byte>.Empty : AwkUtf8Codec.Encode(option);
        var buffer = new byte[ErrorUnsupportedGnuOptionPrefix.Length + optionBytes.Length + 1];
        ErrorUnsupportedGnuOptionPrefix.CopyTo(buffer.AsMemory());
        optionBytes.CopyTo(buffer.AsMemory(ErrorUnsupportedGnuOptionPrefix.Length));
        buffer[^1] = (byte)'\n';
        return buffer;
    }

    internal static ReadOnlyMemory<byte> FormatUndefinedFunction(string name)
        => AwkUtf8Codec.Encode(FormatUndefinedFunctionText(name));

    internal static ReadOnlyMemory<byte> FormatInvalidBuiltinArgumentCount(string name)
    {
        var nameBytes = string.IsNullOrEmpty(name) ? ReadOnlyMemory<byte>.Empty : AwkUtf8Codec.Encode(name);
        var buffer = new byte[ErrorInvalidBuiltinArgumentCountPrefix.Length + nameBytes.Length + 1];
        ErrorInvalidBuiltinArgumentCountPrefix.CopyTo(buffer.AsMemory());
        nameBytes.CopyTo(buffer.AsMemory(ErrorInvalidBuiltinArgumentCountPrefix.Length));
        buffer[^1] = (byte)'\n';
        return buffer;
    }

    internal static ReadOnlyMemory<byte> FormatUndefinedFunctionWithLintWarning(string name)
        => AwkUtf8Codec.Encode(FormatUndefinedFunctionWithLintWarningText(name));

    internal static ReadOnlyMemory<byte> FormatUndefinedFunctionWithFatalLint(string name)
        => AwkUtf8Codec.Encode(FormatUndefinedFunctionWithFatalLintText(name));

    internal static string FormatUndefinedFunctionText(string name) =>
        "awk: undefined function: " + name + "\n";

    internal static string FormatUndefinedFunctionWithLintWarningText(string name) =>
        "awk: warning: function `" + name + "' called but never defined\n" +
        FormatUndefinedFunctionText(name);

    internal static string FormatUndefinedFunctionWithFatalLintText(string name) =>
        "awk: fatal: function `" + name + "' called but never defined\n";

    internal static ReadOnlyMemory<byte> FormatUndefinedSortComparison(string mode)
    {
        var modeBytes = string.IsNullOrEmpty(mode) ? ReadOnlyMemory<byte>.Empty : AwkUtf8Codec.Encode(mode);
        var buffer = new byte[ErrorUndefinedSortComparisonPrefix.Length + modeBytes.Length + 1];
        ErrorUndefinedSortComparisonPrefix.CopyTo(buffer.AsMemory());
        modeBytes.CopyTo(buffer.AsMemory(ErrorUndefinedSortComparisonPrefix.Length));
        buffer[^1] = (byte)'\n';
        return buffer;
    }

    internal static ReadOnlyMemory<byte> FormatScalarAsArray(string name) =>
        FormatSymbolDiagnostic(ErrorScalarAsArrayPrefix, name, ErrorScalarAsArraySuffix);

    internal static ReadOnlyMemory<byte> FormatArrayAsScalar(string name) =>
        FormatSymbolDiagnostic(ErrorArrayAsScalarPrefix, name, ErrorArrayAsScalarSuffix);

    internal static ReadOnlyMemory<byte> FormatCannotModifySpecialArray(string name) =>
        FormatSymbolDiagnostic(ErrorCannotModifySpecialArrayPrefix, name, ErrorCannotModifySpecialArraySuffix);

    internal static ReadOnlyMemory<byte> FormatUninitializedSpecialArrayElement(string name, string index) =>
        AwkUtf8Codec.Encode("awk: reference to uninitialized element `" + name + "[\"" + index + "\"]' is not allowed\n");

    internal static string FormatGensubThirdArgumentWarningText(string value) =>
        "awk: warning: gensub: third argument `" + value + "' treated as 1\n";

    internal static string FormatMathNegativeArgumentWarning(string name, string value) =>
        "awk: warning: " + name + ": received negative argument " + value + "\n";

    internal static string FormatMathRangeWarning(string name, string value) =>
        "awk: warning: " + name + ": argument " + value + " is out of range\n";

    internal static string FormatStringEscapeWarning(char escaped) =>
        "awk: warning: escape sequence `\\" + escaped + "' treated as plain `" + escaped + "'\n";

    internal static string FormatStringHexEscapeWarning() =>
        "awk: warning: no hex digits in `\\x' escape sequence\n";

    internal static string FormatStringUnicodeMissingDigitsWarning() =>
        "awk: warning: no hex digits in `\\u' escape sequence\n";

    internal static string FormatStringUnicodeInvalidScalarWarning() =>
        "awk: warning: invalid `\\u' escape sequence\n";

    private static ReadOnlyMemory<byte> FormatSymbolDiagnostic(
        ReadOnlyMemory<byte> prefix,
        string name,
        ReadOnlyMemory<byte> suffix)
    {
        var nameBytes = string.IsNullOrEmpty(name) ? ReadOnlyMemory<byte>.Empty : AwkUtf8Codec.Encode(name);
        var buffer = new byte[prefix.Length + nameBytes.Length + suffix.Length];
        var offset = 0;
        prefix.CopyTo(buffer.AsMemory(offset));
        offset += prefix.Length;
        nameBytes.CopyTo(buffer.AsMemory(offset));
        offset += nameBytes.Length;
        suffix.CopyTo(buffer.AsMemory(offset));
        return buffer;
    }
}
