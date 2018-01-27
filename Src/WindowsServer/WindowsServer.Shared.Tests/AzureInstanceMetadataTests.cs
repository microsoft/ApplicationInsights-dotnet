namespace Microsoft.ApplicationInsights.WindowsServer
{
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.Mock;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Assert = Xunit.Assert;

    [TestClass]
    public class AzureInstanceMetadataTests
    {
        [TestMethod]
        public void GetAzureInstanceMetadataFieldsAsExpected()
        {
            HeartbeatProviderMock hbeatMock = new HeartbeatProviderMock();
            AzureInstanceMetadataRequestMock azureInstanceRequestorMock = new AzureInstanceMetadataRequestMock();
            AzureHeartbeatProperties azureIMSFields = new AzureHeartbeatProperties(azureInstanceRequestorMock, true);
            foreach (string field in azureIMSFields.DefaultFields)
            {
                azureInstanceRequestorMock.ComputeFields.Add(field, $"testValue");
            }

            var taskWaiter = azureIMSFields.SetDefaultPayload(new string[] { }, hbeatMock).ConfigureAwait(false);
            Assert.True(taskWaiter.GetAwaiter().GetResult()); // no await for tests

            foreach (string fieldName in azureIMSFields.DefaultFields)
            {
                Assert.True(hbeatMock.HbeatProps.ContainsKey(fieldName));
                Assert.False(string.IsNullOrEmpty(hbeatMock.HbeatProps[fieldName]));
            }
        }

        [TestMethod]
        public void FailToObtainAzureInstanceMetadataFieldsAltogether()
        {
            HeartbeatProviderMock hbeatMock = new HeartbeatProviderMock();
            AzureInstanceMetadataRequestMock azureInstanceRequestorMock = new AzureInstanceMetadataRequestMock(
                getAllFields: () =>
                {
                    try
                    {
                        throw new System.Exception("Failure");
                    }
                    catch
                    {
                    }

                    return null;
                }, 
                getSingleFieldFunc: (a) => a);
            var azureIMSFields = new AzureHeartbeatProperties(azureInstanceRequestorMock, true);
            var defaultFields = azureIMSFields.DefaultFields;

            // not adding the fields we're looking for, simulation of the Azure Instance Metadata service not being present...
            var taskWaiter = azureIMSFields.SetDefaultPayload(new string[] { }, hbeatMock).ConfigureAwait(false);
            Assert.False(taskWaiter.GetAwaiter().GetResult()); // nop await for tests

            foreach (string fieldName in defaultFields)
            {
                Assert.False(hbeatMock.HbeatProps.ContainsKey(fieldName));
            }
        }
    }
}
