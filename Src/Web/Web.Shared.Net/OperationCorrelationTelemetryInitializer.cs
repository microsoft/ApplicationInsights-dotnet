namespace Microsoft.ApplicationInsights.Web
{
    using System.Web;
    using Common;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;

    /// <summary>
    /// A telemetry initializer that will set the correlation context for all telemetry items in web application.
    /// </summary>
    public class OperationCorrelationTelemetryInitializer : WebTelemetryInitializerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationCorrelationTelemetryInitializer"/> class.
        /// </summary>
        public OperationCorrelationTelemetryInitializer()
        {
            this.ParentOperationIdHeaderName = RequestResponseHeaders.StandardParentIdHeader;
            this.RootOperationIdHeaderName = RequestResponseHeaders.StandardRootIdHeader;
        }

        /// <summary>
        /// Gets or sets the name of the header to get parent operation Id from.
        /// </summary>
        public string ParentOperationIdHeaderName { get; set; }

        /// <summary>
        /// Gets or sets the name of the header to get root operation Id from.
        /// </summary>
        public string RootOperationIdHeaderName { get; set; }

        /// <summary>
        /// Implements initialization logic.
        /// </summary>
        /// <param name="platformContext">Http context.</param>
        /// <param name="requestTelemetry">Request telemetry object associated with the current request.</param>
        /// <param name="telemetry">Telemetry item to initialize.</param>
        protected override void OnInitializeTelemetry(
            HttpContext platformContext,
            RequestTelemetry requestTelemetry,
            ITelemetry telemetry)
        {
            OperationContext parentContext = requestTelemetry.Context.Operation;
            HttpRequest currentRequest = platformContext.Request;

            // Make sure that RequestTelemetry is initialized.
            if (string.IsNullOrEmpty(parentContext.ParentId))
            {
                if (!string.IsNullOrWhiteSpace(this.ParentOperationIdHeaderName))
                {
                    var parentId = currentRequest.UnvalidatedGetHeader(this.ParentOperationIdHeaderName);
                    if (!string.IsNullOrEmpty(parentId))
                    {
                        parentContext.ParentId = parentId;
                    }
                }
            }

            if (string.IsNullOrEmpty(parentContext.Id))
            {
                if (!string.IsNullOrWhiteSpace(this.RootOperationIdHeaderName))
                {
                    var rootId = currentRequest.UnvalidatedGetHeader(this.RootOperationIdHeaderName);
                    if (!string.IsNullOrEmpty(rootId))
                    {
                        parentContext.Id = rootId;
                    }
                }

                if (string.IsNullOrEmpty(parentContext.Id))
                {
                    parentContext.Id = requestTelemetry.Id;
                }
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
            
            if (string.IsNullOrEmpty(requestTelemetry.Source) && currentRequest.Headers != null)
            {
                string sourceIkey = currentRequest.Headers[RequestResponseHeaders.SourceInstrumentationKeyHeader];

                // If the source header is present on the incoming request,
                // and it is an external component (not the same ikey as the one used by the current component),
                // then populate the source field.
                if (!string.IsNullOrEmpty(sourceIkey)
                    && !string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey)                    
                    && sourceIkey != InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(requestTelemetry.Context.InstrumentationKey))
                {
                    requestTelemetry.Source = sourceIkey;
                }
            }
        }
    }
}
