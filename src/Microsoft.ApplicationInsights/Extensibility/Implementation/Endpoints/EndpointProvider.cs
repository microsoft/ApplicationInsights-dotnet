namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ApplicationInsights.Common.Extensions;

    public class EndpointProvider
    {
        private string connectionString;
        private Dictionary<string, string> connectionStringParsed = new Dictionary<string, string>(0);

        public string ConnectionString
        {
            get
            {
                return this.connectionString;
            }
            set
            {
                this.connectionString = value;
                this.connectionStringParsed = ParseConnectionString(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>key1=value1;key2=value2;key3=value3</remarks>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown if there are duplicate keys.</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown if the input string is in the wrong format.</exception>
        internal static Dictionary<string, string> ParseConnectionString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new Dictionary<string, string>(0);
            }

            return value
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('='))
                .ToDictionary(split => split[0], split => split[1], StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// location.prefix.suffix
        /// https:// westus2.dc.applicationinsights.azure.cn/
        /// </remarks>
        /// <returns></returns>
        internal static Uri BuildUri(string prefix, string suffix, string location = null)
        {
            // Location and Host are user input fields and need to be checked for extra periods.
            var trimPeriod = new char[] { '.' };

            // Location value is optional
            string locationSanitized = null;
            if (!string.IsNullOrEmpty(location))
            {
                locationSanitized = location.TrimEnd(trimPeriod);
            }

            string suffixSanitized = suffix.TrimStart(trimPeriod);

            var uriString = string.Concat("https://"
                + (locationSanitized == null ? string.Empty : locationSanitized + ".")
                + prefix + "."
                + suffixSanitized);

            return new Uri(uriString);
        }

        public Uri GetEndpoint(EndpointName endpointName)
        {
            // 1. check for explicit endpoint (location is ignored)
            // 2. check for endpoint suffix (location is optional)
            // 3. use classic endpoint

            var endpointMeta = endpointName.GetAttribute<EndpointMetaAttribute>();

            if (this.connectionStringParsed.TryGetValue(endpointMeta.ExplicitName, out string explicitEndpoint))
            {
                return new Uri(explicitEndpoint);
            }
            else if (this.connectionStringParsed.TryGetValue("EndpointSuffix", out string endpointSuffix))
            {
                return BuildUri(
                    prefix: endpointMeta.EndpointPrefix, 
                    suffix: endpointSuffix, 
                    location: GetLocation());
            }
            else
            {
                return new Uri(endpointMeta.Default);
            }
        }

        private string GetLocation() => this.connectionStringParsed.TryGetValue("Location", out string location) ? location : null;
    }
}
