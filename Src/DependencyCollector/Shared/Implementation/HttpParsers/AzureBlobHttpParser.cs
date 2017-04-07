namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.HttpParsers
{
    using DataContracts;
    using Implementation;
    using System.Collections.Generic;
    /// <summary>
    /// HTTP Dependency parser that attempts to parse dependency as Azure Blob call.
    /// </summary>
    internal static class AzureBlobHttpParser
    {
        private static readonly string[] AzureBlobHostSuffixes =
            {
                ".blob.core.windows.net",
                ".blob.core.chinacloudapi.cn",
                ".blob.core.cloudapi.de",
                ".blob.core.usgovcloudapi.net"
            };

        private static readonly string[] AzureBlobSupportedVerbs = { "GET", "PUT", "OPTIONS", "HEAD", "DELETE" };

        /// <summary>
        /// Tries parsing given dependency telemetry item. 
        /// </summary>
        /// <param name="httpDependency">Dependency item to parse. It is expected to be of HTTP type.</param>
        /// <returns><code>true</code> if successfully parsed dependency.</returns>
        internal static bool TryParse(ref DependencyTelemetry httpDependency)
        {
            string name = httpDependency.Name;
            string host = httpDependency.Target;
            string url = httpDependency.Data;

            if (name == null || host == null || url == null)
            {
                return false;
            }

            if (!HttpParsingHelper.EndsWithAny(host, AzureBlobHostSuffixes))
            {
                return false;
            }

            ////
            //// Blob Service REST API: https://msdn.microsoft.com/en-us/library/azure/dd135733.aspx
            ////

            string account = host.Substring(0, host.IndexOf('.'));

            string verb;
            string nameWithoutVerb;

            // try to parse out the verb
            HttpParsingHelper.ExtractVerb(name, out verb, out nameWithoutVerb, AzureBlobSupportedVerbs);

            List<string> pathTokens = HttpParsingHelper.TokenizeRequestPath(nameWithoutVerb);
            string container = pathTokens.Count > 0 ? pathTokens[0] : string.Empty;

            // This is very naive overwriting of Azure Blob dependency that is compatible with the today's implementation
            //
            // Possible improvements:
            //
            // 1. Use specific name for specific operations. Like "Lease Blob" for "?comp=lease" query parameter
            // 2. Use account name as a target instead of "account.blob.core.windows.net"
            // 3. Do not include container name into name as it is high cardinality. Move to custom properties
            // 4. Parse blob name and put into custom properties as well

            httpDependency.Type = RemoteDependencyConstants.AzureBlob;
            httpDependency.Name = string.IsNullOrEmpty(verb)
                                      ? account + '/' + container
                                      : verb + " " + account + '/' + container;

            return true;
        }
    }
}
