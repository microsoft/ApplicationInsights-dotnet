// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FakeDataPlatform.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
// Fake Data Platform
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace FuncTest.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;    
    using System.Net;
    using System.Text;
    using System.Threading;    
    using AI;

    /// <summary>
    /// FakeDataPlatform class to listen to the fake data platform endpoint.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public sealed class FakeDataPlatform
    {
        /// <summary>
        /// The http listener        
        /// </summary>
        private HttpListener listener;

        /// <summary>
        /// flag to indicate if listening should be stopped
        /// </summary>
        private bool shutDown;

        /// <summary>
        /// Endpoint to listen
        /// </summary>
        private string listenerUrl;

        /// <summary>
        /// List storing the received data items.
        /// </summary>
        private List<Envelope> receivedDataItems = new List<Envelope>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeDataPlatform"/> class
        /// </summary>
        /// <param name="url">The endpoint at which listener listens</param>
        public FakeDataPlatform(string url)
        {
            this.listener = new HttpListener();
            this.listener.Prefixes.Add(url);
            this.listenerUrl = url;            
        }

        /// <summary>
        /// Starts listening
        /// </summary>        
        public void Start()
        {   
           try
           {
               this.listener.Start();

               this.shutDown = false;
               // Starting a thread to process incoming requests
               ThreadPool.QueueUserWorkItem(new WaitCallback(this.Listen));
           }           
           catch (Exception ex)
           {
               Trace.TraceError("Exception while starting listener:" + ex);               
           }              
        }

        /// <summary>
        /// Returns the list of received data items
        /// <returns>The list of received data items</returns>
        /// </summary>        
        public List<Envelope> GetAllReceivedDataItems()
        {
            return this.receivedDataItems;
        }

        /// <summary>
        /// Stops listening
        /// </summary>        
        public void Stop()
        {
            this.shutDown = true;
            this.receivedDataItems = null;
            if (this.listener != null && this.listener.IsListening)
            {
                this.listener.Stop();
                this.listener.Close();
            }
        }

        /// <summary>
        /// Disposes the listener
        /// </summary>     
        public void Dispose()
        {
            if (this.listener != null && this.listener.IsListening)
            {
                this.listener.Stop();
                this.listener.Close();                
            }
        }

        /// <summary>
        /// Decompresses content in gzip and returns decompressed string        
        /// </summary>
        /// <param name="content">content to decompress</param>
        /// <returns>uncompressed contents</returns>        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private static string Decompress(string content)
        {
            var zippedData = Encoding.Default.GetBytes(content);
            using (var ms = new MemoryStream(zippedData))
            {
                using (var compressedzipStream = new GZipStream(ms, CompressionMode.Decompress))
                {
                    var outputStream = new MemoryStream();
                    var block = new byte[1024];
                    while (true)
                    {
                        int bytesRead = compressedzipStream.Read(block, 0, block.Length);
                        if (bytesRead <= 0)
                        {
                            break;
                        }

                        outputStream.Write(block, 0, bytesRead);
                    }

                    compressedzipStream.Close();
                    return Encoding.UTF8.GetString(outputStream.ToArray());
                }
            }
        }

        /// <summary>
        /// Call back when incoming request is received
        /// </summary>
        /// <param name="state">callback state</param>
        private void Listen(object state)
        {
            while (!this.shutDown)
            {
                try
                {
                    // Accept the next connection
                    HttpListenerContext context = this.listener.GetContext();

                    // Route the request for processing
                    this.RouteRequest(context.Request, context.Response);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception while listening:" + ex);
                    break;
                }                
            }            
        }

        /// <summary>
        /// Call back when incoming request is received
        /// </summary>
        /// <param name="request">incoming request</param>
        /// <param name="response">response to be sent back to invoker</param>
        private void RouteRequest(HttpListenerRequest request, HttpListenerResponse response)
        {            
            var content = request.GetContent();
            if (!string.IsNullOrWhiteSpace(request.Headers["Content-Encoding"]) &&
                string.Equals("gzip", request.Headers["Content-Encoding"], StringComparison.OrdinalIgnoreCase))
            {
                content = Decompress(content);
            }

            this.receivedDataItems.AddRange(TelemetryItemFactory.GetTelemetryItems(content));

            //var stringReader = new StringReader(content);
            //var reader = new JsonTextReader(stringReader);

            //var sr = new JsonSerializer();
            //var objectItem = (JArray)sr.Deserialize(reader, typeof(JArray));
            
            var buff = Encoding.UTF8.GetBytes("<html><body>Hello from HttpListener.</body></html>");

            // Set up the response object
            response.ContentType = "text/html";
            response.ContentLength64 = buff.Length;
            response.StatusCode = 200;  // HTTP "OK"
            
            // Write the response.
            var strm = response.OutputStream;
            strm.Write(buff, 0, buff.Length);
            
            // close the stream.
            strm.Close();


            Trace.TraceInformation(this.listenerUrl + " Request content: " + content);            
        }       
    }
}
