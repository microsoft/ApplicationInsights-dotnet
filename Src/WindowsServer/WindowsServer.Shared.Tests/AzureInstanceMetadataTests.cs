namespace Microsoft.ApplicationInsights.WindowsServer
{
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationInsights.WindowsServer.Mock;
    using Assert = Xunit.Assert;

    [TestClass]
    public class AzureInstanceMetadataTests
    {
        [TestMethod]
        public void GetAzureInstanceMetadataFieldsAsExpected()
        {
            HeartbeatProviderMock hbeatMock = new HeartbeatProviderMock();
            AzureInstanceMetadataRequestMock azureInstanceRequestorMock = new AzureInstanceMetadataRequestMock();
            AzureHeartbeatProperties azFields = new AzureHeartbeatProperties(azureInstanceRequestorMock, true);
            int counter = 1;
            foreach (string field in azFields.DefaultFields)
            {
                azureInstanceRequestorMock.computeFields.Add(field, $"testValue{counter++}");
            }

            var taskWaiter = azFields.SetDefaultPayload(new string[] { }, hbeatMock).ConfigureAwait(false);
            Assert.True(taskWaiter.GetAwaiter().GetResult()); // no await for tests

            foreach (string fieldName in azFields.DefaultFields)
            {
                Assert.True(hbeatMock.hbeatProps.ContainsKey(fieldName));
                Assert.False(string.IsNullOrEmpty(hbeatMock.hbeatProps[fieldName]));
            }
        }

        [TestMethod]
        public void FailToObtainAzureInstanceMetadataFieldsAltogether()
        {
            HeartbeatProviderMock hbeatMock = new HeartbeatProviderMock();
            AzureInstanceMetadataRequestMock azureInstanceRequestorMock = new AzureInstanceMetadataRequestMock();
            var azFields = new AzureHeartbeatProperties(azureInstanceRequestorMock, true);
            var defaultFields = azFields.DefaultFields;
            // not adding the fields we're looking for, simulation of the Azure Instance Metadata service not being present...

            var taskWaiter = azFields.SetDefaultPayload(new string[] { }, hbeatMock).ConfigureAwait(false);
            Assert.True(taskWaiter.GetAwaiter().GetResult()); // nop await for tests

            foreach (string fieldName in defaultFields)
            {
                Assert.True(hbeatMock.hbeatProps.ContainsKey(fieldName));
                Assert.True(string.IsNullOrEmpty(hbeatMock.hbeatProps[fieldName]));
            }
        }
    }
}
