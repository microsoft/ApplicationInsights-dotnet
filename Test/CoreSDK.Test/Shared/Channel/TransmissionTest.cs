namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    using AssertEx = Xunit.AssertEx;

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
            }

            [TestMethod]
            public void ThrowsArgumentNullExceptionWhenEndpointAddressIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new Transmission(null, new byte[1], "content/type", "content/encoding"));
            }

            [TestMethod]
            public void SetsContentPropertyToSpecifiedValue()
            {
                var expectedContent = new byte[42];
                var transmission = new Transmission(new Uri("http://address"), expectedContent, "content/type", "content/encoding");
                Assert.Same(expectedContent, transmission.Content);
            }

            [TestMethod]
            public void ThrowsArgumentNullExceptionWhenContentIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new Transmission(new Uri("http://address"), null, "content/type", "content/encoding"));
            }

            [TestMethod]
            public void SetsContentTypePropertyToSpecifiedValue()
            {
                string expectedContentType = "TestContentType123";
                var transmission = new Transmission(new Uri("http://address"), new byte[1], expectedContentType, "content/encoding");
                Assert.Same(expectedContentType, transmission.ContentType);
            }

            [TestMethod]
            public void ThrowsArgumentNullExceptionWhenContentTypeIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new Transmission(new Uri("http://address"), new byte[1], null, "content/encoding"));
            }

            [TestMethod]
            public void SetContentEncodingPropertyToSpecifiedValue()
            {
                string expectedContentEncoding = "gzip";
                var transmission = new Transmission(new Uri("http://address"), new byte[1], "any/content", expectedContentEncoding);
                Assert.Same(expectedContentEncoding, transmission.ContentEncoding);
            }

            [TestMethod]
            public void SetsTimeoutTo100SecondsByDefaultToMatchHttpWebRequest()
            {
                var transmission = new Transmission(new Uri("http://address"), new byte[1], "content/type", "content/encoding");
                Assert.Equal(TimeSpan.FromSeconds(100), transmission.Timeout);
            }

            [TestMethod]
            public void SetsTimeoutToSpecifiedValue()
            {
                var expectedValue = TimeSpan.FromSeconds(42);
                var transmission = new Transmission(new Uri("http://address"), new byte[1], "content/type", "content/encoding", expectedValue);
                Assert.Equal(expectedValue, transmission.Timeout);
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
                WebRequest request = transmission.TestableCreateRequest(expectedUri);

                Assert.Equal(expectedUri, request.RequestUri);
            }

            [TestMethod]
            public void CreatesHttpWebRequestWithPostMethod()
            {
                var transmission = new TestableTransmission();
                WebRequest request = transmission.TestableCreateRequest(new Uri("http://uri"));
                Assert.Equal("POST", request.Method);
            }

            [TestMethod]
            public void CreatesHttpWebRequestWithContentTypeSpecifiedInConstructor()
            {
                var transmission = new TestableTransmission(contentType: "TestContentType");
                WebRequest request = transmission.TestableCreateRequest(new Uri("http://uri"));
                Assert.Equal(transmission.ContentType, request.ContentType);
            }

            [TestMethod]
            public void CreatesHttpWebRequestWithoutContentTypeIfNotSpecifiedInConstructor()
            {
                var transmission = new TestableTransmission(contentType: string.Empty);
                WebRequest request = transmission.TestableCreateRequest(new Uri("http://uri"));
                Assert.Null(request.ContentType);
            }

            [TestMethod]
            public void CreatesHttpWebRequestWithContentEncodingSpecifiedInConstructor()
            {
                var transmission = new TestableTransmission(contentEncoding: "TestContentEncoding");
                WebRequest request = transmission.TestableCreateRequest(new Uri("http://uri"));
                Assert.Equal(transmission.ContentEncoding, request.Headers[HttpRequestHeader.ContentEncoding]);
            }

#if NET40
            [TestMethod]
            public void CreatesHttpWebRequestWithContentLengthCalculatedFromDataSpecifiedInConstructor()
            {
                byte[] content = Encoding.UTF8.GetBytes("custom data");
                var transmission = new TestableTransmission(new Uri("http://test.uri"), content);
                WebRequest request = transmission.TestableCreateRequest(new Uri("http://uri"));
                Assert.Equal(content.Length, request.ContentLength);
            }
