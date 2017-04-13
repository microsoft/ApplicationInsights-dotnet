namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DiagnosticListeners;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
    using Xunit;

    public class HttpHeadersUtilitiesTest
    {
        [Fact]
        public void GetHeaderValuesShouldReturnHeader()
        {
            IHeaderDictionary headerDictionary = new HeaderDictionary(
                new Dictionary<string, StringValues>() {
                    { "HeaderName", new StringValues("app=id") },
                    { "NoizyName", new StringValues("noizy=noizy-id") }
            });

            IEnumerable<string> headerValues = HttpHeadersUtilities.GetHeaderValues(headerDictionary, "HeaderName");

            Assert.NotNull(headerValues);
            Assert.True(headerValues.Count() == 1);
            Assert.Equal("app=id", headerValues.First());
        }

        [Fact]
        public void GetHeaderKeyValueShouldReturnValue()
        {
            IHeaderDictionary headerDictionary = new HeaderDictionary(
                new Dictionary<string, StringValues>() {
                                { "HeaderName", new StringValues("app=id") },
                                { "NoizyName", new StringValues("noizy=noizy-id") }
            });

            string actual = HttpHeadersUtilities.GetHeaderKeyValue(headerDictionary, "HeaderName", "app");

            Assert.Equal("id", actual);
        }

        [Fact]
        public void GetRequestContextKeyValueShouldReturnContextKeyValue()
        {
            IHeaderDictionary headerDictionary = new HeaderDictionary(
    new Dictionary<string, StringValues>() {
                                { RequestResponseHeaders.RequestContextHeader, new StringValues("app=id") },
                                { "NoizyName", new StringValues("noizy=noizy-id") }
});

            string actual = HttpHeadersUtilities.GetRequestContextKeyValue(headerDictionary, "app");

            Assert.Equal("id", actual);
        }

        [Fact]
        public void SetHeaderKeyValueShouldRequireHeaders()
        {
            Assert.ThrowsAny<ArgumentNullException>(() =>
            {
                IHeaderDictionary nullDictionary = null;
                HttpHeadersUtilities.SetHeaderKeyValue(nullDictionary, "header", "key", "value");
            });
        }

        [Fact]
        public void SetHeaderKeyValueShouldAddTheHeader()
        {
            IHeaderDictionary headers = new HeaderDictionary();
            HttpHeadersUtilities.SetHeaderKeyValue(headers, "newHeader", "key", "value");

            Assert.Equal(1, headers.Count);

            Assert.Equal("newHeader", headers.First().Key);
            Assert.Equal("key=value", headers.First().Value);
        }

        [Fact]
        public void SetHeaderKeyValuesShouldAppendToExistingHeaders()
        {
            IHeaderDictionary headers = new HeaderDictionary(
                new Dictionary<string, StringValues>() { { "HeaderName", new StringValues("app=id") } });
            HttpHeadersUtilities.SetHeaderKeyValue(headers, "newHeader", "key", "value");

            Assert.Equal(2, headers.Count);

            Assert.Equal("HeaderName", headers.First().Key);
            Assert.Equal("app=id", headers.First().Value);
            Assert.Equal("newHeader", headers.Skip(1).First().Key);
            Assert.Equal("key=value", headers.Skip(1).First().Value);
        }

        [Fact]
        public void SetHeaderKeyValuesShouldUpdateExistingHeaders()
        {
            IHeaderDictionary headers = new HeaderDictionary(
                new Dictionary<string, StringValues>() { { "HeaderName", new StringValues("app=id") } });
            HttpHeadersUtilities.SetHeaderKeyValue(headers, "HeaderName", "key", "value");

            Assert.Equal(1, headers.Count);

            Assert.Equal("HeaderName", headers.First().Key);
            Assert.Equal("app=id,key=value", headers.First().Value);
        }

        [Fact]
        public void SetRequestContextKeyValueShouldSetTheHeaders()
        {
            IHeaderDictionary headers = new HeaderDictionary();
            HttpHeadersUtilities.SetRequestContextKeyValue(headers, "key", "value");

            Assert.Equal(1, headers.Count);

            Assert.Equal(RequestResponseHeaders.RequestContextHeader, headers.First().Key);
            Assert.Equal("key=value", headers.First().Value);
        }

        [Fact]
        public void ContainsRequestContextKeyValueShouldReturnTrueWhenExists()
        {
            IHeaderDictionary headers = new HeaderDictionary(
                new Dictionary<string, StringValues>() { { RequestResponseHeaders.RequestContextHeader, new StringValues("app=id,other=otherValue") } });
            Assert.True(HttpHeadersUtilities.ContainsRequestContextKeyValue(headers, "app"));
            Assert.True(HttpHeadersUtilities.ContainsRequestContextKeyValue(headers, "other"));
            Assert.False(HttpHeadersUtilities.ContainsRequestContextKeyValue(headers, "Non-exists"));
        }
    }
}
