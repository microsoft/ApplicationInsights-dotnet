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
        public class CreateRequest : TransmissionTest
        {
            [TestMethod]
            public void CreatesHttpWebRequestWithSpecifiedUri()
            {
                var transmission = new TestableTransmission();
                var expectedUri = new Uri("http://custom.uri");
                HttpRequestMessage request = transmission.TestableCreateRequest(expectedUri, new MemoryStream());

                Assert.AreEqual(expectedUri, request.RequestUri);
            }

            [TestMethod]
            public void CreatesHttpWebRequestWithPostMethod()
            {
                var transmission = new TestableTransmission();
                HttpRequestMessage request = transmission.TestableCreateRequest(new Uri("http://uri"), new MemoryStream());
                Assert.AreEqual(HttpMethod.Post, request.Method);
            }

            [TestMethod]
            public void CreatesHttpWebRequestWithContentTypeSpecifiedInConstructor()
            {
                string expectedContentType = "content/mytype";
                var transmission = new TestableTransmission(contentType: expectedContentType);
                HttpRequestMessage request = transmission.TestableCreateRequest(new Uri("http://uri"), new MemoryStream());
                Assert.AreEqual(transmission.ContentType, request.Content.Headers.ContentType.MediaType);
            }

            [TestMethod]
            public void CreatesHttpWebRequestWithoutContentTypeIfNotSpecifiedInConstructor()
            {
                var transmission = new TestableTransmission(contentType: string.Empty);
                HttpRequestMessage request = transmission.TestableCreateRequest(new Uri("http://uri"), new MemoryStream());
                Assert.IsNull(request.Content.Headers.ContentType);
            }

            [TestMethod]
            public void CreatesHttpWebRequestWithContentEncodingSpecifiedInConstructor()
            {
                var transmission = new TestableTransmission(contentEncoding: "TestContentEncoding");
                HttpRequestMessage request = transmission.TestableCreateRequest(new Uri("http://uri"), new MemoryStream());
                Assert.AreEqual(transmission.ContentEncoding, request.Content.Headers.ContentEncoding.FirstOrDefault());
            }
        }

        [TestClass]
        public class SendAsync : TransmissionTest
        {
            [TestMethod]
            public void ThrowsInvalidOperationExceptionWhenTransmissionIsAlreadySending()
            {
                AsyncTest.Run(async () =>
                {
                    var transmission = new TestableTransmission();
                    FieldInfo isSendingField = typeof(Transmission).GetField("isSending", BindingFlags.NonPublic | BindingFlags.Instance);
                    // isSendingField.SetValue(transmission, 1, BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance, null, null);
                    isSendingField.SetValue(transmission,1);
                    await AssertEx.ThrowsAsync<InvalidOperationException>(() => transmission.SendAsync());
                });
            }

            [TestMethod]
            public async Task SendAsync2Async()
            {
                var client = new StubHttpClient();
                client.OnSendAsync = (request, cancellationToken) =>
                {
                    HttpResponseMessage response = new HttpResponseMessage();
                    response.StatusCode = HttpStatusCode.PartialContent;
                    return Task.FromResult<HttpResponseMessage>(response);
                };

                var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
                Transmission transmission = new Transmission(new Uri("http://uri"), items,client);                
                transmission.Timeout = TimeSpan.FromMilliseconds(1);
                HttpWebResponseWrapper result = await transmission.SendAsync();
                
                Assert.AreEqual(HttpStatusCode.PartialContent,result.StatusCode);

            }

            [TestMethod]
            public void SendAsync1()
            {
                AsyncTest.Run(async () =>
                {
                    var client = new StubHttpClient();
                    client.OnSendAsync = (request, cancellationToken) =>
                    {
                        HttpResponseMessage msg = new HttpResponseMessage();
                        return Task.FromResult<HttpResponseMessage>(msg);
                    };

                    var responseWrapper = new HttpWebResponseWrapper();
                    var transmission = new TestableTransmission();
                    transmission.OnSendAsync = () =>
                    {
                        return Task.FromResult<HttpWebResponseWrapper>(responseWrapper);
                    };

                    await transmission.SendAsync();
                });
            }
        }

        private class TestableTransmission : Transmission
        {
            public Func<Uri, Stream, HttpRequestMessage> OnCreateRequest;
            public Func<Task<HttpWebResponseWrapper>> OnSendAsync;


            public TestableTransmission(Uri endpointAddress = null, byte[] content = null, string contentType = null, string contentEncoding = null, TimeSpan timeout = default(TimeSpan))
                : base(
                    endpointAddress ?? new Uri("http://test.uri"),
                    content ?? new byte[1],
                    contentType ?? "content/type",
                    contentEncoding,
                    timeout)
            {                
                this.OnCreateRequest = base.CreateRequestMessage;
                this.OnSendAsync = base.SendAsync;
            }

            public HttpRequestMessage TestableCreateRequest(Uri address, Stream contentStream)
            {
                return this.OnCreateRequest(address, contentStream);
            }

            protected override HttpRequestMessage CreateRequestMessage(Uri address, Stream contentStream)
            {
                return this.OnCreateRequest(address, contentStream);
            }

            public override Task<HttpWebResponseWrapper> SendAsync()
            {
                return this.OnSendAsync();
            }
        }
    }
}