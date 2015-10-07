namespace Microsoft.ApplicationInsights.TestFramework
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// A stub of <see cref="ITelemetryProcessor"/>.
    /// </summary>
    public sealed class StubTelemetryProcessor2 : ITelemetryProcessor
    {
        /// <summary>
        /// Made public for testing if the chain of processors is correctly created.
        /// </summary>
        public ITelemetryProcessor next;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubTelemetryProcessor"/> class.
        /// </summary>
        public StubTelemetryProcessor2(ITelemetryProcessor next)
        {
            this.next = next;
        }
        
        /// <summary>
        /// Implements the <see cref="ITelemetryProcessor.Initialize"/> method by invoking the process method
        /// </summary>
        public void Process(ITelemetry telemetry)
        {
            
        }
    }
}
