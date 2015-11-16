// <copyright file="Transmission.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net;
#if CORE_PCL || UWP
    using System.Net.Http;
    using System.Net.Http.Headers;
#endif
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
#if WINRT || CORE_PCL || NET45 || NET46 || UWP
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
#if CORE_PCL || UWP
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
#if CORE_PCL || UWP
            this.client = new HttpClient() { Timeout = this.Timeout };
#endif
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
            private set;
        }

        /// <summary>
        /// Gets an id of the transmission.
        /// </summary>
        public string Id
        {
            get; private set;
        }

        /// <summary>
        /// Executes the request that the current transmission represents.
        /// </summary>
        /// <returns>The task to await.</returns>
        public virtual async Task SendAsync()
        {
            if (Interlocked.CompareExchange(ref this.isSending, 1, 0) != 0)
            {
                throw new InvalidOperationException("SendAsync is already in progress.");
            }

            try
            {
#if CORE_PCL || UWP
                using (MemoryStream contentStream = new MemoryStream(this.Content))
                {
                    HttpRequestMessage request = this.CreateRequestMessage(this.EndpointAddress, contentStream);
                    await this.client.SendAsync(request).ConfigureAwait(false);
                }
#else
                WebRequest request = this.CreateRequest(this.EndpointAddress);
                Task timeoutTask = TaskEx.Delay(this.Timeout);
                Task sendTask = this.SendRequestAsync(request);                
                Task completedTask = await TaskEx.WhenAny(timeoutTask, sendTask).ConfigureAwait(false);
                if (completedTask == timeoutTask)
                {
                    request.Abort(); // And force the sendTask to throw WebException.
                }

                // Observe any exceptions the sendTask may have thrown and propagate them to the caller.
                await sendTask.ConfigureAwait(false);
#endif
            }
            finally
            {
                Interlocked.Exchange(ref this.isSending, 0);
            }
        }

#if CORE_PCL || UWP
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
#endif

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

        private async Task SendRequestAsync(WebRequest request)
        {
            using (Stream requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
                await requestStream.WriteAsync(this.Content, 0, this.Content.Length).ConfigureAwait(false);
            }

            using (WebResponse response = await request.GetResponseAsync().ConfigureAwait(false))
            {
            }
        }
    }
}
