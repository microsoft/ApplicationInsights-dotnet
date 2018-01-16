namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.Azure;
    using Microsoft.ApplicationInsights.WindowsServer.Azure.Emulation;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Web.TestFramework;
    using Assert = Xunit.Assert;

    [TestClass]
    public class AzureInstanceMetadataTests
    {

        [TestMethod]
        public void GetAzureInstanceMetadataFieldsAsExpected()
        {
            using (var hbeatMock = new HeartbeatProviderMock())
            {
                AzureInstanceMetadataRequestMock azureInstanceRequestorMock = new AzureInstanceMetadataRequestMock();
                AzureHeartbeatProperties azFields = new AzureHeartbeatProperties(azureInstanceRequestorMock, true);
                int counter = 1;
                foreach (string field in azFields.DefaultFields)
                {
                    azureInstanceRequestorMock.computeFields.Add(field, $"testValue{counter++}");
                }

                var taskWaiter = azFields.SetDefaultPayload(new string[] { }, hbeatMock).ConfigureAwait(false);
                Assert.IsTrue(taskWaiter.GetAwaiter().GetResult()); // no await for tests

                foreach (string fieldName in azFields.DefaultFields)
                {
                    Assert.IsTrue(hbeatMock.HeartbeatProperties.ContainsKey(fieldName));
                    Assert.IsFalse(string.IsNullOrEmpty(hbeatMock.HeartbeatProperties[fieldName].PayloadValue));
                }
            }
        }

        [TestMethod]
        public void FailToObtainAzureInstanceMetadataFieldsAltogether()
        {
            using (var hbeatMock = new HeartbeatProviderMock())
            {
                AzureInstanceMetadataRequestMock azureInstanceRequestorMock = new AzureInstanceMetadataRequestMock();
                var azFields = new AzureHeartbeatProperties(azureInstanceRequestorMock, true);
                var defaultFields = azFields.DefaultFields;
                // not adding the fields we're looking for, simulation of the Azure Instance Metadata service not being present...

                var taskWaiter = azFields.SetDefaultPayload(new string[] { }, hbeatMock).ConfigureAwait(false);
                Assert.IsTrue(taskWaiter.GetAwaiter().GetResult()); // nop await for tests

                foreach (string fieldName in defaultFields)
                {
                    Assert.IsTrue(hbeatMock.HeartbeatProperties.ContainsKey(fieldName));
                    Assert.IsTrue(string.IsNullOrEmpty(hbeatMock.HeartbeatProperties[fieldName].PayloadValue));
                }
            }
        }

        [TestMethod]
        public void InitializeHealthHeartDisablingAzureMetadata()
        {
            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                // disable azure metadata lookup via the IHeartbeatPropertyManager interface
                IHeartbeatPropertyManager hbeat = initializedModule;
                Assert.AreEqual(0, hbeat.ExcludedHeartbeatPropertyProviders.Count);
                hbeat.ExcludedHeartbeatPropertyProviders.Add("AzureInstance");

                // initialize the DiagnosticsTelemetryModule, and ensure the instance metadata is still disabled
                initializedModule.Initialize(new TelemetryConfiguration());
                Assert.IsTrue(hbeat.ExcludedHeartbeatPropertyProviders.Contains("AzureInstance"));
            }
        }
    }
}
