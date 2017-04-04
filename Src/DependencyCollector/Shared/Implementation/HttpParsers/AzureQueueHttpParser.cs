namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.HttpParsers
{
    using System;
    using DataContracts;
    using Implementation;

    /// <summary>
    /// HTTP Dependency parser that attempts to parse dependency as Azure Queue call.
    /// </summary>
    internal static class AzureQueueHttpParser
    {
        private static readonly string[] AzureQueueVerbPrefixes = { "GET ", "PUT ", "OPTIONS ", "HEAD ", "DELETE ", "POST " };

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

            if (!host.EndsWith(".queue.core.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            ////
            //// Queue Service REST API: https://msdn.microsoft.com/en-us/library/azure/dd179423.aspx
            ////

            string account = host.Substring(0, host.IndexOf('.'));

            string verb = null;
            string nameWithoutVerb = name;

            for (int i = 0; i < AzureQueueVerbPrefixes.Length; i++)
            {
                var verbPrefix = AzureQueueVerbPrefixes[i];
                if (name.StartsWith(verbPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    verb = name.Substring(0, verbPrefix.Length);
                    nameWithoutVerb = name.Substring(verbPrefix.Length);
                    break;
                }
            }

            var isFirstSlash = nameWithoutVerb[0] == '/' ? 1 : 0;
            var idx = nameWithoutVerb.IndexOf('/', isFirstSlash); // typically first symbol of the path is '/'
            string queueName = idx != -1 ? nameWithoutVerb.Substring(isFirstSlash, idx - isFirstSlash) : nameWithoutVerb.Substring(isFirstSlash);

            httpDependency.Type = RemoteDependencyConstants.AzureQueue;
            httpDependency.Name = verb + account + '/' + queueName;

            return true;
        }
    }
}
