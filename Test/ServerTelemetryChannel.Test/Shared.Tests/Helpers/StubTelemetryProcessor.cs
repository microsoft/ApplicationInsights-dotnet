namespace Microsoft.ApplicationInsights.Web.TestFramework
{
    using System;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

    /// <summary>
    /// A stub of <see cref="ITelemetryProcessor"/>.
    /// </summary>
    public sealed class StubTelemetryProcessor : ITelemetryProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StubTelemetryProcessor"/> class.
        /// </summary>
        public StubTelemetryProcessor(ITelemetryProcessor next)
        {
            this.OnProcess = telemetry => { };
            this.Next = next;
        }
        
        /// <summary>
        /// Gets or sets the callback invoked by the <see cref="Process"/> method.
        /// </summary>
        public Action<ITelemetry> OnProcess { get; set; }

        internal ITelemetryProcessor Next { get; set; }

        /// <summary>
        /// Implements the <see cref="ITelemetryProcessor.Process"/> method by invoking the <see cref="OnProcess"/> callback.
        /// </summary>
        public void Process(ITelemetry item)
        {
            this.OnProcess(item);
            if (this.Next != null)
            {
                this.Next.Process(item);
            }
        }
    }
}
