namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using Channel;
    using DataContracts;
    using Extensibility;
    using Implementation;

    /// <summary>
    /// Telemetry Initializer that parses http dependencies into well-known types like Azure Storage.
    /// </summary>
    public class HttpDependenciesParsingTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string[] azureBlobVerbPrefixes = { "GET ", "PUT ", "OPTIONS ", "HEAD ", "DELETE " };
        private readonly string[] azureTableVerbPrefixes = { "GET ", "PUT ", "OPTIONS ", "HEAD ", "DELETE ", "MERGE ", "POST " };

        /// <summary>
        /// If telemetry item is http dependency - converts it to the well-known type of the dependency.
        /// </summary>
        /// <param name="telemetry">Telemetry item to convert.</param>
        public void Initialize(ITelemetry telemetry)
        {
            var httpDependency = telemetry as DependencyTelemetry;

            if (httpDependency != null && httpDependency.Type != null && httpDependency.Type.Equals(RemoteDependencyConstants.HTTP, StringComparison.OrdinalIgnoreCase))
            {
                string host = httpDependency.Target;

                string account;
                string verb;
                string container;

                if (this.TryParseAzureBlob(httpDependency.Target, httpDependency.Name, httpDependency.Data, out account, out verb, out container))
                {
                    httpDependency.Type = RemoteDependencyConstants.AzureBlob;

                    // This is very naive overwriting of Azure Blob dependency that is compatible with the today's implementation
                    //
                    // Possible improvements:
                    //
                    // 1. Use specific name for specific operations. Like "Lease Blob" for "?comp=lease" query parameter
                    // 2. Use account name as a target instead of "account.blob.core.windows.net"
                    // 3. Do not include container name into name as it is high cardinality. Move to custom properties
                    // 4. Parse blob name and put into custom properties as well
                    httpDependency.Name = verb + account + '/' + container;
                }
                else if (this.TryParseAzureTable(httpDependency.Target, httpDependency.Name, httpDependency.Data, out account, out verb, out container))
                {
                    httpDependency.Type = RemoteDependencyConstants.AzureTable;
                    httpDependency.Name = verb + account + '/' + container;
                }

                ////else if (host.EndsWith("queue.core.windows.net", StringComparison.OrdinalIgnoreCase))
                ////{
                ////    httpDependency.Type = RemoteDependencyConstants.AzureQueue;
                ////}
            }
        }

        private bool TryParseAzureBlob(string host, string name, string url, out string account, out string verb, out string container)
        {
            bool result = false;

            account = null;
            verb = null;
            container = null;

            if (name != null && host != null && url != null)
            {
                if (host.EndsWith("blob.core.windows.net", StringComparison.OrdinalIgnoreCase))
                {
                    ////
                    //// Blob Service REST API: https://msdn.microsoft.com/en-us/library/azure/dd135733.aspx
                    ////

                    account = host.Substring(0, host.IndexOf('.'));

                    string nameWithoutVerb = name;

                    for (int i = 0; i < this.azureBlobVerbPrefixes.Length; i++)
                    {
                        var verbPrefix = this.azureBlobVerbPrefixes[i];
                        if (name.StartsWith(verbPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            verb = name.Substring(0, verbPrefix.Length);
                            nameWithoutVerb = name.Substring(verbPrefix.Length);
                            break;
                        }
                    }

                    var isFirstSlash = nameWithoutVerb[0] == '/' ? 1 : 0;
                    var idx = nameWithoutVerb.IndexOf('/', isFirstSlash); // typically first symbol of the path is '/'
                    container = idx != -1 ? nameWithoutVerb.Substring(isFirstSlash, idx - isFirstSlash) : nameWithoutVerb.Substring(isFirstSlash);

                    result = true;
                }
            }

            return result;
        }

        private bool TryParseAzureTable(string host, string name, string url, out string account, out string verb, out string tableName)
        {
            bool result = false;

            account = null;
            verb = null;
            tableName = null;

            if (name != null && host != null && url != null)
            {
                if (host.EndsWith("table.core.windows.net", StringComparison.OrdinalIgnoreCase))
                {
                    ////
                    //// Table Service REST API: https://msdn.microsoft.com/en-us/library/azure/dd179423.aspx
                    ////

                    account = host.Substring(0, host.IndexOf('.'));

                    string nameWithoutVerb = name;

                    for (int i = 0; i < this.azureTableVerbPrefixes.Length; i++)
                    {
                        var verbPrefix = this.azureTableVerbPrefixes[i];
                        if (name.StartsWith(verbPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            verb = name.Substring(0, verbPrefix.Length);
                            nameWithoutVerb = name.Substring(verbPrefix.Length);
                            break;
                        }
                    }

                    var isFirstSlash = nameWithoutVerb[0] == '/' ? 1 : 0;
                    var idx = nameWithoutVerb.IndexOf('/', isFirstSlash); // typically first symbol of the path is '/'
                    tableName = idx != -1 ? nameWithoutVerb.Substring(isFirstSlash, idx - isFirstSlash) : nameWithoutVerb.Substring(isFirstSlash);
                    idx = tableName.IndexOf('(');
                    tableName = idx != -1 ? tableName.Substring(0, idx) : tableName;

                    result = true;
                }
            }

            return result;
        }
    }
}
