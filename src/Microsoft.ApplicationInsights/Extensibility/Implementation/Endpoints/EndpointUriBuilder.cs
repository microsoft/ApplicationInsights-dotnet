namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;

    internal class EndpointUriBuilder // : UriBuilder
    {
        public string Location { get; set; } 
        public string Prefix { get; set; }
        public string Host { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// location.prefix.suffix
        /// https:// westus2.dc.applicationinsights.azure.cn/
        /// </remarks>
        /// <returns></returns>
        public Uri ToUri()
        {
            // Location and Host are user input fields and need to be checked for extra periods.
            var trimPeriod = new char[] { '.' };

            // Location value is optional
            string locationSanitized = null;
            if (!string.IsNullOrEmpty(this.Location))
            {
                locationSanitized = this.Location.TrimEnd(trimPeriod);
            }

            string hostSanitized = this.Host.TrimStart(trimPeriod);

            var uriString = string.Concat("https://" 
                + ( locationSanitized == null ? string.Empty : locationSanitized + "." )
                + this.Prefix + "." + hostSanitized);
            
            return new Uri(uriString);
        }

        public static Uri ToUri(string endpoint, string location = null)
        {

        }

        public static Uri ToUrl(string prefix, string suffix, string location = null)
        {

        }
    }
}
