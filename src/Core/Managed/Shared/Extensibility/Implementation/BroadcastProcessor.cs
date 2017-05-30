namespace Microsoft.ApplicationInsights.Shared.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// An <see cref="ITelemetryProcessor"/> that asynchronously sends the data to multiple child telemetry processors.
    /// </summary>
    internal class BroadcastProcessor : ITelemetryProcessor, IDisposable
    {
        private List<TelemetryDispatcher> childrenDispatchers;
        private bool isDisposed;
        private object disposeLock;

        public BroadcastProcessor(IEnumerable<ITelemetryProcessor> children, AsyncCallOptions asyncCallOptions = null)
        {
            if (children == null)
            {
                throw new ArgumentNullException(nameof(children));
            }

            int childrenCount = children.Count();
            if (childrenCount < 2)
            {
                throw new ArgumentException("BroadcastProcessor requires two or more children", nameof(children));
            }

            this.isDisposed = false;
            this.disposeLock = new object();

            if (asyncCallOptions == null)
            {
                asyncCallOptions = new AsyncCallOptions();
                asyncCallOptions.MaxDegreeOfParallelism = 1;  // Do not assume that telemetry processors or channels are thread-safe.
            }

            this.childrenDispatchers = new List<TelemetryDispatcher>(childrenCount);
            bool firstChild = true;
            foreach (ITelemetryProcessor child in children)
            {
                this.childrenDispatchers.Add(new TelemetryDispatcher(child, !firstChild, asyncCallOptions));
                firstChild = false;
            }
        }

        public void Process(ITelemetry item)
        {
            foreach (TelemetryDispatcher dispatcher in this.childrenDispatchers)
            {
                dispatcher.Process(item);
            }
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            lock (this.disposeLock)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.isDisposed = true;

                foreach (TelemetryDispatcher dispatcher in this.childrenDispatchers)
                {
                    dispatcher.Dispose();
                }
            }
        }

        /// <summary>
        /// Utility class that clones incoming telemetry as necessary and sends it to one of the telemetry processor chains 
        /// handled by the parent BroadcastProcessor.
        /// </summary>
        private class TelemetryDispatcher : IDisposable
        {
            private AsyncCall<ITelemetry> processorCall;
            private bool cloneBeforeDispatch;
            private bool isDisposed;
            private ITelemetryProcessor processor;

            public TelemetryDispatcher(ITelemetryProcessor processor, bool cloneBeforeDispatch, AsyncCallOptions asyncCallOptions)
            {
                Debug.Assert(processor != null, "Telemetry processor should not be null");
                this.processor = processor;
                this.cloneBeforeDispatch = cloneBeforeDispatch;
                this.processorCall = new AsyncCall<ITelemetry>(this.ProcessTelemetry, asyncCallOptions);
                this.isDisposed = false;
            }

            public void Dispose()
            {
                this.isDisposed = true;
                this.processorCall.CompleteAsync();
            }

            public void Process(ITelemetry telemetry)
            {
                if (this.isDisposed)
                {
                    // Do not throw ObjectDisposedException here in case there is some residual pipeline activity going on.
                    // Just ignore telemetry that comes in past disposal time.
                    return;
                }

                if (this.cloneBeforeDispatch)
                {
                    var cloneable = telemetry as IDeepCloneable<ITelemetry>;
                    if (cloneable != null)
                    {
                        telemetry = cloneable.DeepClone();
                    }
                    else
                    {
                        CoreEventSource.Log.TelemetryNotCloneable(telemetry.GetType().FullName);
                    }
                }

                this.processorCall.Post(telemetry);
            }

            private void ProcessTelemetry(ITelemetry telemetry)
            {
                try
                {
                    this.processor.Process(telemetry);
                }
                catch (Exception e)
                {
                    CoreEventSource.Log.FailedToSend(e.ToString());
                    throw;
                }
            }
        }
    }
}
