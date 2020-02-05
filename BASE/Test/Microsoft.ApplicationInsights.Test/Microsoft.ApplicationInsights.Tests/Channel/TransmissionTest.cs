namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.CodeDom;

    public class TransmissionTest
    {
        [TestClass]
        public class Constructor
        {
            private readonly Uri testUri = new Uri("https://127.0.0.1/");

            [TestMethod]
            public void SetsEndpointAddressPropertyToSpecifiedValue()
            {
                var transmission = new Transmission(testUri, new byte[1], "content/type", "content/encoding");
                Assert.AreEqual(testUri, transmission.EndpointAddress);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ThrowsArgumentNullExceptionWhenEndpointAddressIsNull() => new Transmission(null, new byte[1], "content/type", "content/encoding");

            [TestMethod]
            public void SetsContentPropertyToSpecifiedValue()
            {
                var expectedContent = new byte[42];
                var transmission = new Transmission(testUri, expectedContent, "content/type", "content/encoding");
                Assert.AreSame(expectedContent, transmission.Content);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ThrowsArgumentNullExceptionWhenContentIsNull() => new Transmission(testUri, (byte[])null, "content/type", "content/encoding");

            [TestMethod]
            public void SetsContentTypePropertyToSpecifiedValue()
            {
                string expectedContentType = "TestContentType123";
                var transmission = new Transmission(testUri, new byte[1], expectedContentType, "content/encoding");
                Assert.AreSame(expectedContentType, transmission.ContentType);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ThrowsArgumentNullExceptionWhenContentTypeIsNull() => new Transmission(testUri, new byte[1], null, "content/encoding");

            [TestMethod]
            public void SetContentEncodingPropertyToSpecifiedValue()
            {
                string expectedContentEncoding = "gzip";
                var transmission = new Transmission(testUri, new byte[1], "any/content", expectedContentEncoding);
                Assert.AreSame(expectedContentEncoding, transmission.ContentEncoding);
            }

            [TestMethod]
            public void SetsTimeoutTo100SecondsByDefaultToMatchHttpWebRequest()
            {
                var transmission = new Transmission(testUri, new byte[1], "content/type", "content/encoding");
                Assert.AreEqual(TimeSpan.FromSeconds(100), transmission.Timeout);
            }

            [TestMethod]
            public void SetsTimeoutToSpecifiedValue()
            {
                var expectedValue = TimeSpan.FromSeconds(42);
                var transmission = new Transmission(testUri, new byte[1], "content/type", "content/encoding", expectedValue);
                Assert.AreEqual(expectedValue, transmission.Timeout);
            }
        }

        [TestClass]
        public class SendAsync
        {
            private readonly Uri testUri = new Uri("https://127.0.0.1/");

            [TestMethod]
            public async Task SendAsyncUsesPostMethodToSpecifiedHttpEndpoint()
            {
                var handler = new HandlerForFakeHttpClient
                {
                    InnerHandler = new HttpClientHandler(),
                    OnSendAsync = (req, cancellationToken) =>
                    {
                        // VALIDATE
                        Assert.AreEqual(testUri, req.RequestUri);
                        Assert.AreEqual(HttpMethod.Post, req.Method);
                        return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage());
                    }
                };

                using (var fakeHttpClient = new HttpClient(handler))
                {
                    var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };

                    // Instantiate Transmission with the mock HttpClient
                    Transmission transmission = new Transmission(testUri, new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, string.Empty, string.Empty);
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
                    var transmission = new Transmission(testUri, new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, expectedContentType, expectedContentEncoding);

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
                    var transmission = new Transmission(testUri, new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, string.Empty, "ContentEncoding");

                    HttpWebResponseWrapper result = await transmission.SendAsync();
                }
            }

            [TestMethod]
            public async Task ThrowsInvalidOperationExceptionWhenTransmissionIsAlreadySending()
            {
                Transmission transmission = new Transmission(testUri, new byte[] { 1, 2, 3, 4, 5 }, new HttpClient(), string.Empty, string.Empty); FieldInfo isSendingField = typeof(Transmission).GetField("isSending", BindingFlags.NonPublic | BindingFlags.Instance);
                isSendingField.SetValue(transmission, 1);
                await AssertEx.ThrowsAsync<InvalidOperationException>(() => transmission.SendAsync());
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
                    Transmission transmission = new Transmission(testUri, new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, string.Empty, string.Empty);

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
                    Transmission transmission = new Transmission(testUri, expectedContent, fakeHttpClient, string.Empty, string.Empty);

                    // ACT
                    HttpWebResponseWrapper result = await transmission.SendAsync();
                }
            }

            [TestMethod]
            public async Task SendAsyncHandlesTimeout()
            {
                int clientTimeoutInMillisecs = 1;

                var handler = new HandlerForFakeHttpClient
                {
                    InnerHandler = new HttpClientHandler(),
                    OnSendAsync = (req, cancellationToken) =>
                    {
                        // Intentionally delay for atleast client timeout.
                        // By this time, client's cancellation token would definitely expire.
                        Task.Delay(100 * clientTimeoutInMillisecs).Wait();
                        // This simulates actual timeout
                        cancellationToken.ThrowIfCancellationRequested();
                        return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage());
                    }
                };

                using (var fakeHttpClient = new HttpClient(handler))
                {
                    // Instantiate Transmission with the mock HttpClient and Timeout to be just 1 msec to force Timeout.
                    Transmission transmission = new Transmission(testUri, new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, string.Empty,
                        string.Empty, TimeSpan.FromMilliseconds(clientTimeoutInMillisecs));

                    // ACT
                    HttpWebResponseWrapper result = await transmission.SendAsync();

                    // VALIDATE
                    Assert.IsNotNull(result);
                    Assert.AreEqual((int) HttpStatusCode.RequestTimeout, result.StatusCode);
                    Assert.IsNull(result.Content, "Content is not to be read except in partial response (206) status.");
                }
            }

            [TestMethod]
            public async Task SendAsyncPropogatesHttpRequestException()
            {
                //ARRANGE
                var handler = new HandlerForFakeHttpClient
                {
                    InnerHandler = new HttpClientHandler(),
                    OnSendAsync = (req, cancellationToken) =>
                    {
                        throw new HttpRequestException();
                    }
                };

                using (var fakeHttpClient = new HttpClient())
                {
                    // Instantiate Transmission with the mock HttpClient and Timeout to be just 1 msec to force Timeout.
                    Transmission transmission = new Transmission(testUri, new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, string.Empty,
                        string.Empty);

                    // ACT & VALIDATE
                    await AssertEx.ThrowsAsync<HttpRequestException>(() => transmission.SendAsync());
                }
            }

            [TestMethod]
            public async Task SendAsyncReturnsCorrectHttpResponseWrapperWhenNoExceptionOccurs()
            {
                // HttpClient.SendAsync throws HttpRequestException only on the following scenarios:
                // "The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout."
                // For every other case, a response is returned, and we expect Transmission.SendAsync to properly return HttpWebResponseWrapper.                

                // ARRANGE
                var handler = new HandlerForFakeHttpClient
                {
                    InnerHandler = new HttpClientHandler(),
                    OnSendAsync = (req, cancellationToken) =>
                    {
                        HttpResponseMessage response = new HttpResponseMessage();
                        response.StatusCode = HttpStatusCode.ServiceUnavailable;
                        return Task.FromResult<HttpResponseMessage>(response);
                    }
                };

                using (var fakeHttpClient = new HttpClient(handler))
                {
                    // Instantiate Transmission with the mock HttpClient
                    Transmission transmission = new Transmission(testUri, new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, string.Empty, string.Empty);

                    // ACT
                    HttpWebResponseWrapper result = await transmission.SendAsync();

                    // VALIDATE
                    Assert.IsNotNull(result);
                    Assert.AreEqual(503, result.StatusCode);
                    Assert.IsNull(result.Content, "Content is not to be read except in partial response (206) status.");
                }

            }

            [TestMethod]
            public async Task SendAsyncReturnsCorrectHttpResponseWrapperWithRetryHeaderWhenNoExceptionOccur()
            {
                // HttpClient.SendAsync throws HttpRequestException only on the following scenarios:
                // "The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout."
                // For every other case, a response is returned, and we expect Transmission.SendAsync to properly return HttpWebResponseWrapper.                

                // ARRANGE
                var handler = new HandlerForFakeHttpClient
                {
                    InnerHandler = new HttpClientHandler(),
                    OnSendAsync = (req, cancellationToken) =>
                    {
                        HttpResponseMessage response = new HttpResponseMessage();
                        response.StatusCode = HttpStatusCode.ServiceUnavailable;
                        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5));
                        return Task.FromResult<HttpResponseMessage>(response);
                    }
                };

                using (var fakeHttpClient = new HttpClient(handler))
                {
                    // Instantiate Transmission with the mock HttpClient
                    Transmission transmission = new Transmission(testUri, new byte[] { 1, 2, 3, 4, 5 }, fakeHttpClient, string.Empty, string.Empty);

                    // ACT
                    HttpWebResponseWrapper result = await transmission.SendAsync();

                    // VALIDATE
                    Assert.IsNotNull(result);
                    Assert.AreEqual(503, result.StatusCode);
                    Assert.AreEqual("5", result.RetryAfterHeader);
                    Assert.IsNull(result.Content, "Content is not to be read except in partial response (206) status.");
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