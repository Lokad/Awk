using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lokad.Awk;

internal sealed class AwkByteFieldSplitter(
    Func<AwkRegexPattern, Regex> createRegex,
    Func<AwkRegexLiteralText, Regex> createLiteralRegex,
    Func<Regex, AwkText, AwkLeasedReadOnlyList<AwkRegexMatch>> enumerateTextMatches,
    Func<Regex, AwkText, AwkLeasedReadOnlyList<AwkRegexMatchSpan>> enumerateMatchSpans,
    Func<bool> isIgnoreCaseEnabled,
    Func<bool> charactersAsBytes,
    Action throwIfCancellationRequested,
    AwkMemoryGovernor memoryGovernor)
{
    public AwkTextSplitResult SplitBySeparator(
        AwkText line,
        string separator,
        bool useDefaultSeparator,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        if (useDefaultSeparator)
            return SplitWhitespace(line, captureSeparators, counters);

        if (separator.Length == 0)
            return SplitCharacters(line, captureSeparators, charactersAsBytes(), counters);

        if (TryGetSingleByteLiteralSeparator(separator, out var separatorByte) && !isIgnoreCaseEnabled())
            return SplitSingleByteLiteral(line, separatorByte, captureSeparators, counters);

        var regex = separator.Length > 1
            ? createRegex(new AwkRegexPattern(separator))
            : createLiteralRegex(new AwkRegexLiteralText(separator));
        return SplitRegex(line, regex, captureSeparators, counters);
    }

    public IReadOnlyList<AwkFieldText> SplitRecordFieldsBySeparator(
        AwkText line,
        string separator,
        bool useDefaultSeparator,
        AwkRuntimeCounters counters)
    {
        if (useDefaultSeparator)
            return SplitWhitespaceRecordFields(line);

        if (separator.Length == 0)
            return ToOwnedFieldTexts(SplitCharacters(line, captureSeparators: false, charactersAsBytes(), counters).Fields);

        if (TryGetSingleByteLiteralSeparator(separator, out var separatorByte) && !isIgnoreCaseEnabled())
            return SplitSingleByteLiteralRecordFields(line, separatorByte);

        var regex = separator.Length > 1
            ? createRegex(new AwkRegexPattern(separator))
            : createLiteralRegex(new AwkRegexLiteralText(separator));
        return SplitRegexRecordFields(line, regex);
    }

    public IReadOnlyList<AwkFieldText> SplitParagraphRecordFieldsBySeparator(
        AwkText line,
        string separator,
        bool useDefaultSeparator,
        AwkRuntimeCounters counters)
    {
        static bool IsSingleCharacter(string value)
        {
            var enumerator = value.EnumerateRunes().GetEnumerator();
            return enumerator.MoveNext() && !enumerator.MoveNext();
        }

        if (useDefaultSeparator || !IsSingleCharacter(separator))
            return SplitRecordFieldsBySeparator(line, separator, useDefaultSeparator, counters);

        if (TryGetSingleByteLiteralSeparator(separator, out var separatorByte) && !isIgnoreCaseEnabled())
            return SplitSingleByteParagraphRecordFields(line, separatorByte);

        var regex = separator.Length > 1
            ? createRegex(new AwkRegexPattern(separator))
            : createLiteralRegex(new AwkRegexLiteralText(separator));
        return SplitRegexParagraphRecordFields(line, regex);
    }

    public AwkTextSplitResult Patsplit(
        AwkText input,
        string pattern,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        var regex = createRegex(new AwkRegexPattern(pattern));
        var fields = new List<AwkText>();
        var separators = captureSeparators ? new List<AwkText>() : null;
        var content = input.AsSpan();
        var position = 0;
        using var matches = enumerateTextMatches(regex, input);
        foreach (var match in matches)
        {
            throwIfCancellationRequested();
            if (separators != null)
                separators.Add(SliceTrustedFieldText(content, position, match.ByteIndex - position, counters));

            fields.Add(match.Value);
            position = match.ByteIndex + match.ByteLength;
        }

        if (separators != null)
            separators.Add(SliceTrustedFieldText(content, position, content.Length - position, counters));

        return new AwkTextSplitResult(fields, separators ?? []);
    }

    private AwkTextSplitResult SplitWhitespace(
        AwkText line,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        var content = line.AsSpan();
        if (content.Length == 0)
            return new AwkTextSplitResult([], captureSeparators ? [AwkText.Empty] : []);

        var fields = new List<AwkText>();
        var separators = captureSeparators ? new List<AwkText>() : [];
        var index = 0;
        var separatorStart = index;
        while (index < content.Length)
        {
            while (index < content.Length && IsDefaultFieldWhitespace(content[index]))
                index++;

            if (captureSeparators)
                separators.Add(SliceTrustedFieldText(content, separatorStart, index - separatorStart, counters));

            if (index >= content.Length)
                break;

            var start = index;
            while (index < content.Length && !IsDefaultFieldWhitespace(content[index]))
                index++;

            fields.Add(SliceTrustedFieldText(content, start, index - start, counters));
            separatorStart = index;
        }

        if (captureSeparators && separators.Count == fields.Count)
            separators.Add(SliceTrustedFieldText(content, separatorStart, content.Length - separatorStart, counters));

        return new AwkTextSplitResult(fields, separators);
    }

    private IReadOnlyList<AwkFieldText> SplitWhitespaceRecordFields(AwkText line)
    {
        var content = line.AsSpan();
        if (content.Length == 0)
            return [];

        var fields = new List<AwkFieldText>();
        var index = 0;
        while (index < content.Length)
        {
            while (index < content.Length && IsDefaultFieldWhitespace(content[index]))
                index++;

            if (index >= content.Length)
                break;

            var start = index;
            while (index < content.Length && !IsDefaultFieldWhitespace(content[index]))
                index++;

            fields.Add(AwkFieldText.FromRecordSlice(start, index - start));
        }

        return fields;
    }

    private AwkTextSplitResult SplitCharacters(
        AwkText line,
        bool captureSeparators,
        bool splitBytes,
        AwkRuntimeCounters counters)
    {
        if (line.ByteLength == 0)
            return new AwkTextSplitResult([], captureSeparators ? [AwkText.Empty] : []);

        if (splitBytes)
            return SplitBytes(line, captureSeparators, counters);

        var fields = new List<AwkText>();
        var content = line.AsSpan();
        var bytePosition = 0;
        while (bytePosition < content.Length)
        {
            var status = Rune.DecodeFromUtf8(content[bytePosition..], out _, out var consumed);
            if (status != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");

            fields.Add(line.CopyByteSlice(bytePosition, consumed, counters, memoryGovernor));
            bytePosition += consumed;
        }

        return new AwkTextSplitResult(
            fields,
            captureSeparators ? Enumerable.Repeat(AwkText.Empty, fields.Count + 1).ToArray() : []);
    }

    private AwkTextSplitResult SplitBytes(
        AwkText line,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        var content = line.AsSpan();
        for (var index = 0; index < content.Length; index++)
        {
            if (content[index] > 0x7F)
                throw new AwkRuntimeException(
                    AwkMessages.ErrorUnsupportedByteModeEmptyFieldSeparator,
                    AwkDiagnosticId.UnsupportedPolicy);
        }

        var fields = new AwkText[content.Length];
        for (var index = 0; index < content.Length; index++)
        {
            fields[index] = AwkText.FromTrustedOwnedBytes([content[index]], counters, memoryGovernor);
        }

        return new AwkTextSplitResult(
            fields,
            captureSeparators ? Enumerable.Repeat(AwkText.Empty, fields.Length + 1).ToArray() : []);
    }

    private AwkTextSplitResult SplitSingleByteLiteral(
        AwkText line,
        byte separator,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        var content = line.AsSpan();
        if (!captureSeparators)
        {
            var fieldCount = CountSingleByteFields(content, separator);
            var fieldArray = new AwkText[fieldCount];
            var fieldIndex = 0;
            var position = 0;
            while (true)
            {
                var next = content[position..].IndexOf(separator);
                if (next < 0)
                    break;

                fieldArray[fieldIndex++] = SliceTrustedFieldText(content, position, next, counters);
                position += next + 1;
            }

            fieldArray[fieldIndex] = SliceTrustedFieldText(content, position, content.Length - position, counters);
            return new AwkTextSplitResult(fieldArray, []);
        }

        var fields = new List<AwkText>();
        var separators = new List<AwkText> { AwkText.Empty };
        var current = 0;
        while (true)
        {
            var next = content[current..].IndexOf(separator);
            if (next < 0)
                break;

            fields.Add(SliceTrustedFieldText(content, current, next, counters));
            separators.Add(SliceTrustedFieldText(content, current + next, 1, counters));
            current += next + 1;
        }

        fields.Add(SliceTrustedFieldText(content, current, content.Length - current, counters));
        separators.Add(AwkText.Empty);
        return new AwkTextSplitResult(fields, separators);
    }

    private IReadOnlyList<AwkFieldText> SplitSingleByteLiteralRecordFields(AwkText line, byte separator)
    {
        var content = line.AsSpan();
        var fieldCount = CountSingleByteFields(content, separator);
        var fields = new AwkFieldText[fieldCount];
        var fieldIndex = 0;
        var position = 0;
        while (true)
        {
            var next = content[position..].IndexOf(separator);
            if (next < 0)
                break;

            fields[fieldIndex++] = AwkFieldText.FromRecordSlice(position, next);
            position += next + 1;
        }

        fields[fieldIndex] = AwkFieldText.FromRecordSlice(position, content.Length - position);
        return fields;
    }

    private static IReadOnlyList<AwkFieldText> SplitSingleByteParagraphRecordFields(AwkText line, byte separator)
    {
        var content = line.AsSpan();
        var fields = new List<AwkFieldText>();
        var position = 0;
        for (var index = 0; index < content.Length; index++)
        {
            if (content[index] != separator && content[index] != (byte)'\n')
                continue;

            fields.Add(AwkFieldText.FromRecordSlice(position, index - position));
            position = index + 1;
        }

        fields.Add(AwkFieldText.FromRecordSlice(position, content.Length - position));
        return fields;
    }

    private AwkTextSplitResult SplitRegex(
        AwkText line,
        Regex separator,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        if (!captureSeparators)
            return SplitRegexBySpans(line, separator, counters);

        var fields = new List<AwkText>();
        var separators = new List<AwkText> { AwkText.Empty };
        var content = line.AsSpan();
        var position = 0;
        using var matches = enumerateTextMatches(separator, line);
        foreach (var match in matches)
        {
            throwIfCancellationRequested();
            fields.Add(SliceTrustedFieldText(content, position, match.ByteIndex - position, counters));
            separators.Add(match.Value);
            position = AdvanceAfterRegexSeparator(line, match.ByteIndex, match.ByteLength);
        }

        fields.Add(SliceTrustedFieldText(content, position, content.Length - position, counters));
        separators.Add(AwkText.Empty);
        return new AwkTextSplitResult(fields, separators);
    }

    private IReadOnlyList<AwkFieldText> SplitRegexRecordFields(AwkText line, Regex separator)
    {
        var fields = new List<AwkFieldText>();
        var position = 0;
        using var matches = enumerateMatchSpans(separator, line);
        foreach (var match in matches)
        {
            throwIfCancellationRequested();
            fields.Add(AwkFieldText.FromRecordSlice(position, match.ByteIndex - position));
            position = AdvanceAfterRegexSeparator(line, match.ByteIndex, match.ByteLength);
        }

        fields.Add(AwkFieldText.FromRecordSlice(position, line.ByteLength - position));
        return fields;
    }

    private IReadOnlyList<AwkFieldText> SplitRegexParagraphRecordFields(AwkText line, Regex separator)
    {
        var content = line.AsSpan();
        var fields = new List<AwkFieldText>();
        var position = 0;
        using var matches = enumerateMatchSpans(separator, line);
        foreach (var match in matches)
        {
            throwIfCancellationRequested();
            while (position < match.ByteIndex)
            {
                var newlineOffset = content[position..match.ByteIndex].IndexOf((byte)'\n');
                if (newlineOffset < 0)
                    break;

                fields.Add(AwkFieldText.FromRecordSlice(position, newlineOffset));
                position += newlineOffset + 1;
            }

            fields.Add(AwkFieldText.FromRecordSlice(position, match.ByteIndex - position));
            position = AdvanceAfterRegexSeparator(line, match.ByteIndex, match.ByteLength);
        }

        while (position < content.Length)
        {
            var newlineOffset = content[position..].IndexOf((byte)'\n');
            if (newlineOffset < 0)
                break;

            fields.Add(AwkFieldText.FromRecordSlice(position, newlineOffset));
            position += newlineOffset + 1;
        }

        fields.Add(AwkFieldText.FromRecordSlice(position, content.Length - position));
        return fields;
    }

    private AwkTextSplitResult SplitRegexBySpans(
        AwkText line,
        Regex separator,
        AwkRuntimeCounters counters)
    {
        var fields = new List<AwkText>();
        var content = line.AsSpan();
        var position = 0;
        using var matches = enumerateMatchSpans(separator, line);
        foreach (var match in matches)
        {
            throwIfCancellationRequested();
            fields.Add(SliceTrustedFieldText(content, position, match.ByteIndex - position, counters));
            position = AdvanceAfterRegexSeparator(line, match.ByteIndex, match.ByteLength);
        }

        fields.Add(SliceTrustedFieldText(content, position, content.Length - position, counters));
        return new AwkTextSplitResult(fields, []);
    }

    private static IReadOnlyList<AwkFieldText> ToOwnedFieldTexts(IReadOnlyList<AwkText> fields)
    {
        if (fields.Count == 0)
            return [];

        var result = new AwkFieldText[fields.Count];
        for (var index = 0; index < fields.Count; index++)
            result[index] = AwkFieldText.FromOwned(fields[index]);

        return result;
    }

    private static bool IsDefaultFieldWhitespace(byte value) =>
        value is (byte)' ' or (byte)'\t' or (byte)'\n';

    private static int AdvanceAfterRegexSeparator(AwkText line, int byteIndex, int byteLength)
    {
        if (byteLength > 0 || byteIndex >= line.ByteLength)
            return byteIndex + byteLength;

        var status = Rune.DecodeFromUtf8(line.AsSpan()[byteIndex..], out _, out var bytesConsumed);
        return byteIndex + (status == OperationStatus.Done && bytesConsumed > 0 ? bytesConsumed : 1);
    }

    private static bool TryGetSingleByteLiteralSeparator(string separator, out byte separatorByte)
    {
        if (separator.Length == 1 && separator[0] <= 0x7F)
        {
            separatorByte = (byte)separator[0];
            return true;
        }

        separatorByte = 0;
        return false;
    }

    private static int CountSingleByteFields(ReadOnlySpan<byte> content, byte separator)
    {
        var count = 1;
        for (var index = 0; index < content.Length; index++)
        {
            if (content[index] == separator)
                count++;
        }

        return count;
    }

    private AwkText SliceTrustedFieldText(
        ReadOnlySpan<byte> content,
        int start,
        int length,
        AwkRuntimeCounters counters)
    {
        if (length == 0)
            return AwkText.Empty;

        return AwkText.FromTrustedOwnedBytes(content.Slice(start, length).ToArray(), counters, memoryGovernor);
    }
}
