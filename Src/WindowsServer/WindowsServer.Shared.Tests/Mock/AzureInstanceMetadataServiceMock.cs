namespace Microsoft.ApplicationInsights.WindowsServer.Mock
{
    using System;
    using System.Net;
    using System.Threading;    
    using System.Threading.Tasks;
#if NETCORE
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
#endif

    internal class AzureInstanceMetadataServiceMock : IDisposable
    {
#if NETCORE
        private readonly IWebHost host;
#else
        private readonly HttpListener listener;
#endif
        private readonly CancellationTokenSource cts;

#if NETCORE

        internal AzureInstanceMetadataServiceMock(string baseUrl, string testName, Action<HttpResponse> onRequest = null)
        {
            ResponseHandlerMock.OnRequestDictionary.Add(testName, onRequest);

            this.cts = new CancellationTokenSource();
            this.host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<ResponseHandlerMock>()
                .UseUrls(baseUrl + "?testName=" + testName)
                .Build();

            Task.Run(() => this.host.Run(this.cts.Token));
        }

#else

        internal AzureInstanceMetadataServiceMock(string url, string testName, Action<HttpListenerResponse> onRequest = null)
        {
            var alistener = new System.Net.Http.HttpClient();
            this.listener = new HttpListener();
            this.listener.Prefixes.Add(url);
            this.listener.Start();
            this.cts = new CancellationTokenSource();

            Task.Run(
                () =>
                {
                    if (!this.cts.IsCancellationRequested)
                    {
                        HttpListenerContext context = this.listener.GetContext();
                        if (onRequest != null)
                        {
                            onRequest(context.Response);
                        }
                        else
                        {
                            context.Response.StatusCode = 200;
                        }

                        context.Response.OutputStream.Close();
                        context.Response.Close();
                    }
                },
                this.cts.Token);
        }
#endif

        public void Dispose()
        {
            this.cts.Cancel(false);
#if NETCORE
            this.host.Dispose();
#else
            this.listener.Abort();
            ((IDisposable)this.listener).Dispose();
#endif
            this.cts.Dispose();
        }

#if NETCORE

        public class ResponseHandlerMock
        {
            public static Dictionary<string, Action<HttpResponse>> OnRequestDictionary { get; set; } = new Dictionary<string, Action<HttpResponse>>();

            public void Configure(IApplicationBuilder app)
            {
                app.Run(async (context) =>
                {
                    string testPath = context.Request.Path.Value.Trim('/');

                    if (ResponseHandlerMock.OnRequestDictionary.ContainsKey(testPath))
                    {
                        Action<HttpResponse> onRequest = ResponseHandlerMock.OnRequestDictionary[testPath];
                        onRequest(context.Response);
                    }
                    else
                    {
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync("Hello World!");
                    }
                });
            }
        }

#endif

    }
}
