namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using System.Globalization;

    internal static class CorrelationIdHelper
    {
        private const string CorrelationIdFormat = "cid-v1:{0}";

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
            ////appId = StringUtilities.EnforceMaxLength(appId, InjectionGuardConstants.AppIdMaxLengeth);
            ////if (string.IsNullOrWhiteSpace(appId))
            ////{
            ////    return null;
            ////}

            ////// String must be sanitized to include only characters safe for http header.
            ////appId = HeadersUtilities.SanitizeString(appId);

            return string.Format(CultureInfo.InvariantCulture, CorrelationIdFormat, appId);
        }
    }
}
