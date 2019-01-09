using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class TransmissionTest : AsyncTest
    {
        private static Stream CreateStream(string text)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(text);
            writer.Flush();

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        [TestClass]
        public class Constructor : TransmissionTest
        {
            [TestMethod]
            public void SetsEndpointAddressPropertyToSpecifiedValue()
            {
                var expectedAddress = new Uri("expected://uri");
                var transmission = new Transmission(expectedAddress, new byte[1], "content/type", "content/encoding");
                Assert.AreEqual(expectedAddress, transmission.EndpointAddress);
            }

            [TestMethod]
            public void ThrowsArgumentNullExceptionWhenEndpointAddressIsNull()
            {
                AssertEx.Throws<ArgumentNullException>(() => new Transmission(null, new byte[1], "content/type", "content/encoding"));
            }

            [TestMethod]
            public void SetsContentPropertyToSpecifiedValue()
            {
                var expectedContent = new byte[42];
                var transmission = new Transmission(new Uri("http://address"), expectedContent, "content/type", "content/encoding");
                Assert.AreSame(expectedContent, transmission.Content);
            }

            [TestMethod]
            public void ThrowsArgumentNullExceptionWhenContentIsNull()
            {
                AssertEx.Throws<ArgumentNullException>(() => new Transmission(new Uri("http://address"), (byte[])null, "content/type", "content/encoding"));
            }

            [TestMethod]
            public void SetsContentTypePropertyToSpecifiedValue()
            {
                string expectedContentType = "TestContentType123";
                var transmission = new Transmission(new Uri("http://address"), new byte[1], expectedContentType, "content/encoding");
                Assert.AreSame(expectedContentType, transmission.ContentType);
            }

            [TestMethod]
            public void ThrowsArgumentNullExceptionWhenContentTypeIsNull()
            {
                AssertEx.Throws<ArgumentNullException>(() => new Transmission(new Uri("http://address"), new byte[1], null, "content/encoding"));
            }

            [TestMethod]
            public void SetContentEncodingPropertyToSpecifiedValue()
            {
                string expectedContentEncoding = "gzip";
                var transmission = new Transmission(new Uri("http://address"), new byte[1], "any/content", expectedContentEncoding);
                Assert.AreSame(expectedContentEncoding, transmission.ContentEncoding);
            }

            [TestMethod]
            public void SetsTimeoutTo100SecondsByDefaultToMatchHttpWebRequest()
            {
                var transmission = new Transmission(new Uri("http://address"), new byte[1], "content/type", "content/encoding");
                Assert.AreEqual(TimeSpan.FromSeconds(100), transmission.Timeout);
            }

            [TestMethod]
            public void SetsTimeoutToSpecifiedValue()
            {
                var expectedValue = TimeSpan.FromSeconds(42);
                var transmission = new Transmission(new Uri("http://address"), new byte[1], "content/type", "content/encoding", expectedValue);
                Assert.AreEqual(expectedValue, transmission.Timeout);
            }
        }

        [TestClass]
        public class SendAsync : TransmissionTest
        {
            [TestMethod]
            public async Task SendAsyncUsesPostMethodToSpecifiedHttpEndpoint()
            {
                var expectedUri = new Uri("http://uri");
                var handler = new HandlerForFakeHttpClient
                {
                    InnerHandler = new HttpClientHandler(),
                    OnSendAsync = (req, cancellationToken) =>
                    {
                        // VALIDATE
                        Assert.AreEqual(expectedUri, req.RequestUri);
                        Assert.AreEqual(HttpMethod.Post, req.Method);
                        return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage());
                    }
                };

                using (var fakeHttpClient = new HttpClient(handler))
                {
                    var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };

                    // Instantiate Transmission with the mock HttpClient
                    Transmission transmission = new Transmission(expectedUri, new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, string.Empty, string.Empty);
                    // transmission.Timeout = TimeSpan.FromMilliseconds(1);

                    HttpWebResponseWrapper result = await transmission.SendAsync();
                }
            }

            [TestMethod]
            public async Task SendAsyncUsesSpecifiedContentTypeAndEncoding()
            {
                var expectedContentType = "content/type";
                var expectedContentEncoding = "contentEncoding";
                var handler = new HandlerForFakeHttpClient
                {
                    InnerHandler = new HttpClientHandler(),
                    OnSendAsync = (req, cancellationToken) =>
                    {
                        // VALIDATE
                        Assert.AreEqual(expectedContentType, req.Content.Headers.ContentType.MediaType);
                        Assert.AreEqual(expectedContentEncoding, req.Content.Headers.ContentEncoding.FirstOrDefault());

                        return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage());
                    }
                };

                using (var fakeHttpClient = new HttpClient(handler))
                {
                    var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };

                    // Instantiate Transmission with the mock HttpClient
                    var transmission = new Transmission(new Uri("http://testuri"), new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, expectedContentType, expectedContentEncoding);

                    HttpWebResponseWrapper result = await transmission.SendAsync();
                }
            }

            [TestMethod]
            public async Task SendAsyncUsesEmptyContentTypeIfNoneSpecifiedInConstructor()
            {
                var handler = new HandlerForFakeHttpClient
                {
                    InnerHandler = new HttpClientHandler(),
                    OnSendAsync = (req, cancellationToken) =>
                    {
                        // VALIDATE
                        Assert.IsNull(req.Content.Headers.ContentType);

                        return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage());
                    }
                };

                using (var fakeHttpClient = new HttpClient(handler))
                {
                    var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };

                    // Instantiate Transmission with the mock HttpClient
                    var transmission = new Transmission(new Uri("http://testuri"), new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, string.Empty, "ContentEncoding");

                    HttpWebResponseWrapper result = await transmission.SendAsync();
                }
            }

            [TestMethod]
            public void ThrowsInvalidOperationExceptionWhenTransmissionIsAlreadySending()
            {
                AsyncTest.Run(async () =>
                {
                    var transmission = new Transmission();
                    FieldInfo isSendingField = typeof(Transmission).GetField("isSending", BindingFlags.NonPublic | BindingFlags.Instance);
                    isSendingField.SetValue(transmission,1);
                    await AssertEx.ThrowsAsync<InvalidOperationException>(() => transmission.SendAsync());
                });
            }

            [TestMethod]
            public async Task SendAsyncHandleResponseForPartialContentResponse()
            {
                var handler = new HandlerForFakeHttpClient
                {
                    InnerHandler = new HttpClientHandler(),
                    OnSendAsync = (req, cancellationToken) =>
                    {
                        HttpResponseMessage response = new HttpResponseMessage();
                        response.StatusCode = HttpStatusCode.PartialContent;
                        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5));
                        return Task.FromResult<HttpResponseMessage>(response);                        
                    }
                };

                using (var fakeHttpClient = new HttpClient(handler))
                {                    
                    // Instantiate Transmission with the mock HttpClient
                    Transmission transmission = new Transmission(new Uri("http://uri"), new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, string.Empty, string.Empty);

                    // ACT
                    HttpWebResponseWrapper result = await transmission.SendAsync();

                    // VALIDATE
                    Assert.AreEqual(206, result.StatusCode);
                    Assert.AreEqual("5", result.RetryAfterHeader);
                    Assert.IsNull(result.Content);
                }
            }

            [TestMethod]
            public async Task SendAsyncSendsContentPassedInConstructor()
            {
                var expectedContent = new byte[] {1, 2, 3, 4, 5};
                var handler = new HandlerForFakeHttpClient
                {
                    InnerHandler = new HttpClientHandler(),
                    OnSendAsync = async (req, cancellationToken) =>
                    {
                        HttpResponseMessage response = new HttpResponseMessage();
                        byte[] actualContent = await req.Content.ReadAsByteArrayAsync();
                        AssertEx.AreEqual(expectedContent, actualContent);
                        return await Task.FromResult<HttpResponseMessage>(response);
                    }
                };

                using (var fakeHttpClient = new HttpClient(handler))
                {
                    // Instantiate Transmission with the mock HttpClient
                    Transmission transmission = new Transmission(new Uri("http://uri"), expectedContent, fakeHttpClient, string.Empty, string.Empty);

                    // ACT
                    HttpWebResponseWrapper result = await transmission.SendAsync();
                }
            }

            [TestMethod]
            [Ignore("https://github.com/Microsoft/ApplicationInsights-dotnet/issues/1047")]
            public async Task SendAsyncHandlesTimeout()
            {
                var handler = new HandlerForFakeHttpClient
                {
                    InnerHandler = new HttpClientHandler(),
                    OnSendAsync = async (req, cancellationToken) =>
                    {
                        return await Task.FromResult<HttpResponseMessage>(new HttpResponseMessage());
                    }
                };

                using (var fakeHttpClient = new HttpClient())
                {
                    // Instantiate Transmission with the mock HttpClient and Timeout to be just 1 msec to force Timeout.
                    Transmission transmission = new Transmission(new Uri("http://uri"), new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, string.Empty, 
                        string.Empty, TimeSpan.FromMilliseconds(1));

                    // ACT
                    HttpWebResponseWrapper result = await transmission.SendAsync();
                }
            }
        }
    }

    /// <summary>
    /// Handler to control the behaviour of HttpClient. HttpClient instance created with this
    /// Unit tests provide the behaviour of this handler.
    /// </summary>
    internal class HandlerForFakeHttpClient : DelegatingHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnSendAsync;
        protected async override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return await OnSendAsync(request, cancellationToken);
        }
    }
}