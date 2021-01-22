namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal readonly struct InFlightTransmission
    {
        public InFlightTransmission(long flushAsyncId, Task<HttpWebResponseWrapper> transmissionTask)
        {
            this.FlushAsyncId = flushAsyncId;
            this.TransmissionTask = transmissionTask;
        }

        internal long FlushAsyncId { get; }

        internal Task<HttpWebResponseWrapper> TransmissionTask { get; }
    }
}
