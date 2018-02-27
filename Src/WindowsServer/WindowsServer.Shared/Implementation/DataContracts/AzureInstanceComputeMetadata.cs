namespace Microsoft.ApplicationInsights.WindowsServer.Implementation.DataContracts
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Class representing the returned structure from an Azure Instance Metadata request
    /// for Compute information.
    /// </summary>
    [DataContract]
    internal class AzureInstanceComputeMetadata
    {
        private static int ResourceGroupNameLengthMax = 90;
        private static int ResourceGroupNameLengthMin = 1;
        private static string ResourceGroupNameValidChars = @"[a-zA-Z0-9()_\-\.]";
        private static int VmNameLenghtMax = 64; // 15 for windows, go with Linux for MAX
        private static int VmNameLengthMin = 1;
        private static string VmNameValidChars = @"[a-zA-Z0-9()_\-]";

        [DataMember(Name = "osType", IsRequired = true)]
        internal string OsType { get; set; }

        [DataMember(Name = "location", IsRequired = true)]
        internal string Location { get; set; }

        [DataMember(Name = "name", IsRequired = true)]
        internal string Name { get; set; }

        [DataMember(Name = "offer", IsRequired = true)]
        internal string Offer { get; set; }

        [DataMember(Name = "platformDefaultDomain", IsRequired = true)]
        internal string PlatformFaultDomain { get; set; }

        [DataMember(Name = "platformUpdateDomain", IsRequired = true)]
        internal string PlatformUpdateDomain { get; set; }

        [DataMember(Name = "publisher", IsRequired = true)]
        internal string Publisher { get; set; }

        [DataMember(Name = "sku", IsRequired = true)]
        internal string Sku { get; set; }

        [DataMember(Name = "version", IsRequired = true)]
        internal string Version { get; set; }

        [DataMember(Name = "vmId", IsRequired = true)]
        internal string VmId { get; set; }

        [DataMember(Name = "vmSize", IsRequired = true)]
        internal string VmSize { get; set; }

        [DataMember(Name = "subscriptionId", IsRequired = true)]
        internal string SubscriptionId { get; set; }

        [DataMember(Name = "resourceGroupName", IsRequired = true)]
        internal string ResourceGroupName { get; set; }

        internal string GetValueForField(string fieldName)
        {
            string aimsValue = string.Empty;
            switch (fieldName)
            {
                case "osType":
                    aimsValue = this.OsType;
                    break;
                case "location":
                    aimsValue = this.Location;
                    break;
                case "name":
                    aimsValue = this.Name;
                    break;
                case "offer":
                    aimsValue = this.Offer;
                    break;
                case "platformFaultDomain":
                    aimsValue = this.PlatformFaultDomain;
                    break;
                case "platformUpdateDomain":
                    aimsValue = this.PlatformUpdateDomain;
                    break;
                case "publisher":
                    aimsValue = this.Publisher;
                    break;
                case "sku":
                    aimsValue = this.Sku;
                    break;
                case "version":
                    aimsValue = this.Version;
                    break;
                case "vmId":
                    aimsValue = this.VmId;
                    break;
                case "vmSize":
                    aimsValue = this.VmSize;
                    break;
                case "subscriptionId":
                    aimsValue = this.SubscriptionId;
                    break;
                case "resourceGroupName":
                    aimsValue = this.ResourceGroupName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, "No field named '{0}' in AzureInstanceComputeMetadata.", fieldName));
            }
            return aimsValue;
        }

        /// <summary>
        /// Because the Azure IMS is on a hijackable IP we need to do some due diligence in our accepting
        /// values returned from it. This method takes the fieldname and value recieved for that field, and
        /// if we can test that value against known limitations of that field we do so here. If the test fails
        /// we return the empty string, otherwise we return the string given.
        /// </summary>
        /// <param name="fieldName">Name of the field acquired from the call to Azure IMS.</param>
        /// <returns>valueToVerify or the empty string.</returns>
        internal string VerifyExpectedValue(string fieldName)
        {
            string valueToVerify = this.GetValueForField(fieldName);
            string value = string.Empty;
            bool valueOk = true;

            if (fieldName.Equals("resourceGroupName", StringComparison.OrdinalIgnoreCase))
            {
                valueOk = valueToVerify.Length <= AzureInstanceComputeMetadata.ResourceGroupNameLengthMax;
                valueOk &= valueToVerify.Length >= AzureInstanceComputeMetadata.ResourceGroupNameLengthMin;
                Regex charMatch = new Regex(AzureInstanceComputeMetadata.ResourceGroupNameValidChars);
                valueOk &= valueToVerify.All(a => charMatch.IsMatch(a.ToString()));
                valueOk &= !valueToVerify.EndsWith(".", StringComparison.OrdinalIgnoreCase);
                if (valueOk)
                {
                    value = valueToVerify;
                }
            }
            else if (fieldName.Equals("subscriptionId", StringComparison.OrdinalIgnoreCase))
            {
                Guid g = new Guid();
                valueOk = Guid.TryParse(valueToVerify, out g);

                if (valueOk)
                {
                    value = valueToVerify;
                }
            }
            else if (fieldName.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                valueOk = valueToVerify.Length <= AzureInstanceComputeMetadata.VmNameLenghtMax;
                valueOk &= valueToVerify.Length >= AzureInstanceComputeMetadata.VmNameLengthMin;
                Regex charMatch = new Regex(AzureInstanceComputeMetadata.VmNameValidChars);
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

            if (!valueOk)
            {
                WindowsServerEventSource.Log.AzureInstanceMetadataValueForFieldInvalid(fieldName);
            }

            return value;
        }

    }
}
