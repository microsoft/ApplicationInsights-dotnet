namespace Functional.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using AI;
    using Functional.Serialization;
    using FunctionalTests.Helpers;

    public class TelemetryHttpListenerObservable : HttpListenerObservableBase<Envelope>
    {
        private int validatedPackages;
        public bool FailureDetected { get; set; }

        public TelemetryHttpListenerObservable(string url) : base(url)
        {
        }

        protected override void OnStart()
        {
            validatedPackages = 0;
            FailureDetected = false;
        }

        protected override IEnumerable<Envelope> CreateNewItemsFromContext(HttpListenerContext context)
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
                if (this.validatedPackages == 0 && (content.Contains("Request") || content.Contains("Exception")))
                {
                    try
                    {
                        this.ValidateItems(content);
                        ++this.validatedPackages;
                    }
                    catch (TaskCanceledException)
                    { }
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
                Trace.WriteLine("Check agains 'Validate' endpoint is done.");
            }
        }
    }
}
