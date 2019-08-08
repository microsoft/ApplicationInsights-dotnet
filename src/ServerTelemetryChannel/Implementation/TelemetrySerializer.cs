namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal class TelemetrySerializer
    {
        internal readonly Transmitter Transmitter;

        private const string DefaultEndpointAddress = "https://dc.services.visualstudio.com/v2/track"; // TODO: REMOVE
        private Uri endpointAddress = new Uri(DefaultEndpointAddress);

        public TelemetrySerializer(Transmitter transmitter)
        {
            if (transmitter == null)
            {
                throw new ArgumentNullException(nameof(transmitter));
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
        public Uri EndpointAddress 
        {
            get 
            { 
                return this.endpointAddress; 
            }

            set
            {
                this.endpointAddress = value ?? throw new ArgumentNullException(nameof(value), nameof(this.EndpointAddress) + " cannot be Null");
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

            var transmission = new Transmission(this.endpointAddress, items);
            this.Transmitter.Enqueue(transmission);
        }
    }
}
