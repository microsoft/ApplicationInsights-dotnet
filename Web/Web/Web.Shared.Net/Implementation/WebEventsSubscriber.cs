namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Collections.Generic;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Threading;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    internal class WebEventsSubscriber : EventListener
    {
        private const long AllKeyword = -1;

        private readonly IDictionary<int, Action<EventWrittenEventArgs>> handlers;

        public WebEventsSubscriber(IDictionary<int, Action<EventWrittenEventArgs>> handlers)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException("handlers");
            }

            this.handlers = handlers;
            this.EnableEvents(WebEventsPublisher.Log, EventLevel.LogAlways, (EventKeywords)AllKeyword);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Action<EventWrittenEventArgs> handler;

            if (this.handlers.TryGetValue(eventData.EventId, out handler))
            {
                try
                {
                    handler(eventData);
                }
                catch (ThreadAbortException)
                {
                    WebEventSource.Log.ThreadAbortWarning();
                }
                catch (Exception exc)
                {
                    WebEventSource.Log.HanderFailure(exc.ToInvariantString());
                }
            }
        }
    }
}
