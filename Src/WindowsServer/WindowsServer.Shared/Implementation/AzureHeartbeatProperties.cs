namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    internal class AzureHeartbeatProperties
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
            "vmSize",
            "subscriptionId",
            "resourceGroupName"
        };

        /// <summary>
        /// Flags that will tell us whether or not Azure VM metadata has been attempted to be gathered or not, and
        /// if we should even attempt to look for it in the first place. Static to ensure we only ever attempt this
        /// once for any given consuming application.
        /// </summary>
        private static bool isAzureMetadataCheckCompleted = false;

        private IAzureMetadataRequestor azureInstanceMetadataRequestor = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureHeartbeatProperties"/> class.
        /// </summary>
        public AzureHeartbeatProperties() : this(null, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureHeartbeatProperties"/> class.
        /// </summary>
        /// <param name="azureInstanceMetadataHandler">For testing: Azure metadata request handler to use when requesting data from azure specifically. If left as null, an instance of AzureMetadataRequestor is used.</param>
        /// <param name="resetCheckCompleteFlag">For testing: set to true to reset the check that we've already acquired this data.</param>
        internal AzureHeartbeatProperties(IAzureMetadataRequestor azureInstanceMetadataHandler = null, bool resetCheckCompleteFlag = false)
        {
            this.azureInstanceMetadataRequestor = azureInstanceMetadataHandler;
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

        public async Task<bool> SetDefaultPayload(IEnumerable<string> disabledFields, IHeartbeatPropertyManager provider)
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
                        propertyValue: await this.azureInstanceMetadataRequestor.GetAzureComputeMetadata(field).ConfigureAwait(false),
                        isHealthy: true);
                    hasSetFields = true;
                }
            }

            return hasSetFields;
        }
    }
}
