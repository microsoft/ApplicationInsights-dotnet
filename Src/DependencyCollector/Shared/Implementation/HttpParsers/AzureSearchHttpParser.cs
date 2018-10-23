namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.HttpParsers
{
    using System.Collections.Generic;

    using DataContracts;
    using Implementation;

    internal static class AzureSearchHttpParser
    {
        private static readonly string[] AzureSearchHostSuffixes =
        {
            ".search.windows.net"
        };

        private static readonly string[] AzureSearchSupportedVerbs = { "GET", "POST", "PUT", "HEAD", "DELETE" };

        private static readonly Dictionary<string, string> OperationNames = new Dictionary<string, string>
        {
            // Index operations
            ["POST /indexes"] = "Create index",
            ["PUT /indexes/*"] = "Update index",
            ["GET /indexes"] = "List indexes",
            ["GET /indexes/*"] = "Get index",
            ["DELETE /indexes/*"] = "Delete index",
            ["GET /indexes/*/stats"] = "Get index statistics",
            ["POST /indexes/*/analyze"] = "Analyze text",

            // Document operations
            ["POST /indexes"] = "Add/update/delete documents",
        };

        internal static bool TryParse(ref DependencyTelemetry httpDependency)
        {
            var name = httpDependency.Name;
            var host = httpDependency.Target;
            var url = httpDependency.Data;

            if (name == null || host == null || url == null)
            {
                return false;
            }

            if (!HttpParsingHelper.EndsWithAny(host, AzureSearchHostSuffixes))
            {
                return false;
            }

            ////
            //// Azure Search REST API: https://docs.microsoft.com/en-us/rest/api/searchservice/
            ////

            var account = host.Substring(0, host.IndexOf('.'));

            HttpParsingHelper.ExtractVerb(name, out var verb, out var nameWithoutVerb, AzureSearchSupportedVerbs);

            var resourcePath = HttpParsingHelper.ParseResourcePath(nameWithoutVerb);

            var operation = HttpParsingHelper.BuildOperationMoniker(verb, resourcePath);
            var operationName = GetOperationName(httpDependency, operation);

            httpDependency.Type = RemoteDependencyConstants.AzureSearch;
            httpDependency.Name = operationName;

            return true;
        }

        private static string GetOperationName(DependencyTelemetry httpDependency, string operation)
        {
            if (!OperationNames.TryGetValue(operation, out var operationName))
            {
                return operation;
            }

            return operationName;
        }
    }
}
