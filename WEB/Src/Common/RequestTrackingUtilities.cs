namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Collections.Specialized;
    using System.Web;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// This class encapsulates setting tracking properties on RequestTelemetry.
    /// This is used by both PostSamplingTelemetryProcessor and RequestTrackingTelemetryModule.
    /// </summary>
    internal static class RequestTrackingUtilities
    {
        /// <summary>Updates requestTelemetry from request with properties, which could be deferred till after sampling.</summary>
        /// <param name="requestTelemetry">RequestTelemetry to be updated.</param>
        /// <param name="request">HttpRequest containing Url and headers.</param>
        /// <param name="applicationIdProvider">Provider for current applicationId.</param>
        internal static void UpdateRequestTelemetryFromRequest(RequestTelemetry requestTelemetry, HttpRequest request, IApplicationIdProvider applicationIdProvider)
        {
            if (requestTelemetry == null || request == null)
            {
                return;
            }

            if (requestTelemetry.Url == null)
            {
                requestTelemetry.Url = request.Unvalidated.Url;
            }

            if (string.IsNullOrEmpty(requestTelemetry.Source))
            {
                var sourceAppId = GetSourceAppId(request.Unvalidated.Headers);
                string currentComponentAppId = GetApplicationId(applicationIdProvider, requestTelemetry.Context?.InstrumentationKey);
                // If the source header is present on the incoming request,
                // and it is an external component (not the same ikey as the one used by the current component),
                // then populate the source field.
                if (!string.IsNullOrEmpty(currentComponentAppId) &&
                    !string.IsNullOrEmpty(sourceAppId) &&
                    sourceAppId != currentComponentAppId)
                {
                    requestTelemetry.Source = sourceAppId;
                }
            }
        }

        private static string GetSourceAppId(NameValueCollection headers)
        {
            string sourceAppId = null;

            try
            {
                sourceAppId = headers.GetNameValueHeaderValue(
                    RequestResponseHeaders.RequestContextHeader,
                    RequestResponseHeaders.RequestContextCorrelationSourceKey);
            }
            catch (Exception ex)
            {
                AppMapCorrelationEventSource.Log.GetCrossComponentCorrelationHeaderFailed(ex.ToInvariantString());
            }

            return sourceAppId;
        }

        private static string GetApplicationId(IApplicationIdProvider applicationIdProvider, string instrumentationKey)
        {
            string currentComponentAppId = null;
            applicationIdProvider?.TryGetApplicationId(instrumentationKey, out currentComponentAppId);
            return currentComponentAppId;
        }
    }
}