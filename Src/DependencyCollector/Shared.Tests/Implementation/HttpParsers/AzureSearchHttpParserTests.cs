namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.HttpParsers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AzureSearchHttpParserTests
    {
        [TestMethod]
        public void AzureSearchHttpParserConvertsValidDependencies()
        {
            var testCases = new List<Tuple<string, string, string>>
            {
                // Index operations
                Tuple.Create("Create index", "POST", "https://myaccount.search.windows.net/indexes?api-version=2017-11-11"),
                Tuple.Create("Update index", "PUT", "https://myaccount.search.windows.net/indexes/myindex?api-version=2017-11-11"),
                Tuple.Create("List indexes", "GET", "https://myaccount.search.windows.net/indexes?api-version=2017-11-11  "),
                Tuple.Create("Get index", "GET", "https://myaccount.search.windows.net/indexes/myindex?api-version=2017-11-11"),
                Tuple.Create("Delete index", "DELETE", "https://myaccount.search.windows.net/indexes/myindex?api-version=2017-11-11"),
                Tuple.Create("Get index statistics", "GET", "https://myaccount.search.windows.net/indexes/myindex/stats?api-version=2017-11-11"),
                Tuple.Create("Analyze text", "POST", "https://myaccount.search.windows.net/indexes/myindex/analyze?api-version=2017-11-11"),

                // Document operations
                Tuple.Create("Add/update/delete documents", "POST", "https://myaccount.search.windows.net/indexes/myindex/docs/index?api-version=2017-11-11"),
                Tuple.Create("Search documents", "GET", "https://myaccount.search.windows.net/indexes/myindex/docs?[query parameters]"),
                Tuple.Create("Search documents", "POST", "https://myaccount.search.windows.net/indexes/myindex/docs/search?api-version=2017-11-11"),
                Tuple.Create("Suggestions", "GET", "https://myaccount.search.windows.net/indexes/myindex/docs/suggest?[query parameters]"),
                Tuple.Create("Suggestions", "POST", "https://myaccount.search.windows.net/indexes/myindex/docs/suggest?api-version=2017-11-11"),
                Tuple.Create("Autocomplete", "GET", "https://myaccount.search.windows.net/indexes/myindex/docs/autocomplete?[query parameters]"),
                Tuple.Create("Autocomplete", "POST", "https://myaccount.search.windows.net/indexes/myindex/docs/autocomplete?api-version=2017-11-11"),
                Tuple.Create("Lookup document", "GET", "https://myaccount.search.windows.net/indexes/myindex/docs/key?[query parameters]"),
                Tuple.Create("Count documents", "GET", "https://myaccount.search.windows.net/indexes/myindex/docs/$count?api-version=2017-11-11"),

                // Indexer operations
                Tuple.Create("Create data source", "POST", "https://myaccount.search.windows.net/datasources?api-version=2017-11-11"),

                // Service operations
                Tuple.Create("Get service statistics", "GET", "https://myaccount.search.windows.net/servicestats?api-version=2017-11-11"),

                // Skillset operations
                Tuple.Create("Create skillset", "PUT", "https://myaccount.search.windows.net/skillsets/myskillset?api-version=2017-11-11-Preview"),
                Tuple.Create("Delete skillset", "DELETE", "https://myaccount.search.windows.net/skillsets/myskillset?api-version=2017-11-11-Preview"),
                Tuple.Create("Get skillset", "GET", "https://myaccount.search.windows.net/skillsets/myskillset?api-version=2017-11-11-Preview"),
                Tuple.Create("List skillsets", "GET", "https://myaccount.search.windows.net/skillsets?api-version=2017-11-11-Preview"),
                Tuple.Create("Update skillset", "PUT", "https://myaccount.search.windows.net/skillsets/myskillset?api-version=2017-11-11-Preview"),

                // Synonyms operations
                Tuple.Create("Create synonym map", "POST", "https://myaccount.search.windows.net/synonymmaps?api-version=2017-11-11"),
                Tuple.Create("Update synonym map", "PUT", "https://myaccount.search.windows.net/synonymmaps/mysynonym?api-version=2017-11-11"),
                Tuple.Create("List synonym maps", "GET", "https://myaccount.search.windows.net/synonymmaps?api-version=2017-11-11"),
                Tuple.Create("Get synonym map", "GET", "https://myaccount.search.windows.net/synonymmaps/mysynonym?api-version=2017-11-11"),
                Tuple.Create("Delete synonym map", "DELETE", "https://myaccount.search.windows.net/synonymmaps/mysynonym?api-version=2017-11-11")
            };

            foreach (var testCase in testCases)
            {
                this.AzureSearchHttpParserConvertsValidDependencies(
                    testCase.Item1,
                    testCase.Item2,
                    testCase.Item3);
            }
        }

        private void AzureSearchHttpParserConvertsValidDependencies(
            string operation,
            string verb,
            string url)
        {
            var parsedUrl = new Uri(url);

            // Parse with verb
            var d = new DependencyTelemetry(
                dependencyTypeName: RemoteDependencyConstants.HTTP,
                target: parsedUrl.Host,
                dependencyName: verb + " " + parsedUrl.AbsolutePath,
                data: parsedUrl.OriginalString);

            var success = AzureSearchHttpParser.TryParse(ref d);

            Assert.IsTrue(success, operation);
            Assert.AreEqual(RemoteDependencyConstants.AzureSearch, d.Type, operation);
            Assert.AreEqual(parsedUrl.Host, d.Target, operation);
            Assert.AreEqual(operation, d.Name, operation);

            // Parse without verb
            d = new DependencyTelemetry(
                dependencyTypeName: RemoteDependencyConstants.HTTP,
                target: parsedUrl.Host,
                dependencyName: parsedUrl.AbsolutePath,
                data: parsedUrl.OriginalString);

            success = AzureSearchHttpParser.TryParse(ref d);

            Assert.IsTrue(success, operation);
            Assert.AreEqual(RemoteDependencyConstants.AzureSearch, d.Type, operation);
            Assert.AreEqual(parsedUrl.Host, d.Target, operation);
            string moniker = HttpParsingHelper.BuildOperationMoniker(null, HttpParsingHelper.ParseResourcePath(parsedUrl.AbsolutePath));
            Assert.AreEqual(moniker, d.Name, operation);
        }
    }
}
