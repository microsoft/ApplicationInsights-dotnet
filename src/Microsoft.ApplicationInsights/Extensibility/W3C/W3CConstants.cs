namespace Microsoft.ApplicationInsights.Extensibility.W3C
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// W3C constants.
    /// </summary>
    internal static class W3CConstants
    {
        /// <summary>
        /// Legacy root Id tag name.
        /// </summary>
        internal const string LegacyRootIdProperty = "ai_legacyRootId";

        /// <summary>
        /// Legacy root Id tag name.
        /// </summary>
        internal const string LegacyRequestIdProperty = "ai_legacyRequestId";

        /// <summary>
        /// Default version value.
        /// </summary>
        internal const string DefaultVersion = "00";

        /// <summary>
        /// Default trace flag value.
        /// </summary>
        internal const string DefaultTraceFlag = "00";

        /// <summary>
        /// String representation of the invalid spanid of all zeroes.
        /// </summary>
        internal const string InvalidSpanID = "0000000000000000";
    }
}
