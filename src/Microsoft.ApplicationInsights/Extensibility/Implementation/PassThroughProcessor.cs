namespace Microsoft.ApplicationInsights.Shared.Extensibility.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// An <see cref="ITelemetryProcessor"/> that just passes data to its downstream telemetry sink.
    /// </summary>
    internal class PassThroughProcessor : ITelemetryProcessor
    {
        private TelemetrySink sink;

        public PassThroughProcessor(TelemetrySink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            this.sink = sink;
        }

        internal TelemetrySink Sink => this.sink;

        public void Process(ITelemetry item)
        {
            this.sink.Process(item);
        }
    }
}
