using System;
using System.Globalization;

namespace Microsoft.ApplicationInsights.AspNetCore.Common
{
    using System.Diagnostics;

    /// <summary>
    /// Generic functions to perform common operations on a string.
    /// </summary>
    public static class StringUtilities
    {
        private static readonly uint[] Lookup32 = CreateLookup32();

        /// <summary>
        /// Check a strings length and trim to a max length if needed.
        /// </summary>
        public static string EnforceMaxLength(string input, int maxLength)
        {
            // TODO: remove/obsolete and use StringUtilities from Web SDK
            Debug.Assert(input != null, $"{nameof(input)} must not be null");
            Debug.Assert(maxLength > 0, $"{nameof(maxLength)} must be greater than 0");

            if (input != null && input.Length > maxLength)
            {
                input = input.Substring(0, maxLength);
            }

            return input;
        }

        /// <summary>
        /// Generates random trace Id as per W3C Distributed tracing specification.
        /// https://github.com/w3c/distributed-tracing/blob/master/trace_context/HTTP_HEADER_FORMAT.md#trace-id
        /// </summary>
        /// <returns>Random 16 bytes array encoded as hex string</returns>
        internal static string GenerateTraceId()
        {
            // See https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/24343727#24343727
            var bytes = Guid.NewGuid().ToByteArray();

            var result = new char[32];
            for (int i = 0; i < 16; i++)
            {
                var val = Lookup32[bytes[i]];
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
