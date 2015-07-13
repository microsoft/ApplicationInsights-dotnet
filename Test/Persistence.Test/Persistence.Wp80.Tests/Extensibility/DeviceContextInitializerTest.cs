namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Security.Cryptography;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Phone.Info;
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using Assert = Xunit.Assert;

    /// <summary>
    /// Silverlight-specific tests for <see cref="DeviceContextInitializer"/>.
    /// </summary>
    public partial class DeviceContextInitializerTest
    {
        [TestMethod]
        public void ReadingDeviceUniqueIdYieldsCorrectValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.Id);

            source.Initialize(telemetryContext);

            string id = telemetryContext.Device.Id;

            object uniqueId;
            DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out uniqueId);
            using (SHA256 hasher = new SHA256Managed())
            {
                Assert.Equal(Convert.ToBase64String(hasher.ComputeHash((byte[])uniqueId)), id);
            }
        }
    }
}
