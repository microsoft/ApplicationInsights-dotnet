namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// A telemetry initializer that will set the correlation context for all telemetry items in web application.
    /// </summary>
    internal class OperationCorrelationTelemetryInitializer : TelemetryInitializerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationCorrelationTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor for retrieving the current HTTP context.</param>
        public OperationCorrelationTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
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
                string sourceIkey = currentRequest.Headers[RequestResponseHeaders.SourceInstrumentationKeyHeader];

                // If the source header is present on the incoming request, and it is an external component (not the same ikey as the one used by the current component), populate the source field.
                if (!string.IsNullOrEmpty(sourceIkey) &&
                    (string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey) ||
                        sourceIkey != InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(requestTelemetry.Context.InstrumentationKey)))
                {
                    requestTelemetry.Source = sourceIkey;
                }
            }
        }
    }
}
