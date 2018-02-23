namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    internal class AzureHeartbeatProperties
    {
        private static int ResourceGroupNameLengthMax = 90;
        private static int ResourceGroupNameLengthMin = 1;
        private static string ResourceGroupNameValidChars = "[a-zA-Z0-9()_\-\.]";
        private static int VmNameLenghtMax = 64; // (15 for windows!)
        private static int VmNameLengthMin = 1;
        private static string VmNameValidChars = "[a-zA-Z0-9()_-]";

        // internal for testing
        internal readonly List<string> DefaultFields = new List<string>()
        {
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

        public async Task<bool> SetDefaultPayload(IEnumerable<string> disabledFields, IHeartbeatPropertyManager provider)
        {
            bool hasSetFields = false;

            if (!this.isAzureMetadataCheckCompleted)
            {
                this.isAzureMetadataCheckCompleted = true;

                var allFields = await this.azureInstanceMetadataRequestor.GetAzureInstanceMetadataComputeFields()
                                .ConfigureAwait(false);

                if (allFields != null && allFields.Count() > 0)
                {
                    var enabledImdsFields = this.DefaultFields.Except(disabledFields).Intersect(allFields);
                    foreach (string field in enabledImdsFields)
                    {
                        string value = await this.azureInstanceMetadataRequestor.GetAzureComputeMetadata(field).ConfigureAwait(false);
                        string verifiedValue = this.VerifyExpectedValue(field, value);
                        // do we want to log if value != verifiedValue?

                        provider.AddHeartbeatProperty(
                            propertyName: field,
                            propertyValue: verifiedValue,
                            isHealthy: true);
                        hasSetFields = true;
                    }
                }
            }

            return hasSetFields;
        }

        /// <summary>
        /// Because the Azure IMS is on a hijackable IP we need to do some due diligence in our accepting
        /// values returned from it. This method takes the fieldname and value recieved for that field, and
        /// if we can test that value against known limitations of that field we do so here. If the test fails
        /// we return the empty string, otherwise we return the string given.
        /// </summary>
        /// <param name="fieldName">Name of the field acquired from the call to Azure IMS.</param>
        /// <param name="valueToVerify">The value aquired for the field that may be verified.</param>
        /// <returns>valueToVerify or the empty string.</returns>
        private string VerifyExpectedValue(string fieldName, string valueToVerify)
        {
            string value = string.Empty;
            if (fieldName.Equals("resourceGroupName", StringComparison.InvariantCultureIgnoreCase))
            {
                var valueOk = valueToVerify.Length <= AzureHeartbeatProperties.ResourceGroupNameLengthMax;
                valueOk &= valueToVerify.Length >= AzureHeartbeatProperties.ResourceGroupNameLengthMin;
                System.Text.RegularExpressions.Regex charMatch = new System.Text.RegularExpressions.Regex(AzureHeartbeatProperties.ResourceGroupNameValidChars);
                valueOk &= valueToVerify.All(a => charMatch.IsMatch(a.ToString()));
                valueOk &= !valueToVerify.EndsWith(".");
                if (valueOk)
                {
                    value = valueToVerify;
                }
            }
            else if (fieldName.Equals("subscriptionId", StringComparison.InvariantCultureIgnoreCase))
            {
                Guid g = new Guid();
                if (Guid.TryParse(valueToVerify, out g))
                {
                    value = valueToVerify;
                }
            }
            else if (fieldName.Equals("name", StringComparison.InvariantCultureIgnoreCase))
            {
                var valueOk = valueToVerify.Length <= AzureHeartbeatProperties.VmNameLenghtMax;
                valueOk &= valueToVerify.Length >= AzureHeartbeatProperties.VmNameLengthMin;
                System.Text.RegularExpressions.Regex charMatch = new System.Text.RegularExpressions.Regex(AzureHeartbeatProperties.VmNameValidChars);
                valueOk &= valueToVerify.All(a => charMatch.IsMatch(a.ToString()));
                if (valueOk)
                {
                    value = valueToVerify;
                }
            }
            else
            {
                // no sanitization method available for this value, just return the given value
                value = valueToVerify;
            }
            return value;
        }
    }
}
