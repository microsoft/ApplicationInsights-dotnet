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
    using AI;

    public class HttpListenerObservable : IObservable<Envelope>, IDisposable
    {
        private readonly HttpListener listener;
        private IObservable<Envelope> stream;        

        public HttpListenerObservable(string url)
        {
            this.listener = new HttpListener();
            this.listener.Prefixes.Add(url);
        }        

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
            if (this.stream != null)
            {
                this.Stop();
            }

            if (!this.listener.IsListening)
            {
                Trace.TraceInformation($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")} Starting listener");
                this.listener.Start();
            }

            this.stream = this.CreateStream();
        }

        public void Stop()
        {
            Trace.TraceInformation($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")} Stopping listener");
            listener.Stop();
        }

        public IDisposable Subscribe(IObserver<Envelope> observer)
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

        private IObservable<Envelope> CreateStream()
        {
            return Observable
                .Create<Envelope>
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

        private IEnumerable<Envelope> CreateNewItemsFromContext(HttpListenerContext context)
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
    }
}
