namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal class TelemetrySerializer
    {
        internal readonly Transmitter Transmitter;

        private const string TelemetryEndpointRelativeUri = "v2/track";
        private const string TelemetryEndpointFullUri = "https://dc.services.visualstudio.com/v2/track";
        private readonly Uri defaultTelemetryEndpoint = new Uri(TelemetryEndpointFullUri);

        public TelemetrySerializer(Transmitter transmitter)
        {
            if (transmitter == null)
            {
                throw new ArgumentNullException("transmitter");
            }

            this.Transmitter = transmitter;
        }

        protected TelemetrySerializer()
        {
            // for stubs and mocks
        }

        /// <summary>
        /// Gets or sets the endpoint address.  
        /// </summary>
        /// <remarks>
        /// If endpoint address is set to null, the default endpoint address will be used. 
        /// </remarks>
        public Uri ServerTelemetryChannelEndpointAddress { get; set; }

        internal Uri EffectiveEndpointAddress
        {
            get
            {
                if (this.ServerTelemetryChannelEndpointAddress != null)
                {
                    return this.ServerTelemetryChannelEndpointAddress;
                }
                else if (TelemetryConfiguration.Active.GetApplicationInsightsEndpointBaseUri() != null)
                {
                    return new Uri(TelemetryConfiguration.Active.GetApplicationInsightsEndpointBaseUri(), TelemetryEndpointRelativeUri);
                }
                else
                {
                    return this.defaultTelemetryEndpoint;
                }
            }
        }

        public virtual void Serialize(ICollection<ITelemetry> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (!items.Any())
            {
                throw new ArgumentException("One or more telemetry item is expected", nameof(items));
            }

            var transmission = new Transmission(this.EffectiveEndpointAddress, items);
            this.Transmitter.Enqueue(transmission);
        }
    }
}
