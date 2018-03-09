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
            CancellationTokenSource cts = new CancellationTokenSource();
            var testMetadata = this.GetTestMetadata();

            using (new AzureInstanceMetadataServiceMock(
                AzureInstanceMetadataEndToEndTests.MockTestUri, 
                (HttpListenerContext context) =>
            {
                HttpListenerResponse response = context.Response;
                context.Response.StatusCode = 200;

                response.ContentEncoding = Encoding.UTF8;
                var jsonStream = this.GetTestMetadataStream(testMetadata);
                response.ContentLength64 = (int)jsonStream.Length;
                context.Response.ContentType = "application/json";
                jsonStream.WriteTo(context.Response.OutputStream);
            }))
            {
                var azureIms = new AzureMetadataRequestor
                {
                    BaseAimsUri = AzureInstanceMetadataEndToEndTests.MockTestUri
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
            CancellationTokenSource cts = new CancellationTokenSource();

            using (new AzureInstanceMetadataServiceMock(
                AzureInstanceMetadataEndToEndTests.MockTestUri, 
                (HttpListenerContext context) =>
            {
                HttpListenerResponse response = context.Response;
                context.Response.StatusCode = 200;

                // Construct a response just like in the positive test but triple it.
                response.ContentEncoding = Encoding.UTF8;

                // Get a response stream and write the response to it.
                var jsonStream = this.GetTestMetadataStream();
                response.ContentLength64 = 3 * (int)jsonStream.Length;
                context.Response.ContentType = "application/json";
                jsonStream.WriteTo(context.Response.OutputStream);
                jsonStream.Position = 0;
                jsonStream.WriteTo(context.Response.OutputStream);
                jsonStream.Position = 0;
                jsonStream.WriteTo(context.Response.OutputStream);
            }))
            {
                var azureIms = new AzureMetadataRequestor
                {
                    BaseAimsUri = AzureInstanceMetadataEndToEndTests.MockTestUri
                };

                var azureIMSData = azureIms.GetAzureComputeMetadataAsync();
                azureIMSData.Wait();

                Assert.Null(azureIMSData.Result);
            }
        }

        [TestMethod]
        public void AzureImsResponseExcludesMalformedValues()
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            using (new AzureInstanceMetadataServiceMock(
                AzureInstanceMetadataEndToEndTests.MockTestUri, 
                (HttpListenerContext context) =>
            {
                HttpListenerResponse response = context.Response;
                context.Response.StatusCode = 200;

                // make it a malicious-ish response...
                var malformedData = this.GetTestMetadata();
                malformedData.Name = "Not allowed for VM names";
                malformedData.ResourceGroupName = "Not allowed for resource group name";
                malformedData.SubscriptionId = "Definitely-not-a GUID up here";
                var malformedJsonStream = this.GetTestMetadataStream(malformedData);

                response.ContentEncoding = Encoding.UTF8;
                response.ContentLength64 = (int)malformedJsonStream.Length;
                context.Response.ContentType = "application/json";
                malformedJsonStream.WriteTo(context.Response.OutputStream);
            }))
            {
                var azureIms = new AzureMetadataRequestor
                {
                    BaseAimsUri = AzureInstanceMetadataEndToEndTests.MockTestUri
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
            CancellationTokenSource cts = new CancellationTokenSource();

            using (new AzureInstanceMetadataServiceMock(
                AzureInstanceMetadataEndToEndTests.MockTestUri, 
                (HttpListenerContext context) =>
            {
                // wait for longer than the request timeout
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));

                HttpListenerResponse response = context.Response;
                context.Response.StatusCode = 200;

                response.ContentEncoding = Encoding.UTF8;
                var jsonStream = this.GetTestMetadataStream();
                response.ContentLength64 = (int)jsonStream.Length;
                context.Response.ContentType = "application/json";
                jsonStream.WriteTo(context.Response.OutputStream);
            }))
            {
                var azureIms = new AzureMetadataRequestor
                {
                    BaseAimsUri = AzureInstanceMetadataEndToEndTests.MockTestUri,
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
            CancellationTokenSource cts = new CancellationTokenSource();

            using (new AzureInstanceMetadataServiceMock(
                AzureInstanceMetadataEndToEndTests.MockTestUri, 
                (HttpListenerContext context) =>
            {
                // don't send anything in content at all, or the context defaults to 200 OK
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }))
            {
                var azureIms = new AzureMetadataRequestor
                {
                    BaseAimsUri = AzureInstanceMetadataEndToEndTests.MockTestUri
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
