namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
#if NETSTANDARD1_6
    using System.Net.Http;
    using System.Net.Http.Headers;
#endif
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

#if !NET40
    using TaskEx = System.Threading.Tasks.Task;
#endif

    /// <summary>
    /// Implements an asynchronous transmission of data to an HTTP POST endpoint.
    /// </summary>
    public class Transmission
    {
        internal const string ContentTypeHeader = "Content-Type";
        internal const string ContentEncodingHeader = "Content-Encoding";

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(100);
#if NETSTANDARD1_6
        private readonly HttpClient client;
#endif
        private int isSending;

        /// <summary>
        /// Initializes a new instance of the <see cref="Transmission"/> class.
        /// </summary>
        public Transmission(Uri address, byte[] content, string contentType, string contentEncoding, TimeSpan timeout = default(TimeSpan))
        {
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            this.EndpointAddress = address;
            this.Content = content;
            this.ContentType = contentType;
            this.ContentEncoding = contentEncoding;
            this.Timeout = timeout == default(TimeSpan) ? DefaultTimeout : timeout;
            this.Id = Convert.ToBase64String(BitConverter.GetBytes(WeakConcurrentRandom.Instance.Next()));
            this.TelemetryItems = null;
#if NETSTANDARD1_6
            this.client = new HttpClient() { Timeout = this.Timeout };
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transmission"/> class.
        /// </summary>
        public Transmission(Uri address, ICollection<ITelemetry> telemetryItems, TimeSpan timeout = default(TimeSpan)) 
            : this(address, JsonSerializer.Serialize(telemetryItems, true), JsonSerializer.ContentType, JsonSerializer.CompressionType, timeout)
        {
            this.TelemetryItems = telemetryItems;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transmission"/> class. This overload is for Test purposes. 
        /// </summary>
        internal Transmission(Uri address, IEnumerable<ITelemetry> telemetryItems, string contentType, string contentEncoding, TimeSpan timeout = default(TimeSpan))
            : this(address, JsonSerializer.Serialize(telemetryItems), contentType, contentEncoding, timeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transmission"/> class. This overload is for Test purposes. 
        /// </summary>
        protected internal Transmission()
        {
        }

        /// <summary>
        /// Gets the Address of the endpoint to which transmission will be sent.
        /// </summary>
        public Uri EndpointAddress
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the content of the transmission.
        /// </summary>
        public byte[] Content
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the content's type of the transmission.
        /// </summary>
        public string ContentType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the encoding method of the transmission.
        /// </summary>
        public string ContentEncoding
        {
            get; 
            private set;            
        }

        /// <summary>
        /// Gets a timeout value for the transmission.
        /// </summary>
        public TimeSpan Timeout
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets an id of the transmission.
        /// </summary>
        public string Id
        {
            get; private set;
        }

        /// <summary>
        /// Gets the number of telemetry items in the transmission.
        /// </summary>
        public ICollection<ITelemetry> TelemetryItems
        {
            get; private set;
        }

#if !NET40
        /// <summary>
        /// Executes the request that the current transmission represents.
        /// </summary>
        /// <returns>The task to await.</returns>
        public virtual async Task<HttpWebResponseWrapper> SendAsync()
        {
            if (Interlocked.CompareExchange(ref this.isSending, 1, 0) != 0)
            {
                throw new InvalidOperationException("SendAsync is already in progress.");
            }

            try
            {
#if NETSTANDARD1_6
                using (MemoryStream contentStream = new MemoryStream(this.Content))
                {
                    HttpRequestMessage request = this.CreateRequestMessage(this.EndpointAddress, contentStream);
                    await this.client.SendAsync(request).ConfigureAwait(false);
                    return null;
                }
#else
                WebRequest request = this.CreateRequest(this.EndpointAddress);
                Task<HttpWebResponseWrapper> sendTask = this.GetResponseAsync(request);
                Task timeoutTask = Task.Delay(this.Timeout).ContinueWith(task =>
                {
                    if (!sendTask.IsCompleted)
                    {
                        request.Abort(); // And force the sendTask to throw WebException.
                    }
                });

                Task completedTask = await Task.WhenAny(timeoutTask, sendTask).ConfigureAwait(false);

                // Observe any exceptions the sendTask may have thrown and propagate them to the caller.
                HttpWebResponseWrapper responseContent = await sendTask.ConfigureAwait(false);
                return responseContent;
#endif
            }
            finally
            {
                Interlocked.Exchange(ref this.isSending, 0);
            }
    }

#else // !NET40
        /// <summary>
        /// Executes the request that the current transmission represents.
        /// </summary>
        /// <returns>The task to await.</returns>
        public virtual Task<HttpWebResponseWrapper> SendAsync()
        {
            if (Interlocked.CompareExchange(ref this.isSending, 1, 0) != 0)
            {
                throw new InvalidOperationException("SendAsync is already in progress.");
            }

            try
            {
                WebRequest request = this.CreateRequest(this.EndpointAddress);
                Task<HttpWebResponseWrapper> sendTask = this.GetResponseAsync(request);
                Task timeoutTask = TaskEx.Delay(this.Timeout).ContinueWith(task =>
                {
                    if (!sendTask.IsCompleted)
                    {
                        request.Abort(); // And force the sendTask to throw WebException.
                    }
                });

                return TaskEx.WhenAny(timeoutTask, sendTask).ContinueWith(
                    task =>
                    {
                        Interlocked.Exchange(ref this.isSending, 0);
                        return sendTask;
                    },
                    TaskContinuationOptions.ExecuteSynchronously).Unwrap();
            }
            catch (Exception)
            {
                Interlocked.Exchange(ref this.isSending, 0);
                throw;
            }
        }
#endif // !NET40

        /// <summary>
        /// Splits the Transmission object into two pieces using a method 
        /// to determine the length of the first piece based off of the length of the transmission.
        /// </summary>
        /// <returns>
        /// A tuple with the first item being a Transmission object with n ITelemetry objects
        /// and the second item being a Transmission object with the remaining ITelemetry objects.
        /// </returns>
        public virtual Tuple<Transmission, Transmission> Split(Func<int, int> calculateLength)
        {
            Transmission transmissionA = this;
            Transmission transmissionB = null;

            // We can be more efficient if we have a copy of the telemetry items still
            if (this.TelemetryItems != null)
            {
                // We don't need to deserialize, we have a copy of each telemetry item
                int numItems = calculateLength(this.TelemetryItems.Count);
                if (numItems != this.TelemetryItems.Count)
                {
                    List<ITelemetry> itemsA = new List<ITelemetry>();
                    List<ITelemetry> itemsB = new List<ITelemetry>();
                    var i = 0;
                    foreach (var item in this.TelemetryItems)
                    {
                        if (i < numItems)
                        {
                            itemsA.Add(item);
                        }
                        else
                        {
                            itemsB.Add(item);
                        }

                        i++;
                    }

                    transmissionA = new Transmission(
                        this.EndpointAddress,
                        itemsA);
                    transmissionB = new Transmission(
                        this.EndpointAddress,
                        itemsB);
                }
            }
            else if (this.ContentType == JsonSerializer.ContentType)
            {
                // We have to decode the payload in order to split
                bool compress = this.ContentEncoding == JsonSerializer.CompressionType;
                string[] payloadItems = JsonSerializer
                    .Deserialize(this.Content, compress)
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                int numItems = calculateLength(payloadItems.Length);

                if (numItems != payloadItems.Length)
                {
                    string itemsA = string.Empty;
                    string itemsB = string.Empty;

                    for (int i = 0; i < payloadItems.Length; i++)
                    {
                        if (i < numItems)
                        {
                            if (!string.IsNullOrEmpty(itemsA))
                            {
                                itemsA += Environment.NewLine;
                            }

                            itemsA += payloadItems[i];
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(itemsB))
                            {
                                itemsB += Environment.NewLine;
                            }

                            itemsB += payloadItems[i];
                        }
                    }

                    transmissionA = new Transmission(
                        this.EndpointAddress,
                        JsonSerializer.ConvertToByteArray(itemsA, compress),
                        this.ContentType,
                        this.ContentEncoding);
                    transmissionB = new Transmission(
                        this.EndpointAddress,
                        JsonSerializer.ConvertToByteArray(itemsB, compress),
                        this.ContentType,
                        this.ContentEncoding);
                }
            }
            else
            {
                // We can't deserialize it!
                // We can say it's of length 1 at the very least
                int numItems = calculateLength(1);

                if (numItems == 0)
                {
                    transmissionA = null;
                    transmissionB = this;
                }
            }

            return Tuple.Create(transmissionA, transmissionB);
        }

#if NETSTANDARD1_6
        /// <summary>
        /// Creates an http request for sending a transmission.
        /// </summary>
        /// <param name="address">The address of the web request.</param>
        /// <param name="contentStream">The stream to write to.</param>
        /// <returns>The request. An object of type HttpRequestMessage.</returns>
        protected virtual HttpRequestMessage CreateRequestMessage(Uri address, Stream contentStream)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, address);
            request.Content = new StreamContent(contentStream);
            if (!string.IsNullOrEmpty(this.ContentType))
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(this.ContentType);
            }

            if (!string.IsNullOrEmpty(this.ContentEncoding))
            {
                request.Content.Headers.Add(ContentEncodingHeader, this.ContentEncoding);
            }

            return request;
        }
