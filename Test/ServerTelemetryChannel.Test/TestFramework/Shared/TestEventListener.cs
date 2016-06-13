namespace Microsoft.ApplicationInsights.Web.TestFramework
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

#if NET45
    using System.Diagnostics.Tracing;
#endif

#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    internal class TestEventListener : EventListener
    {
        private readonly ConcurrentQueue<EventWrittenEventArgs> events;
        
        public TestEventListener()
        {
            this.events = new ConcurrentQueue<EventWrittenEventArgs>();

            this.OnOnEventWritten = e =>
            {
                this.events.Enqueue(e);
            };
        }

        public Action<EventSource> OnOnEventSourceCreated { get; set; }

        public Action<EventWrittenEventArgs> OnOnEventWritten { get; set; }

        public IEnumerable<EventWrittenEventArgs> Messages
        {
            get { return this.events; }
        }
        
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            this.OnOnEventWritten(eventData);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // Check for null because this method is called by the base class constror before we can initialize it
            Action<EventSource> callback = this.OnOnEventSourceCreated;
            if (callback != null)
            {
                callback(eventSource);
            }
        }
    }
}
