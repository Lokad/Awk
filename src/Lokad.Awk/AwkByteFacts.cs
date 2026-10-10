namespace Lokad.Awk;

internal static class AwkByteFacts
{
    public static byte ToAsciiLower(byte value) =>
        value is >= (byte)'A' and <= (byte)'Z'
            ? (byte)(value + 32)
            : value;

    public static int GetOrdinalHashCode(ReadOnlySpan<byte> bytes, Action? checkpoint = null)
    {
        // Stable FNV-1a keeps byte text and compact ordinal array keys in the
        // same hash domain without materializing the ordinal as AwkText.
        unchecked
        {
            var hash = (int)2166136261;
            for (var index = 0; index < bytes.Length; index++)
            {
                if ((index & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                    checkpoint?.Invoke();
                hash ^= bytes[index];
                hash *= 16777619;
            }

            checkpoint?.Invoke();
            return hash;
        }
    }
}
