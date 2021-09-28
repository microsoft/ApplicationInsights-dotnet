#if NETCOREAPP
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.ApplicationInsights.TestFramework
{
    public class LocalInProcHttpServer : IDisposable
    {
        private readonly IWebHost host;
        private readonly CancellationTokenSource cts;
        
        public int RequestCounter = 0;

        //public RequestDelegate ServerLogic = async (httpContext) => await httpContext.Response.WriteAsync("Hello World!");

        public Func<HttpContext, Task> ServerLogic;

        public LocalInProcHttpServer(string url)
        {
            this.cts = new CancellationTokenSource();
            this.host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(url)
                .Configure((app) =>
                {
                    app.Run(Server);
                })
                .Build();

            Task.Run(() => this.host.RunAsync(this.cts.Token));
        }

        private Task Server(HttpContext context)
        {
            this.RequestCounter++;
            return this.ServerLogic(context);
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
