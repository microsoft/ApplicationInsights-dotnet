using System;
using System.Reflection;
using Microsoft.ApplicationInsights.Shared.Vendoring.OpenTelemetry.Resources.Azure;
using OpenTelemetry.Resources;
using Xunit;

namespace IntegrationTests.Tests
{
    [CollectionDefinition("AzureVMResourceDetectorTests", DisableParallelization = true)]
    public class AzureVMResourceDetectorTestsCollectionDefinition { }

    [Collection("AzureVMResourceDetectorTests")]
    public class AzureVMResourceDetectorTests : IDisposable
    {
        private readonly string? originalWebsiteSiteName;
        private readonly Func<AzureVmMetadataResponse?> originalGetAzureVmMetaDataResponse;
        private readonly FieldInfo vmResourceField;
        private readonly Resource? originalVmResource;

        public AzureVMResourceDetectorTests()
        {
            this.originalWebsiteSiteName = Environment.GetEnvironmentVariable(
                ResourceAttributeConstants.AppServiceSiteNameEnvVar);

            this.originalGetAzureVmMetaDataResponse =
                AzureVmMetaDataRequestor.GetAzureVmMetaDataResponse;

            this.vmResourceField = typeof(AzureVMResourceDetector).GetField(
                "vmResource",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            this.originalVmResource =
                (Resource?)this.vmResourceField.GetValue(null);

            this.vmResourceField.SetValue(null, null);
        }

        [Fact]
        public void DetectDoesNotRequestAzureVmMetadataWhenRunningOnAppService()
        {
            // ARRANGE
            var metadataWasRequested = false;

            Environment.SetEnvironmentVariable(
                ResourceAttributeConstants.AppServiceSiteNameEnvVar,
                "test-app-service");

            AzureVmMetaDataRequestor.GetAzureVmMetaDataResponse = () =>
            {
                metadataWasRequested = true;
                return null;
            };

            var detector = new AzureVMResourceDetector();

            // ACT
            detector.Detect();

            // ASSERT
            Assert.False(
                metadataWasRequested,
                "The Azure VM detector should not query IMDS when App Service has already been detected.");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(
                ResourceAttributeConstants.AppServiceSiteNameEnvVar,
                this.originalWebsiteSiteName);

            AzureVmMetaDataRequestor.GetAzureVmMetaDataResponse =
                this.originalGetAzureVmMetaDataResponse;

            this.vmResourceField.SetValue(null, this.originalVmResource);
        }
    }
}