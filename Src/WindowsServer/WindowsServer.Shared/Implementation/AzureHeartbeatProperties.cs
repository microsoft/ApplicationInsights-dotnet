namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    internal class AzureHeartbeatProperties
    {
        private static string HeartbeatPropertyPrefix = "azInst_"; // to ensure no collisions with base heartbeat properties

        /// <summary>
        /// Expected fields extracted from Azure IMS to add to the heartbeat properties. 
        /// Set as internal for testing.
        /// </summary>
        internal readonly List<string> ExpectedAzureImsFields = new List<string>()
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
        private bool isAzureMetadataCheckCompleted = false;

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
                this.isAzureMetadataCheckCompleted = false;
            }
        }

        /// <summary>
        /// Add all enabled, present Azure IMS fields to the heartbeat properties.
        /// </summary>
        /// <param name="disabledFields">Fields that are to be disabled.</param>
        /// <param name="provider">Heartbeat provider to set the properties on.</param>
        /// <returns>True if any property values were successfully set, false if none were set.</returns>
        public async Task<bool> SetDefaultPayload(IEnumerable<string> disabledFields, IHeartbeatPropertyManager provider)
        {
            bool hasSetFields = false;

            if (!this.isAzureMetadataCheckCompleted)
            {
                this.isAzureMetadataCheckCompleted = true;

                var azureComputeMetadata = await this.azureInstanceMetadataRequestor.GetAzureComputeMetadata()
                                .ConfigureAwait(false);

                var enabledImdsFields = this.ExpectedAzureImsFields.Except(disabledFields);
                foreach (string field in enabledImdsFields)
                {
                    string value = azureComputeMetadata.GetValueForField(field);

                    string verifiedValue = azureComputeMetadata.VerifyExpectedValue(field);

                    bool addedProperty = provider.AddHeartbeatProperty(
                                                    propertyName: string.Concat(AzureHeartbeatProperties.HeartbeatPropertyPrefix, field),
                                                    propertyValue: verifiedValue,
                                                    isHealthy: true);
                    if (!addedProperty)
                    {
                        WindowsServerEventSource.Log.AzureInstanceMetadataWasntAddedToHeartbeatProperties(field, verifiedValue);
                    }

                    hasSetFields = hasSetFields || addedProperty;
                }
            }

            return hasSetFields;
        }
    }
}
