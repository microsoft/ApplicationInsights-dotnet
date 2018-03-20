namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation.DataContracts;
    using Microsoft.ApplicationInsights.WindowsServer.Mock;
#if NETCORE
    using Microsoft.AspNetCore.Http;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    /// <summary>
    /// Tests the heartbeat functionality through actual local-only Http calls to mimic
    /// end to end functionality as closely as possible.
    /// </summary>
    [TestClass]
    public class AzureInstanceMetadataEndToEndTests
    {
        internal const string MockTestUri = "http://localhost:9922/";

        [TestMethod]
        public void SpoofedResponseFromAzureIMSDoesntCrash()
        {
            var testMetadata = this.GetTestMetadata();
            string testPath = "spoofedResponse";

            using (new AzureInstanceMetadataServiceMock(
                AzureInstanceMetadataEndToEndTests.MockTestUri,
                testPath,
                (response) =>
                {
                    response.StatusCode = (int)HttpStatusCode.OK;

                    var jsonStream = this.GetTestMetadataStream(testMetadata);
                    response.SetContentLength(jsonStream.Length);
                    response.ContentType = "application/json";
                    response.SetContentEncoding(Encoding.UTF8);
                    response.WriteStreamToBody(jsonStream);
                }))
            {
                var azureIms = new AzureMetadataRequestor
                {
                    BaseAimsUri = string.Concat(AzureInstanceMetadataEndToEndTests.MockTestUri, testPath, "/")
                };

                var azureImsProps = new AzureComputeMetadataHeartbeatPropertyProvider();
                var azureIMSData = azureIms.GetAzureComputeMetadataAsync();
                azureIMSData.Wait();

                foreach (string fieldName in azureImsProps.ExpectedAzureImsFields)
                {
                    string fieldValue = azureIMSData.Result.GetValueForField(fieldName);
                    Assert.NotNull(fieldValue);
                    Assert.Equal(fieldValue, testMetadata.GetValueForField(fieldName));
                }
            }
        }

        [TestMethod]
        public void AzureImsResponseTooLargeStopsCollection()
        {
            string testPath = "tooLarge";

            using (new AzureInstanceMetadataServiceMock(
                AzureInstanceMetadataEndToEndTests.MockTestUri,
                testPath,
                (response) =>
                {
                    response.StatusCode = (int)HttpStatusCode.OK;

                    var tester = this.GetTestMetadata();
                    
                    // ensure we will be outside the max allowed content size by setting a single text field to max length + 1
                    var testStuff = new char[AzureMetadataRequestor.AzureImsMaxResponseBufferSize + 1];
                    for (int i = 0; i < (AzureMetadataRequestor.AzureImsMaxResponseBufferSize + 1); ++i)
                    {
                        testStuff[i] = (char)( (int)'a' + (i % 26) );
                    }
                    tester.Publisher = new string(testStuff);

                    var jsonStream = this.GetTestMetadataStream(tester);
                    response.SetContentLength(3 * jsonStream.Length);
                    response.ContentType = "application/json";
                    response.SetContentEncoding(Encoding.UTF8);
                    response.WriteStreamToBody(jsonStream);
                }))
            {
                var azureIms = new AzureMetadataRequestor
                {
                    BaseAimsUri = string.Concat(AzureInstanceMetadataEndToEndTests.MockTestUri, testPath, "/")
                };

                var azureIMSData = azureIms.GetAzureComputeMetadataAsync();
                azureIMSData.Wait();

                Assert.Null(azureIMSData.Result);
            }
        }

        [TestMethod]
        public void AzureImsResponseExcludesMalformedValues()
        {
            string testPath = "malformedValues";
            using (new AzureInstanceMetadataServiceMock(
                AzureInstanceMetadataEndToEndTests.MockTestUri,
                testPath,
                (response) =>
                {
                    response.StatusCode = (int)HttpStatusCode.OK;

                    // make it a malicious-ish response...
                    var malformedData = this.GetTestMetadata();
                    malformedData.Name = "Not allowed for VM names";
                    malformedData.ResourceGroupName = "Not allowed for resource group name";
                    malformedData.SubscriptionId = "Definitely-not-a GUID up here";
                    var malformedJsonStream = this.GetTestMetadataStream(malformedData);

                    response.SetContentLength(malformedJsonStream.Length);
                    response.ContentType = "application/json";
                    response.SetContentEncoding(Encoding.UTF8);
                    response.WriteStreamToBody(malformedJsonStream);
                }))
            {
                var azureIms = new AzureMetadataRequestor
                {
                    BaseAimsUri = string.Concat(AzureInstanceMetadataEndToEndTests.MockTestUri, testPath, "/")
                };

                var azureImsProps = new AzureComputeMetadataHeartbeatPropertyProvider(azureIms);
                var hbeatProvider = new HeartbeatProviderMock();
                var azureIMSData = azureImsProps.SetDefaultPayloadAsync(hbeatProvider);
                azureIMSData.Wait();

                Assert.Empty(hbeatProvider.HbeatProps["azInst_name"]);
                Assert.Empty(hbeatProvider.HbeatProps["azInst_resourceGroupName"]);
                Assert.Empty(hbeatProvider.HbeatProps["azInst_subscriptionId"]);
            }
        }

        [TestMethod]
        public void AzureImsResponseTimesOut()
        {
            string testPath = "timeOut";
            using (new AzureInstanceMetadataServiceMock(
                AzureInstanceMetadataEndToEndTests.MockTestUri,
                testPath,
                (response) =>
                {
                    // wait for longer than the request timeout
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    response.StatusCode = (int)HttpStatusCode.OK;

                    var jsonStream = this.GetTestMetadataStream();
                    response.SetContentLength(jsonStream.Length);
                    response.ContentType = "application/json";
                    response.SetContentEncoding(Encoding.UTF8);
                    response.WriteStreamToBody(jsonStream);
                }))
            {
                var azureIms = new AzureMetadataRequestor
                {
                    BaseAimsUri = string.Concat(AzureInstanceMetadataEndToEndTests.MockTestUri, testPath, "/"),
                    AzureImsRequestTimeout = TimeSpan.FromSeconds(1)
                };

                var azureIMSData = azureIms.GetAzureComputeMetadataAsync();
                azureIMSData.Wait();

                Assert.Null(azureIMSData.Result);
            }
        }

        [TestMethod]
        public void AzureImsResponseUnsuccessful()
        {
            string testPath = "errorForbidden";

            using (new AzureInstanceMetadataServiceMock(
                AzureInstanceMetadataEndToEndTests.MockTestUri,
                testPath,
                (response) =>
                {
                    // don't send anything in content at all, or the context defaults to 200 OK
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                }))
            {
                var azureIms = new AzureMetadataRequestor
                {
                    BaseAimsUri = string.Concat(AzureInstanceMetadataEndToEndTests.MockTestUri, testPath, "/")
                };

                var azureIMSData = azureIms.GetAzureComputeMetadataAsync();
                azureIMSData.Wait();

                Assert.Null(azureIMSData.Result);
            }
        }

        /// <summary>
        /// Return a memory stream adequate for testing.
        /// </summary>
        /// <param name="inst">An Azure instance metadata compute object.</param>
        /// <returns>Azure Instance Compute Metadata as a JSON-encoded MemoryStream.</returns>
        private MemoryStream GetTestMetadataStream(AzureInstanceComputeMetadata inst = null)
        {
            if (inst == null)
            {
                inst = this.GetTestMetadata();
            }

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AzureInstanceComputeMetadata));

            MemoryStream jsonStream = new MemoryStream();
            serializer.WriteObject(jsonStream, inst);

            return jsonStream;
        }

        /// <summary>
        /// Creates test data for heartbeat e2e.
        /// </summary>
        /// <returns>An Azure Instance Metadata Compute object suitable for use in testing.</returns>
        private AzureInstanceComputeMetadata GetTestMetadata()
        {
            return new AzureInstanceComputeMetadata()
            {
                Location = "US-West",
                Name = "test-vm01",
                Offer = "D9_USWest",
                OsType = "Linux",
                PlacementGroupId = "placement-grp",
                PlatformFaultDomain = "0",
                PlatformUpdateDomain = "0",
                Publisher = "Microsoft",
                ResourceGroupName = "test.resource-group_01",
                Sku = "Windows_10",
                SubscriptionId = Guid.NewGuid().ToString(),
                Tags = "thisTag;thatTag",
                Version = "10.8a",
                VmId = Guid.NewGuid().ToString(),
                VmSize = "A8"
            };
        }
    }
}
