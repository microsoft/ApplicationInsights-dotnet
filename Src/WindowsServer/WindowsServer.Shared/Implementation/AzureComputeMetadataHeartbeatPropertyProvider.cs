namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    internal class AzureComputeMetadataHeartbeatPropertyProvider
    {
        internal const string HeartbeatPropertyPrefix = "azInst_"; // to ensure no collisions with base heartbeat properties

        /// <summary>
        /// Expected fields extracted from Azure IMS to add to the heartbeat properties. 
        /// Set as internal for testing.
        /// </summary>
        internal readonly List<string> ExpectedAzureImsFields = new List<string>()
        {
            "location",
            "name",
            "offer",
            "osType",
            "placementGroupId",
            "platformFaultDomain",
            "platformUpdateDomain",
            "publisher",
            "resourceGroupName",
            "sku",
            "subscriptionId",
            "tags",
            "version",
            "vmId",
            "vmSize"
        };

        /// <summary>
        /// Flags that will tell us whether or not Azure VM metadata has been attempted to be gathered or not, and
        /// if we should even attempt to look for it in the first place. 
        /// </summary>
        private bool isAzureMetadataCheckCompleted = false;

        private IAzureMetadataRequestor azureInstanceMetadataRequestor = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureComputeMetadataHeartbeatPropertyProvider"/> class.
        /// </summary>
        public AzureComputeMetadataHeartbeatPropertyProvider() : this(null, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureComputeMetadataHeartbeatPropertyProvider"/> class.
        /// </summary>
        /// <param name="azureInstanceMetadataHandler">For testing: Azure metadata request handler to use when requesting data from azure specifically. If left as null, an instance of AzureMetadataRequestor is used.</param>
        /// <param name="resetCheckCompleteFlag">For testing: set to true to reset the check that we've already acquired this data.</param>
        internal AzureComputeMetadataHeartbeatPropertyProvider(IAzureMetadataRequestor azureInstanceMetadataHandler = null, bool resetCheckCompleteFlag = false)
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
        /// <param name="provider">Heartbeat provider to set the properties on.</param>
        /// <returns>True if any property values were successfully set, false if none were set.</returns>
        public async Task<bool> SetDefaultPayload(IHeartbeatPropertyManager provider)
        {
            bool hasSetFields = false;
            
            if (!this.isAzureMetadataCheckCompleted)
            {
                this.isAzureMetadataCheckCompleted = true;

                var azureComputeMetadata = await this.azureInstanceMetadataRequestor.GetAzureComputeMetadata()
                                .ConfigureAwait(false);

                if (azureComputeMetadata != null)
                {
                    var enabledImdsFields = this.ExpectedAzureImsFields.Except(provider.ExcludedHeartbeatProperties);
                    foreach (string field in enabledImdsFields)
                    {
                        string verifiedValue = azureComputeMetadata.VerifyExpectedValue(field);

                        bool addedProperty = provider.AddHeartbeatProperty(
                                                        propertyName: string.Concat(AzureComputeMetadataHeartbeatPropertyProvider.HeartbeatPropertyPrefix, field),
                                                        propertyValue: verifiedValue,
                                                        isHealthy: true);
                        if (!addedProperty)
                        {
                            WindowsServerEventSource.Log.AzureInstanceMetadataWasntAddedToHeartbeatProperties(field, verifiedValue);
                        }

                        hasSetFields = hasSetFields || addedProperty;
                    }
                }
                else
                {
                    WindowsServerEventSource.Log.AzureInstanceMetadataNotAdded();
                }
            }

            return hasSetFields;
        }
    }
}
