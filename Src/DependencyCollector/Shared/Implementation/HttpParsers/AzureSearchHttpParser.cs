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

            var account = host.Substring(0, host.IndexOf('.'));

            HttpParsingHelper.ExtractVerb(name, out var verb, out var nameWithotVerb, AzureSearchSupportedVerbs);

            var pathTokens = HttpParsingHelper.TokenizeRequestPath(nameWithotVerb);

            httpDependency.Type = RemoteDependencyConstants.AzureSearch;
            httpDependency.Name = "";

            return true;
        }
    }
}
