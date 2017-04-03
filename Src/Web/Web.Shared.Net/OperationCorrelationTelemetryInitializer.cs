namespace Microsoft.ApplicationInsights.Web
{
#if NET40
    using System.Collections.Generic;
#else
    using System.Diagnostics;
#endif
    using System.Web;
    using Common;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
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
            ActivityHelpers.ParentOperationIdHeaderName = RequestResponseHeaders.StandardParentIdHeader;
            ActivityHelpers.RootOperationIdHeaderName = RequestResponseHeaders.StandardRootIdHeader;
        }

        /// <summary>
        /// Gets or sets the name of the header to get parent operation Id from.
        /// </summary>
        public string ParentOperationIdHeaderName
        {
            get
            {
                return ActivityHelpers.ParentOperationIdHeaderName;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ActivityHelpers.ParentOperationIdHeaderName = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the header to get root operation Id from.
        /// </summary>
        public string RootOperationIdHeaderName
        {
            get
            {
                return ActivityHelpers.RootOperationIdHeaderName;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ActivityHelpers.RootOperationIdHeaderName = value;
                }
            }
        }

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
            // Telemetry is initialized by Base SDK OperationCorrelationTelemetryInitializer from the call context /Current Activity
            // However we still may lose CorrelationContext/AsyncLocal due to IIS managed/native thread hops. 
            // We protect from it with PreRequestHandlerExecute event, that happens right before the handler
            // However some telemetry may be reported between BeginRequest and PreRequestHandlerExecute in the HttpModule pipeline
            // So this telemetry initializer works when:
            //   - telemetry was tracked before PreRequestHandlerExecute
            //   - AND execution context was lost in the HttpModule pipeline: base SDK OperationCorrelationTelemetryInitializer had nothing to initialize telemetry with
            //   - AND Telemetry was tracked from the thread that has HttpContext.Current (synchronously in HttpModule)
            //
            // In other cases, telemetry is not guaranteed to be properly initialized:
            // - if tracked in custom HttpModule
            // - AND in async method or after async method was invoked
            // - AND when the execution context is lost
            if (telemetry != requestTelemetry)
            {
                if (!string.IsNullOrEmpty(telemetry.Context.Operation.Id))
                {
                    // telemetry is already initialized
                    return;
                }

                telemetry.Context.Operation.Id = requestTelemetry.Context.Operation.Id;
                if (string.IsNullOrEmpty(telemetry.Context.Operation.ParentId))
                {
                    telemetry.Context.Operation.ParentId = requestTelemetry.Id;
                }
#if NET45
                var activity = platformContext.Items[ActivityHelpers.RequestActivityItemName] as Activity;
                if (activity == null)
                {
                    return;
                }

                foreach (var item in activity.Baggage)
                {
                    if (!telemetry.Context.Properties.ContainsKey(item.Key))
                    {
                        telemetry.Context.Properties.Add(item);
                    }
                }
#else
                var correlationContext = platformContext.Items[ActivityHelpers.CorrelationContextItemName] as IDictionary<string, string>;
                if (correlationContext != null)
                {
                    foreach (var item in correlationContext)
                    {
                        if (!telemetry.Context.Properties.ContainsKey(item.Key))
                        {
                            telemetry.Context.Properties.Add(item);
                        }
                    }
                }
#endif
            }
        }
    }
}
