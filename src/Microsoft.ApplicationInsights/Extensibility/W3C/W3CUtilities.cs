namespace Microsoft.ApplicationInsights.Extensibility.W3C
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// W3C distributed tracing utilities.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class W3CUtilities
    {
        /// <summary>
        /// Generates random trace Id as per W3C Distributed tracing specification.
        /// https://github.com/w3c/distributed-tracing/blob/master/trace_context/HTTP_HEADER_FORMAT.md#trace-id
        /// </summary>
        /// <returns>Random 16 bytes array encoded as hex string</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("123")]
        public static string GenerateTraceId()
        {
            return $"{Guid.NewGuid():n}";
        }
    }
}
