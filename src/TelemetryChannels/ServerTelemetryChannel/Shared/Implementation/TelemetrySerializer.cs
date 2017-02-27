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

        private const string DefaultEndpointAddress = "https://dc.services.visualstudio.com/v2/track";
        private Uri endpointAddress = new Uri(DefaultEndpointAddress);

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
        public Uri EndpointAddress 
        {
            get 
            { 
                return this.endpointAddress; 
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("EndpointAddress");
                }

                this.endpointAddress = value;
            }
        }

        public virtual void Serialize(ICollection<ITelemetry> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (!items.Any())
            {
                throw new ArgumentException("One or more telemetry item is expected", "items");
            }

            var transmission = new Transmission(this.endpointAddress, items);
            this.Transmitter.Enqueue(transmission);
        }
    }
}
