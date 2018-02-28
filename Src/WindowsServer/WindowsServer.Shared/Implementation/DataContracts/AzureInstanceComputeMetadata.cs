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
        private const int resourceGroupNameLengthMax = 90;
        private const int resourceGroupNameLengthMin = 1;
        private const string resourceGroupNameValidChars = @"^[a-zA-Z0-9\.\-_]+$";
        private const int nameLenghtMax = 64; // 15 for windows, go with Linux for MAX
        private const int nameLengthMin = 1;
        private const string nameValidChars = @"^[a-zA-Z0-9()_\-]+$";
        private readonly TimeSpan regexTimeout = TimeSpan.FromMilliseconds(1000);

        [DataMember(Name = "osType", IsRequired = true)]
        internal string OsType { get; set; }

        [DataMember(Name = "location", IsRequired = true)]
        internal string Location { get; set; }

        [DataMember(Name = "name", IsRequired = true)]
        internal string Name { get; set; }

        [DataMember(Name = "offer", IsRequired = true)]
        internal string Offer { get; set; }

        [DataMember(Name = "platformFaultDomain", IsRequired = true)]
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
            switch (fieldName.ToLower(CultureInfo.InvariantCulture))
            {
                case "ostype":
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
                case "platformfaultdomain":
                    aimsValue = this.PlatformFaultDomain;
                    break;
                case "platformupdatedomain":
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
                case "vmid":
                    aimsValue = this.VmId;
                    break;
                case "vmsize":
                    aimsValue = this.VmSize;
                    break;
                case "subscriptionid":
                    aimsValue = this.SubscriptionId;
                    break;
                case "resourcegroupname":
                    aimsValue = this.ResourceGroupName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, "No field named '{0}' in AzureInstanceComputeMetadata.", fieldName));
            }

            return aimsValue;
        }

        /// <summary>
        /// Because the Azure IMS is on a non-routable IP we need to do some due diligence in our accepting
        /// values returned from it. This method takes the fieldname and value received for that field, and
        /// if we can test that value against known limitations of that field we do so here. If the test fails
        /// we return the empty string, otherwise we return the string given.
        /// </summary>
        /// <param name="fieldName">Name of the field acquired from the call to Azure IMS.</param>
        /// <returns>The value of the field, verified, or the empty string.</returns>
        internal string VerifyExpectedValue(string fieldName)
        {
            string valueToVerify = this.GetValueForField(fieldName);
            string value = string.Empty;
            bool valueOk = true;

            if (fieldName.Equals("resourceGroupName", StringComparison.OrdinalIgnoreCase))
            {
                valueOk = valueToVerify.Length <= AzureInstanceComputeMetadata.resourceGroupNameLengthMax;
                valueOk &= valueToVerify.Length >= AzureInstanceComputeMetadata.resourceGroupNameLengthMin;
                var resGrpMatcher = new Regex(AzureInstanceComputeMetadata.resourceGroupNameValidChars, RegexOptions.None, this.regexTimeout);
                valueOk &= resGrpMatcher.IsMatch(valueToVerify);
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
                valueOk = valueToVerify.Length <= AzureInstanceComputeMetadata.nameLenghtMax;
                valueOk &= valueToVerify.Length >= AzureInstanceComputeMetadata.nameLengthMin;
                var nameMatcher = new Regex(AzureInstanceComputeMetadata.nameValidChars, RegexOptions.None, this.regexTimeout);
                valueOk &= nameMatcher.IsMatch(valueToVerify);

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
