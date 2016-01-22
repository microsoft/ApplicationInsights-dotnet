namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Web;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.Implementation;

    /// <summary>
    /// Represents a telemetry processor for filtering out requests by handler.
    /// </summary>
    public sealed class HandlerTelemetryProcessor : ITelemetryProcessor
    {
        private readonly IList<FilterRequest> handlersToFilter = new List<FilterRequest>();

        /// <summary>
        /// Initializes a new instance of the <see cref="HandlerTelemetryProcessor" /> class.
        /// </summary>
        /// <param name="next">Next TelemetryProcessor in call chain.</param>
        public HandlerTelemetryProcessor(ITelemetryProcessor next)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }

            this.Next = next;
        }

        /// <summary>
        /// Gets the list of handler types for which requests telemetry will not be collected
        /// if request was successful.
        /// </summary>
        public IList<FilterRequest> Handlers
        {
            get
            {
                return this.handlersToFilter;
            }
        }

        /// <summary>
        /// Gets or sets the next TelemetryProcessor in call chain.
        /// </summary>
        private ITelemetryProcessor Next { get; set; }

        /// <summary>
        /// Processes telemetry item.
        /// </summary>
        /// <param name="item">Telemetry item to process.</param>
        public void Process(Channel.ITelemetry item)
        {
            if (HttpContext.Current != null)
            {
                var response = HttpContext.Current.GetResponse();
                if (response != null && response.StatusCode < 400)
                {
                    if (this.IsHandlerToFilter(HttpContext.Current.Handler))
                    {
                        WebEventSource.Log.WebRequestFilteredOutByRequestHandler();
                        return;
                    }
                }
            }

            this.Next.Process(item);
        }

        /// <summary>
        /// Checks whether or not handler is a transfer handler.
        /// </summary>
        /// <param name="handler">An instance of handler to validate.</param>
        /// <returns>True if handler is a transfer handler, otherwise - False.</returns>
        private bool IsHandlerToFilter(IHttpHandler handler)
        {
            if (handler != null)
            {
                var handlerName = handler.GetType().FullName;
                foreach (var h in this.handlersToFilter.Select(t => t.Value))
                {
                    if (string.Equals(handlerName, h, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
