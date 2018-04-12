namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.AspNetCore.Common;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// A telemetry initializer that will set the correlation context for all telemetry items in web application.
    /// </summary>
    internal class OperationCorrelationTelemetryInitializer : TelemetryInitializerBase
    {
        private readonly IApplicationIdProvider applicationIdProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationCorrelationTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor for retrieving the current HTTP context.</param>
        /// <param name="applicationIdProvider">Nullable Provider for resolving application Id to be used by Correlation.</param>
        public OperationCorrelationTelemetryInitializer(IHttpContextAccessor httpContextAccessor, IApplicationIdProvider applicationIdProvider = null) : base(httpContextAccessor)
        {
            this.applicationIdProvider = applicationIdProvider;
        }

        /// <summary>
        /// Implements initialization logic.
        /// </summary>
        /// <param name="platformContext">Http context</param>
        /// <param name="requestTelemetry">Request telemetry object associated with the current request.</param>
        /// <param name="telemetry">Telemetry item to initialize.</param>
        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            HttpRequest currentRequest = platformContext.Request;
            if (currentRequest?.Headers != null && string.IsNullOrEmpty(requestTelemetry.Source))
            {
                string headerCorrelationId = HttpHeadersUtilities.GetRequestContextKeyValue(currentRequest.Headers, RequestResponseHeaders.RequestContextSourceKey);

                string applicationId = null;
                // If the source header is present on the incoming request, and it is an external component (not the same ikey as the one used by the current component), populate the source field.
                if (!string.IsNullOrEmpty(headerCorrelationId))
                {
                    headerCorrelationId = StringUtilities.EnforceMaxLength(headerCorrelationId, InjectionGuardConstants.AppIdMaxLengeth);
                    if (string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey))
                    {
                        requestTelemetry.Source = headerCorrelationId;
                    }
                    else if ((this.applicationIdProvider?.TryGetApplicationId(requestTelemetry.Context.InstrumentationKey, out applicationId) ?? false)
                        && applicationId != headerCorrelationId)
                    {
                        requestTelemetry.Source = headerCorrelationId;
                    }
                }
            }
        }
    }
}