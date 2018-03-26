namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using System.Globalization;
    using System.Text.RegularExpressions;

    internal static class CorrelationIdHelper
    {
        private const string CorrelationIdFormat = "cid-v1:{0}";

        /// <summary>
        /// Max length of AppId allowed in response from Breeze.
        /// </summary>
        private const int AppIdMaxLengeth = 50;

        /// <summary>
        /// Format an AppId string (ex: 00000000-0000-0000-0000-000000000000) 
        /// to a CorrelationId string (ex: ex: cid-v1:00000000-0000-0000-0000-000000000000).
        /// </summary>
        /// <param name="appId">Application Id is expected to be a Guid string.</param>
        /// <remarks>
        /// To protect against injection attacks, AppId will be truncated to a maximum length.
        /// CorrelationIds are expected to Http Header safe, and all non-ASCII characters will be removed.
        /// </remarks>
        internal static string FormatAppId(string appId)
        {
            // Arbitrary maximum length to guard against injections.
            appId = EnforceMaxLength(appId, AppIdMaxLengeth);
            if (string.IsNullOrWhiteSpace(appId))
            {
                return null;
            }

            // String must be sanitized to include only characters safe for http header.
            appId = SanitizeString(appId);

            return string.Format(CultureInfo.InvariantCulture, CorrelationIdFormat, appId);
        }

        /// <summary>
        /// Check a strings length and trim to a max length if needed.
        /// </summary>
        private static string EnforceMaxLength(string input, int maxLength)
        {
            if (input != null && input.Length > maxLength)
            {
                input = input.Substring(0, maxLength);
            }

            return input;
        }

        /// <summary>
        /// Http Headers only allow Printable US-ASCII characters.
        /// Remove all other characters.
        /// </summary>
        private static string SanitizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            // US-ASCII characters (hex: 0x00 - 0x7F) (decimal: 0-127)
            // ASCII Extended characters (hex: 0x80 - 0xFF) (decimal: 0-255) (NOT ALLOWED)
            // Non-Printable ASCII characters are (hex: 0x00 - 0x1F) (decimal: 0-31) (NOT ALLOWED)
            // Printable ASCII characters are (hex: 0x20 - 0xFF) (decimal: 32-255) 
            return Regex.Replace(input, @"[^\u0020-\u007F]", string.Empty);
        }
    }
}
