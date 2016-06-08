namespace Functional.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Net.Http;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Text;
    using System.Threading.Tasks;
    using Functional.Serialization;
    using FunctionalTests.Helpers;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;

    public class HttpListenerObservable : IObservable<TelemetryItem>, IDisposable
    {
        private readonly HttpListener listener;
        private IObservable<TelemetryItem> stream;
        private int validatedPackages;

        public HttpListenerObservable(string url)
        {
            this.listener = new HttpListener();
            this.listener.Prefixes.Add(url);
        }

        public bool FailureDetected { get; set; }

        public void Start()
        {
            this.FailureDetected = false;
            this.validatedPackages = 0;

            if (this.stream != null)
            {
                this.Stop();   
            }

            if (!this.listener.IsListening)
            {
                this.listener.Start();
            }

            this.stream = this.CreateStream();
        }

        public void Stop()
        {
            this.Dispose();
        }

        public IDisposable Subscribe(IObserver<TelemetryItem> observer)
        {
            if (this.stream == null)
            {
                throw new InvalidOperationException("Call HttpListenerObservable.Start before subscribing to the stream");
            }

            return this.stream
                .Subscribe(observer);
        }

        public void Dispose()
        {
            if (listener != null && listener.IsListening)
            {
                listener.Stop();
                listener.Close();
                this.stream = null;
            }
        }

        private IObservable<TelemetryItem> CreateStream()
        {
            return Observable
                .Create<TelemetryItem>
                (obs =>
                    Task.Factory.FromAsync(
                        (a, c) => this.listener.BeginGetContext(a, c),
                        ar => this.listener.EndGetContext(ar),
                        null)
                        .ToObservable()
                        .SelectMany(this.CreateNewItemsFromContext)
                        .Subscribe(obs)
                )
              .Repeat()
              .Publish()
              .RefCount();
        }

        private IEnumerable<TelemetryItem> CreateNewItemsFromContext(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var content = request.GetContent();

                if (!string.IsNullOrWhiteSpace(request.Headers["Content-Encoding"]) &&
                    string.Equals("gzip", request.Headers["Content-Encoding"],
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    content = Decompress(content);
                }

                Trace.WriteLine("=>");
                Trace.WriteLine("Item received: " + content);
                Trace.WriteLine("<=");

                // Validating each package takes too much time, check only first one that have dependency data
                if (this.validatedPackages == 0 && (content.Contains("PerformanceCounter") || content.Contains("Metric")))
                {
                    this.ValidateItems(content);
                    ++this.validatedPackages;
                }

                return TelemetryItemFactory.GetTelemetryItems(content);
            }
            finally
            {
                context.Response.Close();
            }
        }

        /// <summary>
        /// Decompresses content in gzip and returns decompressed string
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
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

        private void ValidateItems(string items)
        {
            HttpClient client = new HttpClient();
            var result = client.PostAsync(
                "https://dc.services.visualstudio.com/v2/validate",
                new ByteArrayContent(Encoding.UTF8.GetBytes(items))).GetAwaiter().GetResult();

            if (result.StatusCode != HttpStatusCode.OK)
            {
                var response = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Trace.WriteLine("ERROR! Backend Response: " + response);
                this.FailureDetected = true;
            }
            else
            {
                Trace.WriteLine("Check against 'Validate' endpoint is done.");
            }
        }
    }
}
