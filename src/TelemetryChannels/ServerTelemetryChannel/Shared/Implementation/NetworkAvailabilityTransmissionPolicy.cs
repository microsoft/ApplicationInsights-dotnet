namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    
    internal class NetworkAvailabilityTransmissionPolicy : TransmissionPolicy
    {
        private readonly INetwork network;

        public NetworkAvailabilityTransmissionPolicy(INetwork network)
        {
            this.network = network;
        }

        public override void Initialize(Transmitter transmitter)
        {
            base.Initialize(transmitter);
            this.SubscribeToNetworkAddressChangedEvents();
            this.SetBufferAndSenderCapacity();
        }

        private void SubscribeToNetworkAddressChangedEvents()
        {
            try
            {
                this.network.AddAddressChangedEventHandler(this.HandleNetworkStatusChangedEvent);
            }
            catch (SocketException se) 
            {
                TelemetryChannelEventSource.Log.SubscribeToNetworkFailureWarning(se.ToString());
            }
            catch (NetworkInformationException nie)
            {
                TelemetryChannelEventSource.Log.SubscribeToNetworkFailureWarning(nie.ToString());
            }
        }

        private void HandleNetworkStatusChangedEvent(object sender, EventArgs e)
        {
            this.SetBufferAndSenderCapacity();
            this.Apply();
        }

        private void SetBufferAndSenderCapacity()
        {
            if (!this.IsNetworkAvailable())
            {
                this.MaxSenderCapacity = 0;
                this.MaxBufferCapacity = 0;
            }
            else
            {
                this.MaxSenderCapacity = null;
                this.MaxBufferCapacity = null;
            }

            this.LogCapacityChanged();
        }

        private bool IsNetworkAvailable()
        {
            try
            {
                return this.network.IsAvailable();
            }
            catch (Exception e) 
            {
                // Catch all exceptions because SocketException and NetworkInformationException are not defined on all platforms
                TelemetryChannelEventSource.Log.NetworkIsNotAvailableWarning(e.ToString());
                return true; 
            }
        }
    }
}
