using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

namespace Microsoft.ApplicationInsights.Extensibility
{
    public class TransmissionProcessor : ITelemetryProcessor
    {
        private ITelemetryProcessor next;
        private ITelemetryChannel channel;
        private TelemetryConfiguration configuration;

        /// <summary>
        /// Gets or sets the channel used by the client helper. Note that this doesn't need to be public as a customer can create a new client 
        /// with a new channel via telemetry configuration.
        /// </summary>
        internal ITelemetryChannel Channel
        {
            get
            {
                ITelemetryChannel output = this.channel;
                if (output == null)
                {
                    output = this.configuration.TelemetryChannel;
                    this.channel = output;
                }

                return output;
            }

            set
            {
                this.channel = value;
            }
        }        

        public TransmissionProcessor(ITelemetryProcessor next, TelemetryConfiguration configuration)
        {
            this.next = next;
            this.configuration = configuration;
        }

        public void Process(ITelemetry item)
        {
            if (this.Channel == null)
            {
                throw new InvalidOperationException("Telemetry channel should be configured for telemetry configuration before tracking telemetry.");
            }
            try
            {
                this.Channel.Send(item);
            }          
             catch (Exception e)
            {
                CoreEventSource.Log.LogVerbose("TransmissionProcessor process failed: ", e.ToString());
            }
        }
    }
}
