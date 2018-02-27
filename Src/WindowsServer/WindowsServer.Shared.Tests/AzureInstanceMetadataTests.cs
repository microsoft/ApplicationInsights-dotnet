namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
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

            var taskWaiter = azureIMSFields.SetDefaultPayload(new string[] { }, hbeatMock).ConfigureAwait(false);
            Assert.True(taskWaiter.GetAwaiter().GetResult()); // no await for tests

            foreach (string fieldName in azureIMSFields.ExpectedAzureImsFields)
            {
                string expectedFieldName = string.Concat(AzureHeartbeatProperties.HeartbeatPropertyPrefix, fieldName);
                Assert.True(hbeatMock.HbeatProps.ContainsKey(expectedFieldName));
                Assert.False(string.IsNullOrEmpty(hbeatMock.HbeatProps[expectedFieldName]));
            }
        }

        [TestMethod]
        public void FailToObtainAzureInstanceMetadataFieldsAltogether()
        {
            HeartbeatProviderMock hbeatMock = new HeartbeatProviderMock();
            AzureInstanceMetadataRequestMock azureInstanceRequestorMock = new AzureInstanceMetadataRequestMock(
                getComputeMetadata: () =>
                {
                    try
                    {
                        throw new System.Exception("Failure");
                    }
                    catch
                    {
                    }

                    return null;
                });
            var azureIMSFields = new AzureHeartbeatProperties(azureInstanceRequestorMock, true);
            var defaultFields = azureIMSFields.ExpectedAzureImsFields;

            // not adding the fields we're looking for, simulation of the Azure Instance Metadata service not being present...
            var taskWaiter = azureIMSFields.SetDefaultPayload(new string[] { }, hbeatMock).ConfigureAwait(false);
            Assert.False(taskWaiter.GetAwaiter().GetResult()); // nop await for tests

            foreach (string fieldName in defaultFields)
            {
                Assert.False(hbeatMock.HbeatProps.ContainsKey(fieldName));
            }
        }

        [TestMethod]
        public void AzureIMSGetAllFieldsFailsWithException()
        {
            var requestor = new AzureMetadataRequestor(makeAzureIMSRequestor: (string uri) =>
            {
                throw new HttpRequestException("MaxResponseContentLength exceeded");
            });

            try
            {
                var result = requestor.GetAzureComputeMetadata();
                Assert.True(null == result.GetAwaiter().GetResult());
            }
            catch
            {
                Assert.True(false, "Expectation is that exceptions will be handled within AzureMetadataRequestor, not the calling code.");
            }
        }
    }
}
