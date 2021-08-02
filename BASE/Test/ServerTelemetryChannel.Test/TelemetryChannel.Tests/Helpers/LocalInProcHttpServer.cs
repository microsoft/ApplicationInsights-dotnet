#if NETCOREAPP
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.ApplicationInsights.WindowsServer.Channel.Helpers
{
    public sealed class LocalInProcHttpServer : IDisposable
    {
        private readonly IWebHost host;
        private readonly CancellationTokenSource cts;

        public RequestDelegate ServerLogic;

        public LocalInProcHttpServer(string url, RequestDelegate serverLogic = null)
        {
            this.cts = new CancellationTokenSource();
            this.ServerLogic = serverLogic;
            this.host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(url)
                .Configure(
                (app) =>
                {
                    app.Run(ServerLogic);
                }
                )
                .Build();

            Task.Run(() => this.host.RunAsync(this.cts.Token));
        }

        public void Dispose()
        {
            this.cts.Cancel(false);
            try
            {
                this.host.Dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}
#endif
