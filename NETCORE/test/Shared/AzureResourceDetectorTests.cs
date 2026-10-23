using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Microsoft.ApplicationInsights.Shared.Vendoring.OpenTelemetry.Resources.Azure;
using OpenTelemetry.Resources;
using Xunit;

#if AI_ASPNETCORE_WEB
namespace Microsoft.ApplicationInsights.AspNetCore.Tests
#else
namespace Microsoft.ApplicationInsights.WorkerService.Tests
#endif
{
    /// <summary>
    /// Verifies the behavior of the vendored OpenTelemetry.Resources.Azure detectors.
    /// </summary>
    public class AzureResourceDetectorTests : IDisposable
    {
        private const string SubscriptionId = "00000000-0000-0000-0000-000000000000";

        private static readonly string[] EnvironmentVariableNames =
        {
            "WEBSITE_SITE_NAME",
            "WEBSITE_OWNER_NAME",
            "WEBSITE_RESOURCE_GROUP",
            "WEBSITE_SLOT_NAME",
            "CONTAINER_APP_NAME",
            "CONTAINER_APP_JOB_NAME",
        };

        private readonly Dictionary<string, string> originalEnvironmentVariables = new Dictionary<string, string>();
        private readonly Func<AzureVmMetadataResponse> originalVmMetadataRequestor;

        public AzureResourceDetectorTests()
        {
            foreach (var name in EnvironmentVariableNames)
            {
                this.originalEnvironmentVariables[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, null);
            }

            this.originalVmMetadataRequestor = AzureVmMetaDataRequestor.GetAzureVmMetaDataResponse;
            AzureVMResourceDetector.ClearCachedResource();
        }

        public void Dispose()
        {
            foreach (var variable in this.originalEnvironmentVariables)
            {
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }

            AzureVmMetaDataRequestor.GetAzureVmMetaDataResponse = this.originalVmMetadataRequestor;
            AzureVMResourceDetector.ClearCachedResource();
        }

        [Fact]
        public void AppServiceDetector_ReportsSemanticConventionAttributes()
        {
            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "my-site");
            Environment.SetEnvironmentVariable("WEBSITE_OWNER_NAME", SubscriptionId + "+my-rg-westuswebspace");
            Environment.SetEnvironmentVariable("WEBSITE_RESOURCE_GROUP", "my-rg");
            Environment.SetEnvironmentVariable("WEBSITE_SLOT_NAME", "staging");

            var attributes = ToDictionary(new AppServiceResourceDetector().Detect());

            Assert.Equal("my-site", attributes["service.name"]);
            Assert.Equal("azure", attributes["cloud.provider"]);
            Assert.Equal("azure.app_service", attributes["cloud.platform"]);
            Assert.Equal(SubscriptionId, attributes["cloud.account.id"]);
            Assert.Equal("my-rg", attributes["azure.resource_group.name"]);
            Assert.Equal($"/subscriptions/{SubscriptionId}/resourceGroups/my-rg/providers/Microsoft.Web/sites/my-site", attributes["cloud.resource_id"]);
            Assert.Equal("staging", attributes["deployment.environment.name"]);
            Assert.DoesNotContain("deployment.environment", attributes.Keys);
        }

        [Fact]
        public void ContainerAppsDetector_ReportsSemanticConventionPlatform()
        {
            Environment.SetEnvironmentVariable("CONTAINER_APP_NAME", "my-container-app");

            var attributes = ToDictionary(new AzureContainerAppsResourceDetector().Detect());

            Assert.Equal("my-container-app", attributes["service.name"]);
            Assert.Equal("azure.container_apps", attributes["cloud.platform"]);
        }

        [Fact]
        public void VmDetector_ReportsSemanticConventionAttributes()
        {
            AzureVmMetaDataRequestor.GetAzureVmMetaDataResponse = () => new AzureVmMetadataResponse
            {
                Location = "westus",
                Name = "my-vm",
                OsType = "Windows",
                ResourceGroupName = "my-rg",
                ResourceId = $"/subscriptions/{SubscriptionId}/resourceGroups/my-rg/providers/Microsoft.Compute/virtualMachines/my-vm",
                SubscriptionId = SubscriptionId,
                StorageProfile = new AzureVmStorageProfile
                {
                    ImageReference = new AzureVmImageReference
                    {
                        Id = "image-id",
                        Publisher = "MicrosoftWindowsServer",
                        Offer = "WindowsServer",
                        Sku = "2022-datacenter",
                    },
                },
                Version = "20348.1",
                VmId = "vm-id",
                VmSize = "Standard_D2s_v3",
            };

            var attributes = ToDictionary(new AzureVMResourceDetector().Detect());

            Assert.Equal("azure.vm", attributes["cloud.platform"]);
            Assert.Equal("windows", attributes["os.type"]);
            Assert.Equal(SubscriptionId, attributes["cloud.account.id"]);
            Assert.Equal("my-rg", attributes["azure.resource_group.name"]);
            Assert.Equal("image-id", attributes["host.image.id"]);
            Assert.Equal("MicrosoftWindowsServer:WindowsServer:2022-datacenter", attributes["host.image.name"]);
            Assert.Equal("20348.1", attributes["host.image.version"]);
            Assert.Equal("vm-id", attributes["service.instance.id"]);
            Assert.DoesNotContain("azure.vm.sku", attributes.Keys);
        }

        [Fact]
        public void VmDetector_IsSkippedOnAppService()
        {
            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "my-site");
            var requested = false;
            AzureVmMetaDataRequestor.GetAzureVmMetaDataResponse = () =>
            {
                requested = true;
                return new AzureVmMetadataResponse();
            };

            var resource = new AzureVMResourceDetector().Detect();

            Assert.Empty(resource.Attributes);
            Assert.False(requested);
        }

        [Fact]
        public void EventSource_HasPackageSpecificName()
        {
#if AI_ASPNETCORE_WEB
            const string expectedName = "Microsoft-ApplicationInsights-AspNetCore-Resources-Azure";
#else
            const string expectedName = "Microsoft-ApplicationInsights-WorkerService-Resources-Azure";
#endif

            Assert.Equal(expectedName, EventSource.GetName(typeof(AzureResourcesEventSource)));
        }

        private static Dictionary<string, object> ToDictionary(Resource resource)
            => resource.Attributes.ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
    }
}
