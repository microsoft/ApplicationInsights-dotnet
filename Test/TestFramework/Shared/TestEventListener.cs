namespace Microsoft.ApplicationInsights.TestFramework
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
#if !NET40
    using System.Diagnostics.Tracing;
#endif
    using System.Threading;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    internal class TestEventListener : EventListener
    {
        private readonly ConcurrentQueue<EventWrittenEventArgs> events;
        private readonly AutoResetEvent eventWritten;

        private readonly bool waitForDelayedEvents;

        public TestEventListener(bool waitForDelayedEvents = true)
        {
            this.events = new ConcurrentQueue<EventWrittenEventArgs>();
            this.eventWritten = new AutoResetEvent(false);
            this.waitForDelayedEvents = waitForDelayedEvents;
            this.OnOnEventWritten = e =>
            {
                this.events.Enqueue(e);
                this.eventWritten.Set();
            };
        }

        public Action<EventSource> OnOnEventSourceCreated { get; set; }

        public Action<EventWrittenEventArgs> OnOnEventWritten { get; set; }

        public IEnumerable<EventWrittenEventArgs> Messages
        {
            get 
            {
                if (this.events.Count == 0 && this.waitForDelayedEvents)
                {
                    this.eventWritten.WaitOne(TimeSpan.FromSeconds(5));
                }

                while (this.events.Count != 0)
                {
                    EventWrittenEventArgs nextEvent;
                    if (this.events.TryDequeue(out nextEvent))
                    {
                        yield return nextEvent;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }
        
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            this.OnOnEventWritten(eventData);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // Check for null because this method is called by the base class constructor before we can initialize it
            Action<EventSource> callback = this.OnOnEventSourceCreated;
            if (callback != null)
            {
                callback(eventSource);
            }
        }
    }
}
