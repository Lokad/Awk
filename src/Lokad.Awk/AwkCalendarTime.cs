namespace Lokad.Awk;

/// <summary>Bounded Gregorian normalization; zone rules come from the embedding policy.</summary>
internal static class AwkCalendarTime
{
    private const long SecondsPerDay = 86_400;
    private const long MinimumYear = (long)int.MinValue + 1900;
    private const long MaximumYear = (long)int.MaxValue + 1900;

    public static long MakeTime(
        ReadOnlySpan<byte> specification,
        bool utc,
        TimeZoneInfo timeZone,
        TimeZoneInfo.AdjustmentRule[] rules,
        AwkRuntimeResourceMonitor resources,
        AwkMemoryAccounting memoryAccounting)
    {
        var reader = new DateReader(specification, resources);
        Span<long> values = stackalloc long[7];
        for (var index = 0; index < 6; index++)
        {
            if (!reader.TryReadInteger(out values[index]) ||
                index > 0 && values[index] is < int.MinValue or > int.MaxValue)
            {
                return -1;
            }
        }
        values[6] = reader.TryReadInteger(out var dst) && dst is >= int.MinValue and <= int.MaxValue ? dst : -1;
        if (values[0] is < MinimumYear or > MaximumYear || values[1] == int.MinValue)
            return -1;

        var monthIndex = values[1] - 1;
        var yearDelta = FloorDivide(monthIndex, 12);
        var year = values[0] + yearDelta;
        var month = monthIndex - yearDelta * 12 + 1;
        var seconds = DaysFromCivil(year, month) * SecondsPerDay + (values[2] - 1) * SecondsPerDay +
            values[3] * 3600 + values[4] * 60 + values[5];
        var normalizedYear = YearFromDays(FloorDivide(seconds, SecondsPerDay));
        if (normalizedYear is < MinimumYear or > MaximumYear)
            return -1;
        if (utc)
            return seconds;

        // Fixed zones do not need DateTime's narrower year range. Dynamic zone
        // rules are defined by TimeZoneInfo only for years 1 through 9999.
        if (rules.Length == 0)
            return seconds - OffsetSeconds(timeZone.BaseUtcOffset) - (values[6] > 0 ? 3600 : 0);
        if (seconds is < -62_135_596_800 or > 253_402_300_799)
            return -1;

        var local = DateTime.SpecifyKind(DateTime.UnixEpoch, DateTimeKind.Unspecified).AddSeconds(seconds);
        var offset = timeZone.GetUtcOffset(local);
        if (values[6] < 0 && timeZone.IsAmbiguousTime(local))
        {
            using var lease = memoryAccounting.ReserveTransient(AwkRuntimeMemory.EstimateTimeZoneOffsetsBytes());
            var offsets = timeZone.GetAmbiguousTimeOffsets(local);
            // Select the later occurrence consistently, including negative DST
            // and transitions that change the standard base offset.
            offset = offsets[0] < offsets[1] ? offsets[0] : offsets[1];
        }
        else if (values[6] >= 0)
        {
            offset = timeZone.BaseUtcOffset;
            var daylightDelta = TimeSpan.FromHours(1);
            if (FindRule(local.Date, rules) is { } rule)
            {
                offset += rule.BaseUtcOffsetDelta;
                if (rule.DaylightDelta != TimeSpan.Zero)
                    daylightDelta = rule.DaylightDelta;
            }
            // Explicit DST flags select their requested offset, even out of season.
            if (values[6] > 0)
                offset += daylightDelta;
        }
        return seconds - OffsetSeconds(offset);
    }

    private static long OffsetSeconds(TimeSpan offset) => offset.Ticks / TimeSpan.TicksPerSecond;

    private static TimeZoneInfo.AdjustmentRule? FindRule(DateTime date, TimeZoneInfo.AdjustmentRule[] rules)
    {
        var first = 0;
        var last = rules.Length - 1;
        while (first <= last)
        {
            var middle = first + (last - first) / 2;
            var rule = rules[middle];
            if (date < rule.DateStart.Date)
                last = middle - 1;
            else if (date > rule.DateEnd.Date)
                first = middle + 1;
            else
                return rule;
        }
        return null;
    }

    // Gregorian civil arithmetic uses 400-year eras, including astronomical year
    // zero and negative years. Intermediate bounds follow the admitted tm fields.
    private static long DaysFromCivil(long year, long month)
    {
        year -= month <= 2 ? 1 : 0;
        var era = FloorDivide(year, 400);
        var yearOfEra = year - era * 400;
        var dayOfYear = (153 * (month + (month > 2 ? -3 : 9)) + 2) / 5;
        var dayOfEra = yearOfEra * 365 + yearOfEra / 4 - yearOfEra / 100 + dayOfYear;
        return era * 146_097 + dayOfEra - 719_468;
    }

    private static long YearFromDays(long days)
    {
        days += 719_468;
        var era = FloorDivide(days, 146_097);
        var dayOfEra = days - era * 146_097;
        var yearOfEra = (dayOfEra - dayOfEra / 1460 + dayOfEra / 36_524 - dayOfEra / 146_096) / 365;
        var year = yearOfEra + era * 400;
        var dayOfYear = dayOfEra - (365 * yearOfEra + yearOfEra / 4 - yearOfEra / 100);
        var month = (5 * dayOfYear + 2) / 153;
        return year + (month >= 10 ? 1 : 0);
    }

    private static long FloorDivide(long value, long divisor)
    {
        var quotient = value / divisor;
        return value % divisor < 0 ? quotient - 1 : quotient;
    }

    private ref struct DateReader(ReadOnlySpan<byte> bytes, AwkRuntimeResourceMonitor resources)
    {
        private readonly ReadOnlySpan<byte> _bytes = bytes;
        private int _position;
        private int _admitted;

        public bool TryReadInteger(out long value)
        {
            value = 0;
            while (Peek() is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r' or (byte)'\v' or (byte)'\f')
                Advance();
            var sign = Peek();
            var negative = sign == (byte)'-';
            if (sign is (byte)'+' or (byte)'-')
                Advance();
            var first = _position;
            ulong magnitude = 0;
            var limit = negative ? 1UL << 63 : long.MaxValue;
            while (Peek() is >= (byte)'0' and <= (byte)'9')
            {
                var digit = (uint)(Peek() - (byte)'0');
                if (magnitude > (limit - digit) / 10)
                    return false;
                magnitude = magnitude * 10 + digit;
                Advance();
            }
            if (_position == first)
                return false;
            value = negative ? magnitude == 1UL << 63 ? long.MinValue : -(long)magnitude : (long)magnitude;
            return true;
        }

        private byte Peek()
        {
            if (_position == _bytes.Length)
                return 0;
            if (_admitted == 0)
            {
                _admitted = Math.Min(AwkExecutionGovernor.WorkChunkBytes, _bytes.Length - _position);
                resources.RecordByteWork(_admitted);
            }
            return _bytes[_position];
        }

        private void Advance()
        {
            _position++;
            _admitted--;
        }
    }
}
