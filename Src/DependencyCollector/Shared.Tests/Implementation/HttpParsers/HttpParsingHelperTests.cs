namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.HttpParsers
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpParsingHelperTests
    {
        [TestMethod]
        public void SplitTests()
        {
            AssertAreDeepEqual(HttpParsingHelper.Split("a/bb/ccc/dddd", '/', 0, -1), "a", "bb", "ccc", "dddd");
            AssertAreDeepEqual(HttpParsingHelper.Split("/a/bb/ccc/dddd", '/', 0, -1), string.Empty, "a", "bb", "ccc", "dddd");
            AssertAreDeepEqual(HttpParsingHelper.Split("/a/bb/ccc/dddd", '/', 1, -1), "a", "bb", "ccc", "dddd");
            AssertAreDeepEqual(HttpParsingHelper.Split("/a/bb/ccc/dddd/", '/', 1, -1), "a", "bb", "ccc", "dddd", string.Empty);
            AssertAreDeepEqual(HttpParsingHelper.Split("/a/bb/ccc/dddd?ee", '/', 1, 14), "a", "bb", "ccc", "dddd");
            AssertAreDeepEqual(HttpParsingHelper.Split("/a/bb/ccc/dddd", '/', 3, 8), "bb", "cc");
            AssertAreDeepEqual(HttpParsingHelper.Split("/a//bb//ccc//dddd/", '/', 1, -1), "a", string.Empty, "bb", string.Empty, "ccc", string.Empty, "dddd", string.Empty);
        }

        [TestMethod]
        public void TokenizeRequestPathTests()
        {
            AssertAreDeepEqual(HttpParsingHelper.TokenizeRequestPath("a/bb/ccc/dddd"), "a", "bb", "ccc", "dddd");
            AssertAreDeepEqual(HttpParsingHelper.TokenizeRequestPath("/a/bb/ccc/dddd"), "a", "bb", "ccc", "dddd");
            AssertAreDeepEqual(HttpParsingHelper.TokenizeRequestPath("/a/bb/ccc/dddd/"), "a", "bb", "ccc", "dddd", string.Empty);
            AssertAreDeepEqual(HttpParsingHelper.TokenizeRequestPath("/a/bb/ccc/dddd?ee"), "a", "bb", "ccc", "dddd");
            AssertAreDeepEqual(HttpParsingHelper.TokenizeRequestPath("/a/bb/ccc/dddd/?ee"), "a", "bb", "ccc", "dddd", string.Empty);
            AssertAreDeepEqual(HttpParsingHelper.TokenizeRequestPath("/a/bb/ccc/dddd#ee"), "a", "bb", "ccc", "dddd");
            AssertAreDeepEqual(HttpParsingHelper.TokenizeRequestPath("/a/bb/ccc/dddd?ee/ff"), "a", "bb", "ccc", "dddd");
        }

        [TestMethod]
        public void ParseResourcePathTests()
        {
            AssertRequestPathIsValid(HttpParsingHelper.ParseResourcePath("a/bb/ccc"), "a", "bb", "ccc", null);
            AssertRequestPathIsValid(HttpParsingHelper.ParseResourcePath("a/bb/ccc/"), "a", "bb", "ccc", string.Empty);
            AssertRequestPathIsValid(HttpParsingHelper.ParseResourcePath("/a/bb/ccc/dddd"), "a", "bb", "ccc", "dddd");
            AssertRequestPathIsValid(HttpParsingHelper.ParseResourcePath("/a/bb/ccc/dddd/"), "a", "bb", "ccc", "dddd");
            AssertRequestPathIsValid(HttpParsingHelper.ParseResourcePath("/a/bb/a/dddd"), "a", "bb", "a", "dddd");
            AssertRequestPathIsValid(HttpParsingHelper.ParseResourcePath("/a/bb/ccc/dddd?ee"), "a", "bb", "ccc", "dddd");
            AssertRequestPathIsValid(HttpParsingHelper.ParseResourcePath("/a/bb/ccc#ee/ff"), "a", "bb", "ccc", null);
            AssertRequestPathIsValid(HttpParsingHelper.ParseResourcePath("/a/bb/ccc/?ee/ff"), "a", "bb", "ccc", string.Empty);
        }

        [TestMethod]
        public void BuildOperationMonikerTests()
        {
            ValidateBuildOperationMoniker("PUT", "a/bb/ccc", "PUT /a/*/ccc");
            ValidateBuildOperationMoniker("PUT", "a/bb/ccc/", "PUT /a/*/ccc/*");
            ValidateBuildOperationMoniker("GET", "/a/bb/ccc/dddd", "GET /a/*/ccc/*");
            ValidateBuildOperationMoniker("GET", "/a/bb/ccc/dddd/", "GET /a/*/ccc/*");
            ValidateBuildOperationMoniker("PUT", "/a/bb/a/dddd", "PUT /a/*/a/*");
            ValidateBuildOperationMoniker("PUT", "/a/bb/ccc/dddd?ee", "PUT /a/*/ccc/*");
            ValidateBuildOperationMoniker("PUT", "/a/bb/ccc#ee/ff", "PUT /a/*/ccc");
            ValidateBuildOperationMoniker("PUT", "/a/bb/ccc/?ee/ff", "PUT /a/*/ccc/*");
        }

        private static void ValidateBuildOperationMoniker(string verb, string url, string expectedMoniker)
        {
            var resourcePath = HttpParsingHelper.ParseResourcePath(url);
            Assert.AreEqual(expectedMoniker, HttpParsingHelper.BuildOperationMoniker(verb, resourcePath));
        }

        private static void AssertRequestPathIsValid(List<KeyValuePair<string, string>> resourcePath, params string[] expected)
        {
            List<string> actual = resourcePath.SelectMany(kvp => new[] { kvp.Key, kvp.Value }).ToList();

            AssertAreDeepEqual(actual, expected);
        }

        private static void AssertAreDeepEqual(List<string> actual, params string[] expected)
        {
            AssertAreDeepEqual(actual, new List<string>(expected));
        }

        private static void AssertAreDeepEqual(List<string> actual, List<string> expected)
        {
            Assert.AreEqual(expected.Count, actual.Count, "List length mismatched");
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i], $"Item at index {i} mismatched");
            }
        }
    }
}
