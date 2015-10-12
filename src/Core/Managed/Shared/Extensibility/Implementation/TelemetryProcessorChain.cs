namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Represents the TelemetryProcessor chain. 
    /// </summary>
    public sealed class TelemetryProcessorChain
    {
        private SnapshottingList<ITelemetryProcessor> telemetryProcessors = new SnapshottingList<ITelemetryProcessor>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryProcessorChain" /> class.
        /// Marked internal, as clients should use TelemetryProcessorBuilder to build the processing chain.
        /// </summary>
        internal TelemetryProcessorChain()
        {            
        }
                
        internal SnapshottingList<ITelemetryProcessor> TelemetryProcessors
        {
            get { return this.telemetryProcessors; }
            set { this.telemetryProcessors = value; }
        }

        /// <summary>
        /// Invokes the process method in the first telemetry processor.
        /// </summary>        
        internal void Process(ITelemetry item)
        {
            this.telemetryProcessors.First().Process(item);
        }
    }
}
