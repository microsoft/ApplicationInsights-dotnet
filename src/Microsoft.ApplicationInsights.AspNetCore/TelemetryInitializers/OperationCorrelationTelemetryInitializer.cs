namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// A telemetry initializer that will set the correlation context for all telemetry items in web application.
    /// </summary>
    internal class OperationCorrelationTelemetryInitializer : TelemetryInitializerBase
    {
        private ICorrelationIdLookupHelper correlationIdLookupHelper = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationCorrelationTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor for retrieving the current HTTP context.</param>
        /// <param name="correlationIdLookupHelper">A store for correlation ids that we don't have to query it everytime.</param>
        public OperationCorrelationTelemetryInitializer(
            IHttpContextAccessor httpContextAccessor, ICorrelationIdLookupHelper correlationIdLookupHelper) : base(httpContextAccessor)
        {
            if (correlationIdLookupHelper == null)
            {
                throw new ArgumentNullException(nameof(correlationIdLookupHelper));
            }
            this.correlationIdLookupHelper = correlationIdLookupHelper;
        }

        /// <summary>
        /// Implements initialization logic.
        /// </summary>
        /// <param name="platformContext">Http context</param>
        /// <param name="requestTelemetry">Request telemetry object associated with the current request.</param>
        /// <param name="telemetry">Telemetry item to initialize.</param>
        protected override void OnInitializeTelemetry(
            HttpContext platformContext,
            RequestTelemetry requestTelemetry,
            ITelemetry telemetry)
        {
            OperationContext parentContext = requestTelemetry.Context.Operation;

            // Make sure that RequestTelemetry is initialized.
            if (string.IsNullOrEmpty(parentContext.Id))
            {
                parentContext.Id = requestTelemetry.Id;
            }

            if (telemetry != requestTelemetry)
            {
                if (string.IsNullOrEmpty(telemetry.Context.Operation.ParentId))
                {
                    telemetry.Context.Operation.ParentId = requestTelemetry.Id;
                }

                if (string.IsNullOrEmpty(telemetry.Context.Operation.Id))
                {
                    telemetry.Context.Operation.Id = parentContext.Id;
                }
            }

            HttpRequest currentRequest = platformContext.Request;
            if (currentRequest?.Headers != null && string.IsNullOrEmpty(requestTelemetry.Source))
            {
                string headerCorrelationId = HttpHeadersUtilities.GetRequestContextKeyValue(currentRequest.Headers, RequestResponseHeaders.RequestContextSourceKey);
                
                string appCorrelationId = null;
                // If the source header is present on the incoming request, and it is an external component (not the same ikey as the one used by the current component), populate the source field.
                if (!string.IsNullOrEmpty(headerCorrelationId))
                {
                    if (string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey))
                    {
                        requestTelemetry.Source = headerCorrelationId;
                    }
                    else if (this.correlationIdLookupHelper.TryGetXComponentCorrelationId(requestTelemetry.Context.InstrumentationKey, out appCorrelationId) &&
                        appCorrelationId != headerCorrelationId)
                    {
                        requestTelemetry.Source = headerCorrelationId;
                    }
                }
            }
        }
    }
}