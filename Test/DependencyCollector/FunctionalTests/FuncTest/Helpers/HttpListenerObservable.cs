namespace FuncTest.Helpers
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

        /// <summary>
        /// Method used between calling ReceiveAllItemsDuringTimeOfType multiple times so the state can be reset.
        /// </summary>
        public void Reset()
        {
            this.Stop();
            this.Start();
        }

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
                Trace.TraceInformation("Starting listener");
                this.listener.Start();
            }

            this.stream = this.CreateStream();
        }

        public void Stop()
        {
            Trace.TraceInformation("Stopping listener");
            listener.Stop();
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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (listener != null && listener.IsListening)
                {
                    listener.Stop();
                    listener.Close();
                    this.stream = null;
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        private IObservable<TelemetryItem> CreateStream()
        {
            return Observable
                .Create<TelemetryItem>
                (obs =>
                    Task.Factory.FromAsync(
                        (a, c) =>
                            {
                            Trace.TraceInformation("BeginGetContext reached");
                            return this.listener.BeginGetContext(a, c);                                
                            }
                        ,
                        ar =>
                        {
                            Trace.TraceInformation("EndGetContext reached");
                            return this.listener.EndGetContext(ar);
                        },
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
                        StringComparison.OrdinalIgnoreCase))
                {
                    content = Decompress(content);
                }

                Trace.TraceInformation("=>\n");
                Trace.TraceInformation("Item received: " + content);
                Trace.TraceInformation("<=\n");
                
                // Validating each package takes too much time, check only first one that have dependency data
                if (this.validatedPackages == 0 && content.Contains("RemoteDependency"))
                {
                    try
                    {
                        this.ValidateItems(content);
                        ++this.validatedPackages;
                    }
                    catch (TaskCanceledException)
                    {}
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
            using (var compressedzipStream = new GZipStream(new MemoryStream(zippedData), CompressionMode.Decompress))
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
                return Encoding.UTF8.GetString(outputStream.ToArray());
            }
        }

        private void ValidateItems(string items)
        {
            HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

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