#endif
        }

        [TestClass]
        public class SendAsync : TransmissionTest
        {
            [TestMethod]
            public void ThrowsInvalidOperationExceptionWhenTransmissionIsAlreadySending()
            {
                AsyncTest.Run(async () =>
                {
                    var request = new StubWebRequest();
                    request.OnBeginGetRequestStream = (callback, state) => TaskEx.Delay(TimeSpan.FromMilliseconds(10)).AsAsyncResult(callback, request);
        
                    var transmission = new TestableTransmission();
                    transmission.OnCreateRequest = uri => request;
        
                    Task dontWait = transmission.SendAsync();
                    await AssertEx.ThrowsAsync<InvalidOperationException>(() => transmission.SendAsync());
                });
            }

            [TestMethod]
            public void BeginsAsynchronouslyGettingRequestStream()
            {
                AsyncTest.Run(async () =>
                {
                    int beginGetRequestStreamCount = 0;
                    var request = new StubWebRequest();
                    request.OnBeginGetRequestStream = (callback, state) =>
                    {
                        beginGetRequestStreamCount++;
                        return TaskEx.FromResult<object>(null).AsAsyncResult(callback, request);
                    };
        
                    var transmission = new TestableTransmission { OnCreateRequest = uri => request };
        
                    await transmission.SendAsync();
        
                    Assert.Equal(1, beginGetRequestStreamCount);
                });
            }

            [TestMethod]
            public void WritesTransmissionContentToRequestStream()
            {
                AsyncTest.Run(async () =>
                {
                    var requestStream = new MemoryStream();
        
                    var request = new StubWebRequest();
                    request.OnEndGetRequestStream = asyncResult => requestStream;
        
                    byte[] transmissionContent = new byte[] { 1, 2, 3, 4, 5 };
                    var transmission = new TestableTransmission(new Uri("http://test.uri"), transmissionContent);
                    transmission.OnCreateRequest = uri => request;
        
                    await transmission.SendAsync();
        
                    Assert.Equal(transmissionContent, requestStream.ToArray());
                });
            }

            [TestMethod]
            public void AsynchronouslyFinishesGettingResponse()
            {
                AsyncTest.Run(async () =>
                {
                    int endGetResponseCount = 0;
                    var request = new StubWebRequest();
                    request.OnEndGetResponse = asyncResult =>
                    {
                        endGetResponseCount++;
                        return new StubWebResponse();
                    };
        
                    var transmission = new TestableTransmission { OnCreateRequest = uri => request };
        
                    await transmission.SendAsync();
        
                    Assert.Equal(1, endGetResponseCount);
                });
            }

            [TestMethod]
            public void DisposesHttpWebResponseToReleaseResources()
            {
                AsyncTest.Run(async () =>
                {
                    bool responseDisposed = false;
                    var response = new StubWebResponse { OnDispose = () => responseDisposed = true };
                    var request = new StubWebRequest { OnEndGetResponse = asyncResult => response };
                    var transmission = new TestableTransmission { OnCreateRequest = uri => request };
        
                    await transmission.SendAsync();
        
                    Assert.True(responseDisposed);
                });
            }

            [TestMethod]
            public void AbortsWebRequestWhenBeginGetRequestStreamTimesOut()
            {
                var requestAborted = new ManualResetEventSlim();
                var finishBeginGetRequestStream = new ManualResetEventSlim();
                var request = new StubWebRequest();
                request.OnAbort = () => requestAborted.Set();
                request.OnBeginGetRequestStream = (callback, state) => TaskEx.Run(() => finishBeginGetRequestStream.Wait()).AsAsyncResult(callback, request);
                var transmission = new TestableTransmission(timeout: TimeSpan.FromTicks(1));
                transmission.OnCreateRequest = uri => request;

                Task sendAsync = transmission.SendAsync();

                Assert.True(requestAborted.Wait(1000));
                finishBeginGetRequestStream.Set();
            }
            
            [TestMethod]
            public void DoesNotAbortRequestThatWasSentSuccessfully()
            {
                AsyncTest.Run(async () =>
                {
                    bool requestAborted = false;
                    var request = new StubWebRequest { OnAbort = () => requestAborted = true };
        
                    var transmission = new TestableTransmission(timeout: TimeSpan.FromMilliseconds(50));
                    transmission.OnCreateRequest = uri => request;
        
                    await transmission.SendAsync();
        
                    await TaskEx.Delay(50); // Let timout detector finish
        
                    Assert.False(requestAborted);
                });
            }
        }

        private class TestableTransmission : Transmission
        {
            public Func<Uri, WebRequest> OnCreateRequest;

            public TestableTransmission(Uri endpointAddress = null, byte[] content = null, string contentType = null, string contentEncoding = null, TimeSpan timeout = default(TimeSpan))
                : base(
                    endpointAddress ?? new Uri("http://test.uri"),
                    content ?? new byte[1],
                    contentType ?? "content/type",
                    contentEncoding ?? "content/encoding",
                    timeout)
            {
                this.OnCreateRequest = base.CreateRequest;
            }

            public WebRequest TestableCreateRequest(Uri address)
            {
                return base.CreateRequest(address);
            }

            protected override WebRequest CreateRequest(Uri address)
            {
                return this.OnCreateRequest(address);
            }
        }
    }
}