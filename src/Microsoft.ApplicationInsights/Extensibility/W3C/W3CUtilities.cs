namespace Microsoft.ApplicationInsights.Extensibility.W3C
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// W3C distributed tracing utilities.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class W3CUtilities
    {
        private static readonly uint[] Lookup32 = CreateLookup32();

        /// <summary>
        /// Generates random trace Id as per W3C Distributed tracing specification.
        /// https://github.com/w3c/distributed-tracing/blob/master/trace_context/HTTP_HEADER_FORMAT.md#trace-id .
        /// </summary>
        /// <returns>Random 16 bytes array encoded as hex string.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string GenerateTraceId()
        {
            byte[] firstHalf = BitConverter.GetBytes(WeakConcurrentRandom.Instance.Next());
            byte[] secondHalf = BitConverter.GetBytes(WeakConcurrentRandom.Instance.Next());

            return GenerateId(firstHalf, secondHalf, 0, 16);
        }

        /// <summary>
        /// Generates random span Id as per W3C Distributed tracing specification.
        /// https://github.com/w3c/distributed-tracing/blob/master/trace_context/HTTP_HEADER_FORMAT.md#span-id .
        /// </summary>
        /// <returns>Random 8 bytes array encoded as hex string.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static string GenerateSpanId()
        {
            return GenerateId(BitConverter.GetBytes(WeakConcurrentRandom.Instance.Next()), 0, 8);
        }

        /// <summary>
        /// Converts byte array to hex lower case string.
        /// </summary>
        /// <returns>Array encoded as hex string.</returns>
        private static string GenerateId(byte[] bytes, int start, int length)
        {
            // See https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/24343727#24343727
            var result = new char[length * 2];
            for (int i = start; i < start + length; i++)
            {
                var val = Lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[(2 * i) + 1] = (char)(val >> 16);
            }

            return new string(result);
        }

        /// <summary>
        /// Converts byte arrays to hex lower case string.
        /// </summary>
        /// <returns>Array encoded as hex string.</returns>
        private static string GenerateId(byte[] firstHalf, byte[] secondHalf, int start, int length)
        {
            // See https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/24343727#24343727
            var result = new char[length * 2];
            int arrayBorder = length / 2;
            for (int i = start; i < start + length; i++)
            {
                var val = Lookup32[i < arrayBorder ? firstHalf[i] : secondHalf[i - arrayBorder]];
                result[2 * i] = (char)val;
                result[(2 * i) + 1] = (char)(val >> 16);
            }

            return new string(result);
        }

        private static uint[] CreateLookup32()
        {
            // See https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/24343727#24343727
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("x2", CultureInfo.InvariantCulture);
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }

            return result;
        }
    }
}
