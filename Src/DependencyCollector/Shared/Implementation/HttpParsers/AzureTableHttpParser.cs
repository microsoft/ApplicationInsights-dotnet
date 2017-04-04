namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.HttpParsers
{
    using System;
    using DataContracts;
    using Implementation;

    /// <summary>
    /// HTTP Dependency parser that attempts to parse dependency as Azure Table call.
    /// </summary>
    internal static class AzureTableHttpParser
    {
        private static readonly string[] AzureTableVerbPrefixes = { "GET ", "PUT ", "OPTIONS ", "HEAD ", "DELETE ", "MERGE ", "POST " };

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

            if (!host.EndsWith(".table.core.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            //// 
            //// Table Service REST API: https://msdn.microsoft.com/en-us/library/azure/dd179363.aspx
            ////

            string account = host.Substring(0, host.IndexOf('.'));

            string verb = null;
            string nameWithoutVerb = name;
            for (int i = 0; i < AzureTableVerbPrefixes.Length; i++)
            {
                var verbPrefix = AzureTableVerbPrefixes[i];
                if (name.StartsWith(verbPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    verb = name.Substring(0, verbPrefix.Length);
                    nameWithoutVerb = name.Substring(verbPrefix.Length);
                    break;
                }
            }

            var slashPrefixShift = nameWithoutVerb[0] == '/' ? 1 : 0;
            var idx = nameWithoutVerb.IndexOf('/', slashPrefixShift); // typically first symbol of the path is '/'

            string tableName = idx != -1 ? nameWithoutVerb.Substring(slashPrefixShift, idx - slashPrefixShift) : nameWithoutVerb.Substring(slashPrefixShift);
            idx = tableName.IndexOf('(');
            tableName = idx != -1 ? tableName.Substring(0, idx) : tableName;

            httpDependency.Type = RemoteDependencyConstants.AzureTable;
            httpDependency.Name = verb + account + '/' + tableName;

            return true;
        }
    }
}
