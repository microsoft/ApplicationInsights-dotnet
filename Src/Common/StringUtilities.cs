namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.ApplicationInsights.W3C;

    /// <summary>
    /// Generic functions to perform common operations on a string.
    /// </summary>
#if DEPENDENCY_COLLECTOR
    public
#else
    internal
#endif
    static class StringUtilities
    {
        private static readonly uint[] Lookup32 = CreateLookup32();

        /// <summary>
        /// Check a strings length and trim to a max length if needed.
        /// </summary>
        public static string EnforceMaxLength(string input, int maxLength)
        {
            Debug.Assert(
                maxLength > 0,
                string.Format(CultureInfo.CurrentCulture, "{0} must be greater than 0", nameof(maxLength)));

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
        public static string GenerateTraceId()
        {
            return GenerateId(Guid.NewGuid().ToByteArray(), 0, 16);
        }

        /// <summary>
        /// Generates random span Id as per W3C Distributed tracing specification.
        /// https://github.com/w3c/distributed-tracing/blob/master/trace_context/HTTP_HEADER_FORMAT.md#span-id
        /// </summary>
        /// <returns>Random 8 bytes array encoded as hex string</returns>
        public static string GenerateSpanId()
        {
            return GenerateId(Guid.NewGuid().ToByteArray(), 0, 8);
        }

        /// <summary>
        /// Formats trace Id and span Id into valid Request-Id: |trace.span.
        /// </summary>
        /// <param name="traceId">Trace Id.</param>
        /// <param name="spanId">Span id.</param>
        /// <returns>valid Request-Id.</returns>
        public static string FormatRequestId(string traceId, string spanId)
        {
            return String.Concat("|", traceId, ".", spanId, ".");
        }

        /// <summary>
        /// Gets root id (string between '|' and the first dot) from the hierarchical Id.
        /// </summary>
        /// <param name="hierarchicalId">Id to extract root from.</param>
        /// <returns>Root operation id.</returns>
        internal static string GetRootId(string hierarchicalId)
        {
            // Returns the root Id from the '|' to the first '.' if any.
            int rootEnd = hierarchicalId.IndexOf('.');
            if (rootEnd < 0)
            {
                rootEnd = hierarchicalId.Length;
            }

            int rootStart = hierarchicalId[0] == '|' ? 1 : 0;
            return hierarchicalId.Substring(rootStart, rootEnd - rootStart);
        }

#pragma warning disable 612, 618
        internal static string FormatAzureTracestate(string appId)
        {
            return String.Concat(W3CConstants.AzureTracestateNamespace, "=", appId);
        }
#pragma warning restore 612, 618

        /// <summary>
        /// Converts byte array to hex lower case string.
        /// </summary>
        /// <returns>Array encoded as hex string</returns>
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
