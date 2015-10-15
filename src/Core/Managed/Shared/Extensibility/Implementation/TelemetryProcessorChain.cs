namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Represents the TelemetryProcessor chain. Clients should use TelemetryProcessorChainBuilder to construct this object.
    /// </summary>
    public sealed class TelemetryProcessorChain
    {
        private ITelemetryProcessor firstTelemetryProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryProcessorChain" /> class.
        /// Marked internal, as clients should use TelemetryProcessorChainBuilder to build the processing chain.
        /// </summary>
        internal TelemetryProcessorChain()
        {            
        }
                
        internal ITelemetryProcessor FirstTelemetryProcessor
        {
            get { return this.firstTelemetryProcessor; }
            set { this.firstTelemetryProcessor = value; }
        }

        /// <summary>
        /// Invokes the process method in the first telemetry processor.
        /// </summary>        
        public void Process(ITelemetry item)
        {
            this.firstTelemetryProcessor.Process(item);
        }
    }
}
