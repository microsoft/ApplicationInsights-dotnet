namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal class AzureHeartbeatProperties : IHeartbeatDefaultPayloadProvider
    {
        // internal for testing
        internal readonly List<string> DefaultFields = new List<string>()
        {
            "osType",
            "location",
            "name",
            "offer",
            "platformFaultDomain",
            "platformUpdateDomain",
            "publisher",
            "sku",
            "version",
            "vmId",
            "vmSize"
        };

        /// <summary>
        /// Flags that will tell us whether or not Azure VM metadata has been attempted to be gathered or not, and
        /// if we should even attempt to look for it in the first place. Static to ensure we only ever attempt this
        /// once for any given consuming application.
        /// </summary>
        private static bool isAzureMetadataCheckCompleted = false;

        private IAzureMetadataRequestor azureInstanceMetadataRequestor = null;

        /// <summary>
        /// Constructor for the Azure specific fields to inject into the heartbeat payload
        /// </summary>
        /// <param name="azInstanceMetadataHandler">(for testing) Azure metadata request handler to use when requesting data from azure specifically. If left as null, an instance of AzureMetadatRequestor is used.</param>
        /// <param name="resetCheckCompleteFlag">(for testing) set to true to reset the check that we've already aquired this data</param>
        public AzureHeartbeatProperties(IAzureMetadataRequestor azInstanceMetadataHandler = null, bool resetCheckCompleteFlag = false)
        {
            this.azureInstanceMetadataRequestor = azInstanceMetadataHandler;
            if (this.azureInstanceMetadataRequestor == null)
            {
                this.azureInstanceMetadataRequestor = new AzureMetadataRequestor();
            }

            if (resetCheckCompleteFlag)
            {
                AzureHeartbeatProperties.isAzureMetadataCheckCompleted = false;
            }
        }

        public bool IsKeyword(string keyword)
        {
            return this.DefaultFields.Contains(keyword, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> SetDefaultPayload(IEnumerable<string> disabledFields, IHeartbeatProvider provider)
        {
            bool hasSetFields = false;

            if (!AzureHeartbeatProperties.isAzureMetadataCheckCompleted)
            {
                // only ever attempt this once
                AzureHeartbeatProperties.isAzureMetadataCheckCompleted = true;

                var allFields = await this.azureInstanceMetadataRequestor.GetAzureInstanceMetadataComputeFields()
                                .ConfigureAwait(false);

                var enabledImdsFields = this.DefaultFields.Except(disabledFields);
                foreach (string field in enabledImdsFields)
                {
                    provider.AddHeartbeatProperty(
                        propertyName: field,
                        overrideDefaultField: true,
                        propertyValue: await this.azureInstanceMetadataRequestor.GetAzureComputeMetadata(field)
                            .ConfigureAwait(false),
                        isHealthy: true);
                    hasSetFields = true;
                }
            }

            return hasSetFields;
        }
    }
}
