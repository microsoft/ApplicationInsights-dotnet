namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.HttpParsers
{
    using System;
    using DataContracts;
    using Implementation;

    /// <summary>
    /// HTTP Dependency parser that attempts to parse dependency as Azure Blob call.
    /// </summary>
    internal static class AzureBlobHttpParser
    {
        private static readonly string[] AzureBlobVerbPrefixes = { "GET ", "PUT ", "OPTIONS ", "HEAD ", "DELETE " };

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

            if (!host.EndsWith(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            ////
            //// Blob Service REST API: https://msdn.microsoft.com/en-us/library/azure/dd135733.aspx
            ////

            string account = host.Substring(0, host.IndexOf('.'));

            string verb = null;
            string nameWithoutVerb = name;

            for (int i = 0; i < AzureBlobVerbPrefixes.Length; i++)
            {
                var verbPrefix = AzureBlobVerbPrefixes[i];
                if (name.StartsWith(verbPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    verb = name.Substring(0, verbPrefix.Length);
                    nameWithoutVerb = name.Substring(verbPrefix.Length);
                    break;
                }
            }

            var slashPrefixShift = nameWithoutVerb[0] == '/' ? 1 : 0;
            var idx = nameWithoutVerb.IndexOf('/', slashPrefixShift); // typically first symbol of the path is '/'
            string container = idx != -1 ? nameWithoutVerb.Substring(slashPrefixShift, idx - slashPrefixShift) : nameWithoutVerb.Substring(slashPrefixShift);

            // This is very naive overwriting of Azure Blob dependency that is compatible with the today's implementation
            //
            // Possible improvements:
            //
            // 1. Use specific name for specific operations. Like "Lease Blob" for "?comp=lease" query parameter
            // 2. Use account name as a target instead of "account.blob.core.windows.net"
            // 3. Do not include container name into name as it is high cardinality. Move to custom properties
            // 4. Parse blob name and put into custom properties as well
            httpDependency.Type = RemoteDependencyConstants.AzureBlob;
            httpDependency.Name = verb + account + '/' + container;

            return true;
        }
    }
}
