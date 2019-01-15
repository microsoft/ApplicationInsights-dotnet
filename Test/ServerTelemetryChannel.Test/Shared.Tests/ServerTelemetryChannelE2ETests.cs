namespace Microsoft.ApplicationInsights.WindowsServer.Channel
{
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Threading;

    [TestClass]
    public class ServerTelemetryChannelE2ETests
    {        
        [TestMethod]
        public void InitializesTransmitterWithNetworkAvailabilityPolicy()
        {

            var channel = new ServerTelemetryChannel();
            var config = new TelemetryConfiguration("dummy");

            channel.Initialize(config);
            Thread.Sleep(50);

            Assert.AreEqual(0, channel.Transmitter.Sender.Capacity);
        }
    }
}
