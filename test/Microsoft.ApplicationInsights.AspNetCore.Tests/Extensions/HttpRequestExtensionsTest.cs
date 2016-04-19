namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Extensions
{
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;
    using System;
    using Xunit;

    public class HttpRequestExtensionsTest
    {
        const string ExpectedSchema = "http";
        const string ExpectedHostName = "randomhost";
        const string ExpectedPath = "/path/path/";
        const string ExpectedQueryString = "?queryType=1";

        [Fact]
        public void TestGetUriThrowsExceptionOnNullRequestObject()
        {
            Assert.Throws(
                typeof(ArgumentNullException), 
                () =>
                {
                    HttpRequestExtensions.GetUri(null);
                });
        }

        [Fact]
        public void TestGetUriThrowsExceptionOnRequestObjectSchemeIsEmpty()
        {
            var request = new DefaultHttpContext().Request;

            var exception = Assert.Throws(
                typeof(ArgumentException),
                () =>
                {
                    HttpRequestExtensions.GetUri(request);
                });

            Assert.True(exception.Message.Contains("Scheme"), "Scheme is not mentioned in the exception");
        }

        [Fact]
        public void TestGetUriThrowsExceptionOnRequestObjectHostIsNotSpecified()
        {
            var request = new DefaultHttpContext().Request;
            request.Scheme = ExpectedSchema;

            var exception = Assert.Throws(
                typeof(ArgumentException),
                () =>
                {
                    HttpRequestExtensions.GetUri(request);
                });

            Assert.True(exception.Message.Contains("Host"), "Host is not mentioned in the exception");
        }

        [Fact]
        public void TestGetUriReturnsCorrectUriIfRequestObjectSchemeAndHostAreSpecified()
        {
            var request = new DefaultHttpContext().Request;

            request.Scheme = ExpectedSchema;
            request.Host = new HostString(ExpectedHostName);

            var uri = HttpRequestExtensions.GetUri(request);

            Assert.Equal(
                new Uri(string.Format("{0}://{1}", ExpectedSchema, ExpectedHostName)), 
                uri);
        }

        [Fact]
        public void TestGetUriReturnsCorrectUriIfRequestObjectSchemeAndHostAndPathAreSpecified()
        {
            var request = new DefaultHttpContext().Request;

            request.Scheme = ExpectedSchema;
            request.Host = new HostString(ExpectedHostName);
            request.Path = new PathString(ExpectedPath);

            var uri = HttpRequestExtensions.GetUri(request);

            Assert.Equal(
                new Uri(string.Format("{0}://{1}{2}", ExpectedSchema, ExpectedHostName, ExpectedPath)), 
                uri);
        }

        [Fact]
        public void TestGetUriReturnsCorrectUriIfRequestObjectSchemeAndHostAndPathAndQueryStringAreSpecified()
        {
            var request = new DefaultHttpContext().Request;

            request.Scheme = ExpectedSchema;
            request.Host = new HostString(ExpectedHostName);
            request.Path = new PathString(ExpectedPath);
            request.QueryString = new QueryString(ExpectedQueryString);

            var uri = HttpRequestExtensions.GetUri(request);

            Assert.Equal(
                new Uri(string.Format("{0}://{1}{2}{3}", ExpectedSchema, ExpectedHostName, ExpectedPath, ExpectedQueryString)),
                uri);
        }
    }
}