#else
        /// <summary>
        /// Creates a post web request.  
        /// </summary>
        /// <param name="address">The Address in the web request.</param>
        /// <returns>A web request pointing to the <c>Address</c>.</returns>
        protected virtual WebRequest CreateRequest(Uri address)
        {
            var request = WebRequest.Create(address);

            request.Method = "POST";

            if (!string.IsNullOrEmpty(this.ContentType))
            {
                request.ContentType = this.ContentType;
            }

            if (!string.IsNullOrEmpty(this.ContentEncoding))
            {
                request.Headers[ContentEncodingHeader] = this.ContentEncoding;
            }
#if NET40
            request.ContentLength = this.Content.Length;
#endif
            return request;
        }

#if NET40
        private Task<HttpWebResponseWrapper> GetResponseAsync(WebRequest request)
        {
            return Task.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, null)
                .ContinueWith(
                    getRequestStreamTask =>
                    {
                        Stream requestStream = getRequestStreamTask.Result;
                        return Task.Factory.FromAsync(
                            (callback, o) => requestStream.BeginWrite(this.Content, 0, this.Content.Length, callback, o),
                            requestStream.EndWrite, 
                            null).ContinueWith(
                                writeTask =>
                                {
                                    requestStream.Dispose();
                                    writeTask.RethrowIfFaulted();
                                });
                    }).Unwrap().ContinueWith(requestTask =>
                    {
                        requestTask.RethrowIfFaulted();
                        return Task.Factory.FromAsync(
                            request.BeginGetResponse, request.EndGetResponse, null);
                    }).Unwrap().ContinueWith(responseTask =>
                    {
                        using (WebResponse response = responseTask.Result)
                        {
                            return this.CheckResponse(response);
                        }
                    });
        }
