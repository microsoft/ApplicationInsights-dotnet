namespace Microsoft.ApplicationInsights.Shared.Extensibility.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// An <see cref="ITelemetryProcessor"/> that just passes data to its downstream processor.
    /// </summary>
    internal class PassThroughProcessor : ITelemetryProcessor
    {
        private ITelemetryProcessor next;

        public PassThroughProcessor(ITelemetryProcessor next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            this.next = next;
        }

        internal ITelemetryProcessor Next => this.next;

        public void Process(ITelemetry item)
        {
            this.next.Process(item);
        }
    }
}