#else // NET40

        private async Task<HttpWebResponseWrapper> GetResponseAsync(WebRequest request)
        {
            using (Stream requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
                await requestStream.WriteAsync(this.Content, 0, this.Content.Length).ConfigureAwait(false);
            }

            using (WebResponse response = await request.GetResponseAsync().ConfigureAwait(false))
            {
                return this.CheckResponse(response);
            }
        }
#endif

        private HttpWebResponseWrapper CheckResponse(WebResponse response)
        {
            HttpWebResponseWrapper wrapper = null;

            var httpResponse = response as HttpWebResponse;
            if (httpResponse != null)
            {
                // Return content only for 206 for performance reasons
                // Currently we do not need it in other cases
                if (httpResponse.StatusCode == HttpStatusCode.PartialContent)
                {
                    wrapper = new HttpWebResponseWrapper
                    {
                        StatusCode = (int)httpResponse.StatusCode,
                        StatusDescription = httpResponse.StatusDescription
                    };

                    if (httpResponse.Headers != null)
                    {
                        wrapper.RetryAfterHeader = httpResponse.Headers["Retry-After"];
                    }

                    using (StreamReader content = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        wrapper.Content = content.ReadToEnd();
                    }
                }
            }

            return wrapper;
        }
#endif
    }
}
